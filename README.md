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

1. Atualize a versão em `package.json` e envie essa alteração ao GitHub.
2. Crie e envie uma tag com a mesma versão, por exemplo `v1.0.1`.
3. O fluxo “Publicar instalador” do GitHub gera o instalador e a Release com os arquivos de atualização.

Para testar ou gerar manualmente no seu computador, execute `npm run build:manual`; ele nunca publica arquivos por conta própria.

Usuários instalados receberão a notificação de nova versão quando ela estiver disponível.
