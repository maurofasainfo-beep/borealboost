# BorealBoost - Security

Data: 2026-08-12
Status: politica de seguranca para arquitetura.

## Principios

- Performance nao justifica quebrar seguranca silenciosamente.
- Alteracao destrutiva exige compatibilidade, snapshot, log, verify e rollback.
- Downloads executaveis precisam de fonte oficial, HTTPS, assinatura e hash quando disponivel.
- Logs nao podem vazar secrets.
- UI nao deve executar comandos arbitrarios.

## Superficie de risco

O BorealBoost pode tocar:

- registry;
- servicos;
- power plans;
- DNS;
- optional features;
- AppX/provisioned apps;
- drivers;
- Windows Update;
- arquivos temporarios;
- restore points;
- relatorios.

Estas areas exigem modelo de permissao, validacao e rollback.

## Elevacao

Modelo obrigatorio:

- UI principal sem privilegio permanente;
- `BorealBoost.Agent` elevado sob demanda por sessao;
- named pipe local com ACL restrita;
- Agent aceita apenas ExecutionPlan revalidado por ele mesmo;
- sem UAC a cada comando;
- Agent encerra por timeout.

O aplicativo inteiro elevado nao e fallback arquitetural da V1.

Controles obrigatorios do Agent:

- handshake com nonce de uso unico;
- validacao de `protocolVersion`, `sessionId`, `requestId`, `sequenceNumber` e `correlationId`;
- validacao da identidade do processo cliente conectado ao pipe;
- ACL do pipe restrita ao usuario interativo, Administrators e LocalSystem quando necessario;
- rejeicao de conexoes remotas e de clientes inesperados;
- limites de payload e timeout por mensagem/operacao;
- cancelamento cooperativo em pontos seguros;
- journal transacional duravel para recovery.

Proibido no canal App-Agent:

- command line arbitraria;
- script PowerShell arbitrario;
- path de executavel enviado pela UI;
- `.ps1`, `.bat`, `.cmd`, `.exe` ou `.msi` como payload operacional;
- operacao que nao exista na allowlist do Agent e no catalogo confiavel.

## Trusted Optimization Catalog

Catalogo e politica de seguranca. Ele deve ter:

- `schemaVersion`;
- `catalogVersion`;
- hash canonico;
- assinatura digital;
- publisher confiavel;
- separacao entre built-in e updated;
- protecao contra downgrade;
- validacao de compatibilidade com App/Agent.

Comportamento:

- catalogo updated em ProgramData so e usado apos schema, hash, assinatura, publisher e versao validos;
- catalogo updated invalido e ignorado, com evento de seguranca;
- catalogo built-in invalido bloqueia apply;
- schema major desconhecido bloqueia catalogo;
- downgrade sem manifest assinado de rollback e bloqueado;
- catalogo atualizado nao pode introduzir novo tipo de operacao privilegiada fora dos handlers existentes do Agent.

## Protecoes que nao devem ser desativadas automaticamente

- Microsoft Defender.
- Firewall.
- Secure Boot.
- BitLocker.
- Windows Update.
- UAC.
- Credential Guard.
- VBS.
- Memory Integrity.

Qualquer item que toque essas areas:

- nao entra em Basico/Medio automaticamente;
- exige classificacao Advanced/Experimental;
- explica risco;
- exige confirmacao individual;
- tem undo/rollback documentado ou e bloqueado.

## Downloads

Permitido apenas quando:

- HTTPS;
- dominio permitido;
- origem oficial;
- hash quando publicado;
- assinatura validada quando aplicavel;
- timeout e retry controlados;
- erro tratado;
- sem auto-execucao arbitraria.

Proibido:

- `irm | iex`;
- baixar script remoto e executar;
- mirrors genericos;
- driver sem assinatura;
- instalador sem origem oficial;
- scraping generico para achar driver;
- executar instalador local sem validacao de assinatura, publisher, fonte e match quando aplicavel.

## Drivers

Politica:

- preferir Windows Update/Microsoft quando automatizavel e seguro;
- em notebooks, priorizar OEM para drivers de plataforma e componentes customizados;
- depois fabricantes oficiais de componente quando houver match e suporte declarado;
- validar assinatura;
- validar INF/CAT, Authenticode quando houver executavel, publisher e hash quando publicado;
- exibir versao atual/proposta;
- exigir confirmacao em driver critico;
- registrar source e resultado;
- rollback planejado.

## Logs e privacidade

Logs podem conter:

- IDs de operacao;
- nomes de componentes;
- valores tecnicos de registry/config;
- codigos de erro;
- duracao;
- resultados.

Logs nao podem conter:

- senhas;
- tokens;
- chaves;
- cookies;
- dados pessoais desnecessarios;
- conteudo de arquivos do usuario.

Retencao e local devem ser configuraveis.

## Snapshot security

Snapshots devem ficar em ProgramData com ACL restrita a Administrators e usuario tecnico quando aplicavel.

Devem evitar capturar valores sensiveis. Se um valor alvo parecer segredo, a operacao deve mascarar ou bloquear captura.

## Relatorios

Relatorio de cliente deve conter apenas dados necessarios:

- hardware;
- Windows;
- findings;
- acoes;
- resultados;
- avisos.

Nao incluir logs tecnicos completos por padrao.

## Licencas e terceiros

- WinUtil: MIT, usado nesta fase apenas como referencia.
- O&O ShutUp10++: nao redistribuir nem integrar ate revisao de licenca.
- Icones, fontes e bibliotecas precisam de licenca verificada antes de uso.

## Threat model inicial

Atores:

- tecnico autorizado;
- usuario local do PC;
- malware local tentando abusar do Agent;
- download adulterado;
- driver malicioso;
- erro de catalogo.

Controles:

- canal local autenticado por ACL;
- allowlist de operacoes;
- schema validation;
- assinatura/hashes;
- protecao contra replay;
- validacao de identidade App-Agent;
- logs auditaveis;
- confirmacoes;
- rollback;
- testes em VM.

## Pendencias

- Definir politica de retencao de logs.
- Definir publisher/certificado oficial para assinatura de catalogo.
- Definir processo de rotacao caso segredo seja capturado acidentalmente.
- Definir checklist legal para terceiros.
- Definir formato final do envelope de protocolo apos prototipo controlado da Fase 1.
