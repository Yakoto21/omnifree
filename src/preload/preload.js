const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('conversorAPI', {
  enviarArquivo: (caminho, formato, configuracoes) => ipcRenderer.send('processar-arquivo', caminho, formato, configuracoes),
  obterOpcoes: (caminho) => ipcRenderer.invoke('opcoes-conversao', caminho),
  escolherPastaDestino: () => ipcRenderer.invoke('escolher-pasta-destino'),
  escolherImagemMarcaDagua: () => ipcRenderer.invoke('escolher-imagem-marca-dagua'),
  obterComponentes: () => ipcRenderer.invoke('componentes-disponiveis'),
  mesclarPdfs: () => ipcRenderer.invoke('mesclar-pdfs'),
  separarPdf: (caminho, pasta) => ipcRenderer.invoke('separar-pdf', caminho, pasta),
  girarPdf: (caminho, pasta) => ipcRenderer.invoke('girar-pdf', caminho, pasta),
  protegerPdf: (caminho, pasta, senha) => ipcRenderer.invoke('proteger-pdf', caminho, pasta, senha),
  otimizarPdf: (caminho, pasta) => ipcRenderer.invoke('otimizar-pdf', caminho, pasta),
  extrairImagensPdf: (caminho, pasta) => ipcRenderer.invoke('extrair-imagens-pdf', caminho, pasta),
  receberStatus: (callback) => ipcRenderer.on('status-conversao', (_event, dados) => callback(dados)),
  receberProgresso: (callback) => ipcRenderer.on('progresso-conversao', (_event, percentual) => callback(percentual)),
  abrirNoExplorador: (caminho) => ipcRenderer.send('abrir-no-explorador', caminho)
});
