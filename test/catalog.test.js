const test = require('node:test');
const assert = require('node:assert/strict');
const { optionsFor, findGroup } = require('../src/main/conversion-catalog');

test('identifica formatos de mídia', () => {
  assert.equal(optionsFor('filme.mp4').supported, true);
  assert.ok(optionsFor('filme.mp4').formats.includes('mp3'));
});

test('seleciona o motor certo para CSV', () => {
  assert.equal(findGroup('dados.csv', 'json').engine, 'data');
  assert.equal(findGroup('dados.csv', 'xlsx').engine, 'libreoffice');
});

test('rejeita extensões desconhecidas', () => assert.equal(optionsFor('arquivo.xyz').supported, false));
