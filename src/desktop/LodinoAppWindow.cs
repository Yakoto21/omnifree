using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Lodino.Desktop
{
    /// <summary>
    /// Janela principal da aplicação gráfica desktop nativa do Lodino.
    /// Implementada com pure Win32 API (User32 / Gdi32 / Comctl32 / Shell32) sem qualquer dependência de Electron, WPF ou WinForms.
    /// </summary>
    public class LodinoAppWindow
    {
        private const string WindowClassName = "LodinoMainWindowClass";

        // IDs dos Controles Filhos (para WM_COMMAND)
        private const int ID_BTN_ADD_FILES     = 1001;
        private const int ID_BTN_REMOVE_FILE   = 1002;
        private const int ID_BTN_CLEAR_QUEUE   = 1003;
        private const int ID_CMB_TARGET_FORMAT = 1004;
        private const int ID_BTN_CHOOSE_DIR    = 1005;
        private const int ID_EDIT_OUT_DIR      = 1006;
        private const int ID_BTN_CONVERT_ALL   = 1007;
        private const int ID_BTN_CANCEL        = 1008;
        private const int ID_BTN_OPEN_OUT_DIR  = 1009;
        private const int ID_LST_QUEUE         = 1010;
        private const int ID_PRG_PROGRESS      = 1011;
        private const int ID_EDIT_LOGS         = 1012;
        private const int ID_EDIT_QUALITY      = 1013;
        private const int ID_CHK_RESIZE        = 1014;
        private const int ID_EDIT_WIDTH        = 1015;
        private const int ID_EDIT_HEIGHT       = 1016;

        private IntPtr _hWnd;
        private readonly ConversionManager _manager;
        private static LodinoAppWindow _currentInstance;

        // Handles dos Controles
        private IntPtr _hHeaderTitle;
        private IntPtr _hHeaderSubtitle;
        private IntPtr _hLblQueue;
        private IntPtr _hLstQueue;
        private IntPtr _hBtnAddFiles;
        private IntPtr _hBtnRemoveFile;
        private IntPtr _hBtnClearQueue;

        private IntPtr _hGrpOptions;
        private IntPtr _hLblTargetFormat;
        private IntPtr _hCmbTargetFormat;
        private IntPtr _hLblOutDir;
        private IntPtr _hEditOutDir;
        private IntPtr _hBtnChooseDir;
        private IntPtr _hLblQuality;
        private IntPtr _hEditQuality;
        private IntPtr _hChkResize;
        private IntPtr _hEditWidth;
        private IntPtr _hEditHeight;

        private IntPtr _hPrgBar;
        private IntPtr _hLblStatus;
        private IntPtr _hBtnConvertAll;
        private IntPtr _hBtnCancel;
        private IntPtr _hBtnOpenOutDir;
        private IntPtr _hEditLogs;

        // Objetos GDI
        private IntPtr _hFontNormal;
        private IntPtr _hFontBold;
        private IntPtr _hFontTitle;
        private IntPtr _hBrushBackground;
        private IntPtr _hBrushCard;
        private IntPtr _hBrushControl;

        private string _selectedOutputDir = null;
        private static NativeMethods.WndProc _staticWndProc;
        private static GCHandle _wndProcPinHandle;

        public IntPtr Handle { get { return _hWnd; } }
        public ConversionManager Manager { get { return _manager; } }

        public LodinoAppWindow(ConversionManager manager)
        {
            _manager = manager ?? new ConversionManager();
            _currentInstance = this;
        }

        public bool CreateAndShow(int width = 980, int height = 680)
        {
            EnsureClassRegistered();

            IntPtr hInstance = Marshal.GetHINSTANCE(typeof(LodinoAppWindow).Module);

            // Calcula centro da tela
            int screenW = 1920;
            int screenH = 1080;
            int startX = Math.Max(50, (screenW - width) / 2);
            int startY = Math.Max(50, (screenH - height) / 2);

            _hWnd = NativeMethods.CreateWindowEx(
                NativeMethods.WS_EX_APPWINDOW | NativeMethods.WS_EX_ACCEPTFILES,
                WindowClassName,
                "Lodino — Conversor Local e Soberano (100% Nativo)",
                NativeMethods.WS_OVERLAPPEDWINDOW | NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_VISIBLE,
                startX, startY, width, height,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);

            if (_hWnd == IntPtr.Zero)
            {
                return false;
            }

            // Habilita tema escuro se suportado pelo Windows 10/11
            try
            {
                int darkMode = 1;
                NativeMethods.DwmSetWindowAttribute(_hWnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            }
            catch { /* Ignora se dwmapi não estiver presente ou SO for mais antigo */ }

            _manager.BindWindow(_hWnd);
            _manager.LogMessage += OnManagerLog;
            RefreshQueueList();

            NativeMethods.ShowWindow(_hWnd, NativeMethods.SW_SHOW);
            NativeMethods.UpdateWindow(_hWnd);

            return true;
        }

        private static void EnsureClassRegistered()
        {
            if (_staticWndProc != null) return;

            _staticWndProc = new NativeMethods.WndProc(StaticWindowProc);
            _wndProcPinHandle = GCHandle.Alloc(_staticWndProc);

            var icc = new NativeMethods.INITCOMMONCONTROLSEX();
            icc.dwSize = Marshal.SizeOf(typeof(NativeMethods.INITCOMMONCONTROLSEX));
            icc.dwICC = NativeMethods.ICC_PROGRESS_CLASS | NativeMethods.ICC_STANDARD_CLASSES;
            try { NativeMethods.InitCommonControlsEx(ref icc); } catch { }

            IntPtr hInstance = Marshal.GetHINSTANCE(typeof(LodinoAppWindow).Module);
            IntPtr hCursor = NativeMethods.LoadCursor(IntPtr.Zero, NativeMethods.IDC_ARROW);

            var wc = new NativeMethods.WNDCLASSEX();
            wc.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.WNDCLASSEX));
            wc.style = 3; // CS_HREDRAW | CS_VREDRAW
            wc.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_staticWndProc);
            wc.cbClsExtra = 0;
            wc.cbWndExtra = 0;
            wc.hInstance = hInstance;
            wc.hIcon = IntPtr.Zero;
            wc.hCursor = hCursor;
            wc.hbrBackground = NativeMethods.CreateSolidBrush(NativeMethods.RGB(28, 31, 34)); // Fundo grafite escuro #1C1F22
            wc.lpszMenuName = null;
            wc.lpszClassName = WindowClassName;
            wc.hIconSm = IntPtr.Zero;

            NativeMethods.RegisterClassEx(ref wc);
        }

        private static IntPtr StaticWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (_currentInstance != null)
            {
                return _currentInstance.WindowProc(hWnd, msg, wParam, lParam);
            }
            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case NativeMethods.WM_CREATE:
                    OnCreate(hWnd);
                    return IntPtr.Zero;

                case NativeMethods.WM_SIZE:
                    OnSize(hWnd);
                    return IntPtr.Zero;

                case NativeMethods.WM_DROPFILES:
                    OnDropFiles(wParam);
                    return IntPtr.Zero;

                case NativeMethods.WM_COMMAND:
                    OnCommand(wParam, lParam);
                    return IntPtr.Zero;

                case NativeMethods.WM_APP_QUEUE_UPDATED:
                    RefreshQueueList();
                    return IntPtr.Zero;

                case NativeMethods.WM_APP_JOB_PROGRESS:
                    int pct = lParam.ToInt32();
                    NativeMethods.SendMessage(_hPrgBar, NativeMethods.PBM_SETPOS, (IntPtr)pct, IntPtr.Zero);
                    return IntPtr.Zero;

                case NativeMethods.WM_APP_JOB_COMPLETE:
                    RefreshQueueList();
                    return IntPtr.Zero;

                case NativeMethods.WM_APP_BATCH_FINISH:
                    OnBatchFinish(wParam.ToInt32(), lParam.ToInt32());
                    return IntPtr.Zero;

                case NativeMethods.WM_CTLCOLORSTATIC:
                case NativeMethods.WM_CTLCOLOREDIT:
                case NativeMethods.WM_CTLCOLORBTN:
                case NativeMethods.WM_CTLCOLORLISTBOX:
                    return OnCtlColor(msg, wParam);

                case NativeMethods.WM_CLOSE:
                    if (_manager != null && _manager.IsRunning)
                    {
                        _manager.Cancel();
                    }
                    NativeMethods.DestroyWindow(hWnd);
                    return IntPtr.Zero;

                case NativeMethods.WM_DESTROY:
                    OnDestroy();
                    NativeMethods.PostQuitMessage(0);
                    return IntPtr.Zero;
            }

            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void OnCreate(IntPtr hWnd)
        {
            _hWnd = hWnd;
            IntPtr hInstance = Marshal.GetHINSTANCE(typeof(LodinoAppWindow).Module);

            // Cria fontes Segoe UI limpas
            _hFontNormal = NativeMethods.CreateFont(-13, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
            _hFontBold   = NativeMethods.CreateFont(-13, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");
            _hFontTitle  = NativeMethods.CreateFont(-18, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 2, 0, "Segoe UI");

            _hBrushBackground = NativeMethods.CreateSolidBrush(NativeMethods.RGB(28, 31, 34)); // #1C1F22
            _hBrushCard       = NativeMethods.CreateSolidBrush(NativeMethods.RGB(36, 40, 45)); // #24282D
            _hBrushControl    = NativeMethods.CreateSolidBrush(NativeMethods.RGB(45, 50, 56)); // #2D3238

            // 1. Cabeçalho
            _hHeaderTitle = CreateControl("STATIC", "LODINO · CONVERSOR PROPRIETÁRIO",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hHeaderTitle, NativeMethods.WM_SETFONT, _hFontTitle, (IntPtr)1);

            _hHeaderSubtitle = CreateControl("STATIC", "100% Local · Sem Nuvem · Sem Dependências · Motores em C# Puro",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hHeaderSubtitle, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            // 2. Seção de Fila
            _hLblQueue = CreateControl("STATIC", "Fila de Arquivos (Arraste e solte arquivos aqui do Explorer):",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hLblQueue, NativeMethods.WM_SETFONT, _hFontBold, (IntPtr)1);

            _hLstQueue = CreateControl("LISTBOX", "",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER | NativeMethods.WS_VSCROLL |
                NativeMethods.LBS_NOTIFY | NativeMethods.LBS_HASSTRINGS | NativeMethods.LBS_NOINTEGRALHEIGHT, ID_LST_QUEUE);
            NativeMethods.SendMessage(_hLstQueue, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hBtnAddFiles = CreateControl("BUTTON", "+ Adicionar Arquivos...",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_PUSHBUTTON | NativeMethods.WS_TABSTOP, ID_BTN_ADD_FILES);
            NativeMethods.SendMessage(_hBtnAddFiles, NativeMethods.WM_SETFONT, _hFontBold, (IntPtr)1);

            _hBtnRemoveFile = CreateControl("BUTTON", "Remover",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_PUSHBUTTON | NativeMethods.WS_TABSTOP, ID_BTN_REMOVE_FILE);
            NativeMethods.SendMessage(_hBtnRemoveFile, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hBtnClearQueue = CreateControl("BUTTON", "Limpar Fila",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_PUSHBUTTON | NativeMethods.WS_TABSTOP, ID_BTN_CLEAR_QUEUE);
            NativeMethods.SendMessage(_hBtnClearQueue, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            // 3. Painel de Opções
            _hGrpOptions = CreateControl("BUTTON", " Configurações de Saída ",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_GROUPBOX, 0);
            NativeMethods.SendMessage(_hGrpOptions, NativeMethods.WM_SETFONT, _hFontBold, (IntPtr)1);

            _hLblTargetFormat = CreateControl("STATIC", "Formato de Destino:",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hLblTargetFormat, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hCmbTargetFormat = CreateControl("COMBOBOX", "",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.CBS_DROPDOWNLIST | NativeMethods.WS_VSCROLL | NativeMethods.WS_TABSTOP, ID_CMB_TARGET_FORMAT);
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.WM_SETFONT, _hFontBold, (IntPtr)1);

            // Popula os formatos disponíveis no núcleo nativo
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_ADDSTRING, IntPtr.Zero, "PNG (Imagem Lossless)");
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_ADDSTRING, IntPtr.Zero, "JPG (JPEG Fotográfico)");
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_ADDSTRING, IntPtr.Zero, "BMP (Bitmap Windows)");
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_ADDSTRING, IntPtr.Zero, "JSON (Dados Estruturados)");
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_ADDSTRING, IntPtr.Zero, "CSV (Planilha / Tabela)");
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_ADDSTRING, IntPtr.Zero, "INI (Configuração)");
            NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_SETCURSEL, (IntPtr)0, IntPtr.Zero);

            _hLblOutDir = CreateControl("STATIC", "Pasta de Destino:",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hLblOutDir, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hEditOutDir = CreateControl("EDIT", "(Mesma pasta do arquivo de entrada)",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER | NativeMethods.ES_AUTOHSCROLL | NativeMethods.ES_READONLY, ID_EDIT_OUT_DIR);
            NativeMethods.SendMessage(_hEditOutDir, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hBtnChooseDir = CreateControl("BUTTON", "Escolher...",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_PUSHBUTTON | NativeMethods.WS_TABSTOP, ID_BTN_CHOOSE_DIR);
            NativeMethods.SendMessage(_hBtnChooseDir, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hLblQuality = CreateControl("STATIC", "Qualidade JPEG (1-100):",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hLblQuality, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hEditQuality = CreateControl("EDIT", "85",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER | NativeMethods.ES_CENTER | NativeMethods.WS_TABSTOP, ID_EDIT_QUALITY);
            NativeMethods.SendMessage(_hEditQuality, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hChkResize = CreateControl("BUTTON", "Redimensionar:",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_AUTOCHECKBOX | NativeMethods.WS_TABSTOP, ID_CHK_RESIZE);
            NativeMethods.SendMessage(_hChkResize, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hEditWidth = CreateControl("EDIT", "800",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER | NativeMethods.ES_CENTER | NativeMethods.WS_TABSTOP, ID_EDIT_WIDTH);
            NativeMethods.SendMessage(_hEditWidth, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hEditHeight = CreateControl("EDIT", "600",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER | NativeMethods.ES_CENTER | NativeMethods.WS_TABSTOP, ID_EDIT_HEIGHT);
            NativeMethods.SendMessage(_hEditHeight, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            // 4. Progresso e Ações
            _hPrgBar = CreateControl("msctls_progress32", "",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER, ID_PRG_PROGRESS);
            NativeMethods.SendMessage(_hPrgBar, NativeMethods.PBM_SETRANGE32, IntPtr.Zero, (IntPtr)100);

            _hLblStatus = CreateControl("STATIC", "Pronto para converter.",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.SS_LEFT, 0);
            NativeMethods.SendMessage(_hLblStatus, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hBtnConvertAll = CreateControl("BUTTON", "CONVERTER TUDO",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_DEFPUSHBUTTON | NativeMethods.WS_TABSTOP, ID_BTN_CONVERT_ALL);
            NativeMethods.SendMessage(_hBtnConvertAll, NativeMethods.WM_SETFONT, _hFontBold, (IntPtr)1);

            _hBtnCancel = CreateControl("BUTTON", "Cancelar",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_PUSHBUTTON | NativeMethods.WS_DISABLED | NativeMethods.WS_TABSTOP, ID_BTN_CANCEL);
            NativeMethods.SendMessage(_hBtnCancel, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            _hBtnOpenOutDir = CreateControl("BUTTON", "Abrir Destino",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.BS_PUSHBUTTON | NativeMethods.WS_DISABLED | NativeMethods.WS_TABSTOP, ID_BTN_OPEN_OUT_DIR);
            NativeMethods.SendMessage(_hBtnOpenOutDir, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            // 5. Log de Atividades
            _hEditLogs = CreateControl("EDIT", "",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_BORDER |
                NativeMethods.ES_MULTILINE | NativeMethods.ES_READONLY | NativeMethods.ES_AUTOVSCROLL | NativeMethods.WS_VSCROLL, ID_EDIT_LOGS);
            NativeMethods.SendMessage(_hEditLogs, NativeMethods.WM_SETFONT, _hFontNormal, (IntPtr)1);

            // Habilita arrastar e soltar arquivos na janela principal
            NativeMethods.DragAcceptFiles(hWnd, true);

            _manager.Log("Aplicação iniciada com sucesso. Modo soberano ativado.");
        }

        private IntPtr CreateControl(string className, string text, uint style, int id)
        {
            IntPtr hInstance = Marshal.GetHINSTANCE(typeof(LodinoAppWindow).Module);
            return NativeMethods.CreateWindowEx(
                0, className, text, style,
                0, 0, 10, 10,
                _hWnd, (IntPtr)id, hInstance, IntPtr.Zero);
        }

        private void OnSize(IntPtr hWnd)
        {
            NativeMethods.RECT rc;
            NativeMethods.GetClientRect(hWnd, out rc);
            int w = rc.Width;
            int h = rc.Height;
            if (w < 600 || h < 400) return;

            int margin = 16;
            int top = 14;

            // Header
            NativeMethods.MoveWindow(_hHeaderTitle, margin, top, w - margin * 2, 24, true);
            NativeMethods.MoveWindow(_hHeaderSubtitle, margin, top + 26, w - margin * 2, 18, true);

            int contentTop = top + 54;
            int leftColW = (int)(w * 0.58) - margin;
            int rightColX = margin + leftColW + margin;
            int rightColW = w - rightColX - margin;

            // Coluna Esquerda: Fila
            NativeMethods.MoveWindow(_hLblQueue, margin, contentTop, leftColW, 20, true);
            int listH = h - contentTop - 210;
            NativeMethods.MoveWindow(_hLstQueue, margin, contentTop + 22, leftColW, listH, true);

            int btnY = contentTop + 22 + listH + 8;
            int btnW = (leftColW - 16) / 3;
            NativeMethods.MoveWindow(_hBtnAddFiles, margin, btnY, btnW, 30, true);
            NativeMethods.MoveWindow(_hBtnRemoveFile, margin + btnW + 8, btnY, btnW, 30, true);
            NativeMethods.MoveWindow(_hBtnClearQueue, margin + (btnW + 8) * 2, btnY, btnW, 30, true);

            // Coluna Direita: Opções
            int grpH = 220;
            NativeMethods.MoveWindow(_hGrpOptions, rightColX, contentTop, rightColW, grpH, true);

            int optX = rightColX + 14;
            int optW = rightColW - 28;
            int optY = contentTop + 24;

            NativeMethods.MoveWindow(_hLblTargetFormat, optX, optY, optW, 18, true);
            NativeMethods.MoveWindow(_hCmbTargetFormat, optX, optY + 20, optW, 26, true);

            NativeMethods.MoveWindow(_hLblOutDir, optX, optY + 54, optW, 18, true);
            NativeMethods.MoveWindow(_hEditOutDir, optX, optY + 74, optW - 84, 24, true);
            NativeMethods.MoveWindow(_hBtnChooseDir, optX + optW - 80, optY + 73, 80, 26, true);

            NativeMethods.MoveWindow(_hLblQuality, optX, optY + 108, 140, 20, true);
            NativeMethods.MoveWindow(_hEditQuality, optX + 145, optY + 106, 50, 22, true);

            NativeMethods.MoveWindow(_hChkResize, optX, optY + 138, 110, 20, true);
            NativeMethods.MoveWindow(_hEditWidth, optX + 115, optY + 136, 55, 22, true);
            NativeMethods.MoveWindow(_hEditHeight, optX + 175, optY + 136, 55, 22, true);

            // Log de Atividades na direita (abaixo das opções)
            int logY = contentTop + grpH + 10;
            int logH = h - logY - 100;
            NativeMethods.MoveWindow(_hEditLogs, rightColX, logY, rightColW, logH, true);

            // Barra de Progresso e Ações Inferiores (Largura Total)
            int bottomY = h - 90;
            NativeMethods.MoveWindow(_hPrgBar, margin, bottomY, w - margin * 2, 16, true);
            NativeMethods.MoveWindow(_hLblStatus, margin, bottomY + 20, w - margin * 2 - 380, 24, true);

            int actionW = 120;
            NativeMethods.MoveWindow(_hBtnOpenOutDir, w - margin - actionW * 3 - 16, bottomY + 18, actionW, 32, true);
            NativeMethods.MoveWindow(_hBtnCancel, w - margin - actionW * 2 - 8, bottomY + 18, actionW, 32, true);
            NativeMethods.MoveWindow(_hBtnConvertAll, w - margin - actionW, bottomY + 18, actionW, 32, true);
        }

        private void OnDropFiles(IntPtr hDrop)
        {
            uint count = NativeMethods.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
            var paths = new List<string>();
            var sb = new StringBuilder(1024);

            for (uint i = 0; i < count; i++)
            {
                sb.Length = 0;
                if (NativeMethods.DragQueryFile(hDrop, i, sb, (uint)sb.Capacity) > 0)
                {
                    string p = sb.ToString();
                    if (File.Exists(p))
                    {
                        paths.Add(p);
                    }
                }
            }

            NativeMethods.DragFinish(hDrop);

            if (paths.Count > 0)
            {
                _manager.AddFiles(paths, GetSelectedFormatCode());
                RefreshQueueList();
            }
        }

        private void OnCommand(IntPtr wParam, IntPtr lParam)
        {
            int id = (int)(wParam.ToInt64() & 0xFFFF);

            switch (id)
            {
                case ID_BTN_ADD_FILES:
                    ShowAddFilesDialog();
                    break;

                case ID_BTN_REMOVE_FILE:
                    RemoveSelectedQueueItem();
                    break;

                case ID_BTN_CLEAR_QUEUE:
                    _manager.Clear();
                    RefreshQueueList();
                    break;

                case ID_BTN_CHOOSE_DIR:
                    ChooseOutputDirectory();
                    break;

                case ID_BTN_CONVERT_ALL:
                    StartConversion();
                    break;

                case ID_BTN_CANCEL:
                    _manager.Cancel();
                    break;

                case ID_BTN_OPEN_OUT_DIR:
                    OpenOutputDirectory();
                    break;
            }
        }

        private void ShowAddFilesDialog()
        {
            var ofn = new NativeMethods.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(typeof(NativeMethods.OPENFILENAME));
            ofn.hwndOwner = _hWnd;
            ofn.lpstrFilter = "Todos os Formatos Suportados (*.png;*.jpg;*.jpeg;*.bmp;*.csv;*.json;*.ini)\0*.png;*.jpg;*.jpeg;*.bmp;*.csv;*.json;*.ini\0Imagens (*.png;*.jpg;*.bmp)\0*.png;*.jpg;*.jpeg;*.bmp\0Dados (*.csv;*.json;*.ini)\0*.csv;*.json;*.ini\0Todos os Arquivos (*.*)\0*.*\0\0";
            ofn.nFilterIndex = 1;

            int bufferSize = 65536;
            IntPtr fileBuffer = Marshal.AllocHGlobal(bufferSize * 2);
            for (int i = 0; i < bufferSize; i++) Marshal.WriteInt16(fileBuffer, i * 2, 0);

            ofn.lpstrFile = fileBuffer;
            ofn.nMaxFile = bufferSize;
            ofn.Flags = NativeMethods.OFN_EXPLORER | NativeMethods.OFN_ALLOWMULTISELECT | NativeMethods.OFN_FILEMUSTEXIST | NativeMethods.OFN_PATHMUSTEXIST;

            try
            {
                if (NativeMethods.GetOpenFileName(ref ofn))
                {
                    var files = ParseOpenFileNameMultiSelect(fileBuffer);
                    if (files.Count > 0)
                    {
                        _manager.AddFiles(files, GetSelectedFormatCode());
                        RefreshQueueList();
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
            }
        }

        private List<string> ParseOpenFileNameMultiSelect(IntPtr buffer)
        {
            var result = new List<string>();
            var strings = new List<string>();
            int offset = 0;

            while (true)
            {
                string s = Marshal.PtrToStringUni(new IntPtr(buffer.ToInt64() + offset * 2));
                if (string.IsNullOrEmpty(s)) break;
                strings.Add(s);
                offset += s.Length + 1;
            }

            if (strings.Count == 1)
            {
                result.Add(strings[0]);
            }
            else if (strings.Count > 1)
            {
                string dir = strings[0];
                for (int i = 1; i < strings.Count; i++)
                {
                    result.Add(Path.Combine(dir, strings[i]));
                }
            }

            return result;
        }

        private void RemoveSelectedQueueItem()
        {
            int sel = (int)NativeMethods.SendMessage(_hLstQueue, NativeMethods.LB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
            if (sel >= 0)
            {
                var jobs = _manager.Jobs;
                if (sel < jobs.Count)
                {
                    _manager.RemoveJob(jobs[sel].Id);
                    RefreshQueueList();
                }
            }
        }

        private void ChooseOutputDirectory()
        {
            var ofn = new NativeMethods.OPENFILENAME();
            ofn.lStructSize = Marshal.SizeOf(typeof(NativeMethods.OPENFILENAME));
            ofn.hwndOwner = _hWnd;
            ofn.lpstrTitle = "Escolha um local de destino para os arquivos convertidos";
            ofn.lpstrFile = Marshal.StringToHGlobalUni("Salvar_Aqui_Nesta_Pasta");
            ofn.nMaxFile = 512;
            ofn.Flags = NativeMethods.OFN_EXPLORER | NativeMethods.OFN_PATHMUSTEXIST;

            try
            {
                if (NativeMethods.GetSaveFileName(ref ofn))
                {
                    string selectedFile = Marshal.PtrToStringUni(ofn.lpstrFile);
                    _selectedOutputDir = Path.GetDirectoryName(selectedFile);
                    NativeMethods.SetWindowText(_hEditOutDir, _selectedOutputDir);
                    _manager.Log("Pasta de saída configurada: " + _selectedOutputDir);
                }
            }
            finally
            {
                if (ofn.lpstrFile != IntPtr.Zero) Marshal.FreeHGlobal(ofn.lpstrFile);
            }
        }

        private void StartConversion()
        {
            if (_manager.Jobs.Count == 0)
            {
                NativeMethods.MessageBox(_hWnd, "Adicione pelo menos um arquivo na fila antes de converter.", "Aviso", 0x30);
                return;
            }

            string targetFormat = GetSelectedFormatCode();
            var parameters = new Dictionary<string, string>();

            // Coleta qualidade
            var sb = new StringBuilder(16);
            NativeMethods.GetWindowText(_hEditQuality, sb, 16);
            int q;
            if (int.TryParse(sb.ToString(), out q))
            {
                parameters["quality"] = Math.Max(1, Math.Min(100, q)).ToString();
            }

            // Coleta redimensionamento se marcado
            int chk = (int)NativeMethods.SendMessage(_hChkResize, NativeMethods.BM_GETCHECK, IntPtr.Zero, IntPtr.Zero);
            if (chk == 1)
            {
                sb.Length = 0;
                NativeMethods.GetWindowText(_hEditWidth, sb, 16);
                int rw;
                if (int.TryParse(sb.ToString(), out rw) && rw > 0)
                {
                    parameters["resizeWidth"] = rw.ToString();
                }

                sb.Length = 0;
                NativeMethods.GetWindowText(_hEditHeight, sb, 16);
                int rh;
                if (int.TryParse(sb.ToString(), out rh) && rh > 0)
                {
                    parameters["resizeHeight"] = rh.ToString();
                }
            }

            NativeMethods.EnableWindow(_hBtnConvertAll, false);
            NativeMethods.EnableWindow(_hBtnCancel, true);
            NativeMethods.SetWindowText(_hLblStatus, "Iniciando processamento...");
            NativeMethods.SendMessage(_hPrgBar, NativeMethods.PBM_SETPOS, IntPtr.Zero, IntPtr.Zero);

            _manager.StartBatch(targetFormat, _selectedOutputDir, parameters);
        }

        private void OnBatchFinish(int success, int failed)
        {
            NativeMethods.EnableWindow(_hBtnConvertAll, true);
            NativeMethods.EnableWindow(_hBtnCancel, false);
            NativeMethods.EnableWindow(_hBtnOpenOutDir, true);

            string msg = string.Format("Lote finalizado: {0} convertidos com sucesso, {1} com falhas.", success, failed);
            NativeMethods.SetWindowText(_hLblStatus, msg);
            NativeMethods.SendMessage(_hPrgBar, NativeMethods.PBM_SETPOS, (IntPtr)100, IntPtr.Zero);

            RefreshQueueList();
        }

        private void OpenOutputDirectory()
        {
            string dir = _selectedOutputDir;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                var jobs = _manager.Jobs;
                if (jobs.Count > 0 && !string.IsNullOrEmpty(jobs[0].DestinationPath))
                {
                    dir = Path.GetDirectoryName(jobs[0].DestinationPath);
                }
                else if (jobs.Count > 0)
                {
                    dir = Path.GetDirectoryName(jobs[0].SourcePath);
                }
            }

            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                NativeMethods.ShellExecute(IntPtr.Zero, "open", dir, null, null, 1);
            }
        }

        private void RefreshQueueList()
        {
            NativeMethods.SendMessage(_hLstQueue, NativeMethods.LB_RESETCONTENT, IntPtr.Zero, IntPtr.Zero);
            var jobs = _manager.Jobs;
            for (int i = 0; i < jobs.Count; i++)
            {
                NativeMethods.SendMessage(_hLstQueue, NativeMethods.LB_ADDSTRING, IntPtr.Zero, jobs[i].ToString());
            }
        }

        private string GetSelectedFormatCode()
        {
            int sel = (int)NativeMethods.SendMessage(_hCmbTargetFormat, NativeMethods.CB_GETCURSEL, IntPtr.Zero, IntPtr.Zero);
            switch (sel)
            {
                case 0: return "png";
                case 1: return "jpg";
                case 2: return "bmp";
                case 3: return "json";
                case 4: return "csv";
                case 5: return "ini";
                default: return "png";
            }
        }

        private void OnManagerLog(string msg)
        {
            int len = (int)NativeMethods.SendMessage(_hEditLogs, 0x000E /* WM_GETTEXTLENGTH */, IntPtr.Zero, IntPtr.Zero);
            NativeMethods.SendMessage(_hEditLogs, 0x00B1 /* EM_SETSEL */, (IntPtr)len, (IntPtr)len);
            NativeMethods.SendMessage(_hEditLogs, 0x00C2 /* EM_REPLACESEL */, IntPtr.Zero, msg + "\r\n");
        }

        private IntPtr OnCtlColor(uint msg, IntPtr wParam)
        {
            NativeMethods.SetBkColor(wParam, NativeMethods.RGB(36, 40, 45)); // #24282D
            NativeMethods.SetTextColor(wParam, NativeMethods.RGB(240, 242, 235)); // #F0F2EB
            return _hBrushCard;
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.BindWindow(IntPtr.Zero);
                _manager.LogMessage -= OnManagerLog;
            }

            if (_hFontNormal != IntPtr.Zero) NativeMethods.DeleteObject(_hFontNormal);
            if (_hFontBold != IntPtr.Zero) NativeMethods.DeleteObject(_hFontBold);
            if (_hFontTitle != IntPtr.Zero) NativeMethods.DeleteObject(_hFontTitle);

            if (_hBrushBackground != IntPtr.Zero) NativeMethods.DeleteObject(_hBrushBackground);
            if (_hBrushCard != IntPtr.Zero) NativeMethods.DeleteObject(_hBrushCard);
            if (_hBrushControl != IntPtr.Zero) NativeMethods.DeleteObject(_hBrushControl);
        }
    }
}
