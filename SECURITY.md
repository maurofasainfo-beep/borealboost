# BorealBoost - Security

Data: 2026-08-13
Status: politica de seguranca aprovada e atualizada ate a Fase 5.

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
- `BorealBoost.Agent` obrigatorio; elevado sob demanda quando a operacao exigir privilegio;
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

## System Scanner

O Scanner da Fase 2 e somente leitura. Ele pode consultar WMI/CIM, Win32 APIs, APIs .NET e Registry read-only quando houver justificativa tecnica, mas nao pode modificar o sistema.

Regras:

- nao elevar o Agent para coleta que o processo do usuario consegue fazer;
- nao executar PowerShell, `cmd.exe`, scripts ou processos externos para scanning;
- nao instalar, atualizar, remover ou procurar drivers fora do inventario local;
- nao alterar Registry, Services, Power, DNS, Windows Update, AppX, Defender, Firewall, features ou firmware;
- nao calcular Boreal Score, benchmark ou recomendacao;
- `Unknown` deve ser usado quando o dado nao for confiavel.
- `Win32_VideoController.AdapterRAM` nao deve ser tratado sozinho como VRAM conhecida.
- WMI/CIM nao deve ser abandonado em background apos timeout/cancelamento; a sessao de scan so muda para concluida/cancelada apos o provider retornar ou falhar.
- A UI deve usar uma sessao central para impedir scans concorrentes.

Privacidade:

- nao coletar username, email, IP publico, MAC, SSID, Windows product key, machine GUID ou tokens;
- nao persistir/exibir serial numbers de motherboard/BIOS/RAM por padrao;
- nao despejar snapshot completo em log;
- registrar apenas `ScanId`, provider, status, duracao, partial scan, warnings e errors sem dados sensiveis.
- classificar campos de snapshot por politica explicita antes de persistir/exportar;
- remover Device Instance ID, Hardware IDs, Compatible IDs, INF, services, processos, startup items e detalhes de adaptadores da copia de relatorio padrao;
- nao exibir machine name por padrao no Dashboard.

## Analysis + Recommendation Engine

A Fase 3 e read-only e opera principalmente sobre `SystemSnapshot`.

Regras:

- `IAnalysisRule` nao deve consultar WMI, Registry, Services, Process, Network, Windows Update ou fontes externas diretamente;
- regra que precisa de dado ausente deve retornar `Unknown`, `NotApplicable`, `Blocked` ou `Conditional`, nunca oportunidade automatica;
- recommendation nao e operacao executavel;
- recommendation nao contem command line, PowerShell, script, caminho de executavel, pacote de driver ou argumento para processo externo;
- nenhum preset executa apply;
- Advanced/Aggressive exigem risco explicito, justificativa, compatibilidade e confirmacao futura;
- reducao de seguranca nunca entra silenciosamente em Basico/Medio;
- falha de regra deve ser isolada e nao pode gerar recommendation falsa;
- `AnalysisResult` nao deve duplicar inventarios sensiveis completos.

Dados permitidos em `AnalysisResult`:

- contagens;
- status;
- categoria;
- risco;
- evidencia minima;
- razoes tecnicas redigidas.

Dados que nao devem ser copiados para recomendacoes por padrao:

- Device Instance ID;
- Hardware IDs;
- Compatible IDs;
- INF;
- nomes/listas completas de processos;
- detalhes completos de services;
- descricoes completas de adaptadores de rede;
- machine name.

## Snapshot security

Snapshots devem ficar em ProgramData com ACL restrita a Administrators e usuario tecnico quando aplicavel.

Devem evitar capturar valores sensiveis. Se um valor alvo parecer segredo, a operacao deve mascarar ou bloquear captura.

## Optimization Execution - Fases 4 e 5

A Fase 4 introduziu infraestrutura de mutacao somente para prova controlada. A Fase 5 adicionou `OperationType.RegistryValue` para o catalogo built-in V1, mantendo allowlist canonica e sem target livre.

Operacao de integracao:

`HKCU\Software\BorealBoost\IntegrationTest\Phase4ControlledValue`

Catalog V1:

- 12 OptimizationDefinitions reais;
- `schemaVersion = 5.1.0`;
- `catalogVersion = 5.1.0-built-in-v1`;
- somente `RegistryValue`;
- targets fixos em `TrustedRegistryOperationTargets.CatalogV1`;
- classificacao explicita de otimizacao tecnica versus preferencia (`TechnicalCategory`, `PerformanceRelevance`, `AutomaticPresetSuitability`);
- fronteira de ativacao e nivel de verificacao declarados por definicao;
- 0 SecurityTradeoff;
- 0 Defender/Firewall/Windows Update/pagefile/BCD/timer;
- detalhes em `OPTIMIZATION_CATALOG.md`.

Controles implementados:

- `OperationSpec` declarativo e tipado;
- `AgentOperationSecurityValidator` revalida OperationType, OptimizationId, OperationId, target, view, timeout, retry, reversibilidade, snapshot e rollback;
- Agent compara `CatalogVersion`, `OptimizationId`, `OperationId`, target e desired state contra a OperationSpec canonica do catalogo built-in confiavel;
- Agent rejeita OperationType desconhecido e target fora da allowlist;
- `JsonUnmappedMemberHandling.Disallow` no protocolo IPC rejeita campos extras desconhecidos em payloads tipados;
- snapshot persistido antes do apply;
- `OperationSnapshotItem` possui hash local e e rejeitado quando adulterado ou pertencente a outra sessao/plano;
- verify obrigatorio antes de commit;
- rollback usa estado original capturado;
- sessao interrompida sem `CompletedAtUtc` entra no recovery foundation;
- recovery expoe artefatos corrompidos/incompativeis como `ManualRecovery`, sem apagamento silencioso;
- lock cross-process impede duas sessoes mutaveis simultaneas por usuario.
- Basic seleciona somente itens `Automatic` Safe compativeis; preferencias de UX, privacidade e atalhos nao entram automaticamente;
- Medium seleciona itens `Automatic` Safe/Medium e mostra itens `OptIn` como `RequiresConfirmation`;
- Basic/Medium nao selecionam SecurityTradeoff nem Experimental;
- Advanced/Custom nao executam itens `Blocked`;
- AnalysisResult stale bloqueia preset;
- Unknown Windows/build/evidence bloqueia selecao automatica.

O handler Registry controlado preserva exatamente existencia da chave, existencia do valor, `String`, `ExpandString`, `DWord`, `QWord`, `MultiString`, `Binary` e `RegistryView` para snapshot/rollback. Tipos nao suportados sao rejeitados antes de apply/rollback; `REG_EXPAND_SZ` nao e expandido para captura.

Nao foi implementado ate a Fase 5:

- alteracao de Services, Power, DNS, drivers ou Windows Update;
- reducao automatica de Defender, Firewall, UAC, Secure Boot, VBS ou Memory Integrity;
- pagefile disable;
- BCD/timer hacks;
- restore point real;
- executor generico de processo, cmd ou PowerShell.

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
