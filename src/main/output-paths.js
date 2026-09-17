const path = require('path');
const fs = require('fs/promises');

async function uniqueOutputPath(directory, baseName, extension) {
  const safeExtension = String(extension || '').replace(/^\./, '');
  const stem = String(baseName || 'OmniFree').replace(/\.[^.]+$/, '');
  let attempt = 0;
  while (attempt < 1000) {
    const suffix = attempt ? ` (${attempt})` : '';
    const candidate = path.join(directory, `${stem}${suffix}.${safeExtension}`);
    try { await fs.access(candidate); attempt += 1; } catch { return candidate; }
  }
  throw new Error('Não foi possível escolher um nome disponível para o resultado.');
}

module.exports = { uniqueOutputPath };
