const test = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const fs = require('node:fs');
const path = require('node:path');
const { isNewerVersion, updateErrorMessage, createUpdateService } = require('../src/main/update-service');
const { conversionErrorMessage } = require('../src/main/error-messages');

test('compara versões de atualização sem aceitar downgrade', () => {
  assert.equal(isNewerVersion('1.0.8', '1.0.7'), true);
  assert.equal(isNewerVersion('v2.0.0', '1.9.9'), true);
  assert.equal(isNewerVersion('1.0.7', '1.0.7'), false);
  assert.equal(isNewerVersion('1.0.6', '1.0.7'), false);
});

test('transforma erros técnicos em mensagens curtas de atualização', () => {
  assert.match(updateErrorMessage(new Error('status code 404')), /atualização publicada/i);
  assert.match(updateErrorMessage(new Error('getaddrinfo ENOTFOUND github.com')), /conexão/i);
  assert.match(updateErrorMessage(new Error('latest.yml missing')), /incompleta/i);
});

test('fluxo de atualização só baixa uma versão comprovadamente nova', async () => {
  class Updater extends EventEmitter { async checkForUpdates() { return { updateInfo: { version: '1.1.0' } }; } async downloadUpdate() { this.emit('update-downloaded', { version: '1.1.0' }); } quitAndInstall() { this.installed = true; } }
  const updater = new Updater(); const statuses = [];
  const service = createUpdateService({ updater, getVersion: () => '1.0.7', isPackaged: () => true, sendStatus: (status) => statuses.push(status) });
  assert.equal((await service.check()).status, 'available');
  assert.equal((await service.download()).status, 'downloaded');
  assert.equal(service.install().status, 'installing');
  assert.equal(updater.installed, true);
  assert.equal(statuses.at(-1).status, 'downloaded');
});

test('erros comuns de conversão têm orientação para a pessoa usuária', () => {
  assert.match(conversionErrorMessage(new Error('spawn qpdf ENOENT')), /componente/i);
  assert.match(conversionErrorMessage(new Error('EACCES permission denied')), /permissão/i);
  assert.match(conversionErrorMessage(new Error('ENOSPC')), /espaço/i);
});

test('interface, preload e processo principal mantêm os canais essenciais', () => {
  const root = path.join(__dirname, '..'); const renderer = fs.readFileSync(path.join(root, 'src/renderer/renderer.js'), 'utf8'); const preload = fs.readFileSync(path.join(root, 'src/preload/preload.js'), 'utf8'); const main = fs.readFileSync(path.join(root, 'src/main/main.js'), 'utf8');
  ['verificarAtualizacoes', 'baixarAtualizacao', 'instalarAtualizacao', 'cancelarConversao'].forEach((name) => assert.match(preload, new RegExp(name)));
  ['verificar-atualizacoes', 'baixar-atualizacao', 'instalar-atualizacao', 'cancelar-conversao'].forEach((channel) => assert.match(main, new RegExp(channel)));
  ['btnVerificarAtualizacoes', 'btnBaixarAtualizacao', 'btnInstalarAtualizacao', 'btnCancelar'].forEach((name) => assert.match(renderer, new RegExp(name)));
});
