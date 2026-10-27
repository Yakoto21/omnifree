const path = require('path');
const os = require('os');
const { uniqueOutputPath } = require('./output-paths');

function createMediaService({ ffmpeg, outputDir }) {
  const render = (input, output, configure) => new Promise((resolve, reject) => { const job = ffmpeg(input); configure(job); job.on('end', resolve).on('error', reject).save(output); });
  return {
    async extractSubtitles(input, folder, format = 'srt') { const output = await uniqueOutputPath(await outputDir(folder), 'OmniFree_legendas', format); await render(input, output, (job) => job.outputOptions(['-map', '0:s:0']).toFormat(format)); return output; },
    async thumbnail(input, folder, time = '00:00:01') { const output = await uniqueOutputPath(await outputDir(folder), 'OmniFree_thumbnail', 'jpg'); await render(input, output, (job) => job.seekInput(time).frames(1).outputOptions(['-q:v', '2'])); return output; }
  };
}
module.exports = { createMediaService };
