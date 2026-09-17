const test = require('node:test');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const sharp = require('sharp');
const ffmpegPath = require('ffmpeg-static');
const { PDFDocument } = require('pdf-lib');

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { windowsHide: true });
    let stderr = '';
    child.stderr.on('data', (data) => { stderr += data; });
    child.on('error', reject);
    child.on('close', (code) => code === 0 ? resolve() : reject(new Error(stderr || `${command} exited with ${code}`)));
  });
}

test('Sharp converte uma imagem de verdade para PNG', async () => {
  const output = await sharp(Buffer.from('<svg width="12" height="12"><rect width="12" height="12" fill="#4477cc"/></svg>')).png().toBuffer();
  assert.equal(output.subarray(1, 4).toString(), 'PNG');
});

test('FFmpeg gera um arquivo de áudio válido', async () => {
  const folder = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-engine-'));
  const output = path.join(folder, 'tone.wav');
  try {
    await run(ffmpegPath, ['-y', '-f', 'lavfi', '-i', 'sine=frequency=440:duration=0.1', output]);
    const stat = await fs.stat(output);
    assert.ok(stat.size > 44);
  } finally { await fs.rm(folder, { recursive: true, force: true }); }
});

test('PDF-lib cria e relê um PDF para as ferramentas de PDF', async () => {
  const document = await PDFDocument.create();
  document.addPage([100, 100]);
  const saved = await document.save();
  const reloaded = await PDFDocument.load(saved);
  assert.equal(reloaded.getPageCount(), 1);
});
