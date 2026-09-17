const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs/promises');
const os = require('os');
const path = require('path');
const { convertData } = require('../src/main/data-converter');

test('converte JSON para YAML', async () => {
  const input = path.join(os.tmpdir(), 'omnifree-test.json'); const output = path.join(os.tmpdir(), 'omnifree-test.yaml');
  await fs.writeFile(input, '{"nome":"OmniFree"}'); await convertData(input, output, 'json', 'yaml');
  assert.match(await fs.readFile(output, 'utf8'), /nome: OmniFree/);
  await fs.rm(input, { force: true }); await fs.rm(output, { force: true });
});
