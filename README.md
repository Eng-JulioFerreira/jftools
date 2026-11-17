<p align="center"><img src="docs/logo.png" width="120" alt="JF Tools"></p>

# JF Tools

Plugin gratuito para Autodesk Revit que exporta pranchas em lote para **PDF**, **DWG**, **IFC** e **NWC**, com nomenclatura padronizada e controle de revisões, tudo numa única janela, sem precisar do pyRevit.

O JF Tools começou como produto pago e agora é **gratuito e de código aberto**. Não há chave de ativação, cadastro nem conexão com a internet: é instalar e usar.

## Baixar

| Sistema | Arquivo | Revit |
|---|---|---|
| Windows 10/11 (64 bits) | [JFTools-Setup.exe](https://github.com/Eng-JulioFerreira/jftools/releases/latest) | 2022 a 2027 |

O mesmo instalador serve todas as versões: ele coloca a compilação certa para cada uma (.NET Framework 4.8 no Revit 2022–2024, .NET 8 no 2025–2026 e .NET 10 no 2027).

1. Baixe e execute o instalador (pede permissão de administrador).
2. Abra o Revit e procure a aba **JFTools**.

O instalador copia o plugin para `C:\ProgramData\JFTools\` e registra o `.addin` em cada versão do Revit (2022 a 2027) encontrada no computador. Se você instalar uma versão nova do Revit depois, abra-a uma vez e rode o instalador de novo. Para desinstalar, use **Configurações → Aplicativos** do Windows.

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
- **NWC**: a API do Revit para exportação NWC não oferece opção de cores; as cores vêm dos materiais e da aparência do modelo. Algumas opções do exportador ainda não estão na interface. Exige o *Navisworks NWC Export Utility* instalado.
- **Excel ⇄ Revit** (em desenvolvimento, desativado): a ideia é ler um intervalo nomeado de uma planilha e reproduzi-lo no Revit com a mesma aparência, e fazer o caminho inverso. O código está no repositório, mas o botão não aparece no ribbon até a ferramenta funcionar corretamente.

Relatos e sugestões são bem-vindos nas [issues](https://github.com/Eng-JulioFerreira/jftools/issues).

## Para desenvolvedores

### Compilar

Pré-requisito: [SDK do .NET 10](https://dotnet.microsoft.com/download). Não é preciso ter o Revit instalado: a API vem dos pacotes de referência [Nice3point.Revit.Api](https://github.com/Nice3point/RevitApi).

O projeto gera uma DLL por faixa de Revit, escolhida pela propriedade `RevitVersion`:

| Comando | Framework | Revit |
|---|---|---|
| `dotnet build JFTools -c Release -p:RevitVersion=2022` | .NET Framework 4.8 | 2022, 2023, 2024 |
| `dotnet build JFTools -c Release -p:RevitVersion=2025` | .NET 8 | 2025, 2026 |
| `dotnet build JFTools -c Release -p:RevitVersion=2027` | .NET 10 | 2027 |

A DLL sai em `JFTools/bin/Release/R<versão>/`. Também dá para compilar contra a API de uma versão intermediária (por exemplo `-p:RevitVersion=2026`) só para conferir compatibilidade.

Para testar sem reinstalar, sobrescreva `C:\ProgramData\JFTools\R<versão>\JFTools.dll` com a DLL compilada e reinicie o Revit.

### Gerar o instalador

1. Rode `.\build.ps1` na raiz do repositório. Ele compila as três versões e copia as DLLs para `Installer/R2022`, `Installer/R2025` e `Installer/R2027`.
2. Se o [Inno Setup 6](https://jrsoftware.org/isinfo.php) estiver instalado, o script já gera `Installer/Output/JFTools-Setup.exe`. Senão, compile `Installer/JFTools-Setup.iss` manualmente.

### Publicar uma versão

O GitHub Actions ([release.yml](.github/workflows/release.yml)) faz tudo sozinho: adicione a seção da versão no [CHANGELOG.md](CHANGELOG.md) e envie uma tag.

```
git tag v2.0.0
git push origin v2.0.0
```

O workflow compila as três versões, gera o instalador e publica o release com o `JFTools-Setup.exe` anexado e as notas do CHANGELOG. Pela aba **Actions → Build e release → Run workflow** dá para só gerar o instalador, sem publicar.

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
│   ├── Compat.cs                 ← diferenças entre .NET Framework 4.8 e .NET 8/10
│   └── L.cs                      ← todos os textos da interface (PT/EN)
└── Views/                        ← janelas WPF (principal, revisões, nomenclatura, Excel)
Installer/JFTools-Setup.iss       ← script do instalador
build.ps1                         ← compila as três versões e prepara o instalador
revocation.txt                    ← mantido vazio: versões antigas (pagas) consultam este arquivo
```

Detalhes técnicos de cada módulo estão em [CLAUDE.md](CLAUDE.md). O histórico de versões está em [CHANGELOG.md](CHANGELOG.md).

## Suporte

Encontrou um problema ou tem uma sugestão? Abra uma [issue](https://github.com/Eng-JulioFerreira/jftools/issues).

## Licença

Código aberto. Veja o arquivo [LICENSE](LICENSE).

---

Desenvolvido por [Eng. Júlio Ferreira](https://eng-julioferreira.com.br).
