# BorealBoost - Compatibility Matrix

Data: 2026-08-12
Status: matriz inicial para planejamento.

## Politica geral

Suporte declarado do BorealBoost:

- Windows 10 x64.
- Windows 11 x64.

Regra critica: uma otimizacao valida no Windows 11 nao e automaticamente valida no Windows 10.

Toda otimizacao deve declarar:

- OS;
- edition;
- build minimo;
- build maximo quando necessario;
- arquitetura;
- desktop/notebook;
- hardware;
- dependencias;
- conflitos;
- contraindicoes.

## Windows 10

Target inicial proposto:

- Windows 10 22H2 x64, build base 19045.

Esse target legado deve permanecer no roadmap da V1. Suporte funcional do BorealBoost a Windows 10 22H2 x64/build 19045 nao deve ser removido sem ADR especifico e aprovacao explicita.

Contexto de suporte:

- Microsoft informa que Windows 10 22H2 foi a versao final e o suporte geral terminou em 2025-10-14 para edicoes gerais.
- LTSC/IoT podem ter ciclos diferentes e devem ser tratados separadamente.

Politica BorealBoost:

- suportar funcionalmente Windows 10 22H2 x64 como alvo legado;
- exibir aviso tecnico de suporte/seguranca quando aplicavel;
- nao prometer hardening completo em sistema fora do suporte geral;
- bloquear recursos que dependem de APIs presentes apenas em Windows 11 ou builds superiores.

## Compatibilidade funcional vs suporte Microsoft

Compatibilidade funcional BorealBoost significa:

- o app instala/abre;
- scanner funciona;
- CompatibilityEngine conhece o build;
- operacoes do catalogo declaram suporte Windows 10 explicitamente;
- Agent, snapshot, rollback e relatorios operam dentro das APIs disponiveis no Windows 10 22H2 x64/build 19045;
- recursos ausentes no Windows 10 sao bloqueados ou escondidos com explicacao.

Estado de suporte Microsoft significa:

- ciclo oficial de seguranca/manutencao do Windows segundo edicao/canal;
- nao e controlado pelo BorealBoost;
- pode exigir aviso tecnico ao cliente;
- nao autoriza o BorealBoost a aplicar tweaks inseguros para compensar fim de suporte.

Consequencia:

- Windows 10 22H2 x64/build 19045 continua target legado funcional;
- relatorios devem distinguir "sistema suportado pelo BorealBoost" de "estado de suporte Microsoft";
- otimizacoes validas em Windows 11 nao sao herdadas automaticamente por Windows 10.

## Classificacao do Scanner - Fase 2

O System Scanner implementa classificacao funcional inicial:

- `LegacySupported`: Windows 10 22H2 x64/build 19045 ou superior dentro da familia Windows 10 suportada funcionalmente pela V1.
- `Supported`: Windows 11 x64 em build V1 conhecido e validado conceitualmente.
- `Unsupported`: arquitetura fora de x64 ou build abaixo do alvo minimo.
- `Unknown`: familia/build nao identificada ou build novo que ainda requer validacao explicita.

Essa classificacao nao afirma estado de suporte Microsoft. Ela informa se o BorealBoost reconhece o sistema como alvo funcional para diagnostico e fases futuras.

## Compatibility em Recommendations - Fase 3

A Fase 3 adiciona compatibilidade no nivel de recomendacao:

- `Compatible`;
- `Conditional`;
- `Incompatible`;
- `Unknown`.

Essa compatibilidade e informativa e read-only. Ela nao autoriza apply. Compatibility operacional de otimizacoes, Detection, ExecutionPlan e revalidacao pelo Agent pertencem a Fase 4 consolidada e ao Catalogo da Fase 5.

Regras:

- Windows 10 `LegacySupported` pode gerar aviso, nao remocao de suporte;
- Windows `Unsupported` bloqueia planejamento automatico futuro;
- VM torna recomendacoes dependentes de hardware fisico condicionais ou bloqueadas;
- notebook/portatil torna energia agressiva Advanced e condicionada;
- Unknown nao e tratado como Compatible.

## Windows 11

Versoes atuais observadas em 2026-08-12 na pagina oficial Microsoft:

| Versao | Build base | Estado de planejamento |
| --- | --- | --- |
| 23H2 | 22631 | Suporte limitado; Home/Pro encerrado, Enterprise/Education ate 2026-11-10 |
| 24H2 | 26100 | Suporte V1 |
| 25H2 | 26200 | Suporte V1 |
| 26H1 | 28000 | Detectar e suportar apenas apos validacao; Microsoft indica foco em novos dispositivos, nao upgrade in-place comum |

Politica:

- Windows 11 24H2 e 25H2 sao alvos principais V1.
- Windows 11 23H2 so deve ser suportado quando edition ainda estiver em suporte ou mediante aviso.
- Windows 11 26H1 deve ser tratado como target novo e pendente de validacao fisica/VM.

## Arquitetura CPU

V1:

- x64 apenas.

Nao suportado inicialmente:

- ARM64;
- x86.

ARM64 fica pendente porque drivers, SetupAPI, WinUI deployment, power plans e performance validation exigem matriz propria.

## Dispositivo

### Desktop

Permitido:

- power plan performance quando compativel;
- otimizacoes de startup, servicos, gaming, storage e drivers.

### Notebook

Restricoes:

- nao aplicar plano maximo automaticamente;
- considerar bateria/AC;
- preservar sleep, Wi-Fi, Bluetooth, touchpad, biometria, cameras, sensores, power management e fabricante.

## CPU/GPU

CPU:

- Intel;
- AMD.

GPU:

- NVIDIA;
- AMD;
- Intel.

Regras:

- GPU-specific so com vendor detectado;
- HAGS apenas se GPU e driver suportarem;
- nao editar perfis proprietarios obscuros;
- recomendacoes manuais quando nao houver API documentada.

## Storage

Tipos:

- HDD;
- SATA SSD;
- NVMe.

Regras:

- HDD recebe recomendacoes diferentes de SSD/NVMe;
- TRIM so em storage compativel;
- saude de disco apenas com API confiavel;
- limpeza nao deve apagar dados pessoais fora de escopo.

## Rede

Regras:

- detectar Ethernet/Wi-Fi/VPN;
- DNS customizado deve permitir reset para DHCP;
- DNS nao pode ser vendido como FPS boost;
- IPv6 nunca desabilitado automaticamente em preset padrao;
- Teredo/IPv4 preferred apenas Advanced/Custom com evidencia.

## Servicos

Classificacao:

- Critical;
- Core;
- Conditional;
- Optional;
- ThirdParty;
- Unknown.

Politica:

- Critical e Unknown nao alteram automaticamente;
- Conditional depende de hardware/uso: impressora, Bluetooth, Xbox, Hyper-V, WSL, biometria, touchscreen, Wi-Fi, VPN, dominio, RDP, Store, OneDrive.

## Seguranca

Nao desabilitar silenciosamente:

- Defender;
- Firewall;
- Secure Boot;
- BitLocker;
- Windows Update;
- UAC;
- Credential Guard;
- VBS;
- Memory Integrity.

Qualquer ajuste relacionado deve ser Advanced/Experimental e exigir confirmacao individual.

## Disponibilidade de ferramentas Windows

Observacoes iniciais:

- PnPUtil `/enum-devices` esta disponivel a partir de Windows 10 1903; algumas flags sao Windows 10 2004, Windows 11 21H2 ou 22H2.
- `Checkpoint-Computer` e suportado em Windows client, mas limitado a um checkpoint por dia.
- Powercfg, DISM, WMI/CIM e Registry APIs sao fontes oficiais, mas cada uso precisa de build/edition check.

## Matriz de testes minima

Planejada:

- Win10 22H2 Desktop Intel + NVIDIA.
- Win10 22H2 Desktop AMD + AMD.
- Win11 24H2 Desktop AMD + NVIDIA.
- Win11 24H2 Desktop Intel + NVIDIA.
- Win11 25H2 Desktop AMD + AMD.
- Win11 Intel iGPU.
- Win11 AMD iGPU.
- Win11 Notebook Intel.
- Win11 Notebook AMD.

## Pendencias

- Confirmar se Windows 10 LTSC/IoT entram no suporte V1.
- Definir builds exatas aceitas para 24H2/25H2 apos primeira validacao.
- Validar Windows 11 26H1 em hardware real ou VM compativel.
- Confirmar disponibilidade de hardware fisico.
