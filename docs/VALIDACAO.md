# Validação do Zilda Organizadora 1.0.0

Data: 5 de novembro de 2026. Compilação nativa Windows x64, C#/WPF, .NET Framework 4.8. Sem pacotes de terceiros no aplicativo ou instalador.

## Resultado

**23 testes automatizados de dados e layouts aprovados**, além das verificações de interface, abertura de processo, instância única, instalação e desinstalação descritas abaixo. Os testes usaram pastas isoladas; as operações de preservação/restauração usaram um desktop simulado.

Os testes cobrem primeiro uso; preservação de documentos; duplicatas; cópia persistente de atalhos; links; destinos inexistentes; cópia e arquivamento opt-in; clonagem de perfis; restauração sem sobrescrita; persistência da ordem e preferências; backup entre pastas; ZIP com travessia ou entradas duplicadas; caminhos absolutos indevidos; limites de configuração; IDs vazios; destinos excessivos; ícones em backups; originais órfãos; responsabilidade por originais importados; ícones de links web; configuração corrompida com e sem backup; e posições dos cinco layouts com diferentes quantidades de itens.

## Interface e abertura

- Gerenciador aberto e renderizado, com três grupos na área de trabalho.
- Navegação pelas páginas de grupo, configurações, perfis e ajuda.
- Painéis Grade, Coluna, Linha, Leque e Anel criados e renderizados com 19 entradas de teste, incluindo paginação.
- Modo Clique com vários painéis: duas janelas abertas simultaneamente.
- Modo Clique com um painel: abertura seguinte fecha o anterior.
- Modo Hover: somente um painel aberto, mesmo com a preferência de múltiplos painéis desligada.
- Abertura pelo Shell confirmada por um executável de prova que gravou um arquivo e encerrou, sem abrir documentos ou aplicativos do usuário.
- Nenhuma exceção inesperada no teste final; o log contém o impedimento esperado de anexação ao Explorer.

## Instância única e perfis

Uma primeira instância foi iniciada na bandeja. A segunda solicitou o perfil `TesteIPC` e encerrou com código zero. A primeira persistiu o perfil solicitado. Uma chamada `--quit` encerrou a primeira instância com sucesso, sem criar outra interface principal.

## Instalador e desinstalador

- Instalação isolada concluída com código zero, incluindo caminho com espaços.
- Extração do aplicativo, configuração, licença, guia e desinstalador.
- Desinstalação pelo executável instalado, com auxiliar de limpeza, removeu o aplicativo e preservou a configuração de teste.
- Marcador de instalação danificado provocou rejeição antes da remoção, preservando o aplicativo.
- Original arquivado ainda presente, com executável principal ausente, impediu a remoção e permaneceu intacto.
- Arquivo extra inserido na pasta de instalação permaneceu após a desinstalação.
- Interface do instalador aberta e capturada em modo de prévia.

Os testes de instalação utilizaram `--test`, que não registra atalhos, inicialização ou entrada real em Apps. Esses pontos foram revisados no código; não foram exercitados contra as configurações reais do usuário.

## Limites da evidência

O ambiente isolado negou a anexação das janelas ao Explorer com erro Win32 5. Foram criadas as janelas e utilizado o modo alternativo de ordem de janelas. As capturas verificam a renderização dos componentes; não certificam todos os gestos nativos de área de trabalho, arrastar para fora, Win + D, reinício do Explorer, monitores com DPI diferente ou compatibilidade com outras versões do Windows.

Os testes do Zilda não demonstram equivalência integral ao DeskRipple. O aplicativo original teve investigação estática com REA e interação na demonstração web com TinyFish; seu executável nativo não foi executado.

## Reproduzir

O pacote de fontes inclui `src/verify.ps1`. Execute-o no Windows com `-Interface` para compilar, rodar os 23 testes, gerar capturas dos painéis e verificar a abertura pelo Shell. As pastas de teste são exclusivas e ficam ao lado da pasta de fontes.
