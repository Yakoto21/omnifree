function conversionErrorMessage(error) {
  const message = String(error?.message || error || 'Erro desconhecido.');
  if (/não foi encontrado|ENOENT/i.test(message)) return 'O componente necessário não foi encontrado. Abra “Aplicativo” para ver como instalá-lo.';
  if (/EACCES|EPERM|permission denied/i.test(message)) return 'Sem permissão para criar o resultado nesta pasta. Escolha outra pasta de destino.';
  if (/ENOSPC|no space/i.test(message)) return 'Não há espaço livre suficiente para concluir a conversão.';
  if (/invalid data|corrupt|moov atom/i.test(message)) return 'O arquivo parece estar corrompido ou não é compatível com esta conversão.';
  return `Não foi possível converter este arquivo: ${message}`;
}
module.exports = { conversionErrorMessage };
