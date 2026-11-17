namespace JFTools.Helpers
{
    /// <summary>
    /// Localization helper. Call L.Init(ptBR) before creating any Window.
    /// All properties return English when ptBR=false, Brazilian Portuguese when ptBR=true.
    /// </summary>
    public static class L
    {
        private static bool _pt;

        public static void Init(bool ptBR) => _pt = ptBR;
        public static bool IsPtBR => _pt;

        private static string S(string en, string pt) => _pt ? pt : en;
        private static T S<T>(T en, T pt)              => _pt ? pt : en;

        // ── App / Startup ─────────────────────────────────────────────
        public static string AppLoadError   => S("Error loading: ",   "Erro ao carregar: ");
        public static string AppErrorLabel  => S("Error",             "Erro");
        public static string NoDocOpen      => S("No document open.", "Nenhum documento aberto.");

        // ── Ribbon ────────────────────────────────────────────────────
        public static string RibbonPanel       => S("Tools",                                           "Ferramentas");
        public static string BtnExportTooltip  => S("Export sheets to PDF, DWG, IFC and NWC",        "Exporta pranchas para PDF, DWG, IFC e NWC");
        public static string BtnExcelTooltip   => S("Export/Import schedule parameters via Excel",    "Exportar/Importar parâmetros de tabelas via Excel");

        // ── Shared ────────────────────────────────────────────────────
        public static string BtnCancel   => S("Cancel",   "Cancelar");
        public static string BtnSave     => S("Save",     "Salvar");
        public static string BtnClose    => S("Close",    "Fechar");
        public static string BtnEdit     => S("Edit",     "Editar");
        public static string BtnDelete   => S("Delete",   "Excluir");
        public static string BtnBrowse   => S("Browse",   "Procurar");
        public static string BtnAdd      => S("Add",      "Adicionar");
        public static string BtnSet      => S("Set",      "Definir");
        public static string AllProjects => S("All Projects", "Todos os Projetos");

        // ── MainWindow ────────────────────────────────────────────────
        public static string MainTitle        => S("Export Sheets",    "Exportar Pranchas");
        public static string TabSelection     => S("Selection",        "Seleção");
        public static string TabExportOpts    => S("Export Options",   "Opções de Exportação");
        public static string TabOutput        => S("Output",           "Saída");

        // Selection tab
        public static string SearchPlaceholder => S("Search by number or name...", "Buscar por número ou nome...");
        public static string BtnSelectAll      => S("Select All",     "Selecionar Todos");
        public static string BtnDeselectAll    => S("Deselect All",   "Desmarcar Todos");
        public static string BtnAddRevision    => S("＋ Add Revision", "＋ Adicionar Rev.");
        public static string LblSelected       => S(" selected",      " selecionado(s)");
        public static string LblProjects       => S("Projects:",      "Projetos:");
        public static string BtnAddProject     => S("＋ Add",          "＋ Adicionar");
        public static string BtnRemoveProject  => S("✕ Remove",        "✕ Remover");

        // Column headers (sort arrow added dynamically by code)
        public static string ColNumber      => S("Number",      "Número");
        public static string ColNumberAsc   => S("Number ▲",    "Número ▲");
        public static string ColSheetName   => S("Sheet Name",  "Nome da Prancha");
        public static string ColProject     => S("Project",     "Projeto");
        public static string ColRev         => S("Rev.",        "Rev.");
        public static string ColRevDate     => S("Rev. Date",   "Data Rev.");
        public static string ColDescription => S("Description", "Descrição");

        // Format cards
        public static string PerSheet   => S("per sheet",   "por prancha");
        public static string PerProject => S("per project", "por projeto");

        // PDF panel
        public static string LblHiddenLines  => S("Hidden Lines",        "Linhas Ocultas");
        public static string RadioVector     => S("Vector Processing",    "Processamento Vetorial");
        public static string RadioRaster     => S("Raster Processing",    "Processamento Raster");
        public static string LblRasterQuality => S("Raster Quality",     "Qualidade Raster");
        public static string LblColorMode    => S("Color Mode",           "Modo de Cor");
        public static string LblHide         => S("Hide",                 "Ocultar");
        public static string ChkRef          => S("Ref / work planes",    "Planos de ref. / trabalho");
        public static string ChkTags         => S("Unreferenced view tags", "Marcações de vista não referenciadas");
        public static string ChkScope        => S("Scope boxes",          "Caixas de escopo");
        public static string ChkCrop         => S("Crop boundaries",      "Limites de corte");
        public static string LblOutputPdf    => S("Output",               "Saída");
        public static string ChkCombinePdf   => S("Combine into single PDF", "Combinar em PDF único");
        public static string LblFilename     => S("Filename",             "Nome do arquivo");
        public static string DefaultPdfName  => S("Sheets",               "Pranchas");

        // DWG panel – setup selector
        public static string LblExportSetup  => S("Export Setup",  "Config. de Exportação");
        public static string BtnPerProject   => S("✎ Per Project", "✎ Por Projeto");
        public static string DwgPerProjTip   => S("Configure a different DWG setup for each project", "Configurar uma configuração DWG diferente para cada projeto");
        public static string ChkBindOle      => S("Bind images as OLE (requires AutoCAD)", "Incorporar imagens como OLE (requer AutoCAD)");
        public static string BindOleTip      => S("After export, opens each DWG in AutoCAD and converts raster image attachments into embedded OLE objects. AutoCAD 2019 or later must be installed.",
                                                   "Após exportar, abre cada DWG no AutoCAD e converte imagens raster em objetos OLE incorporados. Requer AutoCAD 2019 ou superior instalado.");

        // DWG panel – section group headers (match Revit's "Modify DWG/DXF Export Setup" dialog)
        public static string GrpDwgGeneral     => S("General",                 "Geral");
        public static string GrpDwgColors      => S("Colors",                  "Cores");
        public static string GrpDwgTextFonts   => S("Text and Fonts",          "Texto e Fontes");
        public static string GrpDwgSolids      => S("Solids",                  "Sólidos");
        public static string GrpDwgUnitsCoords => S("Units and Coordinates",   "Unidades e Coordenadas");
        public static string GrpDwgLines       => S("Lines",                   "Linhas");


        // DWG panel – combo labels
        public static string LblDwgVersion      => S("AutoCAD file version:",   "Versão do arquivo AutoCAD:");
        public static string LblDwgNonplotSuffix => S("Non-plot suffix:",        "Sufixo de não plotagem:");
        public static string LblDwgACA           => S("ACA objects:",            "Objetos ACA:");
        public static string LblDwgPropOverrides => S("Layer property overrides:", "Substit. de prop. de camada:");
        public static string LblDwgUnits         => S("Units:",                  "Unidades:");

        // DWG panel – checkboxes
        public static string ChkDwgAreas        => S("Export rooms and areas as polylines", "Exportar ambientes e áreas como polilinhas");
        public static string ChkDwgCoincident   => S("Preserve coincident lines",           "Preservar linhas coincidentes");
        public static string ChkDwgNonplot      => S("Mark non-plot layers",                "Marcar camadas de não plotagem");
        public static string ChkDwgHideRef      => S("Hide reference/work planes",          "Ocultar planos de referência/trabalho");
        public static string ChkDwgHideScope    => S("Hide scope boxes",                    "Ocultar caixas de escopo");
        public static string ChkDwgHideTags     => S("Hide unreferenced view tags",         "Ocultar marcações de vista não referenciadas");

        // DWG panel – view export checkbox (MergedViews = !IsChecked)
        public static string ChkDwgViewXRef     => S("Export views on sheets and links as external references (XREF)", "Exportar vistas e vínculos como referências externas (XREF)");
        // Colors (ExportColorMode)
        public static string RadDwgColorTrue    => S("True color",            "Cor verdadeira");
        public static string RadDwgColorIndex   => S("Indexed colors (256)",  "Cores indexadas (256)");
        public static string RadDwgColorPerView => S("True color per view",   "Cor verdadeira por vista");
        // Text treatment (TextTreatment)
        public static string RadDwgTextExact    => S("Preserve as text entities",       "Preservar como entidades de texto");
        public static string RadDwgTextApprox   => S("Approximate with font geometry",  "Aproximar com geometria de fonte");
        // Solids (SolidGeometry)
        public static string RadDwgSolidsPolymesh => S("Polymesh",       "Polimalha");
        public static string RadDwgSolidsACIS     => S("ACIS solids",    "Sólidos ACIS");
        // Coordinate system (SharedCoords)
        public static string RadDwgCoordProject => S("Project internal", "Interno do projeto");
        public static string RadDwgCoordShared  => S("Shared",           "Compartilhado");
        // Line scaling (LineScaling)
        public static string RadDwgLineViewScale => S("Scaled lineweights",                "Espessuras em escala");
        public static string RadDwgLineModel     => S("Modelspace (constant lineweights)", "Espaço do modelo (espessuras constantes)");
        public static string RadDwgLinePaper     => S("Paperspace (constant lineweights)", "Espaço do papel (espessuras constantes)");

        // DWG panel – combos that stay as combos (version, units, ACA, layer overrides)
        public static string[] DwgVersionItems => S(
            new[] { "R2018  (AutoCAD 2018–2025)", "R2013  (AutoCAD 2013–2017)",
                    "R2010  (AutoCAD 2010–2012)", "R2007  (AutoCAD 2007–2009)" },
            new[] { "R2018  (AutoCAD 2018–2025)", "R2013  (AutoCAD 2013–2017)",
                    "R2010  (AutoCAD 2010–2012)", "R2007  (AutoCAD 2007–2009)" });
        public static string[] DwgUnitsItems => S(
            new[] { "Default", "Foot", "Inch", "Millimeter", "Centimeter", "Meter" },
            new[] { "Padrão",  "Pé",   "Polegada", "Milímetro", "Centímetro", "Metro" });
        public static string[] DwgACAItems => S(
            new[] { "Export as AutoCAD Architecture Objects", "Export as AutoCAD Geometry" },
            new[] { "Exportar como Objetos AutoCAD Architecture", "Exportar como Geometria AutoCAD" });
        public static string[] DwgPropOverridesItems => S(
            new[] { "By object", "By layer", "New layer" },
            new[] { "Por objeto", "Por camada", "Nova camada" });

        // IFC – tabs
        public static string TabIfcBasic    => S("Basic Settings", "Configurações Básicas");
        public static string TabIfcAdvanced => S("Advanced",       "Avançado");

        // IFC – GroupBoxes
        public static string GrpGeneral    => S("General",            "Geral");
        public static string GrpAdditional => S("Additional Content", "Conteúdo Adicional");
        public static string GrpLOD        => S("Level of Detail",    "Nível de Detalhe");
        public static string GrpPropSets   => S("Property Sets",      "Conjuntos de Propriedades");

        // IFC – General
        public static string LblIfcVersion   => S("IFC Version",      "Versão IFC");
        public static string LblFileType     => S("File Type",        "Tipo de Arquivo");
        public static string LblSpaceBound   => S("Space Boundaries", "Limites de Espaço");
        public static string ChkSplitWalls   => S("Split walls, columns and ducts by level", "Dividir paredes, colunas e dutos por nível");
        public static string ChkSteelElem    => S("Include steel elements", "Incluir elementos de aço");

        // IFC – Additional Content
        public static string LblLinkedFiles  => S("Linked files",  "Arq. vinculados");
        public static string ChkExport2D     => S("Export 2D plan view elements",        "Exportar elementos de planta 2D");
        public static string ChkVisibleOnly  => S("Export only elements visible in view", "Exportar somente elementos visíveis na vista");
        public static string ChkRooms        => S("Export rooms in 3D view",             "Exportar ambientes na vista 3D");

        // IFC – Level of Detail
        public static string LblTessellation => S("Tessellation", "Tesselação");
        public static string TessExtraLow    => S("Extra Low",    "Muito Baixo");
        public static string TessLow         => S("Low",          "Baixo");
        public static string TessMedium      => S("Medium",       "Médio");
        public static string TessHigh        => S("High",         "Alto");
        public static string TessExtraHigh   => S("Extra High",   "Muito Alto");

        // IFC – Property Sets
        public static string ChkRevitPsets   => S("Export Revit property sets",           "Exportar conjuntos de propriedades do Revit");
        public static string ChkCommonPsets  => S("Export IFC common property sets",      "Exportar conjuntos de propriedades comuns IFC");
        public static string ChkBaseQty      => S("Export base quantities",               "Exportar quantidades base");
        public static string ChkSchedPsets   => S("Export schedules as property sets",    "Exportar tabelas como conjuntos de propriedades");
        public static string ChkMatPsets     => S("Export material property sets",        "Exportar conjuntos de propriedades de material");
        public static string ChkUserPsets    => S("Export user-defined property sets",    "Exportar conjuntos de propriedades personalizados");
        public static string ChkParamMap     => S("Export parameter mapping table",       "Exportar tabela de mapeamento de parâmetros");

        // IFC – Advanced
        public static string ChkIfcParts     => S("Export parts as building elements",                    "Exportar partes como elementos construtivos");
        public static string ChkIfcSolid     => S("Allow use of mixed Solid Model representation",        "Permitir representação de Modelo Sólido misto");
        public static string ChkIfcActView   => S("Use active view when creating geometry",               "Usar vista ativa ao criar geometria");
        public static string ChkIfcFamType   => S("Use family and type name for reference",               "Usar nome de família e tipo para referência");
        public static string ChkIfc2DRoom    => S("Use 2D room boundaries for room volume",               "Usar limites 2D de ambiente para volume");
        public static string ChkIfcSiteElev  => S("Include IFCSITE elevation in the site local placement origin", "Incluir elevação do IFCSITE na origem de posicionamento local do terreno");
        public static string ChkIfcGUID      => S("Store the IFC GUID in an element parameter after export", "Armazenar o GUID IFC em parâmetro de elemento após exportação");
        public static string ChkIfcTriangle  => S("Keep tessellated geometry as triangulation",           "Manter geometria tesselada como triangulação");
        public static string ChkIfcTypeName  => S("Use type name only for IFCType name",                  "Usar apenas o nome do tipo para IFCType");
        public static string ChkIfcVisName   => S("Use visible Revit name as IFCEntity name",             "Usar nome visível do Revit como nome IFCEntity");
        public static string LblSitePlacement => S("Site Placement",      "Posic. do Terreno");
        public static string LblExchangeReq   => S("Exchange Requirement","Req. de Intercâmbio");

        // Output tab – Export Settings Profile
        public static string GrpExportProfile       => S("Export Profile",             "Perfil de Exportação");
        public static string BtnSaveExportProfile   => S("💾", "💾");
        public static string TipSaveExportProfile   => S("Save current settings to profile", "Salvar configurações atuais no perfil");
        public static string TipEditExportProfiles  => S("Edit profiles", "Editar perfis");
        public static string MsgExportProfileName   => S("Profile name:", "Nome do perfil:");
        public static string MsgExportProfileExists => S("A profile with that name already exists. Overwrite it?", "Já existe um perfil com esse nome. Sobrescrever?");
        public static string MsgConfirmDeleteExportProfile => S("Delete profile \"{0}\"?", "Excluir perfil \"{0}\"?");
        public static string MsgOverwriteProfile    => S("Overwrite profile \"{0}\"?\n\nYes = overwrite   ·   No = save as new profile", "Sobrescrever perfil \"{0}\"?\n\nSim = sobrescrever   ·   Não = salvar como novo perfil");
        public static string ExportProfileSummary(string formats, string naming) =>
            S($"Formats: {formats}   ·   Naming: {naming}", $"Formatos: {formats}   ·   Nomenclatura: {naming}");
        public static string NoneLower => S("none", "nenhum");
        public static string ExportProfileEditorHeader => S("Manage export profiles. \"New from current\" captures all settings from the main window.", "Gerencie os perfis de exportação. \"Novo (config. atual)\" captura todas as configurações da janela principal.");

        // Output tab
        public static string GrpNamingProfile => S("Naming Profile",  "Perfil de Nomenclatura");
        public static string BtnRefresh        => S("Refresh",         "Atualizar");
        public static string LblPreview        => S("Preview  ",       "Pré-visualização  ");
        public static string GrpDestination    => S("Destination",     "Destino");
        public static string ChkSubfolders     => S("Subfolders by format  ( /PDF   /DWG )",    "Subpastas por formato  ( /PDF   /DWG )");
        public static string ChkProjFolders    => S("Subfolders by project  ( /Project Name )", "Subpastas por projeto  ( /Nome do Projeto )");
        public static string BtnExportSheets   => S("Export Sheets",   "Exportar Pranchas");

        // MainWindow – combo items (populated in code-behind)
        public static string[] QualityItems  => S(new[] { "Low", "Medium", "High", "Presentation" },
                                                   new[] { "Baixa", "Média", "Alta", "Apresentação" });
        public static string[] ColorItems    => S(new[] { "Color", "Grayscale", "Black & White" },
                                                   new[] { "Colorido", "Escala de Cinza", "Preto e Branco" });
        public static string[] IfcSpaceBoundaryItems => S(
            new[] { "None (0)", "1st Level (1)", "2nd Level (2)" },
            new[] { "Nenhum (0)", "1º Nível (1)", "2º Nível (2)" });
        public static string[] IfcLinkedItems => S(
            new[] { "Do not export linked files", "Export each linked model as a separate IFC file" },
            new[] { "Não exportar arquivos vinculados", "Exportar cada modelo vinculado como arquivo IFC separado" });

        // MainWindow – messages
        public static string MsgSelectFormat => S("Select at least one format.",   "Selecione pelo menos um formato.");
        public static string MsgNoSheets     => S("No sheets selected.",           "Nenhuma prancha selecionada.");
        public static string MsgSelectFolder => S("Select a destination folder.",  "Selecione uma pasta de destino.");
        public static string MsgNoOtherProj  => S("No other open project documents found.", "Nenhum outro documento de projeto aberto.");
        public static string TitleExport          => S("Export",                          "Exportação");
        public static string TitleAddProject      => S("Add Project",                     "Adicionar Projeto");
        public static string TitleExportCancelled => S("Export Cancelled",                "Exportação Cancelada");
        public static string TitleExportSuccess   => S("Export — Success",                "Exportação — Sucesso");
        public static string TitleExportErrors    => S("Export — Completed with Errors",  "Exportação — Concluída com Erros");

        public static string MsgExportCancelled(int processed, int total) =>
            S($"Export cancelled.\n{processed} of {total} sheet(s) processed.",
              $"Exportação cancelada.\n{processed} de {total} prancha(s) processada(s).");
        public static string MsgExportSuccess(int count) =>
            S($"{count} sheet(s) exported successfully.",
              $"{count} prancha(s) exportada(s) com sucesso.");
        public static string MsgExportErrors(int processed, int errCount) =>
            S($"{processed} sheet(s) processed, {errCount} error(s):",
              $"{processed} prancha(s) processada(s), {errCount} erro(s):");

        // ExportHelper – per-item error messages
        public static string ErrFileNotCreated  => S("Revit did not create the file. Check that the output folder exists and is writable.",
                                                     "O Revit não criou o arquivo. Verifique se a pasta de destino existe e permite gravação.");
        public static string ErrDwgNotCreated   => S("Revit did not create the file. Check the DWG export setup and output folder.",
                                                     "O Revit não criou o arquivo. Verifique a configuração de exportação DWG e a pasta de destino.");
        public static string ErrSheetInvalid(string sheet) =>
            S($"Sheet {sheet}: the sheet element is no longer valid or was deleted in Revit.",
              $"Prancha {sheet}: o elemento da prancha não é mais válido ou foi excluído no Revit.");

        // ProjectPickerDialog (in MainWindow.cs)
        public static string TitleAddProjects    => S("Add Projects",    "Adicionar Projetos");
        public static string TitleRemoveProjects => S("Remove Projects", "Remover Projetos");
        public static string LblSelectToAdd      => S("Select projects to add:",    "Selecione os projetos a adicionar:");
        public static string LblSelectToRemove   => S("Select projects to remove:", "Selecione os projetos a remover:");
        public static string ColProjectPicker    => S("Project", "Projeto");
        public static string BtnRemove           => S("Remove",  "Remover");

        // DwgPerProjectDialog (in MainWindow.cs)
        public static string DwgPerProjTitle  => S("DWG Export Setup per Project",             "Configuração de Exportação DWG por Projeto");
        public static string DwgPerProjHeader => S("Select the DWG export setup for each project:", "Selecione a configuração de exportação DWG para cada projeto:");

        // ── ExcelDialog ───────────────────────────────────────────────
        public static string ExcelTitle      => S("Excel — Parameter Export / Import", "Excel — Exportar / Importar Parâmetros");
        public static string TabExcelExport  => S("Export",  "Exportar");
        public static string TabExcelImport  => S("Import",  "Importar");
        public static string GrpProject      => S("Project", "Projeto");
        public static string LblProjectColon => S("Project:", "Projeto:");
        public static string GrpSchedule     => S("Schedule", "Tabela");
        public static string LblSelectSched  => S("Select one schedule to export:", "Selecione uma tabela para exportar:");
        public static string GrpDestFile     => S("Destination File", "Arquivo de Destino");
        public static string ExcelNote       => S(
            "The exported file includes a UniqueId column used to match elements on import. Do not delete or edit that column.",
            "O arquivo exportado inclui uma coluna UniqueId usada para identificar os elementos na importação. Não exclua nem edite essa coluna.");
        public static string BtnExportLabel  => S("Export",  "Exportar");
        public static string GrpExcelFile    => S("Excel File",           "Arquivo Excel");
        public static string GrpWorksheet    => S("Schedule (worksheet)", "Tabela (planilha)");
        public static string LblSheetColon   => S("Sheet:",   "Planilha:");
        public static string GrpTargetProj   => S("Target Project", "Projeto de Destino");
        public static string GrpResults      => S("Results",  "Resultados");
        public static string BtnImportLabel  => S("Import",   "Importar");

        // Excel messages
        public static string MsgSelectSchedule   => S("Select a schedule.",       "Selecione uma tabela.");
        public static string MsgSelectDestFile   => S("Select a destination file.", "Selecione um arquivo de destino.");
        public static string MsgSelectExcelFile  => S("Select an Excel file.",    "Selecione um arquivo Excel.");
        public static string MsgSelectWorksheet  => S("Select a worksheet.",      "Selecione uma planilha.");
        public static string MsgSelectTargetProj => S("Select a target project.", "Selecione um projeto de destino.");
        public static string MsgSchedListError   => S("Error listing schedules:\n",   "Erro ao listar tabelas:\n");
        public static string MsgExcelExportError => S("Export error:\n",              "Erro ao exportar:\n");
        public static string MsgExcelReadError   => S("Error reading file:\n",        "Erro ao ler o arquivo:\n");
        public static string MsgExcelImportError => S("Import error:\n",              "Erro ao importar:\n");
        public static string MsgImportDone       => S("Done.",  "Concluído.");
        public static string TitleExcelExport    => S("JFTools — Export", "JFTools — Exportar");
        public static string TitleExcelImport    => S("JFTools — Import", "JFTools — Importar");
        public static string SaveExcelTitle      => S("Save Excel file",                "Salvar arquivo Excel");
        public static string OpenExcelTitle      => S("Open exported schedule workbook","Abrir planilha exportada");

        public static string MsgExcelExportSuccess(string path) =>
            S($"Schedule exported successfully to:\n{path}\n\nEdit the parameter values in Excel, then use the Import tab to apply the changes back to Revit.",
              $"Tabela exportada com sucesso para:\n{path}\n\nEdite os valores dos parâmetros no Excel e use a aba Importar para aplicar as alterações de volta ao Revit.");

        // ── RevisionEditorDialog ──────────────────────────────────────
        public static string RevTitle         => S("Edit Revisions",   "Editar Revisões");
        public static string LblProjFilter    => S("Project filter:",  "Filtro de projeto:");
        public static string LblEditRemNote   => S("  Edit and Remove affect only the filtered project.", "  Editar e Remover afetam somente o projeto filtrado.");
        public static string ColRevNum        => S("#",           "#");
        public static string ColRevDateShort  => S("Date",        "Data");
        public static string ColRevDesc       => S("Description", "Descrição");
        public static string ColRevIssuedBy   => S("Issued By",   "Emitido Por");
        public static string ColRevIssuedTo   => S("Issued To",   "Emitido Para");
        public static string ColRevProject    => S("Project",     "Projeto");
        public static string ColRevSheets     => S("Sheets",      "Pranchas");
        public static string BtnRevAdd        => S("＋ Add",       "＋ Adicionar");
        public static string BtnRevEdit       => S("✎ Edit",      "✎ Editar");
        public static string BtnRevRemove     => S("✕ Remove",    "✕ Remover");
        public static string LblDate          => S("Date",        "Data");
        public static string LblDescription   => S("Description", "Descrição");
        public static string LblIssuedBy      => S("Issued By",   "Emitido Por");
        public static string LblIssuedTo      => S("Issued To",   "Emitido Para");
        public static string LblCreateIn      => S("Create in:",  "Criar em:");
        public static string RadioAllProj     => S("All projects",          "Todos os projetos");
        public static string RadioFilteredProj => S("Filtered project only", "Somente o projeto filtrado");
        public static string ChkApplySheets   => S("Apply to selected sheets", "Aplicar às pranchas selecionadas");

        // Revision messages
        public static string MsgSelectRevToEdit   => S("Select a revision to edit.",   "Selecione uma revisão para editar.");
        public static string MsgSelectRevToRemove => S("Select a revision to remove.", "Selecione uma revisão para remover.");
        public static string MsgRevErrors         => S("Errors:\n", "Erros:\n");
        public static string MsgRevError          => S("Error:\n",  "Erro:\n");

        // ── NamingProfileEditorDialog ─────────────────────────────────
        public static string NamingTitle       => S("Edit Naming Profiles",   "Editar Perfis de Nomenclatura");
        public static string LblProfileColon   => S("Profile:",               "Perfil:");
        public static string BtnNew            => S("+ New",            "+ Novo");
        public static string BtnNewFromCurrent => S("+ New from current", "+ Novo (config. atual)");
        public static string BtnDuplicate      => S("Duplicate",   "Duplicar");
        public static string BtnRename         => S("Rename",      "Renomear");
        public static string GrpAvailParams    => S("Available Parameters",   "Parâmetros Disponíveis");
        public static string TabSheet          => S("Sheet",       "Prancha");
        public static string TabProjectNaming  => S("Project",     "Projeto");
        public static string BtnAddArrow       => S("Add →",       "Adicionar →");
        public static string GrpNamingPattern  => S("Naming Pattern",         "Padrão de Nomenclatura");
        public static string BtnRemoveToken    => S("✕ Remove",    "✕ Remover");
        public static string LblQuickSep       => S("Quick separators:",      "Separadores rápidos:");
        public static string TxtPatternLabel   => S("Pattern  ",              "Padrão  ");
        public static string TxtPreviewLabel   => S("Preview  ",              "Pré-visualização  ");
        public static string TooltipSpace      => S("Space",       "Espaço");

        // Naming profile messages
        public static string MsgProfileExists   => S("Profile already exists.", "Este perfil já existe.");
        public static string TitleNewProfile    => S("New Profile",      "Novo Perfil");
        public static string TitleRenameProfile => S("Rename Profile",   "Renomear Perfil");
        public static string TitleDuplProfile   => S("Duplicate Profile","Duplicar Perfil");
        public static string TitleDelProfile    => S("Delete",            "Excluir");
        public static string PromptProfileName  => S("Profile name:",    "Nome do perfil:");
        public static string PromptNewName      => S("New name:",        "Novo nome:");
        public static string PromptDuplName     => S("New profile name:","Nome do novo perfil:");
        public static string CopySuffix         => S(" (copy)",          " (cópia)");

        public static string MsgDeleteProfile(string name) =>
            S($"Delete profile \"{name}\"?", $"Excluir o perfil \"{name}\"?");

        // ── NWC panel ────────────────────────────────────────────────
        public static string LblCoordinates      => S("Coordinates",              "Coordenadas");
        public static string LblNwcParams        => S("Parameters",               "Parâmetros");
        public static string ChkNwcLinks         => S("Export linked files",      "Exportar arquivos vinculados");
        public static string ChkNwcConvertProps  => S("Convert element properties","Converter prop. de elementos");
        public static string ChkNwcDivide        => S("Divide into levels",       "Dividir em níveis");
        public static string[] NwcCoordItems     => S(
            new[] { "Shared Coordinates", "Internal Origin" },
            new[] { "Coord. Compartilhadas", "Origem Interna" });
        public static string[] NwcParamItems     => S(
            new[] { "None", "Model Properties", "All Properties" },
            new[] { "Nenhum", "Prop. do Modelo", "Todas as Prop." });

        // IFC – Import / Save
        public static string BtnImportIfcSettings => S("Import Settings", "Importar Configurações");
        public static string BtnSaveIfcSettings   => S("Save Settings",   "Salvar Configurações");

        // NWC – tabs
        public static string TabNwcBasic     => S("Basic Settings", "Configurações Básicas");
        public static string TabNwcAdvanced  => S("Advanced",       "Avançado");

        // NWC – Advanced panel
        public static string LblFacetingFactor    => S("Faceting Factor",          "Fator de Facetagem");
        public static string ChkNwcUrls           => S("Export URLs",              "Exportar URLs");
        public static string ChkNwcMissingMat     => S("Find missing materials",   "Localizar materiais ausentes");
        public static string ChkNwcRoomAttr       => S("Export rooms as attribute","Exportar ambientes como atributo");
        public static string ChkNwcRoomGeom       => S("Export room geometry",     "Exportar geometria de ambiente");
        public static string BtnImportNwcSettings => S("Import Settings",          "Importar Configurações");
        public static string BtnSaveNwcSettings   => S("Save Settings",            "Salvar Configurações");

        // ── MainWindow — runtime strings ──────────────────────────
        public static string BtnCancelling   => S("Cancelling...", "Cancelando...");
        public static string MsgSelectOneSheet => S("Select at least one sheet in the list first.", "Selecione pelo menos uma prancha na lista primeiro.");

        public static string MsgExporting(int cur, int tot, string label) =>
            S($"Exporting {cur} of {tot}  —  {label}",
              $"Exportando {cur} de {tot}  —  {label}");
    }
}
