const dropZone = document.getElementById('drop-zone');
const fileInput = document.getElementById('file-input');
const folderInput = document.getElementById('folder-input');
const fileDetails = document.getElementById('file-details');
const fileName = document.getElementById('file-name');
const fileMeta = document.getElementById('file-meta');
const previewPanel = document.getElementById('preview-panel');
const previewKind = document.getElementById('preview-kind');
const previewContent = document.getElementById('preview-content');
const queuePanel = document.getElementById('queue-panel');
const queueItems = document.getElementById('queue-items');
const formatHelp = document.getElementById('format-help');
const formatoSaida = document.getElementById('formato-saida');
const formatSearch = document.getElementById('format-search');
const categoryFilter = document.getElementById('category-filter');
const smartTip = document.getElementById('smart-tip');
const advancedOptions = document.getElementById('advanced-options');
const pdfTools = document.getElementById('pdf-tools');
const btnConverter = document.getElementById('btn-converter');
const btnTrocar = document.getElementById('btn-trocar');
const btnPasta = document.getElementById('btn-pasta');
const progressContainer = document.getElementById('progress-container');
const progressBar = document.getElementById('progress-bar');
const progressText = document.getElementById('progress-text');
const progressState = document.getElementById('progress-state');
const btnCancelar = document.getElementById('btn-cancelar');
const resultPanel = document.getElementById('result-panel');
const resultIcon = document.getElementById('result-icon');
const resultMessage = document.getElementById('result-message');
const btnAbrir = document.getElementById('btn-abrir');
const btnCopiarCaminho = document.getElementById('btn-copiar-caminho');
const btnArrastarResultado = document.getElementById('btn-arrastar-resultado');
const preset = document.getElementById('preset');
const btnSalvarPerfil = document.getElementById('btn-salvar-perfil');
const btnConfiguracoes = document.getElementById('btn-configuracoes');
const settingsDialog = document.getElementById('settings-dialog');
const btnFecharConfiguracoes = document.getElementById('btn-fechar-configuracoes');
const btnSalvarConfiguracoes = document.getElementById('btn-salvar-configuracoes');
const btnThumbnail = document.getElementById('btn-thumbnail');
const btnExportarRelatorio = document.getElementById('btn-exportar-relatorio');
const privacyMode = document.getElementById('privacy-mode');
const reportDialog = document.getElementById('report-dialog');
const btnExportCsv = document.getElementById('btn-export-csv');
const btnExportJson = document.getElementById('btn-export-json');
const btnDestinoPadrao = document.getElementById('btn-destino-padrao');
const autoUpdates = document.getElementById('auto-updates');
const settingsTheme = document.getElementById('settings-theme');
const settingsLanguage = document.getElementById('settings-language');
const btnAdicionarRegra = document.getElementById('btn-adicionar-regra');
const rulesStatus = document.getElementById('rules-status');
const quality = document.getElementById('quality');
const qualityValue = document.getElementById('quality-value');
const btnDestino = document.getElementById('btn-destino');
const width = document.getElementById('width');
const height = document.getElementById('height');
const startTime = document.getElementById('start-time');
const duration = document.getElementById('duration');
const outputName = document.getElementById('output-name');
const namePrefix = document.getElementById('name-prefix');
const nameSuffix = document.getElementById('name-suffix');
const nameFind = document.getElementById('name-find');
const nameReplace = document.getElementById('name-replace');
const nameDate = document.getElementById('name-date');
const nameNumber = document.getElementById('name-number');
const audioOnly = document.getElementById('audio-only');
const noAudio = document.getElementById('no-audio');
const removeMetadata = document.getElementById('remove-metadata');
const watermark = document.getElementById('watermark');
const cropImage = document.getElementById('crop-image');
const cropGrid = document.getElementById('crop-grid');
const batchSizes = document.getElementById('batch-sizes');
const btnWatermarkImage = document.getElementById('btn-watermark-image');
const watermarkImageName = document.getElementById('watermark-image-name');
const componentsStatus = document.getElementById('components-status');
const componentsHelp = document.getElementById('components-help');
const btnComponentes = document.getElementById('btn-componentes');
const btnVerificarAtualizacoes = document.getElementById('btn-verificar-atualizacoes');
const updatesStatus = document.getElementById('updates-status');
const btnBaixarAtualizacao = document.getElementById('btn-baixar-atualizacao');
const btnInstalarAtualizacao = document.getElementById('btn-instalar-atualizacao');
const btnJuntarPdf = document.getElementById('btn-juntar-pdf');
const btnSepararPdf = document.getElementById('btn-separar-pdf');
const btnExtrairPaginas = document.getElementById('btn-extrair-paginas');
const btnGirarPdf = document.getElementById('btn-girar-pdf');
const btnTheme = document.getElementById('btn-theme');
const language = document.getElementById('language');
const btnProtegerPdf = document.getElementById('btn-proteger-pdf');
const btnOtimizarPdf = document.getElementById('btn-otimizar-pdf');
const btnExtrairImagens = document.getElementById('btn-extrair-imagens');
const btnWebPdf = document.getElementById('btn-web-pdf');
const btnLegendas = document.getElementById('btn-legendas');
const btnOcr = document.getElementById('btn-ocr');
const btnLerCodigo = document.getElementById('btn-ler-codigo');
const comparisonPanel = document.getElementById('comparison-panel');
const comparisonContent = document.getElementById('comparison-content');
const historyList = document.getElementById('history-list');
const historyItems = document.getElementById('history-items');
const btnLimparHistorico = document.getElementById('btn-limpar-historico');
const btnVerHistorico = document.getElementById('btn-ver-historico');
const historyDialog = document.getElementById('history-dialog');
const historyDialogItems = document.getElementById('history-dialog-items');
const btnFecharHistorico = document.getElementById('btn-fechar-historico');
const inputDialog = document.getElementById('input-dialog');
const inputForm = document.getElementById('input-form');
const inputDialogTitle = document.getElementById('input-dialog-title');
const inputDialogMessage = document.getElementById('input-dialog-message');
const inputDialogValue = document.getElementById('input-dialog-value');
const btnConfirmarInput = document.getElementById('btn-confirmar-input');
const btnCancelarInput = document.getElementById('btn-cancelar-input');
const btnCancelarInputSecundario = document.getElementById('btn-cancelar-input-secundario');
const savedProfilesList = document.getElementById('saved-profiles-list');
const rulesList = document.getElementById('rules-list');
const ruleHistoryList = document.getElementById('rule-history-list');

let arquivoSelecionado = null;
let ultimoArquivoConvertido = '';
let pastaDestino = '';
let arquivosSelecionados = [];
let queueResolve = null;
let formatosDisponiveis = [];
let watermarkImage = '';
let cropPosition = 'centre';
let opcoesAtuais = null;
let pastaDestinoPadrao = localStorage.getItem('omnifree-default-output') || '';
let regrasAutomaticas = [];
[['reels', 'Instagram Reels'], ['tiktok', 'TikTok'], ['shorts', 'YouTube Shorts'], ['discord', 'Discord'], ['email', 'E-mail']].forEach(([value, label]) => { if (![...preset.options].some((option) => option.value === value)) preset.add(new Option(label, value)); });
let previewUrl = '';
let activeJobId = '';
let cancelRequested = false;
let detalhesEntrada = null;
let activeQueueFile = null;
let retryOnlyFiles = null;
let draggedQueueFile = null;

const translations = {
  'pt-BR': { subtitle: 'Converta arquivos no seu computador, com privacidade.', local: '● 100% local', choose: 'Escolha um arquivo', chooseHint: 'Arraste-o para cá ou selecione-o no computador.', drop: 'Arraste um arquivo ou pasta aqui', browse: 'ou clique para procurar', folder: 'Pasta', change: 'Trocar', formatTitle: 'Defina o formato', formatHint: 'Escolha um arquivo para ver os formatos disponíveis.', convert: 'Converter arquivo', outputName: 'Nome do resultado', preset: 'Pré-ajuste', balanced: 'Equilibrado', small: 'Arquivo menor', highQuality: 'Maior qualidade', whatsapp: 'Compartilhar no WhatsApp', quality: 'Qualidade', destination: 'Destino', desktop: 'Área de Trabalho', more: 'Ajustes específicos do arquivo', width: 'Largura (imagem)', height: 'Altura (imagem)', crop: 'Recortar para preencher', cropArea: 'Área do recorte', batchSizes: 'Tamanhos extras no lote', watermark: 'Marca-d’água (imagem)', watermarkImage: 'Imagem de marca-d’água', chooseImage: 'Escolher imagem', start: 'Início de mídia', duration: 'Duração de mídia', audioOnly: 'Extrair somente áudio', noAudio: 'Remover áudio', metadata: 'Remover metadados', recent: 'Conversões recentes', viewAll: 'Ver tudo', clear: 'Limpar', updates: 'Atualizações', updatesHint: 'Verifique se há uma versão nova do OmniFree.', checkUpdates: 'Verificar agora', historyTitle: 'Histórico completo', historyHint: 'Abra, repita ou remova uma conversão.', tipIdle: 'Comece escolhendo um arquivo ou uma pasta.', tipImage: 'Imagem detectada: escolha o formato e converta. Ajustes de imagem ficam logo abaixo.', tipMedia: 'Mídia detectada: escolha o formato. Em ajustes, você pode cortar, extrair ou remover o áudio.', tipPdf: 'PDF detectado: converta normalmente ou use as ferramentas exclusivas de PDF abaixo.', tipGeneric: 'Escolha o formato de saída e clique em Converter arquivo.' },
  en: { subtitle: 'Convert files on your computer, privately.', local: '● 100% local', choose: 'Choose a file', chooseHint: 'Drag it here or select it from your computer.', drop: 'Drag a file or folder here', browse: 'or click to browse', folder: 'Folder', change: 'Change', formatTitle: 'Choose the format', formatHint: 'Choose a file to see available formats.', convert: 'Convert file', outputName: 'Output name', preset: 'Preset', balanced: 'Balanced', small: 'Smaller file', highQuality: 'Higher quality', whatsapp: 'Share on WhatsApp', quality: 'Quality', destination: 'Destination', desktop: 'Desktop', more: 'File-specific adjustments', width: 'Width (image)', height: 'Height (image)', crop: 'Crop to fill', cropArea: 'Crop area', batchSizes: 'Extra batch sizes', watermark: 'Watermark (image)', watermarkImage: 'Image watermark', chooseImage: 'Choose image', start: 'Media start', duration: 'Media duration', audioOnly: 'Extract audio only', noAudio: 'Remove audio', metadata: 'Remove metadata', recent: 'Recent conversions', viewAll: 'View all', clear: 'Clear', updates: 'Updates', updatesHint: 'Check whether a newer OmniFree version is available.', checkUpdates: 'Check now', historyTitle: 'Full history', historyHint: 'Open, repeat, or remove a conversion.', tipIdle: 'Start by choosing a file or folder.', tipImage: 'Image detected: choose a format and convert. Image adjustments are available below.', tipMedia: 'Media detected: choose a format. In adjustments, you can trim, extract, or remove audio.', tipPdf: 'PDF detected: convert it normally or use the PDF-specific tools below.', tipGeneric: 'Choose an output format and click Convert file.' }
};
function t(key) { return (translations[language.value] || translations['pt-BR'])[key] || key; }

function solicitarTexto({ title, message = '', value = '', placeholder = '', type = 'text', confirmLabel } = {}) {
  return new Promise((resolve) => {
    inputDialogTitle.textContent = title || (language.value === 'en' ? 'Information' : 'Informação');
    inputDialogMessage.textContent = message;
    inputDialogValue.type = type;
    inputDialogValue.value = value;
    inputDialogValue.placeholder = placeholder;
    btnConfirmarInput.textContent = confirmLabel || (language.value === 'en' ? 'Confirm' : 'Confirmar');
    btnCancelarInputSecundario.textContent = language.value === 'en' ? 'Cancel' : 'Cancelar';
    const finish = (answer = null) => {
      inputForm.removeEventListener('submit', submit);
      btnCancelarInput.removeEventListener('click', cancel);
      btnCancelarInputSecundario.removeEventListener('click', cancel);
      inputDialog.removeEventListener('cancel', cancel);
      if (inputDialog.open) inputDialog.close();
      resolve(answer);
    };
    const submit = (event) => { event.preventDefault(); finish(inputDialogValue.value.trim()); };
    const cancel = (event) => { if (event) event.preventDefault(); finish(); };
    inputForm.addEventListener('submit', submit);
    btnCancelarInput.addEventListener('click', cancel);
    btnCancelarInputSecundario.addEventListener('click', cancel);
    inputDialog.addEventListener('cancel', cancel);
    inputDialog.showModal();
    inputDialogValue.focus();
    inputDialogValue.select();
  });
}

const profilePrefix = 'omnifree-profile-';
function profileSettings() { return { quality: quality.value, width: width.value, height: height.value, target: formatoSaida.value }; }
function savedProfiles() { return Object.keys(localStorage).filter((key) => key.startsWith(profilePrefix)).map((key) => ({ id: key.slice(profilePrefix.length), ...JSON.parse(localStorage.getItem(key) || '{}') })); }
function applyProfile(id) { const selected = JSON.parse(localStorage.getItem(`${profilePrefix}${id}`) || '{}'); if (selected.quality) quality.value = selected.quality; if (selected.width) width.value = selected.width; if (selected.height) height.value = selected.height; if (selected.target && [...formatoSaida.options].some((option) => option.value === selected.target)) formatoSaida.value = selected.target; preset.value = id; atualizarQualidade(); }
async function confirmRemoval(message) { const answer = await solicitarTexto({ title: language.value === 'en' ? 'Confirm removal' : 'Confirmar remoção', message: `${message} ${language.value === 'en' ? 'Type REMOVE to continue.' : 'Digite REMOVER para continuar.'}`, placeholder: language.value === 'en' ? 'REMOVE' : 'REMOVER', confirmLabel: language.value === 'en' ? 'Remove' : 'Remover' }); return answer === (language.value === 'en' ? 'REMOVE' : 'REMOVER'); }
function renderSavedProfiles() {
  savedProfilesList.innerHTML = ''; const profiles = savedProfiles();
  profiles.forEach((profile) => { if (![...preset.options].some((option) => option.value === profile.id)) preset.add(new Option(profile.name || profile.id, profile.id)); });
  if (!profiles.length) { savedProfilesList.textContent = language.value === 'en' ? 'No saved profiles yet.' : 'Nenhum perfil salvo ainda.'; return; }
  profiles.forEach((profile) => { const row = document.createElement('div'); row.className = 'manager-row'; const copy = document.createElement('div'); const title = document.createElement('strong'); title.textContent = profile.name || profile.id; const info = document.createElement('small'); info.textContent = `${profile.target || '—'} · ${profile.quality || 80}%`; copy.append(title, info); const actions = document.createElement('span'); const use = document.createElement('button'); use.className = 'quiet-button'; use.textContent = language.value === 'en' ? 'Use' : 'Usar'; use.addEventListener('click', () => { applyProfile(profile.id); settingsDialog.close(); }); const edit = document.createElement('button'); edit.className = 'quiet-button'; edit.textContent = language.value === 'en' ? 'Edit' : 'Editar'; edit.addEventListener('click', async () => { const name = await solicitarTexto({ title: language.value === 'en' ? 'Edit profile' : 'Editar perfil', message: language.value === 'en' ? 'The current output settings will be saved to this profile.' : 'Os ajustes atuais de saída serão salvos neste perfil.', value: profile.name || profile.id }); if (!name) return; localStorage.setItem(`${profilePrefix}${profile.id}`, JSON.stringify({ ...profileSettings(), name })); const option = preset.querySelector(`option[value="${profile.id}"]`); if (option) option.textContent = name; renderSavedProfiles(); }); const remove = document.createElement('button'); remove.className = 'quiet-button danger'; remove.textContent = '×'; remove.title = language.value === 'en' ? 'Remove profile' : 'Remover perfil'; remove.addEventListener('click', async () => { if (!await confirmRemoval(language.value === 'en' ? `Remove “${profile.name || profile.id}”?` : `Remover “${profile.name || profile.id}”?`)) return; localStorage.removeItem(`${profilePrefix}${profile.id}`); preset.querySelector(`option[value="${profile.id}"]`)?.remove(); renderSavedProfiles(); }); actions.append(use, edit, remove); row.append(copy, actions); savedProfilesList.appendChild(row); });
}
function ruleHistory() { return JSON.parse(localStorage.getItem('omnifree-rule-history') || '[]'); }
function renderRules(history = ruleHistory()) {
  rulesStatus.textContent = regrasAutomaticas.length ? `${regrasAutomaticas.length} ${ui('activeRules')}` : ui('noRule'); rulesList.innerHTML = '';
  regrasAutomaticas.forEach((rule, index) => { const row = document.createElement('div'); row.className = 'manager-row'; const copy = document.createElement('div'); const title = document.createElement('strong'); title.textContent = `.${rule.target}`; const info = document.createElement('small'); info.textContent = `${rule.folder} → ${rule.outputDir}`; copy.append(title, info); const actions = document.createElement('span'); const edit = document.createElement('button'); edit.className = 'quiet-button'; edit.textContent = language.value === 'en' ? 'Edit' : 'Editar'; edit.addEventListener('click', async () => { const outputDir = await window.conversorAPI.escolherPastaDestino(); if (!outputDir) return; const target = await solicitarTexto({ title: language.value === 'en' ? 'Automatic format' : 'Formato automático', message: 'mp3, pdf, webp', value: rule.target }); if (!['mp3', 'pdf', 'webp'].includes((target || '').toLowerCase())) return; regrasAutomaticas[index] = { ...rule, outputDir, target: target.toLowerCase() }; await window.conversorAPI.salvarRegras(regrasAutomaticas); renderRules(); }); const remove = document.createElement('button'); remove.className = 'quiet-button danger'; remove.textContent = '×'; remove.title = language.value === 'en' ? 'Remove rule' : 'Remover regra'; remove.addEventListener('click', async () => { if (!await confirmRemoval(language.value === 'en' ? 'Remove this automatic rule?' : 'Remover esta regra automática?')) return; regrasAutomaticas.splice(index, 1); await window.conversorAPI.salvarRegras(regrasAutomaticas); renderRules(); }); actions.append(edit, remove); row.append(copy, actions); rulesList.appendChild(row); });
  ruleHistoryList.innerHTML = ''; history.slice(0, 8).forEach((entry) => { const item = document.createElement('p'); item.textContent = `${new Date(entry.date).toLocaleString(language.value)} · ${entry.name} → .${entry.target}`; ruleHistoryList.appendChild(item); }); if (!history.length) ruleHistoryList.textContent = language.value === 'en' ? 'No automatic conversions yet.' : 'Nenhuma conversão automática ainda.';
}

function atualizarHistorico() {
  const history = JSON.parse(localStorage.getItem('omnifree-history') || '[]');
  historyList.textContent = history.length ? history.slice(0, 3).map((item) => item.name).join(' · ') : (language.value === 'en' ? 'No recent conversions.' : 'Nenhuma conversão recente.');
  historyItems.innerHTML = '';
  const addRow = (parent, item, index, full) => { const row = document.createElement('div'); row.className = 'history-item'; const open = document.createElement('button'); open.textContent = `${language.value === 'en' ? 'Open' : 'Abrir'}: ${item.name}`; open.disabled = !item.output; open.addEventListener('click', () => item.output && window.conversorAPI.abrirNoExplorador(item.output)); row.appendChild(open); if (full) { const repeat = document.createElement('button'); repeat.textContent = language.value === 'en' ? 'Repeat' : 'Repetir'; repeat.disabled = !item.input || !item.target; repeat.addEventListener('click', () => repetirConversao(item)); row.appendChild(repeat); } const remove = document.createElement('button'); remove.textContent = '×'; remove.title = language.value === 'en' ? 'Remove from history' : 'Remover do histórico'; remove.addEventListener('click', () => { history.splice(index, 1); localStorage.setItem('omnifree-history', JSON.stringify(history)); atualizarHistorico(); }); row.appendChild(remove); parent.appendChild(row); };
  history.slice(0, 5).forEach((item, index) => addRow(historyItems, item, index, false));
  historyDialogItems.innerHTML = ''; history.forEach((item, index) => addRow(historyDialogItems, item, index, true));
}
function registrarHistorico(name, output = '', input = '', target = '', settings = {}) {
  const history = JSON.parse(localStorage.getItem('omnifree-history') || '[]');
  history.unshift({ name, output, input, target, settings, date: Date.now() }); localStorage.setItem('omnifree-history', JSON.stringify(history.slice(0, 50))); atualizarHistorico();
}
function exportarRelatorio(type) {
  const history = JSON.parse(localStorage.getItem('omnifree-history') || '[]');
  const content = type === 'json' ? JSON.stringify(history, null, 2) : ['arquivo,resultado,data', ...history.map((item) => `"${String(item.name).replace(/"/g, '""')}","${String(item.output).replace(/"/g, '""')}","${new Date(item.date).toISOString()}"`)].join('\n');
  const link = document.createElement('a'); link.href = URL.createObjectURL(new Blob([content], { type: type === 'json' ? 'application/json' : 'text/csv' })); link.download = `OmniFree_relatorio.${type}`; link.click(); URL.revokeObjectURL(link.href); reportDialog.close();
}
btnLimparHistorico.addEventListener('click', () => { localStorage.removeItem('omnifree-history'); atualizarHistorico(); });
atualizarHistorico();
privacyMode.checked = localStorage.getItem('omnifree-privacy-mode') === 'true';
privacyMode.addEventListener('change', () => localStorage.setItem('omnifree-privacy-mode', privacyMode.checked));
btnExportarRelatorio.addEventListener('click', () => reportDialog.showModal());
btnExportCsv.addEventListener('click', () => exportarRelatorio('csv'));
btnExportJson.addEventListener('click', () => exportarRelatorio('json'));
btnVerHistorico.addEventListener('click', () => historyDialog.showModal());
btnFecharHistorico.addEventListener('click', () => historyDialog.close());
historyDialog.addEventListener('click', (event) => { if (event.target === historyDialog) historyDialog.close(); });
const savedTheme = localStorage.getItem('omnifree-theme');
if (savedTheme === 'light') document.body.classList.add('light');
document.body.dataset.theme = savedTheme === 'light' ? 'light' : 'dark';
function updateThemeButton() {
  const isLight = document.body.classList.contains('light');
  btnTheme.textContent = isLight ? '◐' : '☼';
  btnTheme.title = isLight ? 'Usar tema escuro' : 'Usar tema claro';
  btnTheme.setAttribute('aria-label', btnTheme.title);
}
updateThemeButton();
btnTheme.addEventListener('click', () => { document.body.classList.toggle('light'); const theme = document.body.classList.contains('light') ? 'light' : 'dark'; document.body.dataset.theme = theme; localStorage.setItem('omnifree-theme', theme); updateThemeButton(); });
language.value = localStorage.getItem('omnifree-language') || 'pt-BR';
const staticCopy = {
  'pt-BR': { topbar: 'CONVERSOR LOCAL · SEUS ARQUIVOS, SEU COMPUTADOR', flow: 'FLUXO', add: 'Adicionar', addHint: 'Arquivo ou pasta', prepare: 'Preparar', prepareHint: 'Formato e qualidade', export: 'Exportar', exportHint: 'Resultado local', privacy: 'PRIVACIDADE', privacyHint: 'Nenhum arquivo é enviado.', newConversion: 'NOVA CONVERSÃO', title: 'Transforme um arquivo,\nsem sair do seu computador.', activity: 'ATIVIDADE', recent: 'Conversões recentes', quick: 'Ferramentas rápidas', rename: 'Renomear em lote', app: 'Aplicativo', web: 'Página web', webHint: 'Salvar como PDF', captions: 'Legendas', captionsHint: 'Extrair de vídeo', thumbnail: 'Thumbnail', thumbnailHint: 'Gerar capa', ocr: 'OCR', ocrHint: 'Imagem para texto', codes: 'QR e barras', codesHint: 'Ler código' },
  en: { topbar: 'LOCAL CONVERTER · YOUR FILES, YOUR COMPUTER', flow: 'WORKFLOW', add: 'Add', addHint: 'File or folder', prepare: 'Prepare', prepareHint: 'Format and quality', export: 'Export', exportHint: 'Local result', privacy: 'PRIVACY', privacyHint: 'No file is uploaded.', newConversion: 'NEW CONVERSION', title: 'Transform a file,\nwithout leaving your computer.', activity: 'ACTIVITY', recent: 'Recent conversions', quick: 'Quick tools', rename: 'Batch rename', app: 'Application', web: 'Web page', webHint: 'Save as PDF', captions: 'Captions', captionsHint: 'Extract from video', thumbnail: 'Thumbnail', thumbnailHint: 'Generate cover', ocr: 'OCR', ocrHint: 'Image to text', codes: 'QR & barcodes', codesHint: 'Read code' }
};
const interfaceLabels = {
  'pt-BR': { folder: 'Pasta', change: 'Trocar', preview: 'Pré-visualização', queue: 'Fila de conversão', queueHint: 'Use as setas para alterar a ordem.', ready: 'Pronto para exportar', readyHint: 'Ajuste nome, destino e qualidade se quiser.', output: 'Opções de saída', advanced: 'Ajustes específicos do arquivo', pdfTitle: 'Ferramentas de PDF', pdfHint: 'Operações disponíveis para este arquivo.', join: 'Juntar', split: 'Separar', pages: 'Páginas', rotate: 'Girar', password: 'Senha', compress: 'Compactar', images: 'Imagens', cancel: 'Cancelar', openFolder: 'Abrir na pasta', copyPath: 'Copiar caminho', dragFile: 'Arrastar arquivo', compare: 'Antes e depois', clearHistory: 'Limpar histórico', viewAll: 'Ver tudo', progressPreparing: 'Preparando', prefix: 'Prefixo', suffix: 'Sufixo', find: 'Trocar', replace: 'Por', date: 'Data', number: 'Número', components: 'Componentes', refresh: 'Atualizar', exportReport: 'Exportar relatório', privacyClose: 'Limpar histórico ao fechar', settings: 'Configurações', settingsHint: 'Preferências deste computador.', theme: 'Tema', dark: 'Escuro', light: 'Claro', defaultOutput: 'Destino padrão', checkStartup: 'Verificar atualizações ao abrir', addRule: 'Adicionar regra automática', saveSettings: 'Salvar configurações', report: 'Relatório', exportCsv: 'Exportar CSV', exportJson: 'Exportar JSON', history: 'Histórico completo', historyHint: 'Abra, repita ou remova uma conversão.', footer: 'OMNIFREE · CONVERSÃO LOCAL DE ARQUIVOS', chosenFolder: 'Pasta escolhida', desktop: 'Área de Trabalho', noRule: 'Nenhuma regra automática', activeRules: 'regra(s) ativa(s)' },
  en: { folder: 'Folder', change: 'Change', preview: 'Preview', queue: 'Conversion queue', queueHint: 'Use the arrows to change the order.', ready: 'Ready to export', readyHint: 'Adjust the name, destination, and quality if needed.', output: 'Output options', advanced: 'File-specific adjustments', pdfTitle: 'PDF tools', pdfHint: 'Available operations for this file.', join: 'Merge', split: 'Split', pages: 'Pages', rotate: 'Rotate', password: 'Password', compress: 'Compress', images: 'Images', cancel: 'Cancel', openFolder: 'Open folder', copyPath: 'Copy path', dragFile: 'Drag file', compare: 'Before and after', clearHistory: 'Clear history', viewAll: 'View all', progressPreparing: 'Preparing', prefix: 'Prefix', suffix: 'Suffix', find: 'Find', replace: 'Replace with', date: 'Date', number: 'Number', components: 'Components', refresh: 'Refresh', exportReport: 'Export report', privacyClose: 'Clear history when closing', settings: 'Settings', settingsHint: 'Preferences for this computer.', theme: 'Theme', dark: 'Dark', light: 'Light', defaultOutput: 'Default destination', checkStartup: 'Check for updates on startup', addRule: 'Add automatic rule', saveSettings: 'Save settings', report: 'Report', exportCsv: 'Export CSV', exportJson: 'Export JSON', history: 'Full history', historyHint: 'Open, repeat, or remove a conversion.', footer: 'OMNIFREE · LOCAL FILE CONVERSION', chosenFolder: 'Selected folder', desktop: 'Desktop', noRule: 'No automatic rules', activeRules: 'active rule(s)' }
};
function ui(key) { return (interfaceLabels[language.value] || interfaceLabels['pt-BR'])[key] || key; }
function setText(selector, value) { const element = document.querySelector(selector); if (element) element.textContent = value; }
function setLabelText(selector, value) { const element = document.querySelector(selector); const text = element && [...element.childNodes].find((node) => node.nodeType === Node.TEXT_NODE && node.nodeValue.trim()); if (text) text.nodeValue = value; }
function setFieldLabel(id, value) { const element = document.getElementById(id); const label = element?.closest('label'); if (label) { const text = [...label.childNodes].find((node) => node.nodeType === Node.TEXT_NODE && node.nodeValue.trim()); if (text) text.nodeValue = value; } }
function applyInterfaceLabels(locale) {
  const labels = interfaceLabels[locale] || interfaceLabels['pt-BR'];
  [['#btn-pasta', 'folder'], ['#btn-trocar', 'change'], ['.preview-panel > div > strong', 'preview'], ['.queue-panel > div > strong', 'queue'], ['.queue-panel > div > span', 'queueHint'], ['.export-strip strong', 'ready'], ['.export-strip small', 'readyHint'], ['.control-drawer:not(#advanced-options) > summary', 'output'], ['#advanced-options > summary', 'advanced'], ['#pdf-tools strong', 'pdfTitle'], ['#pdf-tools span', 'pdfHint'], ['#btn-juntar-pdf', 'join'], ['#btn-separar-pdf', 'split'], ['#btn-extrair-paginas', 'pages'], ['#btn-girar-pdf', 'rotate'], ['#btn-proteger-pdf', 'password'], ['#btn-otimizar-pdf', 'compress'], ['#btn-extrair-imagens', 'images'], ['#btn-cancelar', 'cancel'], ['#btn-abrir', 'openFolder'], ['#btn-copiar-caminho', 'copyPath'], ['#btn-arrastar-resultado', 'dragFile'], ['.comparison-panel > strong', 'compare'], ['#btn-ver-historico', 'viewAll'], ['#btn-limpar-historico', 'clearHistory'], ['#btn-componentes', 'refresh'], ['#btn-exportar-relatorio', 'exportReport'], ['#btn-adicionar-regra', 'addRule'], ['#btn-salvar-configuracoes', 'saveSettings'], ['#btn-export-csv', 'exportCsv'], ['#btn-export-json', 'exportJson'], ['footer', 'footer']].forEach(([selector, key]) => setText(selector, labels[key]));
  [['.rename-tools label:nth-child(1)', 'prefix'], ['.rename-tools label:nth-child(2)', 'suffix'], ['.rename-tools label:nth-child(3)', 'find'], ['.rename-tools label:nth-child(4)', 'replace'], ['.rename-tools label:nth-child(5)', 'date'], ['.rename-tools label:nth-child(6)', 'number'], ['.components strong', 'components'], ['label:has(#privacy-mode)', 'privacyClose'], ['.settings-content label:nth-child(1)', 'theme'], ['.settings-content label:nth-child(2)', 'language'], ['.settings-content label:nth-child(3)', 'defaultOutput'], ['.settings-content label:nth-child(4)', 'checkStartup']].forEach(([selector, key]) => setLabelText(selector, labels[key] || (key === 'language' ? (locale === 'en' ? 'Language' : 'Idioma') : '')));
  const settingsOptions = settingsTheme.options; if (settingsOptions.length >= 2) { settingsOptions[0].textContent = labels.dark; settingsOptions[1].textContent = labels.light; }
  setText('#history-dialog .dialog-heading strong', labels.history); setText('#history-dialog .dialog-heading span', labels.historyHint); setText('#settings-dialog .dialog-heading strong', labels.settings); setText('#settings-dialog .dialog-heading span', labels.settingsHint); setText('#report-dialog strong', labels.report);
  const managerTitles = document.querySelectorAll('.manager-section > div:first-child'); if (managerTitles[0]) { managerTitles[0].querySelector('strong').textContent = locale === 'en' ? 'Saved profiles' : 'Perfis salvos'; managerTitles[0].querySelector('span').textContent = locale === 'en' ? 'Edit or remove profiles you created.' : 'Edite ou remova perfis criados por você.'; } if (managerTitles[1]) { managerTitles[1].querySelector('strong').textContent = locale === 'en' ? 'Automatic rules' : 'Regras automáticas'; managerTitles[1].querySelector('span').textContent = locale === 'en' ? 'Folders watched on this computer.' : 'Pastas monitoradas neste computador.'; }
  const profileLabels = locale === 'en' ? ['Balanced', 'Smaller file', 'Higher quality', 'WhatsApp', 'Instagram', 'YouTube', 'Print'] : ['Equilibrado', 'Arquivo menor', 'Maior qualidade', 'WhatsApp', 'Instagram', 'YouTube', 'Impressão']; ['balanced', 'small', 'quality', 'whatsapp', 'instagram', 'youtube', 'print'].forEach((value, index) => { const option = preset.querySelector(`option[value="${value}"]`); if (option) option.textContent = profileLabels[index]; });
  const fields = locale === 'en' ? { 'output-name': 'Output name', preset: 'Profile', quality: 'Quality ', 'btn-destino': 'Destination', width: 'Width', height: 'Height', 'crop-image': 'Crop to fill', 'batch-sizes': 'Batch sizes', watermark: 'Watermark', 'start-time': 'Start time', duration: 'Duration', 'audio-only': 'Extract audio', 'no-audio': 'Remove audio', 'remove-metadata': 'Remove metadata' } : { 'output-name': 'Nome do resultado', preset: 'Perfil', quality: 'Qualidade ', 'btn-destino': 'Destino', width: 'Largura', height: 'Altura', 'crop-image': 'Recortar para preencher', 'batch-sizes': 'Lote', watermark: 'Marca-d’água', 'start-time': 'Início', duration: 'Duração', 'audio-only': 'Extrair áudio', 'no-audio': 'Remover áudio', 'remove-metadata': 'Remover metadados' };
  Object.entries(fields).forEach(([id, value]) => setFieldLabel(id, value));
  setText('#btn-watermark-image', locale === 'en' ? 'Choose image' : 'Escolher imagem'); setFieldLabel('crop-grid', locale === 'en' ? 'Crop area' : 'Área');
}
function applyStaticCopy(locale) {
  const copy = staticCopy[locale] || staticCopy['pt-BR']; const set = (selector, value) => { const element = document.querySelector(selector); if (element) element.textContent = value; };
  const setTitle = (selector, value) => { const element = document.querySelector(selector); if (element) element.innerHTML = value.split('\n').join('<br>'); };
  set('.topbar-context', copy.topbar); set('.workflow-rail > span', copy.flow); set('.rail-step:nth-of-type(1) strong', copy.add); set('.rail-step:nth-of-type(1) small', copy.addHint); set('.rail-step:nth-of-type(2) strong', copy.prepare); set('.rail-step:nth-of-type(2) small', copy.prepareHint); set('.rail-step:nth-of-type(3) strong', copy.export); set('.rail-step:nth-of-type(3) small', copy.exportHint); set('.workflow-rail p', `${copy.privacy}\n${copy.privacyHint}`); set('.workspace-heading > span', copy.newConversion); setTitle('.workspace-heading h1', copy.title); set('.desk-card header span', copy.activity); set('.desk-card > strong', copy.recent); set('.side-desk details:nth-of-type(1) summary', copy.quick); set('.side-desk details:nth-of-type(2) summary', copy.rename); set('.side-desk details:nth-of-type(3) summary', copy.app);
  const labelText = locale === 'en' ? ['SEARCH', 'CATEGORY', 'CONVERT TO'] : ['BUSCAR', 'CATEGORIA', 'CONVERTER PARA'];
  document.querySelectorAll('.format-controls label').forEach((label, index) => { const textNode = [...label.childNodes].find((node) => node.nodeType === Node.TEXT_NODE); if (textNode) textNode.nodeValue = labelText[index] || ''; });
  const tools = [['#btn-web-pdf', copy.web, copy.webHint], ['#btn-legendas', copy.captions, copy.captionsHint], ['#btn-thumbnail', copy.thumbnail, copy.thumbnailHint], ['#btn-ocr', copy.ocr, copy.ocrHint], ['#btn-ler-codigo', copy.codes, copy.codesHint]];
  tools.forEach(([selector, title, hint]) => { const button = document.querySelector(selector); if (button) button.innerHTML = `<b>${title}</b><small>${hint}</small>`; });
}
function applyLanguage() { const locale = language.value; document.documentElement.lang = locale; document.querySelectorAll('[data-i18n]').forEach((element) => { element.textContent = t(element.dataset.i18n); }); applyStaticCopy(locale); applyInterfaceLabels(locale); formatSearch.placeholder = locale === 'en' ? 'Search format' : 'Pesquisar formato'; const labels = locale === 'en' ? ['All categories', 'Images', 'Audio & video', 'Documents', 'Archives', 'E-books', 'Data'] : ['Todas as categorias', 'Imagens', 'Áudio e vídeo', 'Documentos', 'Compactados', 'E-books', 'Dados']; [...categoryFilter.options].forEach((option, index) => { option.textContent = labels[index]; }); atualizarHistorico(); atualizarInterfaceInteligente(opcoesAtuais); }
applyLanguage();
language.addEventListener('change', () => { localStorage.setItem('omnifree-language', language.value); applyLanguage(); });

function atualizarQualidade() { qualityValue.textContent = `${quality.value}%`; }
quality.addEventListener('input', atualizarQualidade);
preset.addEventListener('change', () => {
  const profiles = { balanced: { quality: 80 }, small: { quality: 55 }, quality: { quality: 95 }, whatsapp: { quality: 65, width: 1280, height: 720, target: 'mp4' }, instagram: { quality: 85, width: 1080, height: 1350, target: 'jpg' }, youtube: { quality: 95, width: 1920, height: 1080, target: 'mp4' }, print: { quality: 100, width: 3000, target: 'png' } };
  const selected = profiles[preset.value] || JSON.parse(localStorage.getItem(`omnifree-profile-${preset.value}`) || '{}');
  if (selected.quality) quality.value = selected.quality; if (selected.width) width.value = selected.width; if (selected.height) height.value = selected.height;
  if (selected.target && [...formatoSaida.options].some((option) => option.value === selected.target)) formatoSaida.value = selected.target;
  atualizarQualidade();
});
btnSalvarPerfil.addEventListener('click', async () => { const editingId = preset.value.startsWith('saved-') ? preset.value : ''; const oldName = preset.querySelector(`option[value="${editingId}"]`)?.textContent || ''; const name = await solicitarTexto({ title: language.value === 'en' ? 'Save conversion profile' : 'Salvar perfil de conversão', message: editingId ? (language.value === 'en' ? 'This updates the selected saved profile.' : 'Isso atualiza o perfil salvo selecionado.') : (language.value === 'en' ? 'Give this configuration a memorable name.' : 'Dê um nome para identificar esta configuração.'), value: oldName, placeholder: language.value === 'en' ? 'Example: My Instagram' : 'Exemplo: Meu Instagram' }); if (!name) return; const id = editingId || `saved-${Date.now()}`; localStorage.setItem(`${profilePrefix}${id}`, JSON.stringify({ ...profileSettings(), name: name.trim() })); const option = preset.querySelector(`option[value="${id}"]`); if (option) option.textContent = name.trim(); else preset.add(new Option(name.trim(), id)); preset.value = id; renderSavedProfiles(); });
btnDestino.addEventListener('click', async () => {
  const pasta = await window.conversorAPI.escolherPastaDestino();
  if (pasta) { pastaDestino = pasta; btnDestino.textContent = 'Pasta escolhida'; }
});
btnConfiguracoes.addEventListener('click', async () => { pastaDestinoPadrao = localStorage.getItem('omnifree-default-output') || ''; btnDestinoPadrao.textContent = pastaDestinoPadrao ? ui('chosenFolder') : ui('desktop'); settingsTheme.value = document.body.classList.contains('light') ? 'light' : 'dark'; settingsLanguage.value = language.value; const preferences = await window.conversorAPI.obterPreferencias(); autoUpdates.checked = preferences.autoUpdates !== false; regrasAutomaticas = await window.conversorAPI.obterRegras(); renderSavedProfiles(); renderRules(); settingsDialog.showModal(); });
btnAdicionarRegra.addEventListener('click', async () => { const folder = await window.conversorAPI.escolherPastaDestino(); if (!folder) return; const outputDir = await window.conversorAPI.escolherPastaDestino(); if (!outputDir || outputDir === folder) { mostrarResultado('error', language.value === 'en' ? 'Choose a different output folder.' : 'Escolha uma pasta de saída diferente.'); return; } const target = await solicitarTexto({ title: language.value === 'en' ? 'Automatic format' : 'Formato automático', message: language.value === 'en' ? 'Use mp3, pdf, or webp.' : 'Use mp3, pdf ou webp.', value: 'webp' }); if (!['mp3', 'pdf', 'webp'].includes((target || '').toLowerCase())) return; regrasAutomaticas.push({ id: crypto.randomUUID(), folder, outputDir, target: target.toLowerCase() }); await window.conversorAPI.salvarRegras(regrasAutomaticas); renderRules(); });
btnFecharConfiguracoes.addEventListener('click', () => settingsDialog.close());
btnDestinoPadrao.addEventListener('click', async () => { const folder = await window.conversorAPI.escolherPastaDestino(); if (folder) { pastaDestinoPadrao = folder; btnDestinoPadrao.textContent = 'Pasta escolhida'; } });
btnSalvarConfiguracoes.addEventListener('click', async () => { localStorage.setItem('omnifree-default-output', pastaDestinoPadrao); document.body.classList.toggle('light', settingsTheme.value === 'light'); document.body.dataset.theme = settingsTheme.value; localStorage.setItem('omnifree-theme', settingsTheme.value); updateThemeButton(); language.value = settingsLanguage.value; localStorage.setItem('omnifree-language', language.value); applyLanguage(); await window.conversorAPI.salvarPreferencias({ autoUpdates: autoUpdates.checked }); await window.conversorAPI.salvarRegras(regrasAutomaticas); settingsDialog.close(); });
window.conversorAPI.receberArquivoRegra(async (rule) => { const options = await window.conversorAPI.obterOpcoes(rule.input); if (!options.formats.includes(rule.target)) return; const history = ruleHistory(); history.unshift({ date: Date.now(), name: rule.input.split(/[\\/]/).pop(), target: rule.target }); localStorage.setItem('omnifree-rule-history', JSON.stringify(history.slice(0, 30))); renderRules(history); const id = crypto.randomUUID(); window.conversorAPI.enviarArquivo(rule.input, rule.target, { outputDir: rule.outputDir }, id); });
btnWatermarkImage.addEventListener('click', async () => { const selected = await window.conversorAPI.escolherImagemMarcaDagua(); if (selected) { watermarkImage = selected; watermarkImageName.textContent = selected.split(/[\\/]/).pop(); } });
cropGrid.addEventListener('click', (event) => { const button = event.target.closest('button[data-position]'); if (!button) return; cropPosition = button.dataset.position; cropGrid.querySelectorAll('button').forEach((item) => item.classList.toggle('selected', item === button)); });
async function atualizarComponentes() {
  const components = await window.conversorAPI.obterComponentes();
  const active = Object.values(components).filter(Boolean).length;
  componentsStatus.textContent = `${active}/${Object.keys(components).length} componentes prontos`;
  const commands = { LibreOffice: 'Instale LibreOffice.', Pandoc: 'Instale Pandoc.', '7-Zip': 'Instale 7-Zip.', Calibre: 'Instale Calibre.', QPDF: 'choco install qpdf -y', Poppler: 'choco install poppler -y' };
  const missing = Object.keys(components).filter((name) => !components[name] && commands[name]);
  componentsHelp.textContent = missing.length ? `Faltam: ${missing.join(', ')}. ${missing.map((name) => commands[name]).join(' ')}` : 'Todos os componentes opcionais estão prontos.';
}
btnComponentes.addEventListener('click', atualizarComponentes);
atualizarComponentes();
btnVerificarAtualizacoes.addEventListener('click', async () => {
  btnVerificarAtualizacoes.disabled = true; updatesStatus.textContent = language.value === 'en' ? 'Checking for updates…' : 'Verificando atualizações…';
  const result = await window.conversorAPI.verificarAtualizacoes();
  updatesStatus.textContent = result.message;
  btnBaixarAtualizacao.hidden = result.status !== 'available';
  btnInstalarAtualizacao.hidden = true;
  btnVerificarAtualizacoes.disabled = false;
});
btnBaixarAtualizacao.addEventListener('click', async () => {
  btnBaixarAtualizacao.disabled = true;
  updatesStatus.textContent = language.value === 'en' ? 'Downloading update…' : 'Baixando atualização…';
  const result = await window.conversorAPI.baixarAtualizacao();
  if (result.message) updatesStatus.textContent = result.message;
  if (result.status === 'downloaded') { btnBaixarAtualizacao.hidden = true; btnInstalarAtualizacao.hidden = false; }
  btnBaixarAtualizacao.disabled = false;
});
btnInstalarAtualizacao.addEventListener('click', () => {
  btnInstalarAtualizacao.disabled = true;
  updatesStatus.textContent = language.value === 'en' ? 'Restarting to install…' : 'Reiniciando para instalar…';
  window.conversorAPI.instalarAtualizacao();
});
window.conversorAPI.receberStatusAtualizacao((status) => {
  if (status.status === 'downloading') updatesStatus.textContent = language.value === 'en' ? `Downloading update: ${status.percent}%` : `Baixando atualização: ${status.percent}%`;
  if (status.status === 'downloaded') {
    updatesStatus.textContent = language.value === 'en' ? `Version ${status.version} is ready to install.` : `A versão ${status.version} está pronta para instalar.`;
    btnBaixarAtualizacao.hidden = true; btnInstalarAtualizacao.hidden = false;
  }
  if (status.status === 'error') updatesStatus.textContent = `${language.value === 'en' ? 'Update error' : 'Erro na atualização'}: ${status.message}`;
});
btnJuntarPdf.addEventListener('click', async () => {
  try {
    const result = await window.conversorAPI.mesclarPdfs();
    if (!result.canceled) { ultimoArquivoConvertido = result.output; btnAbrir.hidden = false; mostrarResultado('success', 'PDFs unidos com sucesso.'); }
  } catch (error) { mostrarResultado('error', `Não foi possível unir os PDFs: ${error.message}`); }
});
btnSepararPdf.addEventListener('click', async () => {
  try {
    if (!arquivoSelecionado || !arquivoSelecionado.name.toLowerCase().endsWith('.pdf')) throw new Error('Selecione um PDF primeiro.');
    const files = await window.conversorAPI.separarPdf(arquivoSelecionado.path, pastaDestino);
    ultimoArquivoConvertido = files[0]; btnAbrir.hidden = false; mostrarResultado('success', `${files.length} página(s) foram separadas.`);
  } catch (error) { mostrarResultado('error', error.message); }
});
btnExtrairPaginas.addEventListener('click', async () => { try { const pages = await solicitarTexto({ title: language.value === 'en' ? 'Extract PDF pages' : 'Extrair páginas do PDF', message: language.value === 'en' ? 'Example: 1, 3-5' : 'Exemplo: 1, 3-5', placeholder: '1, 3-5' }); if (!pages) return; ultimoArquivoConvertido = await window.conversorAPI.extrairPaginasPdf(pdfSelecionado(), pastaDestino || pastaDestinoPadrao, pages); btnAbrir.hidden = false; mostrarResultado('success', 'Páginas extraídas com sucesso.'); } catch (error) { mostrarResultado('error', error.message); } });
btnGirarPdf.addEventListener('click', async () => {
  try {
    if (!arquivoSelecionado || !arquivoSelecionado.name.toLowerCase().endsWith('.pdf')) throw new Error('Selecione um PDF primeiro.');
    ultimoArquivoConvertido = await window.conversorAPI.girarPdf(arquivoSelecionado.path, pastaDestino); btnAbrir.hidden = false; mostrarResultado('success', 'PDF girado em 90° com sucesso.');
  } catch (error) { mostrarResultado('error', error.message); }
});
function pdfSelecionado() {
  if (!arquivoSelecionado || !arquivoSelecionado.name.toLowerCase().endsWith('.pdf')) throw new Error('Selecione um PDF primeiro.');
  return arquivoSelecionado.path;
}
btnProtegerPdf.addEventListener('click', async () => {
  try {
    const password = await solicitarTexto({ title: language.value === 'en' ? 'Protect PDF' : 'Proteger PDF', message: language.value === 'en' ? 'Set the password required to open this PDF.' : 'Defina a senha necessária para abrir este PDF.', type: 'password' }); if (!password) return;
    ultimoArquivoConvertido = await window.conversorAPI.protegerPdf(pdfSelecionado(), pastaDestino, password); btnAbrir.hidden = false; mostrarResultado('success', 'PDF protegido com senha.');
  } catch (error) { mostrarResultado('error', error.message); }
});
btnOtimizarPdf.addEventListener('click', async () => {
  try { const selected = await solicitarTexto({ title: language.value === 'en' ? 'Compress PDF' : 'Compactar PDF', message: language.value === 'en' ? 'Type: smaller, balanced, or quality.' : 'Digite: menor, equilibrado ou qualidade.', value: language.value === 'en' ? 'balanced' : 'equilibrado' }); if (!selected) return; const mode = selected.toLowerCase().startsWith('men') || selected.toLowerCase().startsWith('small') ? 'small' : selected.toLowerCase().startsWith('qual') ? 'quality' : 'balanced'; ultimoArquivoConvertido = await window.conversorAPI.otimizarPdf(pdfSelecionado(), pastaDestino || pastaDestinoPadrao, mode); btnAbrir.hidden = false; mostrarResultado('success', 'PDF compactado.'); } catch (error) { mostrarResultado('error', error.message); }
});
btnExtrairImagens.addEventListener('click', async () => {
  try { const folder = await window.conversorAPI.extrairImagensPdf(pdfSelecionado(), pastaDestino); ultimoArquivoConvertido = folder; btnAbrir.hidden = false; mostrarResultado('success', 'Imagens extraídas para uma nova pasta.'); } catch (error) { mostrarResultado('error', error.message); }
});
btnWebPdf.addEventListener('click', async () => { try { const url = await solicitarTexto({ title: language.value === 'en' ? 'Web page to PDF' : 'Página web para PDF', message: language.value === 'en' ? 'Paste a complete address beginning with https://.' : 'Cole um endereço completo começando com https://.', placeholder: 'https://' }); if (!url) return; ultimoArquivoConvertido = await window.conversorAPI.paginaWebParaPdf(url, pastaDestino || pastaDestinoPadrao); btnAbrir.hidden = false; mostrarResultado('success', 'Página salva como PDF.'); } catch (error) { mostrarResultado('error', error.message); } });
btnLegendas.addEventListener('click', async () => { try { if (!arquivoSelecionado) throw new Error('Selecione um vídeo primeiro.'); const format = await solicitarTexto({ title: language.value === 'en' ? 'Extract captions' : 'Extrair legendas', message: language.value === 'en' ? 'Choose srt, vtt, or ass.' : 'Escolha srt, vtt ou ass.', value: 'srt' }); if (!format) return; ultimoArquivoConvertido = await window.conversorAPI.extrairLegendas(arquivoSelecionado.path, pastaDestino || pastaDestinoPadrao, ['srt', 'vtt', 'ass'].includes(format.toLowerCase()) ? format.toLowerCase() : 'srt'); btnAbrir.hidden = false; mostrarResultado('success', 'Legenda extraída com sucesso.'); } catch (error) { mostrarResultado('error', error.message); } });
btnThumbnail.addEventListener('click', async () => { try { if (!arquivoSelecionado) throw new Error('Selecione um vídeo primeiro.'); const time = await solicitarTexto({ title: language.value === 'en' ? 'Generate thumbnail' : 'Gerar thumbnail', message: language.value === 'en' ? 'Choose the moment for the cover.' : 'Escolha o instante para a capa.', value: '00:00:01', placeholder: '00:00:01' }); if (!time) return; ultimoArquivoConvertido = await window.conversorAPI.gerarThumbnail(arquivoSelecionado.path, pastaDestino || pastaDestinoPadrao, time); btnAbrir.hidden = false; btnCopiarCaminho.hidden = false; btnArrastarResultado.hidden = false; mostrarResultado('success', 'Thumbnail gerada.'); } catch (error) { mostrarResultado('error', error.message); } });
btnOcr.addEventListener('click', async () => { try { if (!arquivoSelecionado) throw new Error('Selecione uma imagem ou PDF primeiro.'); const result = await window.conversorAPI.ocrArquivo(arquivoSelecionado.path, pastaDestino || pastaDestinoPadrao, 'por+eng'); ultimoArquivoConvertido = result.output; btnAbrir.hidden = false; mostrarResultado('success', `Texto extraído: ${result.text.slice(0, 120) || 'sem texto reconhecido'}`); } catch (error) { mostrarResultado('error', error.message); } });
btnLerCodigo.addEventListener('click', async () => { try { if (!arquivoSelecionado) throw new Error('Selecione uma imagem ou PDF primeiro.'); const result = await window.conversorAPI.lerCodigo(arquivoSelecionado.path); mostrarResultado('success', `Código lido: ${result.text}`); } catch (error) { mostrarResultado('error', error.message); } });

function formatarTamanho(bytes) {
  if (!bytes) return 'Arquivo vazio';
  const units = ['B', 'KB', 'MB', 'GB']; let index = 0; let value = bytes;
  while (value >= 1024 && index < units.length - 1) { value /= 1024; index += 1; }
  return `${value.toFixed(index ? 1 : 0)} ${units[index]}`;
}

function renderizarPrevia(file) {
  if (previewUrl) URL.revokeObjectURL(previewUrl);
  previewContent.innerHTML = ''; previewPanel.hidden = true;
  if (!file) return;
  const extension = (file.name.split('.').pop() || '').toLowerCase();
  const image = ['png', 'jpg', 'jpeg', 'webp', 'avif', 'gif', 'bmp'].includes(extension);
  const video = ['mp4', 'mkv', 'mov', 'avi', 'webm', 'm4v'].includes(extension);
  const audio = ['mp3', 'wav', 'm4a', 'ogg', 'flac', 'opus'].includes(extension);
  const pdf = extension === 'pdf';
  if (!image && !video && !audio && !pdf) return;
  previewUrl = URL.createObjectURL(file); previewPanel.hidden = false;
  previewKind.textContent = image ? 'Imagem' : video ? 'Vídeo' : audio ? 'Áudio' : 'PDF';
  const media = document.createElement(image ? 'img' : video ? 'video' : audio ? 'audio' : 'embed');
  media.src = previewUrl; if (video || audio) media.controls = true; if (video) media.muted = true; if (pdf) media.type = 'application/pdf';
  previewContent.appendChild(media);
}

function renderizarFila() {
  queueItems.innerHTML = ''; queuePanel.hidden = arquivosSelecionados.length < 2;
  arquivosSelecionados.forEach((file, index) => {
    const row = document.createElement('div'); row.className = `queue-item ${file.queueStatus || ''}`; row.draggable = !activeQueueFile;
    const handle = document.createElement('span'); handle.textContent = '⠿'; handle.title = language.value === 'en' ? 'Drag to reorder' : 'Arraste para reordenar';
    const copy = document.createElement('div'); copy.className = 'queue-copy'; const name = document.createElement('span'); name.textContent = `${index + 1}. ${file.name}`; const state = document.createElement('small'); const status = file.queueStatus === 'processing' ? (language.value === 'en' ? `Converting · ${file.queueProgress || 0}%` : `Convertendo · ${file.queueProgress || 0}%`) : file.queueStatus === 'done' ? (language.value === 'en' ? 'Completed' : 'Concluído') : file.queueStatus === 'error' ? `${language.value === 'en' ? 'Failed' : 'Falhou'}: ${file.queueError || (language.value === 'en' ? 'Unknown error' : 'Erro desconhecido')}` : file.queueStatus === 'cancelled' ? (language.value === 'en' ? 'Cancelled' : 'Cancelado') : (language.value === 'en' ? 'Waiting' : 'Aguardando'); state.textContent = status; state.title = file.queueError || status; copy.append(name, state);
    const actions = document.createElement('div'); actions.className = 'queue-actions'; const retry = document.createElement('button'); retry.textContent = '↻'; retry.title = language.value === 'en' ? 'Retry this file' : 'Tentar este arquivo novamente'; retry.hidden = file.queueStatus !== 'error'; retry.addEventListener('click', () => { retryOnlyFiles = [file]; btnConverter.click(); }); const remove = document.createElement('button'); remove.textContent = '×'; remove.title = language.value === 'en' ? 'Remove from queue' : 'Remover da fila'; remove.disabled = Boolean(activeQueueFile); remove.addEventListener('click', () => { arquivosSelecionados.splice(index, 1); if (arquivoSelecionado === file) escolherArquivo(arquivosSelecionados[0], arquivosSelecionados); else renderizarFila(); }); actions.append(retry, remove); row.append(handle, copy, actions);
    if (file.queueStatus === 'processing') { const progress = document.createElement('div'); progress.className = 'queue-progress'; const fill = document.createElement('span'); fill.style.width = `${file.queueProgress || 0}%`; progress.appendChild(fill); row.appendChild(progress); }
    row.addEventListener('dragstart', () => { draggedQueueFile = file; row.classList.add('dragging'); }); row.addEventListener('dragend', () => { draggedQueueFile = null; row.classList.remove('dragging'); }); row.addEventListener('dragover', (event) => { if (!draggedQueueFile || draggedQueueFile === file) return; event.preventDefault(); row.classList.add('drag-over'); }); row.addEventListener('dragleave', () => row.classList.remove('drag-over')); row.addEventListener('drop', (event) => { event.preventDefault(); row.classList.remove('drag-over'); if (!draggedQueueFile || draggedQueueFile === file) return; const from = arquivosSelecionados.indexOf(draggedQueueFile); const to = arquivosSelecionados.indexOf(file); arquivosSelecionados.splice(from, 1); arquivosSelecionados.splice(to, 0, draggedQueueFile); renderizarFila(); }); queueItems.appendChild(row);
  });
}

function mostrarResultado(type, message) {
  resultPanel.hidden = false;
  resultPanel.className = `result-panel ${type}`;
  resultIcon.textContent = type === 'success' ? '✓' : '!' ;
  resultMessage.textContent = message;
}
function mostrarComparacao(before, after) { if (!before || !after) return; const percent = before.bytes ? Math.round((1 - after.bytes / before.bytes) * 100) : 0; const dimensions = (item) => item.width ? `${item.width}×${item.height}` : item.pages ? `${item.pages} página(s)` : '—'; comparisonContent.innerHTML = `<span>Antes: ${formatarTamanho(before.bytes)} · ${dimensions(before)}</span><span>Depois: ${formatarTamanho(after.bytes)} · ${dimensions(after)}</span><strong>${percent >= 0 ? `${percent}% menor` : `${Math.abs(percent)}% maior`}</strong>`; comparisonPanel.hidden = false; }

function atualizarInterfaceInteligente(opcoes) {
  opcoesAtuais = opcoes || null;
  const category = opcoes?.category || '';
  const isImage = category.includes('Imagem');
  const isMedia = category.includes('Áudio e vídeo');
  const isPdf = opcoes?.extension === 'pdf';
  document.querySelectorAll('.image-option').forEach((element) => { element.hidden = !isImage; });
  document.querySelectorAll('.media-option').forEach((element) => { element.hidden = !isMedia; });
  document.querySelectorAll('.media-or-image-option').forEach((element) => { element.hidden = !isImage && !isMedia; });
  advancedOptions.hidden = !opcoes?.supported || (!isImage && !isMedia);
  if (advancedOptions.hidden) advancedOptions.open = false;
  pdfTools.hidden = !isPdf;
  smartTip.textContent = !opcoes ? t('tipIdle') : !opcoes.supported ? (language.value === 'en' ? 'This file type is not supported yet.' : 'Esse tipo de arquivo ainda não é compatível.') : isPdf ? t('tipPdf') : isImage ? t('tipImage') : isMedia ? t('tipMedia') : t('tipGeneric');
}

function preencherFormatos(opcoes) {
  formatoSaida.innerHTML = '';
  if (!opcoes.supported) {
    formatoSaida.add(new Option('Formato não reconhecido', ''));
    formatoSaida.disabled = true; btnConverter.disabled = true;
    formatHelp.textContent = `Ainda não há conversor para .${opcoes.extension || 'este formato'}.`;
    atualizarInterfaceInteligente(opcoes);
    return;
  }
  formatosDisponiveis = opcoes.formats; renderizarFormatos();
  formatoSaida.disabled = false; btnConverter.disabled = false;
  formatHelp.textContent = `${opcoes.category}: ${opcoes.formats.length} formatos de saída disponíveis.`;
  atualizarInterfaceInteligente(opcoes);
}
function renderizarFormatos() {
  const term = formatSearch.value.trim().toLowerCase();
  const mediaFormats = ['mp4', 'mkv', 'mov', 'avi', 'webm', 'gif', 'mp3', 'wav', 'ogg', 'flac', 'm4a', 'opus'];
  const imageFormats = ['png', 'jpg', 'jpeg', 'webp', 'avif', 'tiff'];
  const documentFormats = ['pdf', 'docx', 'odt', 'rtf', 'txt', 'html', 'xlsx', 'ods', 'csv', 'tsv', 'pptx', 'odp'];
  const archiveFormats = ['zip', '7z', 'tar', 'gz', 'bz2', 'xz'];
  const ebookFormats = ['epub', 'mobi', 'azw3'];
  const categoryOf = (format) => imageFormats.includes(format) ? 'image' : mediaFormats.includes(format) ? 'media' : documentFormats.includes(format) ? 'document' : archiveFormats.includes(format) ? 'archive' : ebookFormats.includes(format) ? 'ebook' : 'data';
  formatoSaida.innerHTML = '';
  formatosDisponiveis.filter((format) => format.includes(term) && (!categoryFilter.value || categoryOf(format) === categoryFilter.value)).forEach((format) => formatoSaida.add(new Option(format.toUpperCase(), format)));
  if (!formatoSaida.options.length) formatoSaida.add(new Option(language.value === 'en' ? 'No format found' : 'Nenhum formato encontrado', ''));
}
formatSearch.addEventListener('input', renderizarFormatos);
categoryFilter.addEventListener('change', renderizarFormatos);

async function escolherArquivo(file, files = [file]) {
  if (!file) return;
  arquivosSelecionados = [...files]; arquivoSelecionado = file;
  resultPanel.hidden = true; btnAbrir.hidden = true; progressContainer.hidden = true;
  fileName.textContent = file.name;
  fileMeta.textContent = `${formatarTamanho(file.size)}${arquivosSelecionados.length > 1 ? ` · +${arquivosSelecionados.length - 1} na fila` : ''}`;
  fileDetails.hidden = false;
  comparisonPanel.hidden = true; detalhesEntrada = await window.conversorAPI.detalhesArquivo(file.path).catch(() => null);
  renderizarPrevia(file); renderizarFila();
  const opcoes = await window.conversorAPI.obterOpcoes(file.path);
  preencherFormatos(opcoes);
  if (!opcoes.supported) mostrarResultado('error', `O tipo .${opcoes.extension || 'desconhecido'} ainda não é compatível.`);
}

dropZone.addEventListener('click', () => fileInput.click());
dropZone.addEventListener('keydown', (event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); fileInput.click(); } });
fileInput.addEventListener('change', () => escolherArquivo(fileInput.files[0], fileInput.files));
btnTrocar.addEventListener('click', () => fileInput.click());
btnPasta.addEventListener('click', () => folderInput.click());
folderInput.addEventListener('change', () => escolherArquivo(folderInput.files[0], folderInput.files));
dropZone.addEventListener('dragover', (event) => { event.preventDefault(); dropZone.classList.add('dragover'); });
dropZone.addEventListener('dragleave', () => dropZone.classList.remove('dragover'));
async function filesFromDrop(dataTransfer) {
  const entries = [...(dataTransfer.items || [])].map((item) => item.webkitGetAsEntry && item.webkitGetAsEntry()).filter(Boolean);
  if (!entries.length) return [...dataTransfer.files];
  const collect = async (entry) => {
    if (entry.isFile) return new Promise((resolve) => entry.file((file) => resolve([file]), () => resolve([])));
    if (!entry.isDirectory) return [];
    const reader = entry.createReader(); const entriesInFolder = [];
    const readBatch = () => new Promise((resolve) => reader.readEntries(resolve, () => resolve([])));
    for (;;) { const batch = await readBatch(); if (!batch.length) break; entriesInFolder.push(...batch); }
    return (await Promise.all(entriesInFolder.map(collect))).flat();
  };
  return (await Promise.all(entries.map(collect))).flat();
}
dropZone.addEventListener('drop', async (event) => { event.preventDefault(); dropZone.classList.remove('dragover'); const files = await filesFromDrop(event.dataTransfer); escolherArquivo(files[0], files); });

btnConverter.addEventListener('click', async () => {
  if (!arquivoSelecionado || !formatoSaida.value) return;
  btnConverter.disabled = true; progressContainer.hidden = false; resultPanel.hidden = true; btnAbrir.hidden = true; cancelRequested = false; btnCancelar.hidden = false; btnCancelar.disabled = false;
  const sizeVariants = batchSizes.value.split(',').map((value) => value.trim().match(/^(\d{1,5})\s*[xX]\s*(\d{1,5})$/)).filter(Boolean).map((match) => ({ width: match[1], height: match[2], label: `${match[1]}x${match[2]}` }));
  const jobs = [];
  const sourceFiles = retryOnlyFiles || arquivosSelecionados; retryOnlyFiles = null;
  for (const file of sourceFiles) {
    const options = await window.conversorAPI.obterOpcoes(file.path);
    if (!options.formats.includes(formatoSaida.value)) continue;
    (sizeVariants.length ? sizeVariants : [{ width: width.value, height: height.value, label: '' }]).forEach((size) => jobs.push({ file, size }));
  }
  let completed = 0;
  for (const [jobIndex, job] of jobs.entries()) {
    if (cancelRequested) break;
    const { file, size } = job;
    activeQueueFile = file; file.queueStatus = 'processing'; file.queueProgress = 0; file.queueError = ''; renderizarFila();
    progressBar.style.width = '0%'; progressText.textContent = '0%'; progressState.textContent = `Arquivo ${jobIndex + 1} de ${jobs.length}: ${file.name}`;
    const suffix = [jobs.length > 1 ? jobIndex + 1 : '', size.label].filter(Boolean).join('_');
    let baseName = outputName.value || file.name.replace(/\.[^.]+$/, '');
    if (nameFind.value) baseName = baseName.split(nameFind.value).join(nameReplace.value);
    const date = nameDate.checked ? `_${new Date().toISOString().slice(0, 10)}` : '';
    const number = nameNumber.checked || jobs.length > 1 ? `_${String(jobIndex + 1).padStart(3, '0')}` : '';
    const batchName = `${namePrefix.value}${baseName}${nameSuffix.value}${date}${number}${suffix ? `_${suffix}` : ''}`;
    const settings = { outputDir: pastaDestino || pastaDestinoPadrao, outputName: batchName, quality: quality.value, width: size.width, height: size.height, cropImage: cropImage.checked, cropPosition, watermark: watermark.value, watermarkImage, startTime: startTime.value, duration: duration.value, audioOnly: audioOnly.checked, noAudio: noAudio.checked, removeMetadata: removeMetadata.checked };
    activeJobId = crypto.randomUUID();
    const result = await new Promise((resolve) => { queueResolve = resolve; window.conversorAPI.enviarArquivo(file.path, formatoSaida.value, settings, activeJobId); });
    activeJobId = '';
    if (result.status === 'concluido') { completed += 1; file.queueStatus = 'done'; file.queueProgress = 100; registrarHistorico(file.name, result.caminhoArquivo, file.path, formatoSaida.value, settings); if (file === arquivoSelecionado) mostrarComparacao(detalhesEntrada, await window.conversorAPI.detalhesArquivo(result.caminhoArquivo).catch(() => null)); }
    else if (result.status === 'cancelado') { cancelRequested = true; file.queueStatus = 'cancelled'; }
    else { file.queueStatus = 'error'; file.queueError = result.mensagem || 'Erro desconhecido'; }
    activeQueueFile = null; renderizarFila();
  }
  btnConverter.disabled = false; btnCancelar.hidden = true; mostrarResultado(cancelRequested ? 'error' : completed ? 'success' : 'error', cancelRequested ? `Conversão cancelada. ${completed} arquivo(s) concluído(s).` : completed ? `${completed} arquivo(s) convertido(s) na fila.` : 'Nenhum arquivo da fila aceita esse formato.');
});
btnCancelar.addEventListener('click', () => { cancelRequested = true; btnCancelar.disabled = true; progressState.textContent = 'Cancelando conversão…'; if (activeJobId) window.conversorAPI.cancelarConversao(activeJobId); });

async function repetirConversao(item) {
  if (!item.input || !item.target) return;
  historyDialog.close(); btnConverter.disabled = true; progressContainer.hidden = false; resultPanel.hidden = true;
  try {
    const settings = { ...(item.settings || {}), outputName: '' };
    activeJobId = crypto.randomUUID();
    const result = await new Promise((resolve) => { queueResolve = resolve; window.conversorAPI.enviarArquivo(item.input, item.target, settings, activeJobId); }); activeJobId = '';
    if (result.status === 'concluido') { ultimoArquivoConvertido = result.caminhoArquivo; btnAbrir.hidden = false; registrarHistorico(item.name, result.caminhoArquivo, item.input, item.target, settings); mostrarResultado('success', language.value === 'en' ? 'Conversion repeated successfully.' : 'Conversão repetida com sucesso.'); }
    else mostrarResultado('error', result.mensagem);
  } finally { btnConverter.disabled = !arquivoSelecionado; }
}

window.conversorAPI.receberProgresso((percentual) => { progressBar.style.width = `${percentual}%`; progressText.textContent = `${percentual}%`; if (activeQueueFile) { activeQueueFile.queueProgress = percentual; renderizarFila(); } });
window.conversorAPI.receberStatus((dados) => {
  if (dados.status === 'processando') return;
  if (queueResolve) { const resolve = queueResolve; queueResolve = null; if (dados.status === 'concluido') ultimoArquivoConvertido = dados.caminhoArquivo; resolve(dados); return; }
  btnConverter.disabled = !arquivoSelecionado;
  if (dados.status === 'concluido') {
    progressBar.style.width = '100%'; progressText.textContent = '100%'; progressState.textContent = 'Conversão concluída';
    ultimoArquivoConvertido = dados.caminhoArquivo; mostrarResultado('success', dados.mensagem); btnAbrir.hidden = false; btnCopiarCaminho.hidden = false; btnArrastarResultado.hidden = false;
  } else if (dados.status === 'erro') { progressContainer.hidden = true; mostrarResultado('error', dados.mensagem); }
});
btnAbrir.addEventListener('click', () => { if (ultimoArquivoConvertido) window.conversorAPI.abrirNoExplorador(ultimoArquivoConvertido); });
btnCopiarCaminho.addEventListener('click', () => { if (ultimoArquivoConvertido) window.conversorAPI.copiarCaminho(ultimoArquivoConvertido); });
btnArrastarResultado.addEventListener('dragstart', () => { if (ultimoArquivoConvertido) window.conversorAPI.arrastarArquivo(ultimoArquivoConvertido); });
document.addEventListener('keydown', (event) => { if (event.ctrlKey && event.key.toLowerCase() === 'o') { event.preventDefault(); fileInput.click(); } if (event.ctrlKey && event.key === 'Enter' && !btnConverter.disabled) btnConverter.click(); });
window.addEventListener('beforeunload', () => { if (localStorage.getItem('omnifree-privacy-mode') === 'true') localStorage.removeItem('omnifree-history'); });
