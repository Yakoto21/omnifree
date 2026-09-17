const fs = require('fs/promises');
const YAML = require('yaml');
const TOML = require('@iarna/toml');
const { XMLParser, XMLBuilder } = require('fast-xml-parser');

function escapeCsv(value) { const text = String(value ?? ''); return /[\",\n]/.test(text) ? `\"${text.replace(/\"/g, '\"\"')}\"` : text; }
function parseCsv(text) {
  const lines = text.replace(/^\uFEFF/, '').trim().split(/\r?\n/).filter(Boolean); if (!lines.length) return [];
  const parse = (line) => { const fields = []; let value = ''; let quoted = false; for (let i = 0; i < line.length; i += 1) { if (line[i] === '"' && quoted && line[i + 1] === '"') { value += '"'; i += 1; } else if (line[i] === '"') quoted = !quoted; else if (line[i] === ',' && !quoted) { fields.push(value); value = ''; } else value += line[i]; } fields.push(value); return fields; };
  const headers = parse(lines.shift()); return lines.map((line) => Object.fromEntries(headers.map((header, index) => [header, parse(line)[index] ?? ''])));
}
function parseIni(text) {
  const result = {}; let section = result;
  text.split(/\r?\n/).forEach((line) => { const clean = line.trim(); if (!clean || /^[;#]/.test(clean)) return; const heading = clean.match(/^\[([^\]]+)]$/); if (heading) { section = result[heading[1]] = {}; return; } const split = clean.indexOf('='); if (split > 0) section[clean.slice(0, split).trim()] = clean.slice(split + 1).trim(); });
  return result;
}
function stringifyIni(value) {
  return Object.entries(value).flatMap(([key, item]) => item && typeof item === 'object' && !Array.isArray(item) ? [`[${key}]`, ...Object.entries(item).map(([name, content]) => `${name}=${content}`), ''] : [`${key}=${item}`]).join('\n');
}
function toCsv(value) { const rows = Array.isArray(value) ? value : [value]; const columns = [...new Set(rows.flatMap((row) => Object.keys(row || {})))]; return [columns.join(','), ...rows.map((row) => columns.map((key) => escapeCsv(row?.[key])).join(','))].join('\n'); }
function fromXml(text) { return new XMLParser({ ignoreAttributes: false, attributeNamePrefix: '@_' }).parse(text); }
function toXml(value) { return '<?xml version="1.0" encoding="UTF-8"?>\n' + new XMLBuilder({ ignoreAttributes: false, format: true }).build(value); }
function parseSource(source, raw) {
  if (source === 'json') return JSON.parse(raw);
  if (source === 'csv') return parseCsv(raw);
  if (source === 'xml') return fromXml(raw);
  if (source === 'yaml' || source === 'yml') return YAML.parse(raw);
  if (source === 'toml') return TOML.parse(raw);
  if (source === 'ini') return parseIni(raw);
  throw new Error('Formato de dados não suportado.');
}
function writeTarget(target, value) {
  if (target === 'json') return JSON.stringify(value, null, 2);
  if (target === 'csv') return toCsv(value);
  if (target === 'xml') return toXml(value);
  if (target === 'yaml') return YAML.stringify(value);
  if (target === 'toml') return TOML.stringify(value);
  if (target === 'ini') return stringifyIni(value);
  throw new Error('Formato de dados não suportado.');
}
async function convertData(input, output, source, target) { const raw = await fs.readFile(input, 'utf8'); await fs.writeFile(output, writeTarget(target, parseSource(source, raw)), 'utf8'); }
module.exports = { convertData };
