const { app, BrowserWindow, ipcMain, shell, dialog, clipboard } = require('electron');
const path = require('path');
const os = require('os');
const fs = require('fs/promises');
const { spawn, execFile } = require('child_process');
const ffmpeg = require('fluent-ffmpeg');
const ffmpegStatic = require('ffmpeg-static');
const sharp = require('sharp');
const { autoUpdater } = require('electron-updater');
const { PDFDocument, degrees } = require('pdf-lib');
const { findGroup, optionsFor, extensionOf } = require('./conversion-catalog');
const { convertData } = require('./data-converter');
const { uniqueOutputPath } = require('./output-paths');
const { MultiFormatReader, BinaryBitmap, HybridBinarizer, RGBLuminanceSource } = require('@zxing/library');

ffmpeg.setFfmpegPath(ffmpegStatic);
const activeConversions = new Map();
const ruleWatchers = new Map();
let mainWindow;
const preferencesFile = () => path.join(app.getPath('userData'), 'omnifree-preferences.json');
async function readPreferences() { try { return JSON.parse(await fs.readFile(preferencesFile(), 'utf8')); } catch { return { autoUpdates: true, rules: [] }; } }
async function writePreferences(preferences) { await fs.writeFile(preferencesFile(), JSON.stringify(preferences), 'utf8'); }

function createWindow() {
  const win = new BrowserWindow({ width: 1200, height: 800, minWidth: 900, minHeight: 600, backgroundColor: '#0a0e1a', webPreferences: { preload: path.join(__dirname, '../preload/preload.js'), nodeIntegration: false, contextIsolation: true } });
  win.setMenu(null);
  win.loadFile(path.join(__dirname, '../renderer/index.html')); mainWindow = win;
}

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { windowsHide: true }); let error = '';
    child.stderr.on('data', (chunk) => { error += chunk; });
    child.on('error', () => reject(new Error(`O programa necessário “${command}” não foi encontrado.`)));
    child.on('close', (code) => code === 0 ? resolve() : reject(new Error(error || `${command} terminou com erro.`)));
  });
}

async function convertWithLibreOffice(input, output, target) {
  const tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-'));
  try {
    await run('soffice', ['--headless', '--convert-to', target, '--outdir', tempDir, input]);
    await fs.copyFile(path.join(tempDir, `${path.basename(input, path.extname(input))}.${target}`), output);
  } finally { await fs.rm(tempDir, { recursive: true, force: true }); }
}

async function convertWith7zip(input, output) {
  const tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-'));
  try {
    await run('7z', ['x', input, `-o${tempDir}`, '-y']);
    await run('7z', ['a', `-t${extensionOf(output)}`, output, path.join(tempDir, '*')]);
  } finally { await fs.rm(tempDir, { recursive: true, force: true }); }
}

function send(event, payload) { event.sender.send('status-conversao', payload); }

function commandAvailable(command) {
  return new Promise((resolve) => execFile('where.exe', [command], { windowsHide: true }, (error) => resolve(!error)));
}
function runOutput(command, args) {
  return new Promise((resolve, reject) => { const child = spawn(command, args, { windowsHide: true }); let output = ''; let error = ''; child.stdout.on('data', (chunk) => { output += chunk; }); child.stderr.on('data', (chunk) => { error += chunk; }); child.on('error', () => reject(new Error(`O programa necessário “${command}” não foi encontrado.`))); child.on('close', (code) => code === 0 ? resolve(output) : reject(new Error(error || `${command} terminou com erro.`))); });
}
async function decodeBarcode(imagePath) {
  const { data, info } = await sharp(imagePath).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const source = new RGBLuminanceSource(new Uint8ClampedArray(data), info.width, info.height);
  return new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source))).getText();
}

ipcMain.handle('opcoes-conversao', (_event, filePath) => optionsFor(filePath));
ipcMain.handle('escolher-pasta-destino', async () => {
  const result = await dialog.showOpenDialog({ properties: ['openDirectory', 'createDirectory'] });
  return result.canceled ? null : result.filePaths[0];
});
ipcMain.handle('escolher-imagem-marca-dagua', async () => {
  const result = await dialog.showOpenDialog({ properties: ['openFile'], filters: [{ name: 'Imagem', extensions: ['png', 'jpg', 'jpeg', 'webp', 'avif'] }] });
  return result.canceled ? null : result.filePaths[0];
});
ipcMain.handle('componentes-disponiveis', async () => ({
  LibreOffice: await commandAvailable('soffice'),
  Pandoc: await commandAvailable('pandoc'),
  '7-Zip': await commandAvailable('7z'),
  Calibre: await commandAvailable('ebook-convert'),
  QPDF: await commandAvailable('qpdf'),
  Poppler: await commandAvailable('pdfimages'),
  Ghostscript: await commandAvailable('gswin64c'),
  Tesseract: await commandAvailable('tesseract'),
  FFmpeg: Boolean(ffmpegStatic),
  Sharp: true
}));
ipcMain.handle('detalhes-arquivo', async (_event, filePath) => {
  const stat = await fs.stat(filePath); const details = { bytes: stat.size };
  const extension = extensionOf(filePath);
  if (['png', 'jpg', 'jpeg', 'webp', 'avif', 'tif', 'tiff', 'bmp'].includes(extension)) { const meta = await sharp(filePath).metadata(); details.width = meta.width; details.height = meta.height; }
  if (extension === 'pdf') { const pdf = await PDFDocument.load(await fs.readFile(filePath)); details.pages = pdf.getPageCount(); }
  return details;
});
ipcMain.handle('ler-codigo', async (_event, input) => {
  const extension = extensionOf(input); let tempDir = '';
  try {
    if (extension !== 'pdf') return { text: await decodeBarcode(input) };
    tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-code-')); await run('pdfimages', ['-png', input, path.join(tempDir, 'page')]);
    for (const name of await fs.readdir(tempDir)) { try { return { text: await decodeBarcode(path.join(tempDir, name)) }; } catch {} }
    throw new Error('Nenhum QR Code ou código de barras foi encontrado.');
  } finally { if (tempDir) await fs.rm(tempDir, { recursive: true, force: true }); }
});
ipcMain.handle('ocr-arquivo', async (_event, input, outputDir, language = 'por+eng') => {
  if (!await commandAvailable('tesseract')) throw new Error('Instale o Tesseract OCR para usar esta ferramenta.');
  const targetDir = await pdfTargetDir(outputDir); const extension = extensionOf(input); let images = [input]; let tempDir = '';
  try {
    if (extension === 'pdf') { tempDir = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-ocr-')); await run('pdftoppm', ['-png', '-r', '200', input, path.join(tempDir, 'pagina')]); images = (await fs.readdir(tempDir)).filter((name) => name.endsWith('.png')).map((name) => path.join(tempDir, name)); }
    const text = (await Promise.all(images.map((image) => runOutput('tesseract', [image, 'stdout', '-l', language])))).join('\n\n');
    const output = await uniqueOutputPath(targetDir, 'OmniFree_OCR', 'txt'); await fs.writeFile(output, text, 'utf8'); return { output, text };
  } finally { if (tempDir) await fs.rm(tempDir, { recursive: true, force: true }); }
});
function parsePages(value, count) {
  const pages = new Set();
  for (const part of String(value || '').split(',')) {
    const match = part.trim().match(/^(\d+)(?:\s*-\s*(\d+))?$/); if (!match) continue;
    const first = Number(match[1]); const last = Number(match[2] || first);
    for (let page = Math.max(1, first); page <= Math.min(count, last); page += 1) pages.add(page - 1);
  }
  return [...pages].sort((a, b) => a - b);
}
ipcMain.handle('verificar-atualizacoes', async () => {
  if (!app.isPackaged) return { status: 'development', message: 'A verificação de atualizações funciona na versão instalada do OmniFree.' };
  try {
    const result = await autoUpdater.checkForUpdates();
    const latest = result?.updateInfo?.version;
    if (latest && latest !== app.getVersion()) return { status: 'available', message: `A versão ${latest} está disponível e será baixada automaticamente.` };
    return { status: 'latest', message: `Você já está usando a versão mais recente (${app.getVersion()}).` };
  } catch (error) {
    return { status: 'error', message: `Não foi possível verificar atualizações: ${error.message}` };
  }
});
ipcMain.handle('obter-preferencias', readPreferences);
ipcMain.handle('salvar-preferencias', async (_event, preferences) => { await writePreferences({ autoUpdates: preferences?.autoUpdates !== false }); return readPreferences(); });
function configureRules(rules = []) {
  ruleWatchers.forEach((watcher) => watcher.close()); ruleWatchers.clear();
  rules.forEach((rule) => { try { const watcher = require('fs').watch(rule.folder, { recursive: true }, (_event, filename) => { if (!filename) return; const input = path.join(rule.folder, filename); if (path.resolve(input).startsWith(path.resolve(rule.outputDir))) return; setTimeout(() => mainWindow?.webContents.send('arquivo-regra', { input, target: rule.target, outputDir: rule.outputDir }), 500); }); ruleWatchers.set(rule.folder, watcher); } catch (error) { console.warn('Não foi possível monitorar regra:', error.message); } });
}
ipcMain.handle('obter-regras', async () => (await readPreferences()).rules || []);
ipcMain.handle('salvar-regras', async (_event, rules) => { const preferences = await readPreferences(); preferences.rules = Array.isArray(rules) ? rules : []; await writePreferences(preferences); configureRules(preferences.rules); return preferences.rules; });
ipcMain.handle('mesclar-pdfs', async () => {
  const selected = await dialog.showOpenDialog({ properties: ['openFile', 'multiSelections'], filters: [{ name: 'PDF', extensions: ['pdf'] }] });
  if (selected.canceled || selected.filePaths.length < 2) return { canceled: true };
  const merged = await PDFDocument.create();
  for (const filePath of selected.filePaths) {
    const source = await PDFDocument.load(await fs.readFile(filePath));
    const pages = await merged.copyPages(source, source.getPageIndices()); pages.forEach((page) => merged.addPage(page));
  }
  const output = path.join(os.homedir(), 'Desktop', `OmniFree_unido_${Date.now()}.pdf`);
  await fs.writeFile(output, await merged.save());
  return { output };
});
ipcMain.handle('separar-pdf', async (_event, input, outputDir) => {
  if (extensionOf(input) !== 'pdf') throw new Error('Escolha um arquivo PDF para separar.');
  const targetDir = outputDir && await fs.stat(outputDir).then((stat) => stat.isDirectory()).catch(() => false) ? outputDir : path.join(os.homedir(), 'Desktop');
  const source = await PDFDocument.load(await fs.readFile(input)); const outputs = [];
  for (const [index, page] of source.getPages().entries()) {
    const result = await PDFDocument.create(); result.addPage(await result.copyPages(source, [index]).then((pages) => pages[0]));
    const output = path.join(targetDir, `OmniFree_pagina_${index + 1}_${Date.now()}.pdf`); await fs.writeFile(output, await result.save()); outputs.push(output);
  }
  return outputs;
});
ipcMain.handle('extrair-paginas-pdf', async (_event, input, outputDir, pagesText) => {
  if (extensionOf(input) !== 'pdf') throw new Error('Selecione um PDF primeiro.');
  const targetDir = await pdfTargetDir(outputDir); const source = await PDFDocument.load(await fs.readFile(input)); const indexes = parsePages(pagesText, source.getPageCount());
  if (!indexes.length) throw new Error('Informe páginas válidas, por exemplo: 1, 3-5.');
  const document = await PDFDocument.create(); (await document.copyPages(source, indexes)).forEach((page) => document.addPage(page));
  const output = await uniqueOutputPath(targetDir, 'OmniFree_paginas', 'pdf'); await fs.writeFile(output, await document.save()); return output;
});
ipcMain.handle('girar-pdf', async (_event, input, outputDir) => {
  if (extensionOf(input) !== 'pdf') throw new Error('Selecione um PDF primeiro.');
  const targetDir = outputDir && await fs.stat(outputDir).then((stat) => stat.isDirectory()).catch(() => false) ? outputDir : path.join(os.homedir(), 'Desktop');
  const document = await PDFDocument.load(await fs.readFile(input));
  document.getPages().forEach((page) => page.setRotation(degrees((page.getRotation().angle + 90) % 360)));
  const output = path.join(targetDir, `OmniFree_girado_${Date.now()}.pdf`); await fs.writeFile(output, await document.save({ useObjectStreams: true }));
  return output;
});
function pdfTargetDir(outputDir) { return outputDir && fs.stat(outputDir).then((stat) => stat.isDirectory()).catch(() => false) ? outputDir : path.join(os.homedir(), 'Desktop'); }
ipcMain.handle('proteger-pdf', async (_event, input, outputDir, password) => {
  if (extensionOf(input) !== 'pdf' || !password) throw new Error('Selecione um PDF e informe uma senha.');
  const targetDir = await pdfTargetDir(outputDir); const output = path.join(targetDir, `OmniFree_protegido_${Date.now()}.pdf`);
  await run('qpdf', ['--encrypt', password, password, '256', '--', input, output]); return output;
});
ipcMain.handle('otimizar-pdf', async (_event, input, outputDir, mode = 'balanced') => {
  if (extensionOf(input) !== 'pdf') throw new Error('Selecione um PDF primeiro.');
  const targetDir = await pdfTargetDir(outputDir); const output = path.join(targetDir, `OmniFree_otimizado_${Date.now()}.pdf`);
  const ghostscript = await commandAvailable('gswin64c');
  if (ghostscript) { const preset = mode === 'small' ? '/screen' : mode === 'quality' ? '/printer' : '/ebook'; await run('gswin64c', ['-sDEVICE=pdfwrite', '-dCompatibilityLevel=1.4', `-dPDFSETTINGS=${preset}`, '-dNOPAUSE', '-dQUIET', '-dBATCH', `-sOutputFile=${output}`, input]); }
  else await run('qpdf', ['--stream-data=compress', '--object-streams=generate', input, output]); return output;
});
ipcMain.handle('pagina-web-para-pdf', async (_event, url, outputDir) => {
  let parsed; try { parsed = new URL(url); } catch { throw new Error('Informe uma URL válida.'); }
  if (!['http:', 'https:'].includes(parsed.protocol)) throw new Error('Use apenas páginas HTTP ou HTTPS.');
  const targetDir = await pdfTargetDir(outputDir); const output = await uniqueOutputPath(targetDir, 'OmniFree_pagina_web', 'pdf');
  const page = new BrowserWindow({ show: false, webPreferences: { sandbox: true } });
  try { await page.loadURL(parsed.toString()); await fs.writeFile(output, await page.webContents.printToPDF({ printBackground: true, pageSize: 'A4' })); return output; } finally { if (!page.isDestroyed()) page.destroy(); }
});
ipcMain.handle('extrair-legendas', async (_event, input, outputDir, format = 'srt') => {
  const targetDir = await pdfTargetDir(outputDir); const output = await uniqueOutputPath(targetDir, 'OmniFree_legendas', format);
  await new Promise((resolve, reject) => ffmpeg(input).outputOptions(['-map', '0:s:0']).toFormat(format).on('end', resolve).on('error', reject).save(output)); return output;
});
ipcMain.handle('gerar-thumbnail', async (_event, input, outputDir, time = '00:00:01') => {
  const targetDir = await pdfTargetDir(outputDir); const output = await uniqueOutputPath(targetDir, 'OmniFree_thumbnail', 'jpg');
  await new Promise((resolve, reject) => ffmpeg(input).seekInput(time).frames(1).outputOptions(['-q:v', '2']).on('end', resolve).on('error', reject).save(output)); return output;
});
ipcMain.handle('extrair-imagens-pdf', async (_event, input, outputDir) => {
  if (extensionOf(input) !== 'pdf') throw new Error('Selecione um PDF primeiro.');
  const targetDir = await pdfTargetDir(outputDir); const folder = path.join(targetDir, `OmniFree_imagens_${Date.now()}`); await fs.mkdir(folder);
  await run('pdfimages', ['-all', input, path.join(folder, 'imagem')]); return folder;
});
ipcMain.on('processar-arquivo', async (event, input, target, settings = {}, jobId = '') => {
  const job = { cancelled: false, cancel: null };
  if (jobId) activeConversions.set(jobId, job);
  let output = '';
  const group = findGroup(input, target); const source = extensionOf(input);
  if (!group || !group.outputs.includes(target)) return send(event, { status: 'erro', mensagem: 'Esta combinação de arquivo e formato não é compatível.' });
  try {
    await fs.access(input);
    const outputDir = settings.outputDir && await fs.stat(settings.outputDir).then((stat) => stat.isDirectory()).catch(() => false) ? settings.outputDir : path.join(os.homedir(), 'Desktop');
    const requestedName = String(settings.outputName || '').replace(/[<>:"/\\|?*\x00-\x1F]/g, '').trim();
    output = await uniqueOutputPath(outputDir, requestedName || `OmniFree_${Date.now()}`, target);
    send(event, { status: 'processando', mensagem: `Convertendo para ${target.toUpperCase()}...` });
    if (job.cancelled) throw new Error('CONVERSAO_CANCELADA');
    if (group.engine === 'sharp') {
      let image = sharp(input, { sequentialRead: true });
      if (settings.width || settings.height) image = image.resize({ width: Number(settings.width) || undefined, height: Number(settings.height) || undefined, fit: settings.cropImage ? 'cover' : 'inside', position: settings.cropPosition || 'centre', withoutEnlargement: !settings.cropImage });
      const overlays = [];
      if (settings.watermark) overlays.push({ input: Buffer.from(`<svg width="800" height="80"><text x="20" y="55" font-size="42" fill="white" fill-opacity="0.7">${String(settings.watermark).replace(/[<&>]/g, '')}</text></svg>`), gravity: 'southeast' });
      if (settings.watermarkImage) { await fs.access(settings.watermarkImage); overlays.push({ input: settings.watermarkImage, gravity: 'southeast', opacity: 0.72 }); }
      if (overlays.length) image = image.composite(overlays);
      if (!settings.removeMetadata) image = image.withMetadata();
      await image.toFormat(target === 'jpg' ? 'jpeg' : target, settings.quality ? { quality: Number(settings.quality) } : {}).toFile(output);
    }
    else if (group.engine === 'data') await convertData(input, output, source, target);
    else if (group.engine === 'ffmpeg') await new Promise((resolve, reject) => {
      const job = ffmpeg(input).toFormat(target);
      activeConversions.get(jobId).cancel = () => job.kill('SIGKILL');
      if (settings.startTime) job.setStartTime(settings.startTime);
      if (settings.duration) job.setDuration(settings.duration);
      if (settings.audioOnly) job.noVideo();
      if (settings.noAudio) job.noAudio();
      if (settings.removeMetadata) job.outputOptions(['-map_metadata', '-1']);
      if (settings.quality) job.outputOptions(['-crf', String(Math.max(0, Math.min(51, 51 - Number(settings.quality) / 2)))]);
      job.on('progress', (p) => p.percent && event.sender.send('progresso-conversao', Math.round(p.percent))).on('end', resolve).on('error', (error) => activeConversions.get(jobId)?.cancelled ? reject(new Error('CONVERSAO_CANCELADA')) : reject(error)).save(output);
    });
    else if (group.engine === 'libreoffice') await convertWithLibreOffice(input, output, target);
    else if (group.engine === '7zip') await convertWith7zip(input, output);
    else if (group.engine === 'calibre') await run('ebook-convert', [input, output]);
    if (activeConversions.get(jobId)?.cancelled) throw new Error('CONVERSAO_CANCELADA');
    send(event, { status: 'concluido', mensagem: 'Sucesso! Arquivo convertido.', caminhoArquivo: output, jobId });
  } catch (error) {
    console.error(error);
    if (error.message === 'CONVERSAO_CANCELADA') { await fs.rm(output || '', { force: true }).catch(() => {}); send(event, { status: 'cancelado', mensagem: 'Conversão cancelada.', jobId }); return; }
    const missing = error.message.includes('não foi encontrado');
    send(event, { status: 'erro', mensagem: missing ? `${error.message} Instale o componente correspondente e tente novamente.` : `Não foi possível converter este arquivo: ${error.message}`, jobId });
  } finally {
    if (jobId) activeConversions.delete(jobId);
  }
});
ipcMain.on('cancelar-conversao', (_event, jobId) => { const job = activeConversions.get(jobId); if (job) { job.cancelled = true; job.cancel?.(); } });

ipcMain.on('abrir-no-explorador', (_event, filePath) => { if (filePath) shell.showItemInFolder(filePath); });
ipcMain.on('copiar-caminho', (_event, filePath) => { if (filePath) clipboard.writeText(filePath); });
ipcMain.on('arrastar-arquivo', (event, filePath) => { if (filePath) event.sender.startDrag({ file: filePath, icon: path.join(__dirname, '../../build/omnifree-icon.png') }); });
app.whenReady().then(async () => {
  createWindow();
  const preferences = await readPreferences();
  configureRules(preferences.rules || []);
  if (app.isPackaged && preferences.autoUpdates !== false) autoUpdater.checkForUpdatesAndNotify().catch((error) => console.warn('Não foi possível verificar atualizações:', error.message));
  app.on('activate', () => { if (!BrowserWindow.getAllWindows().length) createWindow(); });
});
app.on('window-all-closed', () => { if (process.platform !== 'darwin') app.quit(); });
