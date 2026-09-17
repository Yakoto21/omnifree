const path = require('path');

const groups = [
  { id: 'image', label: 'Imagem', inputs: ['png', 'jpg', 'jpeg', 'webp', 'tif', 'tiff', 'avif', 'heic', 'heif', 'bmp', 'svg', 'psd', 'gif'], outputs: ['png', 'jpg', 'webp', 'avif', 'tiff'], engine: 'sharp' },
  { id: 'media', label: 'Áudio e vídeo', inputs: ['mp4', 'mkv', 'mov', 'avi', 'webm', 'flv', 'wmv', 'm4v', 'mpg', 'mpeg', 'mts', 'm2ts', '3gp', 'ts', 'vob', 'ogv', 'mp3', 'wav', 'aac', 'm4a', 'ogg', 'flac', 'wma', 'aiff', 'opus', 'amr', 'ac3', 'gif'], outputs: ['mp4', 'mkv', 'mov', 'avi', 'webm', 'gif', 'mp3', 'wav', 'ogg', 'flac', 'm4a', 'opus'], engine: 'ffmpeg' },
  { id: 'document', label: 'Documento', inputs: ['pdf', 'doc', 'docx', 'docm', 'odt', 'rtf', 'txt', 'md', 'html', 'htm', 'xps'], outputs: ['pdf', 'docx', 'odt', 'rtf', 'txt', 'html'], engine: 'libreoffice' },
  { id: 'spreadsheet', label: 'Planilha', inputs: ['xlsx', 'xls', 'xlsm', 'xlsb', 'ods', 'csv', 'tsv', 'dbf'], outputs: ['xlsx', 'ods', 'csv', 'tsv', 'pdf'], engine: 'libreoffice' },
  { id: 'presentation', label: 'Apresentação', inputs: ['ppt', 'pptx', 'pptm', 'pps', 'ppsx', 'pot', 'potx', 'odp'], outputs: ['pptx', 'odp', 'pdf'], engine: 'libreoffice' },
  { id: 'archive', label: 'Arquivo compactado', inputs: ['zip', '7z', 'tar', 'gz', 'bz2', 'xz', 'cab', 'iso', 'rar'], outputs: ['zip', '7z', 'tar', 'gz', 'bz2', 'xz'], engine: '7zip' },
  { id: 'ebook', label: 'E-book', inputs: ['epub', 'mobi', 'azw3', 'fb2', 'lrf', 'pdb', 'cbz', 'cbr'], outputs: ['epub', 'mobi', 'azw3', 'pdf'], engine: 'calibre' },
  { id: 'data', label: 'Dados', inputs: ['json', 'csv', 'xml', 'yaml', 'yml', 'toml', 'ini'], outputs: ['json', 'csv', 'xml', 'yaml', 'toml', 'ini'], engine: 'data' }
];

function extensionOf(filePath) { return path.extname(filePath).slice(1).toLowerCase(); }
function findGroup(filePath, target) {
  const matches = groups.filter((group) => group.inputs.includes(extensionOf(filePath)) && (!target || group.outputs.includes(target)));
  if (!target || matches.length < 2) return matches[0];
  const data = matches.find((group) => group.id === 'data');
  return ['json', 'xml', 'yaml', 'toml', 'ini'].includes(target) && data ? data : matches.find((group) => group.id !== 'data') || data;
}
function optionsFor(filePath) {
  const matches = groups.filter((group) => group.inputs.includes(extensionOf(filePath)));
  if (!matches.length) return { supported: false, extension: extensionOf(filePath), formats: [] };
  return { supported: true, extension: extensionOf(filePath), category: matches.map((group) => group.label).join(' / '), formats: [...new Set(matches.flatMap((group) => group.outputs))] };
}
module.exports = { groups, findGroup, optionsFor, extensionOf };
