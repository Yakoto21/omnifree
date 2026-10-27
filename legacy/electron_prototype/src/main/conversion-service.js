const path = require('path');
const os = require('os');
const fs = require('fs/promises');
const { findGroup, extensionOf } = require('./conversion-catalog');
const { convertData } = require('./data-converter');
const { uniqueOutputPath } = require('./output-paths');
const { conversionErrorMessage } = require('./error-messages');

function createConversionService({ sharp, ffmpeg, run }) {
  const jobs = new Map();
  const defaultOutput = () => path.join(os.homedir(), 'Desktop');
  async function office(input, output, target) { const folder = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-')); try { await run('soffice', ['--headless', '--convert-to', target, '--outdir', folder, input]); await fs.copyFile(path.join(folder, `${path.basename(input, path.extname(input))}.${target}`), output); } finally { await fs.rm(folder, { recursive: true, force: true }); } }
  async function archive(input, output) { const folder = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-')); try { await run('7z', ['x', input, `-o${folder}`, '-y']); await run('7z', ['a', `-t${extensionOf(output)}`, output, path.join(folder, '*')]); } finally { await fs.rm(folder, { recursive: true, force: true }); } }
  return {
    async process(event, input, target, settings = {}, jobId = '') {
      const state = { cancelled: false, cancel: null }; if (jobId) jobs.set(jobId, state); let output = ''; const send = (payload) => event.sender.send('status-conversao', payload);
      const group = findGroup(input, target); const source = extensionOf(input); if (!group || !group.outputs.includes(target)) return send({ status: 'erro', mensagem: 'Esta combinação de arquivo e formato não é compatível.', jobId });
      try {
        await fs.access(input); const outputDir = settings.outputDir && await fs.stat(settings.outputDir).then((stat) => stat.isDirectory()).catch(() => false) ? settings.outputDir : defaultOutput(); const requestedName = String(settings.outputName || '').replace(/[<>:"/\\|?*\x00-\x1F]/g, '').trim(); output = await uniqueOutputPath(outputDir, requestedName || `OmniFree_${Date.now()}`, target); send({ status: 'processando', mensagem: `Convertendo para ${target.toUpperCase()}...`, jobId });
        if (state.cancelled) throw new Error('CONVERSAO_CANCELADA');
        if (group.engine === 'sharp') { let image = sharp(input, { sequentialRead: true }); if (settings.width || settings.height) image = image.resize({ width: Number(settings.width) || undefined, height: Number(settings.height) || undefined, fit: settings.cropImage ? 'cover' : 'inside', position: settings.cropPosition || 'centre', withoutEnlargement: !settings.cropImage }); const overlays = []; if (settings.watermark) overlays.push({ input: Buffer.from(`<svg width="800" height="80"><text x="20" y="55" font-size="42" fill="white" fill-opacity="0.7">${String(settings.watermark).replace(/[<&>]/g, '')}</text></svg>`), gravity: 'southeast' }); if (settings.watermarkImage) { await fs.access(settings.watermarkImage); overlays.push({ input: settings.watermarkImage, gravity: 'southeast', opacity: 0.72 }); } if (overlays.length) image = image.composite(overlays); if (!settings.removeMetadata) image = image.withMetadata(); await image.toFormat(target === 'jpg' ? 'jpeg' : target, settings.quality ? { quality: Number(settings.quality) } : {}).toFile(output); }
        else if (group.engine === 'data') await convertData(input, output, source, target);
        else if (group.engine === 'ffmpeg') await new Promise((resolve, reject) => { const task = ffmpeg(input).toFormat(target); state.cancel = () => task.kill('SIGKILL'); if (settings.startTime) task.setStartTime(settings.startTime); if (settings.duration) task.setDuration(settings.duration); if (settings.audioOnly) task.noVideo(); if (settings.noAudio) task.noAudio(); if (settings.removeMetadata) task.outputOptions(['-map_metadata', '-1']); if (settings.quality) task.outputOptions(['-crf', String(Math.max(0, Math.min(51, 51 - Number(settings.quality) / 2)))]); task.on('progress', (p) => p.percent && event.sender.send('progresso-conversao', Math.round(p.percent))).on('end', resolve).on('error', (error) => state.cancelled ? reject(new Error('CONVERSAO_CANCELADA')) : reject(error)).save(output); });
        else if (group.engine === 'libreoffice') await office(input, output, target); else if (group.engine === '7zip') await archive(input, output); else if (group.engine === 'calibre') await run('ebook-convert', [input, output]);
        if (state.cancelled) throw new Error('CONVERSAO_CANCELADA'); send({ status: 'concluido', mensagem: 'Sucesso! Arquivo convertido.', caminhoArquivo: output, jobId });
      } catch (error) { if (error.message === 'CONVERSAO_CANCELADA') { await fs.rm(output || '', { force: true }).catch(() => {}); send({ status: 'cancelado', mensagem: 'Conversão cancelada.', jobId }); } else send({ status: 'erro', mensagem: conversionErrorMessage(error), jobId }); } finally { if (jobId) jobs.delete(jobId); }
    },
    cancel(jobId) { const job = jobs.get(jobId); if (job) { job.cancelled = true; job.cancel?.(); } }
  };
}
module.exports = { createConversionService };
