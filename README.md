<p align="center"><img src="docs/logo.png" width="120" alt="JF Tools"></p>

# JF Tools

Plugin gratuito para Autodesk Revit que exporta pranchas em lote para **PDF**, **DWG**, **IFC** e **NWC**, com nomenclatura padronizada e controle de revisões, tudo numa única janela, sem precisar do pyRevit.

O JF Tools começou como produto pago e agora é **gratuito e de código aberto**. Não há chave de ativação, cadastro nem conexão com a internet: é instalar e usar.

## Baixar

| Sistema | Arquivo | Revit |
|---|---|---|
| Windows 10/11 (64 bits) | [JFTools-Setup.exe](https://github.com/Eng-JulioFerreira/jftools/releases/latest) | 2022, 2023, 2024 e 2025 |

1. Baixe e execute o instalador (pede permissão de administrador).
2. Abra o Revit e procure a aba **JFTools**.

O instalador copia o plugin para `C:\ProgramData\JFTools\` e registra o `.addin` em todas as versões do Revit encontradas no computador. Para desinstalar, use **Configurações → Aplicativos** do Windows.

> Já usava uma versão paga? Basta instalar esta versão por cima. A chave antiga não é mais necessária e seus perfis de nomenclatura e de exportação são mantidos.

## Principais recursos

- **Exportação em lote**: selecione dezenas de pranchas, de um ou de vários projetos abertos, e exporte tudo de uma vez.
- **PDF**: um arquivo por prancha ou um PDF único combinado; qualidade raster, modo de cor e elementos ocultos.
- **DWG**: editor completo das opções de exportação (versão do AutoCAD, cores, texto, sólidos, unidades, coordenadas e espessuras de linha), usando as configurações salvas no Revit ou a configuração da sessão. Permite uma configuração diferente por projeto.
- **Imagens como OLE no DWG** (opcional, requer AutoCAD 2019 ou superior): converte as imagens raster vinculadas em objetos OLE incorporados, sem referências externas soltas.
- **IFC**: configurações básicas e avançadas no mesmo padrão do exportador da Autodesk, incluindo modelos vinculados exportados como arquivos IFC separados.
- **NWC (Navisworks)**: coordenadas, parâmetros, vínculos e divisão por níveis.
- **Nomenclatura padronizada**: monte o nome dos arquivos com parâmetros da prancha e do projeto, com pré-visualização em tempo real.
- **Editor de revisões**: crie, edite e remova revisões e aplique-as às pranchas selecionadas sem sair da janela.
- **Perfis de exportação**: salve todas as configurações (formatos, opções e pastas) como perfis nomeados e troque de perfil com um clique.
- **Interface em português e inglês**, de acordo com o idioma do Revit.

## O fluxo de uma exportação

| Etapa | Onde | O que fazer |
|---|---|---|
| 1 | Aba **Seleção** | Marque as pranchas. Use a busca e o botão **＋ Adicionar** para incluir outros projetos abertos. |
| 2 | Aba **Opções de Exportação** | Ative os formatos (PDF, DWG, IFC, NWC) e ajuste as opções de cada um. |
| 3 | Aba **Saída** | Escolha o perfil de nomenclatura, a pasta de destino e se quer subpastas por projeto e por formato. |
| 4 | Botão **Exportar Pranchas** | Acompanhe o progresso; é possível cancelar a qualquer momento. |

Os arquivos ficam organizados assim: `Destino/[Projeto/][PDF|DWG|IFC|NWC/]arquivo`.

## Onde ficam as configurações

| Arquivo | Conteúdo |
|---|---|
| `%APPDATA%\JFTools\export_profiles.txt` | Perfis de nomenclatura |
| `%APPDATA%\JFTools\export_settings.ini` | Perfis de exportação |

Os dois são criados automaticamente na primeira execução e editados pela própria interface.

## Limitações conhecidas

- **IFC**: algumas opções avançadas ainda não se comportam exatamente como no exportador nativo da Autodesk.
- **NWC**: ainda não é possível configurar cores e algumas outras opções do exportador do Navisworks.
- **Excel ⇄ Revit** (em desenvolvimento, desativado): a ideia é ler um intervalo nomeado de uma planilha e reproduzi-lo no Revit com a mesma aparência, e fazer o caminho inverso. O código está no repositório, mas o botão não aparece no ribbon até a ferramenta funcionar corretamente.

Relatos e sugestões são bem-vindos nas [issues](https://github.com/Eng-JulioFerreira/jftools/issues).

## Para desenvolvedores

### Compilar

Pré-requisitos: Visual Studio 2022, .NET Framework 4.8 e o Revit instalado.

1. Abra `JFTools/JFTools.csproj` no Visual Studio.
2. Ajuste `<RevitApiPath>` no `.csproj` para a pasta do seu Revit (onde ficam `RevitAPI.dll` e `RevitAPIUI.dll`).
3. Restaure os pacotes NuGet (EPPlus 4.5.3) e compile com **Ctrl+Shift+B**.
4. A DLL é gerada em `JFTools/bin/Debug/` ou `JFTools/bin/Release/`.

Para testar sem reinstalar, sobrescreva `C:\ProgramData\JFTools\JFTools.dll` com a DLL compilada e reinicie o Revit.

### Gerar o instalador

1. Compile em **Release**.
2. Copie `JFTools.dll` (de `JFTools/bin/Release/`) e `JFTools/export_profiles.txt` para `Installer/`.
3. Compile `Installer/JFTools-Setup.iss` com o [Inno Setup 6](https://jrsoftware.org/isinfo.php). O instalador sai em `Installer/Output/JFTools-Setup.exe`.

### Estrutura

```
JFTools/
├── JFToolsApp.cs                 ← cria a aba JFTools e o botão no ribbon
├── ExportSheetsCommand.cs        ← abre a janela principal
├── Commands/ExcelCommand.cs      ← ferramenta Excel (experimental, sem botão no ribbon)
├── Models/SheetItem.cs           ← dados de cada prancha
├── Helpers/
│   ├── ExportHelper.cs           ← exportação PDF/DWG/IFC/NWC
│   ├── ExportSettingsHelper.cs   ← perfis de exportação (.ini)
│   ├── NamingHelper.cs           ← perfis de nomenclatura e tokens
│   ├── OleImageBinder.cs         ← imagens → OLE via AutoCAD
│   ├── ExcelHelper.cs            ← Excel ⇄ Revit (experimental)
│   └── L.cs                      ← todos os textos da interface (PT/EN)
└── Views/                        ← janelas WPF (principal, revisões, nomenclatura, Excel)
Installer/JFTools-Setup.iss       ← script do instalador
revocation.txt                    ← mantido vazio: versões antigas (pagas) consultam este arquivo
```

Detalhes técnicos de cada módulo estão em [CLAUDE.md](CLAUDE.md). O histórico de versões está em [CHANGELOG.md](CHANGELOG.md).

## Suporte

Encontrou um problema ou tem uma sugestão? Abra uma [issue](https://github.com/Eng-JulioFerreira/jftools/issues).

## Licença

Código aberto. Veja o arquivo [LICENSE](LICENSE).

---

Desenvolvido por [Eng. Júlio Ferreira](https://eng-julioferreira.com.br).
