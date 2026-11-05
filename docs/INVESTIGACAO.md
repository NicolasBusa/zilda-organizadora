# Investigação do DeskRipple com TinyFish e REA

Data: 5 de novembro de 2026. Ferramenta: REA 6.8.0, CLI isolada na pasta de trabalho. Escopo: artefato público do DeskRipple 1.1.2 e script público da demonstração. O programa original não foi instalado nem executado. Nenhum código, ícone ou recurso proprietário foi incorporado ao Zilda Organizadora.

## Interação web com TinyFish

A TinyFish leu o [site do produto](https://deskripple.com/), a [ajuda oficial](https://deskripple.com/help.html) e a [demonstração pública](https://deskripple.com/playground.html). Em uma sessão de navegador, interagiu com os grupos da demonstração: Grade abriu por hover/clique em duas linhas, Leque abriu em arco e Coluna abriu por clique em uma faixa vertical. Foram observados abertura, recolhimento e legendas de estilo; a tecla Escape fechou a Grade. A primeira tentativa de hover na Coluna não produziu uma abertura visível, e o clique foi necessário para confirmá-la.

A demonstração exibiu três estilos interativos, não os cinco do aplicativo nativo. Linha e Anel foram identificados na documentação publicada e corroborados pela estrutura do pacote. Não foram feitos compra, cadastro, envio de formulário ou instalação do original.

Sessão auditável: [execução TinyFish](https://agent.tinyfish.ai/runs/cfa0b1be-9fb0-44f1-a046-4c4e2a4dbce9), concluída em 30 passos, aproximadamente 184 segundos. Essa observação se refere à demonstração web. A afirmação de que suas fórmulas correspondem ao aplicativo é do próprio site e não foi provada por um teste nativo.

As seções seguintes documentam a análise estática por REA. O JavaScript da página foi executado pelo navegador da TinyFish; o REA analisou os arquivos de forma inerte. O executável nativo do DeskRipple não foi executado.

## Identidade e método

O ZIP público foi obtido de [DeskRipple-Portable.zip](https://github.com/DeskRipple/deskripple/releases/latest/download/DeskRipple-Portable.zip), com 78.063.581 bytes e SHA-256 `0be7a20cbdb89a11d962ba525d9b7c311bf88f1b26525a3447400e7df1699a69`. A versão 1.1.2 consta nos metadados `sq.version` e no executável.

O executável principal `current/DeskRipple.App.exe` tem 175.869.136 bytes e SHA-256 `89d01dfa3eee2c8610d62f5ad65ddf05cd408468e30ba0d2d433050c1b78992d`. A inspeção gerenciada direta excedeu o limite fixo de 128 MiB do REA. Um parser independente, estritamente de leitura, percorreu o manifesto single-file .NET e extraiu somente a DLL principal e seus dois arquivos de configuração, preservando offsets, tamanhos e hashes em `dotnet-bundle-manifest.json`.

O manifesto possui versão 6.0 e 450 entradas. A DLL `DeskRipple.App.dll`, no offset 28.962.816, tem 917.504 bytes, não está comprimida e tem SHA-256 `c18b1d9b53c8beb2f800da7c49faa7743e6df69a22a1c83c809d064df7fa4add`. Essa entrada completa foi então analisada pelo REA, sem carregar ou invocar seu código.

O parser seguiu a serialização documentada no código oficial da Microsoft: [Manifest.cs](https://github.com/dotnet/runtime/blob/main/src/installer/managed/Microsoft.NET.HostModel/Bundle/Manifest.cs) e [FileEntry.cs](https://github.com/dotnet/runtime/blob/main/src/installer/managed/Microsoft.NET.HostModel/Bundle/FileEntry.cs). A extração é uma operação auxiliar do investigador; os resultados de inspeção da DLL são evidências REA.

## Achados e limites

| Questão | Observação | Situação |
| --- | --- | --- |
| Plataforma e interface | Referências gerenciadas a WindowsBase, PresentationFramework, PresentationCore, System.Xaml e H.NotifyIcon.Wpf. A configuração embutida declara .NET 8.0.22 / WindowsDesktop. | Respondida por metadados estáticos. |
| Organização por grupos | Tipos FolderConfig, FolderDockViewModel e FolderEditorPanel; campos de nome, ícone, posição, monitor, estilo, ação de clique duplo e atalhos. | Estrutura confirmada; interação não executada. |
| Cinco estilos | A ajuda oficial descreve grade, coluna, linha, leque e anel. A demo tem funções de grade, coluna e leque; a montagem inclui FanLayoutMath. | Documentado; não houve teste nativo dos cinco estilos. |
| Abertura e aparência | ProfileSettings declara HoverExpandDelay, CollapseDelay, DragHoldDelay, AnimationDurationMs, MaxGridColumns, MaxGridRows, ExpandMode, ExpandedBackgroundOpacity e PanelTheme. | Campos confirmados; valores padrão não inferidos. |
| Estado de painéis | ProfileSettings declara OnlyOneExpandedFolder, ClickModeCollapseOnMouseLeave, PersistExpandedState e ExpandedFolderIds. | Estrutura confirmada; restauração efetiva não executada. |
| Atalhos preservados | ShortcutFileStore declara CopyToStore/CopyIconToStore; ShortcutArchiveService declara Archive, Restore e MoveWithCrossVolumeFallback. ShortcutItem mantém caminhos de origem e de arquivo. | Declarações corroboram a ajuda; integridade após falhas não testada. |
| Perfis | ProfileService declara CreateProfile, CloneProfile, RenameProfile, DeleteProfile, RequestSwitch e RewriteStorePaths; existe ProfilesPanel. | Fluxo estrutural confirmado; troca não executada. |
| Integração com Explorer | DesktopIntegration declara OnForegroundChanged, CheckShownDesktopState, EnforceZOrder e OnDisplaySettingsChanged; há DesktopIconManipulator e DesktopIconMetricsService. | Integração estática confirmada; equivalência de comportamento não estabelecida. |
| DPI e múltiplos monitores | P/Invokes declaradas para GetDpiForMonitor, GetDpiForWindow, EnumDisplayMonitors e GetMonitorInfoW; FolderConfig mantém identidades de monitor. | APIs e dados confirmados; diferentes escalas não testadas. |
| Atualizações e privacidade | Referência a Velopack e campos AutoUpdateEnabled, TelemetryEnabled, StartOnLogin e LicenseKey. | Presença confirmada; tráfego e efeitos não observados. |

Uma inferência útil, limitada ao inventário: `EnforceZOrder` e `CheckShownDesktopState`, junto às declarações de SetWindowPos/GetForegroundWindow/ShowWindow e à ausência de SetParent entre os 81 P/Invokes inventariados, sugerem gerenciamento da ordem das janelas. Isso não demonstra como cada janela é exibida em execução, nem exclui resolução dinâmica de outras APIs.

O REA inventariou 359 tipos, 2.022 campos, 3.237 métodos e 16.784 arestas de chamadas na DLL. As declarações nativas incluem 81 P/Invokes em seis módulos. O cabeçalho indica ReadyToRun; isso não substitui observação de execução.

## Evidências preservadas

O arquivo `evidencias-rea-resumo.json` contém os hashes, a seleção de nomes de campos/métodos, as declarações de APIs e os IDs completos das evidências, sem código, CIL ou ativos do original. Os registros completos ficam na pasta de investigação para auditoria.

| Operação | ID REA |
| --- | --- |
| ZIP e inventário | `ev_2acdaa29de7566ab395ebc81ecfe3c6209db2caf698cb2a1a41735aff62c88a1` |
| Identidade gerenciada | `ev_7061aba3cfc40a3fd1da622688b56f109782048f37102e26e8610456de454a81` |
| Membros gerenciados | `ev_d63e9d89096f481f2c8637b0277856ba0f7c07d7d6b3b271c876b2480ed28cd7` |
| Fronteiras nativas declaradas | `ev_aed9ebc4a10ca40643cbb8f38484ceb320d83ba0ad5bbd055f444f90103e96cc` |
| Importações PE do executável | `ev_591be0ecb94710995279a80773cdbd255386af3b247cebda8380e9ef65fae0e5` |
| Recursos PE | `ev_126a08b3dd9f1d76a8761522df8696d7c4378c0645d1d77a178397bb2c0ef0bd` |
| Script público da demo | `ev_eeedd208fa12c6a17b01c59b9829b4973378ea893a11740c675d7bace1074784` |

A inspeção PE encontrou 19 descritores DLL e 458 funções importadas, além de cinco funções com carga adiada; a de recursos reteve 15 recursos. Isso descreve tabelas no arquivo, sem demonstrar que as funções foram chamadas ou que um recurso foi exibido.

## Demonstração pública e recuperação de ferramentas

A [demonstração pública](https://deskripple.com/playground) foi baixada e seu script inline foi extraído com um parser HTML, ignorando comentários. O REA analisou 34.177 bytes e 4.947 nós AST, sem falhas de parse nessa versão corrigida. A análise inicial usou uma extração que incluiu um comentário HTML e registrou falha de parse; esse registro anterior não sustenta os achados finais. As funções de layout e temporização foram identificadas por leitura auxiliar do texto público. O script não foi executado, e bibliotecas externas da página não foram incluídas nessa inspeção.

O plugin REA instalado estava sem a dependência `zod`. Foi usada uma instalação temporária isolada do pacote oficial 6.8.0 via npm na pasta `work/investigacao`; nenhuma configuração do Codex, MCP ou instalação de ferramenta de análise nativa foi alterada. O diagnóstico indicou ausência de registro MCP e de motores nativos; as operações portáteis de metadados empregadas aqui funcionaram por CLI. A tentativa inicial de gravar um bundle de resumo de diretório falhou; seu resultado parcial não foi usado para afirmar cobertura completa.

## Orientação à reconstrução

A base comprovada permite especificar um aplicativo independente com grupos expansíveis, cinco layouts, gerenciamento de atalhos, perfis, aparência e temporização, preservação/restauração de atalhos e integração Windows. Os nomes internos servem somente como evidência dos subsistemas; não houve transplante de código. Instalação, abertura/fechamento, operações com arquivos e posicionamento do Zilda Organizadora precisam de validação própria. Testes do Zilda não demonstram equivalência integral ao DeskRipple, especialmente em arranjos incomuns de monitores, DPI e reinicialização do Explorer.

Fonte funcional complementar: [ajuda oficial do DeskRipple](https://deskripple.com/help). Este relatório distingue documentação publicada, declarações estáticas e inferências; comportamento nativo em execução permanece fora do escopo desta investigação.
