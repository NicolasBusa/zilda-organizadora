# Zilda Organizadora — fontes

Aplicativo Windows nativo em C#/WPF, com instalador por usuário em Windows Forms. Implementação independente; não contém binários ou código do DeskRipple. Sem dependências NuGet ou npm.

Requisitos: Windows x64 com .NET Framework 4.8 e o compilador `csc.exe` fornecido pelo Framework. Abra PowerShell na pasta extraída.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\verify.ps1 -Interface
powershell -NoProfile -ExecutionPolicy Bypass -File .\src\package.ps1
```

O build fica em `build/`; o instalador e o ZIP portátil ficam em `outputs/`. Os testes ficam em `tests/`, usando pastas isoladas e um desktop simulado para operações de arquivos. `-Interface` abre a interface temporariamente, captura páginas e layouts, e lança um executável de prova criado pelo teste. Nenhum aplicativo/documento do usuário é aberto pelo teste.

`--smoke` e `--self-test` são opções de desenvolvimento: sempre use `--data-dir` com uma pasta exclusiva de teste. O teste de interface cria links fictícios nessa configuração. O aplicativo normal usa `%APPDATA%\ZildaOrganizadora`.

Arquivos principais: `Models.cs` guarda estado, atalhos e backups; `Native.cs` contém a integração Windows; `Visuals.cs` implementa os grupos e cinco layouts; `Manager.cs` contém o gerenciador; `App.cs` cuida de instância única, bandeja e inicialização; `Installer.cs` contém instalação/desinstalação; `SelfTests.cs` verifica dados e layouts.

O instalador não é assinado digitalmente. Distribuição com assinatura e atualização automática exige uma infraestrutura própria e não está configurada nesta versão.
