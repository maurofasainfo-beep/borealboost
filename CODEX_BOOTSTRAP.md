```markdown
# CODEX — BOREALBOOST BOOTSTRAP

Você está trabalhando no projeto **BorealBoost**.

Antes de realizar qualquer implementação, alteração arquitetural, refatoração ou criação de funcionalidade:

1. localize e leia integralmente:

   `BOREALBOOST_MASTER_SPEC.md`

2. trate esse documento como a principal especificação funcional e técnica do projeto;

3. analise a estrutura completa atual do repositório;

4. identifique:
   - tecnologias;
   - projetos;
   - módulos;
   - dependências;
   - arquitetura;
   - estado atual da implementação;
   - funcionalidades completas;
   - funcionalidades incompletas;
   - TODOs;
   - problemas;
   - riscos;
   - incompatibilidades com o Master Spec;

5. leia também todos os documentos técnicos existentes em `/docs` e na raiz do projeto que possam afetar a tarefa;

6. não faça suposições quando for possível verificar no código;

7. preserve funcionalidades existentes que estejam corretas;

8. evite refatorações sem necessidade;

9. nenhuma otimização de Windows pode ser adicionada sem:
   - Compatibility Check;
   - Detection;
   - Apply;
   - Verify;
   - Undo/rollback quando aplicável;
   - Risk Level;
   - Evidence Level;
   - logging.

10. nenhuma métrica de performance pode ser inventada.

11. não execute scripts remotos arbitrários.

12. não adicione drivers provenientes de fontes não oficiais.

13. alterações agressivas precisam respeitar as políticas de segurança definidas no Master Spec.

---

# PRIMEIRA TAREFA DA SESSÃO

Antes de modificar código, produza um diagnóstico resumido contendo:

## Estado atual

- arquitetura encontrada;
- módulos existentes;
- stack;
- estado do projeto.

## Relação com o Master Spec

Classifique os principais requisitos como:

- IMPLEMENTADO
- PARCIAL
- NÃO IMPLEMENTADO
- INCOMPATÍVEL
- NECESSITA VALIDAÇÃO

## Riscos encontrados

Liste:

- arquitetura;
- segurança;
- Windows 10;
- Windows 11;
- rollback;
- drivers;
- performance;
- manutenção.

## Plano da sessão

Informe exatamente:

1. o que pretende alterar;
2. por quê;
3. arquivos afetados;
4. riscos;
5. como será validado.

Somente depois desse diagnóstico prossiga com a tarefa solicitada.

---

# REGRAS DURANTE IMPLEMENTAÇÃO

Para cada mudança:

1. entenda o fluxo atual;
2. identifique impacto;
3. implemente a menor mudança coerente;
4. preserve padrões existentes;
5. trate erros;
6. adicione logs quando necessário;
7. adicione testes;
8. valide compatibility;
9. valide rollback quando aplicável;
10. atualize documentação.

---

# AO FINAL DA SESSÃO

Entregue obrigatoriamente:

## Resumo

O que foi feito.

## Arquivos alterados

Liste arquivos criados/modificados.

## Implementação

Explique as principais decisões.

## Testes executados

Informe:

- comandos;
- resultados;
- testes não executados.

## Validação

Informe se os critérios da tarefa foram atendidos.

## Riscos restantes

Liste problemas ou pontos ainda não validados.

## Próximo passo recomendado

Informe a próxima etapa lógica do roadmap.

Nunca declare algo como concluído se não tiver sido efetivamente validado.
```
