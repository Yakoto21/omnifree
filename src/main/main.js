const { app, BrowserWindow, ipcMain, shell, dialog } = require('electron');
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

ffmpeg.setFfmpegPath(ffmpegStatic);

function createWindow() {
  const win = new BrowserWindow({ width: 1200, height: 800, minWidth: 900, minHeight: 600, backgroundColor: '#0a0e1a', webPreferences: { preload: path.join(__dirname, '../preload/preload.js'), nodeIntegration: false, contextIsolation: true } });
  win.setMenu(null);
  win.loadFile(path.join(__dirname, '../renderer/index.html'));
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
  FFmpeg: Boolean(ffmpegStatic),
  Sharp: true
}));
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
ipcMain.handle('otimizar-pdf', async (_event, input, outputDir) => {
  if (extensionOf(input) !== 'pdf') throw new Error('Selecione um PDF primeiro.');
  const targetDir = await pdfTargetDir(outputDir); const output = path.join(targetDir, `OmniFree_otimizado_${Date.now()}.pdf`);
  await run('qpdf', ['--stream-data=compress', '--object-streams=generate', input, output]); return output;
});
ipcMain.handle('extrair-imagens-pdf', async (_event, input, outputDir) => {
  if (extensionOf(input) !== 'pdf') throw new Error('Selecione um PDF primeiro.');
  const targetDir = await pdfTargetDir(outputDir); const folder = path.join(targetDir, `OmniFree_imagens_${Date.now()}`); await fs.mkdir(folder);
  await run('pdfimages', ['-all', input, path.join(folder, 'imagem')]); return folder;
});
ipcMain.on('processar-arquivo', async (event, input, target, settings = {}) => {
  const group = findGroup(input, target); const source = extensionOf(input);
  if (!group || !group.outputs.includes(target)) return send(event, { status: 'erro', mensagem: 'Esta combinação de arquivo e formato não é compatível.' });
  try {
    await fs.access(input);
    const outputDir = settings.outputDir && await fs.stat(settings.outputDir).then((stat) => stat.isDirectory()).catch(() => false) ? settings.outputDir : path.join(os.homedir(), 'Desktop');
    const requestedName = String(settings.outputName || '').replace(/[<>:"/\\|?*\x00-\x1F]/g, '').trim();
    const output = path.join(outputDir, `${requestedName ? requestedName.replace(/\.[^.]+$/, '') : `OmniFree_${Date.now()}`}.${target}`);
    send(event, { status: 'processando', mensagem: `Convertendo para ${target.toUpperCase()}...` });
    if (group.engine === 'sharp') {
      let image = sharp(input, { sequentialRead: true });
      if (settings.width || settings.height) image = image.resize({ width: Number(settings.width) || undefined, height: Number(settings.height) || undefined, fit: settings.cropImage ? 'cover' : 'inside', position: 'centre', withoutEnlargement: !settings.cropImage });
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
      if (settings.startTime) job.setStartTime(settings.startTime);
      if (settings.duration) job.setDuration(settings.duration);
      if (settings.audioOnly) job.noVideo();
      if (settings.noAudio) job.noAudio();
      if (settings.removeMetadata) job.outputOptions(['-map_metadata', '-1']);
      if (settings.quality) job.outputOptions(['-crf', String(Math.max(0, Math.min(51, 51 - Number(settings.quality) / 2)))]);
      job.on('progress', (p) => p.percent && event.sender.send('progresso-conversao', Math.round(p.percent))).on('end', resolve).on('error', reject).save(output);
    });
    else if (group.engine === 'libreoffice') await convertWithLibreOffice(input, output, target);
    else if (group.engine === '7zip') await convertWith7zip(input, output);
    else if (group.engine === 'calibre') await run('ebook-convert', [input, output]);
    send(event, { status: 'concluido', mensagem: 'Sucesso! Arquivo convertido.', caminhoArquivo: output });
  } catch (error) {
    console.error(error);
    const missing = error.message.includes('não foi encontrado');
    send(event, { status: 'erro', mensagem: missing ? `${error.message} Instale o componente correspondente e tente novamente.` : `Não foi possível converter este arquivo: ${error.message}` });
  }
});

ipcMain.on('abrir-no-explorador', (_event, filePath) => { if (filePath) shell.showItemInFolder(filePath); });
app.whenReady().then(() => {
  createWindow();
  if (app.isPackaged) autoUpdater.checkForUpdatesAndNotify().catch((error) => console.warn('Não foi possível verificar atualizações:', error.message));
  app.on('activate', () => { if (!BrowserWindow.getAllWindows().length) createWindow(); });
});
app.on('window-all-closed', () => { if (process.platform !== 'darwin') app.quit(); });
