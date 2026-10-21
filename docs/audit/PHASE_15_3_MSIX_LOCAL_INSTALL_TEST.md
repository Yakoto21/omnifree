# RELATÓRIO DE AUDITORIA — FASE 15.3: TESTE LOCAL DO MSIX COM CERTIFICADO TEMPORÁRIO

**Data/Hora:** 2026-10-20  
**Branch:** `feature/msix-packaging`  
**HEAD Base:** `84c6974 feat(dist): compilacao e validacao estrutural do primeiro pacote MSIX (Fase 15.2)`  
**Veredito:** `BLOQUEADO` (instalação local não permitida por confiança restrita a `CurrentUser`; exigência de repositório de máquina `LocalMachine` identificada; ambiente limpo e seguro preservado)

---

## 1. Verificação Inicial e Preservação de Artefatos

Todos os pacotes de distribuição no diretório `dist/` foram auditados antes e depois da execução dos procedimentos, confirmando preservação integral e idêntica:

| Artefato | SHA-256 Inicial | SHA-256 Final | Status |
|---|---|---|---|
| `dist/Lodino-v1.0.0-rc.1-Portable-win-x64.zip` | `0bbcc757c1d50ad4c4154b03749bbe90e0c2513dae56c47636ee1c4038e31e07` | `0bbcc757c1d50ad4c4154b03749bbe90e0c2513dae56c47636ee1c4038e31e07` | Íntegro |
| `dist/Lodino-v1.0.0-rc.2-Portable-win-x64.zip` | `931ee594ed9ce1090094107fb9b79db52d4877f0cb685aa1cca7dfea9a8f7128` | `931ee594ed9ce1090094107fb9b79db52d4877f0cb685aa1cca7dfea9a8f7128` | Íntegro |
| `dist/Lodino-v1.0.0-rc.2-Setup-win-x64.exe` | `38593eb8b2fce844d83e2c32634fecb8e4933213030429ae720b4a69456c4107` | `38593eb8b2fce844d83e2c32634fecb8e4933213030429ae720b4a69456c4107` | Íntegro |
| `dist/Lodino-v1.0.0.0-win-x64.msix` | `1d718b83b1fbfc001d1860c9e179c6c461bfd6b2888480232d9a4940754703be` | `1d718b83b1fbfc001d1860c9e179c6c461bfd6b2888480232d9a4940754703be` | Íntegro |

O pacote MSIX de produção em `dist/` nunca foi modificado.

---

## 2. Emissão do Certificado Temporário de Teste

Em estrita obediência aos limites de segurança autorizados pelo usuário, um certificado temporário foi gerado exclusivamente no repositório pessoal do usuário:

| Parâmetro | Valor Configurado |
|---|---|
| **Subject** | `CN=0AD9F801-E67A-48F1-A087-28743E1FE069` (100% idêntico ao Publisher do manifesto) |
| **EKU** | `1.3.6.1.5.5.7.3.3` (Code Signing) |
| **Key Usage** | `DigitalSignature` |
| **Algoritmo / Tamanho** | `RSA 2048 bits` |
| **Hash** | `SHA-256` |
| **Validade** | 14 dias (expira em 2026-11-03) |
| **Thumbprint SHA-1** | `17972657F0CBD3596B5A2F11C6CD63FC9BBED9C7` |
| **Repositório da Chave Privada** | `Cert:\CurrentUser\My` |
| **Repositório de Confiança** | `Cert:\CurrentUser\TrustedPeople` (exclusivo do usuário atual) |
| **Repositórios Globais da Máquina** | `Cert:\LocalMachine\*` — **NENHUMA ALTERAÇÃO** (proibição estrita respeitada) |

---

## 3. Assinatura da Cópia de Teste

Para garantir a preservação do artefato oficial, foi criada e assinada uma cópia isolada em `scratch/msix_local_test/`:

- **Arquivo Original:** `dist/Lodino-v1.0.0.0-win-x64.msix` (156.450 bytes)  
  `SHA-256: 1d718b83b1fbfc001d1860c9e179c6c461bfd6b2888480232d9a4940754703be`
- **Cópia de Teste:** `scratch/msix_local_test/Lodino-test-signed.msix` (159.266 bytes)  
  `SHA-256: 353e30b0740f7fb395e579069609c9d5946d8a7703aa93a012c12e90f78595ac`
- **Rótulo:** `TESTE LOCAL — NÃO DISTRIBUIR`
- **Comando SignTool:**
  ```text
  signtool.exe sign /fd SHA256 /f scratch\msix_local_test\LodinoTest.pfx /p <sec-pass> /v scratch\msix_local_test\Lodino-test-signed.msix
  ```
- **Resultado do SignTool:** Exit Code `0` (`Successfully signed: scratch\msix_local_test\Lodino-test-signed.msix`).

---

## 4. Tentativa de Instalação Local e Diagnóstico Técnico

A tentativa de instalação na conta do usuário sem elevação administrativa foi executada via comando oficial:

```powershell
Add-AppxPackage -Path "scratch\msix_local_test\Lodino-test-signed.msix"
```

### 4.1 Erro e Código de Retorno
- **HRESULT:** `0x800B0109` (`CERT_E_UNTRUSTEDROOT`)
- **Mensagem do Subsistema AppX:**
  ```text
  Falha na implantação com HRESULT: 0x800B0109, Uma cadeia de certificação foi processada, mas terminou em um certificado raiz que não é confiável para o provedor de confiabilidade.
  erro 0x800B0109: o certificado raiz da assinatura no lote ou no pacote do aplicativo deve ser confiável.
  ```

### 4.2 Causa Arquitetural do Windows
- O serviço de implantação de aplicativos do Windows (`AppXSvc` / AppX Deployment Service) opera sob o contexto de segurança de alta autoridade `NT AUTHORITY\SYSTEM`.
- Ao validar a cadeia de confiança de pacotes MSIX/AppX, o `AppXSvc` consulta estritamente os repositórios de certificados em nível de computador:
  - `Cert:\LocalMachine\TrustedPeople` (para pacotes de teste/sideloading corporativo)
  - `Cert:\LocalMachine\Root` (autoridades certificadoras raiz)
- O `AppXSvc` **não** herda nem confia em certificados inseridos exclusivamente em `Cert:\CurrentUser\TrustedPeople` por uma sessão de usuário desprovida de privilégios de administrador.

### 4.3 Aplicação da Regra Mandatória da Fase
A diretriz do usuário determinou explicitamente:
> *"Se a confiança por usuário não permitir instalar o pacote, registrar `BLOCKED`. Não solicitar elevação nem modificar o armazenamento de certificados da máquina para contornar a falha."*

Em conformidade rigorosa com essa salvaguarda, o assistente **não solicitou elevação UAC** e **não tocou nos repositórios da máquina** (`LocalMachine`), registrando formalmente o status da instalação como **`BLOQUEADO`**.

---

## 5. Limpeza e Restauração Integral do Ambiente

Imediatamente após a conclusão dos testes de implantação, os procedimentos de limpeza atômica foram executados:

1. **Verificação de Resíduos do Pacote:**
   - `Get-AppxPackage -Name "LuizGustavoGodoy.Lodino"`: Nenhum pacote instalado.
2. **Exclusão do Certificado de Teste:**
   - Removido pelo thumbprint `17972657F0CBD3596B5A2F11C6CD63FC9BBED9C7` de `Cert:\CurrentUser\TrustedPeople` (restantes: 0).
   - Removido pelo thumbprint `17972657F0CBD3596B5A2F11C6CD63FC9BBED9C7` de `Cert:\CurrentUser\My` (restantes: 0).
   - Nenhum outro certificado de usuário foi afetado.
3. **Exclusão de Chaves Privadas e PFX:**
   - Arquivo `scratch/msix_local_test/LodinoTest.pfx` excluído.
   - Arquivo temporário de senha `.pass` excluído.
4. **Verificação de Hashes:** Todos os artefatos de `dist/` permanecem inalterados e confirmados bit a bit.

---

## 6. Matriz de Resultados Funcionais

| Teste | Status na Fase 15.3 | Motivo / Evidência |
|---|---|---|
| **Criação do Certificado de Teste** | **PASS** | Subject oficial, EKU Code Signing, RSA 2048, SHA-256 |
| **Assinatura da Cópia do MSIX** | **PASS** | SignTool exit code 0 |
| **Instalação via CurrentUser Trust** | **BLOQUEADO** | Windows AppXSvc exige `LocalMachine\TrustedPeople` (HRESULT `0x800B0109`) |
| **Execução da GUI sob AppModel** | **BLOQUEADO** | Depende da instalação do pacote |
| **Execução da CLI sob AppModel** | **BLOQUEADO** | Depende da instalação do pacote |
| **Conversões e Fila sob MSIX** | **BLOQUEADO** | Depende da instalação do pacote (aprovadas 100% no modo portátil/NSIS) |
| **Limpeza e Higienização de Certificados** | **PASS** | Chaves e certificados de teste removidos do sistema |
| **Preservação de Artefatos em `dist/`** | **PASS** | Todos os 4 artefatos 100% íntegros |

---

## 7. Veredito da Fase 15.3

**VEREDITO FORMAL:** `BLOQUEADO`

**Justificativa:** A cópia de teste do pacote MSIX foi assinada com sucesso pelo `SignTool.exe` oficial utilizando o certificado temporário gerado em estrita conformidade com a identidade oficial. A instalação do pacote no Windows, contudo, é bloqueada arquiteturalmente pelo serviço `AppXSvc` por não confiar no repositório restrito `CurrentUser\TrustedPeople`. Em respeito à proibição estrita de alterar os certificados globais de `LocalMachine` e de solicitar elevação UAC, a instalação e os testes sob identidade de pacote foram bloqueados de forma segura, com o ambiente integralmente restaurado e limpo.
