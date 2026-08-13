# BorealBoost - Third Party Notices

Data: 2026-08-12

## Estado atual

A Fase 1 adicionou dependencias NuGet para a foundation .NET/WinUI e testes. Nenhum codigo do WinUtil, O&O ShutUp10++, asset, marca, logo, fonte ou recurso visual de terceiros foi incorporado.

## WinUtil

Projeto:

- ChrisTitusTech/winutil
- URL: `https://github.com/ChrisTitusTech/winutil`
- Snapshot analisado: `aee3e7a1f4a3249ff2f95e75b5bd3768626a21b6`
- Licenca observada: MIT
- Copyright observado: CT Tech Group LLC

Uso nesta sessao:

- referencia funcional;
- estudo de arquitetura;
- estudo de categorias, presets, DNS, features, fixes e undo;
- nenhuma incorporacao de codigo.

Se codigo ou trechos forem usados no futuro:

- identificar arquivo e commit;
- preservar copyright;
- preservar texto MIT exigido;
- incluir aviso nesta pagina;
- nao copiar marca, UI ou identidade visual.

## O&O ShutUp10++

Estado:

- apenas identificado como integracao existente no WinUtil;
- nao incorporado;
- nao redistribuido;
- nao recomendado para V1 sem revisao de licenca.

Pendencia:

- verificar licenca comercial/distribuicao antes de qualquer integracao.

## Microsoft documentation

Documentacao Microsoft foi consultada como fonte tecnica primaria para arquitetura e compatibilidade.

Nenhum conteudo Microsoft foi incorporado como codigo.

Principais URLs:

- `https://dotnet.microsoft.com/en-us/platform/support/policy`
- `https://learn.microsoft.com/en-us/windows/apps/winui/winui3/`
- `https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry`
- `https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options`
- `https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil-command-syntax`
- `https://learn.microsoft.com/en-us/windows-hardware/drivers/install/setupapi`
- `https://learn.microsoft.com/en-us/windows/win32/wua_sdk/using-the-windows-update-agent-api`

## Dependencias NuGet adicionadas na Fase 1

Metadados de licenca verificados nos `.nuspec`/arquivos de licenca restaurados no cache NuGet local.

| Pacote | Versao | Licenca declarada | Motivo |
| --- | --- | --- | --- |
| Microsoft.WindowsAppSDK | 2.3.1 | Microsoft Software License Terms | WinUI 3 / Windows App SDK para shell desktop nativo |
| Microsoft.Extensions.Hosting | 10.0.11 | MIT | Host, DI, configuration e logging foundation |
| Microsoft.Extensions.Logging | 10.0.11 | MIT | Abstracoes e provider de logging estruturado |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.11 | MIT | Leitura de configuracao sem acoplamento concreto |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.11 | MIT | Registro de servicos de infraestrutura |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | Execucao de testes .NET |
| xunit | 2.9.3 | Apache-2.0 | Testes unitarios, integracao e system safety |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 | Descoberta/execucao de testes via `dotnet test` |
| coverlet.collector | 6.0.4 | MIT | Coleta futura de cobertura em testes |

## Bibliotecas candidatas futuras

Ainda nao incorporadas:

- CommunityToolkit.Mvvm;
- Serilog;
- QuestPDF ou alternativa de PDF;
- WiX Toolset.

Antes de adicionar nova dependencia:

- verificar licenca;
- registrar versao;
- registrar motivo;
- validar compatibilidade Windows 10/11;
- incluir notice se exigido.

## Pendencias

- Atualizar este arquivo quando a primeira dependencia real for adicionada.
- Criar checklist de licencas na Fase 1.
