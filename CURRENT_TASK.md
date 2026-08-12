```markdown
# CURRENT_TASK.md
## BorealBoost — Tarefa Atual

> Este arquivo define **apenas a tarefa da sessão atual**.
>
> O objetivo é impedir que o agente tente desenvolver o projeto inteiro de uma vez.
>
> O agente deve executar exclusivamente as atividades descritas abaixo.

---

# STATUS DO PROJETO

Fase Atual:

✅ FASE 0 — Discovery e Arquitetura

O projeto ainda **não deve iniciar a implementação**.

Todo o esforço desta sessão deve ser direcionado para análise, pesquisa, arquitetura e planejamento.

---

# OBJETIVO DESTA SESSÃO

Antes de escrever qualquer código:

1. Ler completamente:

- `BOREALBOOST_MASTER_SPEC.md`
- `CODEX_BOOTSTRAP.md`

2. Compreender todos os requisitos do BorealBoost.

3. Analisar toda a estrutura atual do projeto.

4. Identificar inconsistências.

5. Pesquisar tecnicamente a versão mais recente do projeto oficial:

ChrisTitusTech / WinUtil

6. Estudar sua arquitetura.

7. Estudar seus módulos.

8. Estudar seus mecanismos de:

- Tweaks
- Presets
- Features
- DNS
- Drivers (quando existir)
- Undo
- Restore
- Config
- Detection
- Updates
- Install
- Automation

9. Avaliar quais funcionalidades realmente agregam valor ao BorealBoost.

10. Não copiar interface.

11. Não copiar identidade visual.

12. Verificar compatibilidade das funcionalidades com Windows 10 e Windows 11.

13. Verificar possíveis restrições de licença para qualquer código ou recurso reutilizado.

---

# PESQUISA TÉCNICA

Pesquisar documentação oficial da Microsoft sobre:

- Registry
- Services
- Power Management
- Windows Features
- Device Manager
- Drivers
- SetupAPI
- Windows Update
- DISM
- SFC
- Restore Points
- WMI
- CIM
- PowerShell
- Win32 APIs
- Performance Counters
- ETW (quando relevante)
- Windows Internals
- Game Mode
- Graphics Settings
- Storage
- Networking

O objetivo é validar tecnicamente cada otimização antes que ela entre no catálogo.

---

# DEFINIR A ARQUITETURA

Projetar completamente:

- Arquitetura da solução
- Estrutura de projetos
- Estrutura de pastas
- Camadas
- Dependências
- Fluxo de dados
- Injeção de dependência
- Navegação
- Logging
- Sistema de Configuração
- Sistema de Atualização
- Sistema de Drivers
- Sistema de Benchmark
- Sistema de Rollback
- Sistema de Restore Point
- Optimization Engine
- Recommendation Engine
- Compatibility Engine
- Boreal Score

Nenhuma implementação nesta fase.

---

# DEFINIR A UX

Projetar todas as telas.

No mínimo:

- Dashboard
- Scanner
- Análise
- Otimização
- Drivers
- Personalizado
- Ferramentas
- Resultados
- Logs
- Configurações
- Restaurar

Definir:

- Fluxos
- Navegação
- Componentes
- Cards
- Estados
- Loading
- Feedback
- Modais
- Diálogos

Sem implementação.

---

# DEFINIR O DOMÍNIO

Projetar:

- Entidades
- Objetos
- Value Objects
- Serviços
- Regras
- Estados
- Sessões
- Catálogo de otimizações

Cada otimização deverá possuir um modelo consistente.

---

# DEFINIR O OPTIMIZATION ENGINE

Projetar:

- Detection
- Compatibility
- Apply
- Verify
- Undo
- Rollback
- Snapshot
- Logging

Definir responsabilidades de cada componente.

---

# DEFINIR O DRIVER ENGINE

Projetar:

- Scanner
- Hardware IDs
- Vendor IDs
- Fontes oficiais
- Verificação de assinatura
- Compatibilidade
- Instalação
- Validação
- Rollback

Sem implementação.

---

# DEFINIR O ROLLBACK ENGINE

Projetar:

- Restore Point
- Snapshot
- Session Restore
- Undo individual
- Undo em lote
- Validação pós-restauração

---

# DEFINIR O BOREAL SCORE

Criar metodologia documentada.

Explicar:

- critérios
- pesos
- cálculo
- limitações

Não utilizar números arbitrários.

---

# DEFINIR MATRIZ DE COMPATIBILIDADE

Documentar:

Windows 10

- versões suportadas
- builds suportadas

Windows 11

- versões suportadas
- builds suportadas

Desktop

Notebook

Intel

AMD

NVIDIA

Hardware legado

---

# IDENTIFICAR RISCOS

Listar:

- riscos técnicos
- riscos de compatibilidade
- riscos de manutenção
- riscos de segurança
- riscos de desempenho
- riscos legais
- riscos de licença

Apresentar possíveis estratégias de mitigação.

---

# ENTREGÁVEIS OBRIGATÓRIOS

Ao final desta sessão gerar:

- DISCOVERY.md
- REQUIREMENTS.md
- ARCHITECTURE.md
- ARCHITECTURE_DECISION_RECORD.md
- DOMAIN_MODEL.md
- OPTIMIZATION_ENGINE.md
- DRIVER_ENGINE.md
- ROLLBACK_ENGINE.md
- BOREAL_SCORE.md
- UX_SPECIFICATION.md
- WINUTIL_ANALYSIS.md
- COMPATIBILITY_MATRIX.md
- IMPLEMENTATION_ROADMAP.md
- SECURITY.md
- THIRD_PARTY_NOTICES.md (caso necessário)

---

# O QUE NÃO FAZER

Nesta sessão é proibido:

- implementar telas
- criar interface definitiva
- criar banco de dados
- implementar otimizações
- escrever tweaks
- modificar Registry
- executar scripts
- criar drivers
- implementar rollback
- desenvolver funcionalidades

Tudo deve permanecer em nível de arquitetura e documentação.

---

# CRITÉRIO DE CONCLUSÃO

A tarefa somente estará concluída quando:

- toda a arquitetura estiver documentada;
- todas as decisões técnicas estiverem justificadas;
- todos os módulos estiverem definidos;
- o roadmap estiver completo;
- os riscos estiverem documentados;
- houver um plano claro para iniciar a Fase 1.

Ao concluir, interrompa a execução e aguarde a aprovação da arquitetura antes de iniciar qualquer implementação.
```
