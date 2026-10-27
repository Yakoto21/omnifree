const path = require('path');
const os = require('os');
const fs = require('fs/promises');
const { RGBLuminanceSource, BinaryBitmap, HybridBinarizer, MultiFormatReader } = require('@zxing/library');
const { uniqueOutputPath } = require('./output-paths');
const { extensionOf } = require('./conversion-catalog');

function createRecognitionService({ sharp, run, runOutput, commandAvailable, outputDir }) {
  async function decode(imagePath) { const { data, info } = await sharp(imagePath).removeAlpha().raw().toBuffer({ resolveWithObject: true }); return new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(new Uint8ClampedArray(data), info.width, info.height)))).getText(); }
  return {
    async readCode(input) { const extension = extensionOf(input); let temp = ''; try { if (extension !== 'pdf') return { text: await decode(input) }; temp = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-code-')); await run('pdfimages', ['-png', input, path.join(temp, 'page')]); for (const name of await fs.readdir(temp)) { try { return { text: await decode(path.join(temp, name)) }; } catch {} } throw new Error('Nenhum QR Code ou código de barras foi encontrado.'); } finally { if (temp) await fs.rm(temp, { recursive: true, force: true }); } },
    async ocr(input, folder, language = 'por+eng') { if (!await commandAvailable('tesseract')) throw new Error('Instale o Tesseract OCR para usar esta ferramenta.'); const extension = extensionOf(input); let images = [input]; let temp = ''; try { if (extension === 'pdf') { temp = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-ocr-')); await run('pdftoppm', ['-png', '-r', '200', input, path.join(temp, 'pagina')]); images = (await fs.readdir(temp)).filter((name) => name.endsWith('.png')).map((name) => path.join(temp, name)); } const text = (await Promise.all(images.map((image) => runOutput('tesseract', [image, 'stdout', '-l', language])))).join('\n\n'); const output = await uniqueOutputPath(await outputDir(folder), 'OmniFree_OCR', 'txt'); await fs.writeFile(output, text, 'utf8'); return { output, text }; } finally { if (temp) await fs.rm(temp, { recursive: true, force: true }); } }
  };
}
module.exports = { createRecognitionService };
