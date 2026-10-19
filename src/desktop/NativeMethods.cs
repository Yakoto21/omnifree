using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Lodino.Desktop
{
    /// <summary>
    /// Definições P/Invoke e estruturas nativas da API Win32 do Windows.
    /// Utilizado para gerenciamento de janelas, controles, diálogo de arquivos e drag-and-drop.
    /// </summary>
    internal static class NativeMethods
    {
        // -------------------------------------------------------------
        // Constantes de Mensagens do Windows (WM_*)
        // -------------------------------------------------------------
        public const uint WM_NULL             = 0x0000;
        public const uint WM_CREATE           = 0x0001;
        public const uint WM_DESTROY          = 0x0002;
        public const uint WM_MOVE             = 0x0003;
        public const uint WM_SIZE             = 0x0005;
        public const uint WM_PAINT            = 0x000F;
        public const uint WM_CLOSE            = 0x0010;
        public const uint WM_QUIT             = 0x0012;
        public const uint WM_ERASEBKGND       = 0x0014;
        public const uint WM_SETFONT          = 0x0030;
        public const uint WM_GETFONT          = 0x0031;
        public const uint WM_COMMAND          = 0x0111;
        public const uint WM_TIMER            = 0x0113;
        public const uint WM_CTLCOLORSTATIC   = 0x0138;
        public const uint WM_CTLCOLOREDIT     = 0x0133;
        public const uint WM_CTLCOLORBTN      = 0x0135;
        public const uint WM_CTLCOLORLISTBOX  = 0x0134;
        public const uint WM_DROPFILES        = 0x0233;

        // Mensagens customizadas da aplicação (WM_APP + X)
        public const uint WM_APP              = 0x8000;
        public const uint WM_APP_QUEUE_UPDATED= WM_APP + 100;
        public const uint WM_APP_JOB_PROGRESS = WM_APP + 101;
        public const uint WM_APP_JOB_COMPLETE = WM_APP + 102;
        public const uint WM_APP_BATCH_FINISH = WM_APP + 103;
        public const uint WM_APP_LOG_MESSAGE  = WM_APP + 104;

        // -------------------------------------------------------------
        // Constantes de Estilos de Janela e Controles
        // -------------------------------------------------------------
        public const uint WS_OVERLAPPED       = 0x00000000;
        public const uint WS_POPUP            = 0x80000000;
        public const uint WS_CHILD            = 0x40000000;
        public const uint WS_VISIBLE          = 0x10000000;
        public const uint WS_DISABLED         = 0x08000000;
        public const uint WS_CLIPSIBLINGS     = 0x04000000;
        public const uint WS_CLIPCHILDREN     = 0x02000000;
        public const uint WS_CAPTION          = 0x00C00000;
        public const uint WS_BORDER           = 0x00800000;
        public const uint WS_DLGFRAME         = 0x00400000;
        public const uint WS_VSCROLL          = 0x00200000;
        public const uint WS_HSCROLL          = 0x00100000;
        public const uint WS_SYSMENU          = 0x00080000;
        public const uint WS_THICKFRAME       = 0x00040000;
        public const uint WS_GROUP            = 0x00020000;
        public const uint WS_TABSTOP          = 0x00010000;
        public const uint WS_MINIMIZEBOX      = 0x00020000;
        public const uint WS_MAXIMIZEBOX      = 0x00010000;

        public const uint WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU |
                                                 WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);

        public const uint WS_EX_CLIENTEDGE    = 0x00000200;
        public const uint WS_EX_ACCEPTFILES   = 0x00000010;
        public const uint WS_EX_APPWINDOW     = 0x00040000;

        // Estilos de Botão (BUTTON)
        public const uint BS_PUSHBUTTON       = 0x00000000;
        public const uint BS_DEFPUSHBUTTON    = 0x00000001;
        public const uint BS_CHECKBOX         = 0x00000002;
        public const uint BS_AUTOCHECKBOX     = 0x00000003;
        public const uint BS_GROUPBOX         = 0x00000007;

        // Estilos de Caixa de Texto (EDIT)
        public const uint ES_LEFT             = 0x00000000;
        public const uint ES_CENTER           = 0x00000001;
        public const uint ES_RIGHT            = 0x00000002;
        public const uint ES_MULTILINE        = 0x00000004;
        public const uint ES_AUTOVSCROLL      = 0x00000040;
        public const uint ES_AUTOHSCROLL      = 0x00000080;
        public const uint ES_READONLY         = 0x00000800;

        // Estilos de Lista Suspensa (COMBOBOX)
        public const uint CBS_DROPDOWN        = 0x00000002;
        public const uint CBS_DROPDOWNLIST    = 0x00000003;
        public const uint CBS_HASSTRINGS      = 0x00000200;

        // Estilos de Lista (LISTBOX)
        public const uint LBS_NOTIFY          = 0x00000001;
        public const uint LBS_HASSTRINGS      = 0x00000040;
        public const uint LBS_NOINTEGRALHEIGHT= 0x00000100;

        // Estilos Estáticos (STATIC)
        public const uint SS_LEFT             = 0x00000000;
        public const uint SS_CENTER           = 0x00000001;
        public const uint SS_RIGHT            = 0x00000002;
        public const uint SS_NOPREFIX         = 0x00000080;

        // Mensagens Comuns de Controles
        public const uint BM_GETCHECK         = 0x00F0;
        public const uint BM_SETCHECK         = 0x00F1;
        public const uint CB_ADDSTRING        = 0x0143;
        public const uint CB_SETCURSEL        = 0x014E;
        public const uint CB_GETCURSEL        = 0x0147;
        public const uint CB_GETLBTEXT        = 0x0148;
        public const uint CB_GETLBTEXTLEN     = 0x0149;
        public const uint CB_RESETCONTENT     = 0x014B;
        public const uint LB_ADDSTRING        = 0x0180;
        public const uint LB_DELETESTRING     = 0x0182;
        public const uint LB_RESETCONTENT     = 0x0184;
        public const uint LB_SETCURSEL        = 0x0186;
        public const uint LB_GETCURSEL        = 0x0188;
        public const uint LB_GETTEXT          = 0x0189;
        public const uint LB_GETTEXTLEN       = 0x018A;
        public const uint LB_GETCOUNT         = 0x018B;

        // Progress Bar
        public const uint PBM_SETRANGE32      = 0x0406;
        public const uint PBM_SETPOS          = 0x0402;
        public const uint PBM_SETSTEP         = 0x0404;
        public const uint PBM_STEPIT          = 0x0405;

        // Códigos de Notificação
        public const ushort BN_CLICKED        = 0;
        public const ushort EN_CHANGE         = 0x0300;
        public const ushort CBN_SELCHANGE     = 1;
        public const ushort LBN_SELCHANGE     = 1;

        // DWM Immersive Dark Mode
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // -------------------------------------------------------------
        // Estruturas Win32
        // -------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public IntPtr lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        public const int OFN_READONLY             = 0x00000001;
        public const int OFN_OVERWRITEPROMPT      = 0x00000002;
        public const int OFN_HIDEREADONLY         = 0x00000004;
        public const int OFN_NOCHANGEDIR          = 0x00000008;
        public const int OFN_ALLOWMULTISELECT     = 0x00000200;
        public const int OFN_PATHMUSTEXIST        = 0x00000800;
        public const int OFN_FILEMUSTEXIST        = 0x00001000;
        public const int OFN_EXPLORER             = 0x00080000;

        [StructLayout(LayoutKind.Sequential)]
        public struct INITCOMMONCONTROLSEX
        {
            public int dwSize;
            public uint dwICC;
        }
        public const uint ICC_PROGRESS_CLASS = 0x00000020;
        public const uint ICC_STANDARD_CLASSES = 0x00004000;

        // -------------------------------------------------------------
        // Delegates
        // -------------------------------------------------------------
        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // -------------------------------------------------------------
        // Funções da User32.dll
        // -------------------------------------------------------------
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle,
            string lpClassName,
            string lpWindowName,
            uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        public static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        public const int IDC_ARROW = 32512;
        public const int SW_SHOW = 5;

        // -------------------------------------------------------------
        // Funções da Gdi32.dll
        // -------------------------------------------------------------
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateSolidBrush(uint crColor);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern uint SetTextColor(IntPtr hdc, uint crColor);

        [DllImport("gdi32.dll")]
        public static extern uint SetBkColor(IntPtr hdc, uint crColor);

        [DllImport("gdi32.dll")]
        public static extern int SetBkMode(IntPtr hdc, int iBkMode);

        public const int TRANSPARENT = 1;
        public const int OPAQUE = 2;

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFont(
            int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
            uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
            uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality,
            uint fdwPitchAndFamily, string lpszFace);

        [DllImport("user32.dll")]
        public static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);

        // -------------------------------------------------------------
        // Funções da Shell32.dll
        // -------------------------------------------------------------
        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        // -------------------------------------------------------------
        // Funções da Comdlg32.dll
        // -------------------------------------------------------------
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetOpenFileName([In, Out] ref OPENFILENAME ofn);

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetSaveFileName([In, Out] ref OPENFILENAME ofn);

        // -------------------------------------------------------------
        // Funções da Comctl32.dll
        // -------------------------------------------------------------
        [DllImport("comctl32.dll")]
        public static extern bool InitCommonControlsEx([In] ref INITCOMMONCONTROLSEX icc);

        // -------------------------------------------------------------
        // Funções da Dwmapi.dll (Tema Escuro Imersivo Opcional)
        // -------------------------------------------------------------
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static uint RGB(byte r, byte g, byte b)
        {
            return (uint)(r | (g << 8) | (b << 16));
        }
    }
}
