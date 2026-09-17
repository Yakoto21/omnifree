const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { uniqueOutputPath } = require('../src/main/output-paths');

test('preserva arquivos existentes ao criar um nome de saída único', async () => {
  const folder = await fs.mkdtemp(path.join(os.tmpdir(), 'omnifree-output-'));
  try {
    await fs.writeFile(path.join(folder, 'resultado.png'), 'original');
    assert.equal(await uniqueOutputPath(folder, 'resultado', 'png'), path.join(folder, 'resultado (1).png'));
  } finally { await fs.rm(folder, { recursive: true, force: true }); }
});
