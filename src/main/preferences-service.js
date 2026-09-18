const path = require('path');
const fs = require('fs/promises');
const fsSync = require('fs');

function createPreferencesService({ getUserDataPath, sendRule }) {
  const watchers = new Map();
  const file = () => path.join(getUserDataPath(), 'omnifree-preferences.json');
  async function read() { try { return JSON.parse(await fs.readFile(file(), 'utf8')); } catch { return { autoUpdates: true, rules: [] }; } }
  async function write(preferences) { await fs.writeFile(file(), JSON.stringify(preferences, null, 2), 'utf8'); }
  function configureRules(rules = []) { watchers.forEach((watcher) => watcher.close()); watchers.clear(); rules.forEach((rule) => { try { const watcher = fsSync.watch(rule.folder, { recursive: true }, (_event, filename) => { if (!filename) return; const input = path.join(rule.folder, filename); if (path.resolve(input).startsWith(path.resolve(rule.outputDir))) return; setTimeout(() => sendRule({ input, target: rule.target, outputDir: rule.outputDir }), 500); }); watchers.set(rule.folder, watcher); } catch (error) { console.warn('Não foi possível monitorar regra:', error.message); } }); }
  async function savePreferences(preferences) { const current = await read(); await write({ ...current, autoUpdates: preferences?.autoUpdates !== false }); return read(); }
  async function saveRules(rules) { const current = await read(); current.rules = Array.isArray(rules) ? rules : []; await write(current); configureRules(current.rules); return current.rules; }
  return { read, configureRules, savePreferences, saveRules };
}
module.exports = { createPreferencesService };
