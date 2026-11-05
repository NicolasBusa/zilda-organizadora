# Zilda Organizadora 1.0.0

Aplicativo nativo para Windows, em português, com grupos de atalhos expansíveis na área de trabalho. Funciona sem conta e sem conexão. Implementação independente inspirada nas funções públicas do DeskRipple, investigadas com TinyFish e REA.

## Instalação

1. Abra **Zilda-Organizadora-Instalar.exe**.
2. Escolha se deseja iniciar com o Windows e abrir após instalar.
3. Clique em **Instalar Zilda**.

A instalação é por usuário e não exige administrador. O aplicativo aparece no menu Iniciar e em **Configurações do Windows → Apps**. Requer Windows 10/11 de 64 bits e .NET Framework 4.8, já disponível no computador usado para os testes. O instalador verifica esse componente e não baixa dependências.

O executável não tem assinatura digital de publicador. As somas dos arquivos entregues estão em **SHA256SUMS.txt**.

Para usar sem instalar, extraia **Zilda-Organizadora-Portatil.zip** e abra **ZildaOrganizadora.exe**. A versão portátil também mantém as configurações na pasta de dados do usuário.

## Primeiro uso

O gerenciador começa com três grupos vazios: Trabalho, Criatividade e Favoritos. Escolha um grupo, adicione seus atalhos e selecione um estilo. Você também pode criar novos grupos e importar atalhos da área de trabalho.

Na área de trabalho, passe o mouse sobre um grupo para abri-lo. Um clique em um atalho abre seu destino. Arraste o grupo para mudar sua posição; o botão direito oferece opções de edição, expansão e remoção. Em Configurações, você pode trocar a abertura por hover para clique.

O **× do gerenciador** mantém os grupos funcionando. Para voltar, dê dois cliques no ícone da Zilda perto do relógio; ele pode estar no menu **^**. Para encerrar completamente, use **Sair da Zilda** nesse ícone. **Ctrl + Alt + Z** abre o gerenciador quando esse atalho global estiver disponível.

## Funções incluídas

| Área | O que você pode fazer |
|---|---|
| Grupos | Criar, renomear, remover, mover e personalizar cores, símbolos e ícones |
| Cinco estilos | Grade, Coluna, Linha, Leque e Anel, com paginação para grupos grandes |
| Atalhos | Adicionar arquivos, aplicativos, pastas, links e atalhos `.lnk`/`.url` |
| Organização | Pesquisar, renomear, reordenar, abrir localização, usar um ícone no grupo e devolver ao desktop |
| Abertura | Hover ou clique, tempos de espera, animação e um ou vários painéis abertos |
| Aparência | Tema dos painéis, opacidade, tamanho dos ícones e dimensões da grade |
| Perfis | Criar, duplicar, ativar, renomear e excluir configurações independentes |
| Preservação | Copiar atalhos, arquivar originais opcionalmente e restaurar sem sobrescrever arquivos |
| Backup | Exportar e restaurar ZIP com perfis, cópias de atalhos, ícones importados e originais arquivados |
| Windows | Bandeja, instância única, inicialização opcional, desinstalador e troca de perfil por comando |

## Seus arquivos e atalhos

Por padrão, os originais ficam no lugar. Arquivos `.lnk` e `.url` recebem uma cópia privada; documentos, aplicativos e pastas são referências aos locais originais. Se um documento ou aplicativo for movido, adicione-o novamente.

Para limpar a área de trabalho, ative **Arquivar atalhos arrastados da área de trabalho**. Essa opção vale somente para `.lnk` e `.url` arrastados da sua área de trabalho pessoal. Documentos, aplicativos e itens da área de trabalho pública permanecem no lugar. A importação pelo gerenciador mantém os originais.

Ao remover uma entrada, grupo ou perfil, seus originais arquivados voltam ao desktop. A desinstalação também tenta restaurá-los antes de remover o programa. Nomes já existentes recebem um sufixo, sem sobrescrita. Se restarem arquivos no arquivo de originais, o desinstalador interrompe a remoção e preserva os dados.

Arrastar de um painel para o Explorer ou desktop faz uma **cópia**. Para retirar a entrada do grupo e devolver o original, use **Devolver à área de trabalho**. Dentro do painel, arrastar reordena; no gerenciador, use as setas.

## Backup e recuperação

Use **Configurações → Exportar backup**. O ZIP não inclui os aplicativos e documentos de destino; inclui seus atalhos e as configurações. Ao mudar de computador, alguns destinos podem precisar ser adicionados novamente.

Antes de importar um backup, a Zilda salva uma cópia preventiva do estado atual e devolve seus originais arquivados. O arquivo importado passa a ser a configuração ativa. Ícones internos e caminhos das cópias são remapeados para a nova pasta de dados.

Uma configuração danificada é preservada em arquivo separado. A Zilda tenta recuperar o último backup válido; na ausência dele, cria um perfil novo e mantém os arquivos privados. A restauração total também recupera atalhos órfãos deixados por uma configuração perdida, usando nomes de recuperação.

## Desinstalação

Use **Configurações do Windows → Apps → Zilda Organizadora → Desinstalar**, ou **DesinstalarZilda.exe** na pasta instalada. Os perfis são preservados por padrão. A opção de apagá-los é explícita e só é aplicada após a etapa de restauração.

O desinstalador remove apenas seus arquivos conhecidos e preserva arquivos extras colocados na pasta de instalação. Um pequeno auxiliar de desinstalação pode permanecer em `%TEMP%`, sujeito à limpeza normal do Windows.

## Diferenças e limites desta versão

- A grade de alinhamento é própria, de 96 pixels. A Zilda não desloca ícones nativos do Windows.
- O tamanho dos ícones é configurável; não acompanha automaticamente o Ctrl + scroll do desktop.
- Grupos longos usam páginas. O gerenciador mantém tema escuro; o tema escolhido se aplica aos painéis.
- Não há telemetria, licença paga, conta ou atualização automática.
- A integração tenta anexar as janelas ao desktop do Explorer. Quando o Windows bloqueia esse acesso, utiliza ordem de janelas como alternativa. No ambiente isolado dos testes, a anexação recebeu **Win32 5 — acesso negado**; foi exercitado o modo alternativo.
- A interface foi renderizada e verificada no Windows disponível. Não foram certificados todos os comportamentos de Win + D, reinício do Explorer, diferentes versões do Windows, DPI misto ou substitutos do Explorer. A equivalência integral ao DeskRipple não foi estabelecida.
- O registro real no menu Iniciar, em Apps e na inicialização foi implementado, mas os testes do instalador utilizaram um modo isolado que evita alterar essas configurações do usuário.

## Pastas e comandos

Dados e logs: `%APPDATA%\ZildaOrganizadora`.

Instalação: `%LOCALAPPDATA%\Programs\ZildaOrganizadora`.

```text
ZildaOrganizadora.exe --tray
ZildaOrganizadora.exe --profile "Nome do perfil"
ZildaOrganizadora.exe --quit
ZildaOrganizadora.exe --restore-archives --quit
```

## Arquivos complementares

- **Zilda-Organizadora-Fontes.zip**: fontes, licença, scripts de compilação, empacotamento e testes.
- **Investigacao-TinyFish-REA.md**: método, achados e limites da investigação do original.
- **Evidencias-REA.json**: resumo das evidências estáticas com identidades e hashes.
- **Validacao-Zilda.md**: verificações efetuadas no aplicativo entregue.
- **Previa-Zilda.png**: captura do gerenciador em uma configuração isolada.

O pacote contém somente o código e os ativos próprios da Zilda. Os binários e recursos proprietários investigados do DeskRipple não estão incluídos.
