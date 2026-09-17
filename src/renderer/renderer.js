const dropZone = document.getElementById('drop-zone');
const fileInput = document.getElementById('file-input');
const folderInput = document.getElementById('folder-input');
const fileDetails = document.getElementById('file-details');
const fileName = document.getElementById('file-name');
const fileMeta = document.getElementById('file-meta');
const formatHelp = document.getElementById('format-help');
const formatoSaida = document.getElementById('formato-saida');
const btnConverter = document.getElementById('btn-converter');
const btnTrocar = document.getElementById('btn-trocar');
const btnPasta = document.getElementById('btn-pasta');
const progressContainer = document.getElementById('progress-container');
const progressBar = document.getElementById('progress-bar');
const progressText = document.getElementById('progress-text');
const progressState = document.getElementById('progress-state');
const resultPanel = document.getElementById('result-panel');
const resultIcon = document.getElementById('result-icon');
const resultMessage = document.getElementById('result-message');
const btnAbrir = document.getElementById('btn-abrir');
const preset = document.getElementById('preset');
const quality = document.getElementById('quality');
const qualityValue = document.getElementById('quality-value');
const btnDestino = document.getElementById('btn-destino');
const width = document.getElementById('width');
const height = document.getElementById('height');
const startTime = document.getElementById('start-time');
const duration = document.getElementById('duration');
const outputName = document.getElementById('output-name');
const componentsStatus = document.getElementById('components-status');
const btnComponentes = document.getElementById('btn-componentes');
const btnJuntarPdf = document.getElementById('btn-juntar-pdf');
const btnSepararPdf = document.getElementById('btn-separar-pdf');
const btnGirarPdf = document.getElementById('btn-girar-pdf');
const btnTheme = document.getElementById('btn-theme');
const btnProtegerPdf = document.getElementById('btn-proteger-pdf');
const btnOtimizarPdf = document.getElementById('btn-otimizar-pdf');
const btnExtrairImagens = document.getElementById('btn-extrair-imagens');
const historyList = document.getElementById('history-list');
const btnLimparHistorico = document.getElementById('btn-limpar-historico');

let arquivoSelecionado = null;
let ultimoArquivoConvertido = '';
let pastaDestino = '';
let arquivosSelecionados = [];
let queueResolve = null;

function atualizarHistorico() {
  const history = JSON.parse(localStorage.getItem('omnifree-history') || '[]');
  historyList.textContent = history.length ? history.slice(0, 3).map((item) => item.name).join(' · ') : 'Nenhuma conversão recente.';
}
function registrarHistorico(name) {
  const history = JSON.parse(localStorage.getItem('omnifree-history') || '[]');
  history.unshift({ name, date: Date.now() }); localStorage.setItem('omnifree-history', JSON.stringify(history.slice(0, 20))); atualizarHistorico();
}
btnLimparHistorico.addEventListener('click', () => { localStorage.removeItem('omnifree-history'); atualizarHistorico(); });
atualizarHistorico();
const savedTheme = localStorage.getItem('omnifree-theme');
if (savedTheme === 'light') document.body.classList.add('light');
btnTheme.textContent = document.body.classList.contains('light') ? '◐' : '☼';
btnTheme.addEventListener('click', () => { document.body.classList.toggle('light'); const theme = document.body.classList.contains('light') ? 'light' : 'dark'; localStorage.setItem('omnifree-theme', theme); btnTheme.textContent = theme === 'light' ? '◐' : '☼'; });

function atualizarQualidade() { qualityValue.textContent = `${quality.value}%`; }
quality.addEventListener('input', atualizarQualidade);
preset.addEventListener('change', () => {
  const values = { balanced: 80, small: 55, quality: 95, whatsapp: 65 };
  quality.value = values[preset.value]; atualizarQualidade();
});
btnDestino.addEventListener('click', async () => {
  const pasta = await window.conversorAPI.escolherPastaDestino();
  if (pasta) { pastaDestino = pasta; btnDestino.textContent = 'Pasta escolhida'; }
});
async function atualizarComponentes() {
  const components = await window.conversorAPI.obterComponentes();
  const active = Object.values(components).filter(Boolean).length;
  componentsStatus.textContent = `${active}/${Object.keys(components).length} componentes prontos`;
}
btnComponentes.addEventListener('click', atualizarComponentes);
atualizarComponentes();
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
    const password = window.prompt('Defina uma senha para abrir o PDF:'); if (!password) return;
    ultimoArquivoConvertido = await window.conversorAPI.protegerPdf(pdfSelecionado(), pastaDestino, password); btnAbrir.hidden = false; mostrarResultado('success', 'PDF protegido com senha.');
  } catch (error) { mostrarResultado('error', error.message); }
});
btnOtimizarPdf.addEventListener('click', async () => {
  try { ultimoArquivoConvertido = await window.conversorAPI.otimizarPdf(pdfSelecionado(), pastaDestino); btnAbrir.hidden = false; mostrarResultado('success', 'PDF otimizado.'); } catch (error) { mostrarResultado('error', error.message); }
});
btnExtrairImagens.addEventListener('click', async () => {
  try { const folder = await window.conversorAPI.extrairImagensPdf(pdfSelecionado(), pastaDestino); ultimoArquivoConvertido = folder; btnAbrir.hidden = false; mostrarResultado('success', 'Imagens extraídas para uma nova pasta.'); } catch (error) { mostrarResultado('error', error.message); }
});

function formatarTamanho(bytes) {
  if (!bytes) return 'Arquivo vazio';
  const units = ['B', 'KB', 'MB', 'GB']; let index = 0; let value = bytes;
  while (value >= 1024 && index < units.length - 1) { value /= 1024; index += 1; }
  return `${value.toFixed(index ? 1 : 0)} ${units[index]}`;
}

function mostrarResultado(type, message) {
  resultPanel.hidden = false;
  resultPanel.className = `result-panel ${type}`;
  resultIcon.textContent = type === 'success' ? '✓' : '!' ;
  resultMessage.textContent = message;
}

function preencherFormatos(opcoes) {
  formatoSaida.innerHTML = '';
  if (!opcoes.supported) {
    formatoSaida.add(new Option('Formato não reconhecido', ''));
    formatoSaida.disabled = true; btnConverter.disabled = true;
    formatHelp.textContent = `Ainda não há conversor para .${opcoes.extension || 'este formato'}.`;
    return;
  }
  opcoes.formats.forEach((formato) => formatoSaida.add(new Option(formato.toUpperCase(), formato)));
  formatoSaida.disabled = false; btnConverter.disabled = false;
  formatHelp.textContent = `${opcoes.category}: ${opcoes.formats.length} formatos de saída disponíveis.`;
}

async function escolherArquivo(file, files = [file]) {
  if (!file) return;
  arquivosSelecionados = [...files]; arquivoSelecionado = file;
  resultPanel.hidden = true; btnAbrir.hidden = true; progressContainer.hidden = true;
  fileName.textContent = file.name;
  fileMeta.textContent = `${formatarTamanho(file.size)}${arquivosSelecionados.length > 1 ? ` · +${arquivosSelecionados.length - 1} na fila` : ''}`;
  fileDetails.hidden = false;
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
dropZone.addEventListener('drop', (event) => { event.preventDefault(); dropZone.classList.remove('dragover'); escolherArquivo(event.dataTransfer.files[0], event.dataTransfer.files); });

btnConverter.addEventListener('click', async () => {
  if (!arquivoSelecionado || !formatoSaida.value) return;
  btnConverter.disabled = true; progressContainer.hidden = false; resultPanel.hidden = true; btnAbrir.hidden = true;
  let completed = 0;
  for (const file of arquivosSelecionados) {
    const options = await window.conversorAPI.obterOpcoes(file.path);
    if (!options.formats.includes(formatoSaida.value)) continue;
    progressBar.style.width = '0%'; progressText.textContent = '0%'; progressState.textContent = `Arquivo ${completed + 1} de ${arquivosSelecionados.length}: ${file.name}`;
    const result = await new Promise((resolve) => { queueResolve = resolve; window.conversorAPI.enviarArquivo(file.path, formatoSaida.value, { outputDir: pastaDestino, outputName: arquivosSelecionados.length === 1 ? outputName.value : '', quality: quality.value, width: width.value, height: height.value, startTime: startTime.value, duration: duration.value }); });
    if (result.status === 'concluido') { completed += 1; registrarHistorico(file.name); }
  }
  btnConverter.disabled = false; mostrarResultado(completed ? 'success' : 'error', completed ? `${completed} arquivo(s) convertido(s) na fila.` : 'Nenhum arquivo da fila aceita esse formato.');
});

window.conversorAPI.receberProgresso((percentual) => { progressBar.style.width = `${percentual}%`; progressText.textContent = `${percentual}%`; });
window.conversorAPI.receberStatus((dados) => {
  if (dados.status === 'processando') return;
  if (queueResolve) { const resolve = queueResolve; queueResolve = null; if (dados.status === 'concluido') ultimoArquivoConvertido = dados.caminhoArquivo; resolve(dados); return; }
  btnConverter.disabled = !arquivoSelecionado;
  if (dados.status === 'concluido') {
    progressBar.style.width = '100%'; progressText.textContent = '100%'; progressState.textContent = 'Conversão concluída';
    ultimoArquivoConvertido = dados.caminhoArquivo; mostrarResultado('success', dados.mensagem); btnAbrir.hidden = false;
  } else if (dados.status === 'erro') { progressContainer.hidden = true; mostrarResultado('error', dados.mensagem); }
});
btnAbrir.addEventListener('click', () => { if (ultimoArquivoConvertido) window.conversorAPI.abrirNoExplorador(ultimoArquivoConvertido); });
