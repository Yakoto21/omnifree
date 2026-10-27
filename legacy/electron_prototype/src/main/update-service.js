function isNewerVersion(candidate, current) {
  const parse = (version) => String(version).replace(/^v/i, '').split('-')[0].split('.').map((part) => Number(part) || 0);
  const candidateParts = parse(candidate); const currentParts = parse(current);
  for (let index = 0; index < Math.max(candidateParts.length, currentParts.length); index += 1) {
    const difference = (candidateParts[index] || 0) - (currentParts[index] || 0);
    if (difference !== 0) return difference > 0;
  }
  return false;
}

function updateErrorMessage(error) {
  const message = String(error?.message || '');
  if (/404|not found/i.test(message)) return 'Não existe uma atualização publicada compatível com esta versão.';
  if (/latest\.yml|sha512|blockmap/i.test(message)) return 'A atualização foi publicada de forma incompleta. Tente novamente mais tarde.';
  if (/401|403|authentication|authorization/i.test(message)) return 'O GitHub recusou a consulta de atualizações. Tente novamente mais tarde.';
  if (/ENOTFOUND|ECONNREFUSED|ETIMEDOUT|network/i.test(message)) return 'Não foi possível alcançar o GitHub. Confira sua conexão e tente novamente.';
  return 'Não foi possível verificar atualizações. Confira sua conexão e tente novamente.';
}

function createUpdateService({ updater, getVersion, isPackaged, sendStatus }) {
  let updateAvailable = false;
  let updateDownloaded = false;
  updater.autoDownload = false;
  updater.autoInstallOnAppQuit = false;
  updater.on('download-progress', (progress) => sendStatus({ status: 'downloading', percent: Math.round(progress.percent || 0) }));
  updater.on('update-downloaded', (info) => { updateDownloaded = true; sendStatus({ status: 'downloaded', version: info.version }); });
  updater.on('error', (error) => sendStatus({ status: 'error', message: updateErrorMessage(error) }));

  return {
    async check() {
      if (!isPackaged()) return { status: 'development', message: 'A verificação de atualizações funciona na versão instalada do OmniFree.' };
      try {
        updateAvailable = false; updateDownloaded = false;
        const result = await updater.checkForUpdates(); const latest = result?.updateInfo?.version;
        if (latest && isNewerVersion(latest, getVersion())) { updateAvailable = true; return { status: 'available', version: latest, message: `A versão ${latest} está disponível no GitHub.` }; }
        return { status: 'latest', message: `Você já está usando a versão mais recente (${getVersion()}).` };
      } catch (error) { return { status: 'error', message: updateErrorMessage(error) }; }
    },
    async download() {
      if (!isPackaged()) return { status: 'development', message: 'Instale o OmniFree para atualizar pelo GitHub.' };
      if (!updateAvailable) return { status: 'none', message: 'Nenhuma atualização disponível para baixar.' };
      try { await updater.downloadUpdate(); return { status: updateDownloaded ? 'downloaded' : 'downloading' }; } catch (error) { return { status: 'error', message: updateErrorMessage(error) }; }
    },
    install() { if (!updateDownloaded) return { status: 'none' }; updater.quitAndInstall(); return { status: 'installing' }; }
  };
}

module.exports = { isNewerVersion, updateErrorMessage, createUpdateService };
