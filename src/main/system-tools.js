const path = require('path');
const fs = require('fs');
const { spawn, execFile } = require('child_process');

function configureOptionalToolPath() {
  const ghostscriptFolders = (() => { try { return fs.readdirSync('C:\\Program Files\\gs', { withFileTypes: true }).filter((entry) => entry.isDirectory()).map((entry) => path.join('C:\\Program Files\\gs', entry.name, 'bin')); } catch { return []; } })();
  const folders = ['C:\\Program Files\\7-Zip', 'C:\\Program Files\\Calibre2', 'C:\\Program Files\\LibreOffice\\program', 'C:\\Program Files\\Tesseract-OCR', ...ghostscriptFolders];
  process.env.PATH = `${folders.join(path.delimiter)}${path.delimiter}${process.env.PATH}`;
}
function run(command, args) { return new Promise((resolve, reject) => { const child = spawn(command, args, { windowsHide: true }); let error = ''; child.stderr.on('data', (chunk) => { error += chunk; }); child.on('error', () => reject(new Error(`O programa necessário “${command}” não foi encontrado.`))); child.on('close', (code) => code === 0 ? resolve() : reject(new Error(error || `${command} terminou com erro.`))); }); }
function runOutput(command, args) { return new Promise((resolve, reject) => { const child = spawn(command, args, { windowsHide: true }); let output = ''; let error = ''; child.stdout.on('data', (chunk) => { output += chunk; }); child.stderr.on('data', (chunk) => { error += chunk; }); child.on('error', () => reject(new Error(`O programa necessário “${command}” não foi encontrado.`))); child.on('close', (code) => code === 0 ? resolve(output) : reject(new Error(error || `${command} terminou com erro.`))); }); }
function commandAvailable(command) { return new Promise((resolve) => execFile('where.exe', [command], { windowsHide: true }, (error) => resolve(!error))); }
async function componentStatus(ffmpegAvailable) { return { LibreOffice: await commandAvailable('soffice'), Pandoc: await commandAvailable('pandoc'), '7-Zip': await commandAvailable('7z'), Calibre: await commandAvailable('ebook-convert'), QPDF: await commandAvailable('qpdf'), Poppler: await commandAvailable('pdfimages'), Ghostscript: await commandAvailable('gswin64c'), Tesseract: await commandAvailable('tesseract'), FFmpeg: Boolean(ffmpegAvailable), Sharp: true }; }
module.exports = { configureOptionalToolPath, run, runOutput, commandAvailable, componentStatus };
