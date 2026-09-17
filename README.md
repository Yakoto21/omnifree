# OmniFree

Conversor local para imagens, mídia, documentos, dados e PDFs.

## Desenvolvimento

```powershell
npm install
npm start
npm test
npm run build:manual
```

O instalador é criado em `dist/OmniFree Setup 1.0.0.exe`.

## Atualizações pelo GitHub

O aplicativo verifica novas versões quando está instalado. Para publicar uma atualização:

1. Atualize a versão em `package.json`.
2. Execute `npm run build:manual`.
3. No GitHub, crie uma Release com a mesma versão, por exemplo `v1.0.1`.
4. Anexe o instalador `.exe` e os arquivos de atualização gerados na pasta `dist`.

Usuários instalados receberão a notificação de nova versão quando ela estiver disponível.
