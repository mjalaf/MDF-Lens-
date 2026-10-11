using System;
using System.Collections.Generic;

namespace MdfLens.Services;

public enum AppLanguage
{
    English,
    Spanish
}

public class LocalizationService
{
    public static LocalizationService Instance { get; } = new();

    // Default to English as requested
    public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

    public event Action? LanguageChanged;

    private readonly Dictionary<string, (string En, string Es)> _translations = new()
    {
        // Top App Bar
        ["AppTagline"] = ("MDF Database Inspector & Studio", "Inspector y Visor de Bases de Datos MDF"),
        ["NoFileLoadedPill"] = ("No file loaded | Open an MDF file to begin", "Sin archivo cargado | Abre un archivo MDF para comenzar"),
        ["DatabaseLoadedPill"] = ("Database loaded", "Base de datos cargada"),
        ["LangLabel"] = ("EN (English)", "ES (Español)"),
        ["ThemeLight"] = ("Light", "Claro"),
        ["ThemeDark"] = ("Dark", "Oscuro"),

        // Group Labels
        ["GroupDatabaseFile"] = ("Database & File", "Base de datos y Archivo"),
        ["GroupFile"] = ("File", "Archivo"),
        ["GroupDatabase"] = ("Database", "Base de datos"),
        ["GroupQueryExport"] = ("Query & Export", "Consulta y Exportación"),
        ["GroupConnection"] = ("Connection", "Conexión"),

        // Sub-Toolbar Buttons
        ["OpenMdf"] = ("Open MDF", "Abrir Archivo MDF"),
        ["AttachDetectDbs"] = ("Attach / Detect DBs", "Bases Detectadas"),
        ["Refresh"] = ("Refresh", "Actualizar"),
        ["RunQueryBtn"] = ("Run Query", "Consola SQL"),
        ["Export"] = ("Export", "Exportar"),
        ["DetachCloseDb"] = ("Detach / Close DB", "Cerrar Base"),
        ["ExportCsv"] = ("Export to CSV (.csv)", "Exportar a CSV (.csv)"),
        ["ExportTsv"] = ("Export to Excel / TSV (.tsv)", "Exportar a Excel / TSV (.tsv)"),
        ["ExportJson"] = ("Export to JSON (.json)", "Exportar a JSON (.json)"),
        ["ExportSql"] = ("Export as SQL Script (.sql)", "Exportar a Script SQL (.sql)"),

        // Far-Left Activity Bar
        ["NavHome"] = ("Home", "Inicio"),
        ["NavTables"] = ("Tables", "Tablas"),
        ["NavQuery"] = ("Query", "Consulta"),
        ["NavHeader"] = ("Binary Header", "Encabezado Binario"),
        ["NavExport"] = ("Export", "Exportar"),
        ["NavSettings"] = ("Settings", "Configuración"),

        // Left Sidebar: Database Explorer
        ["ExplorerTitle"] = ("Database Explorer", "Explorador de Base de Datos"),
        ["SearchTables"] = ("Search tables...", "Filtrar tablas..."),
        ["ShowViews"] = ("Include Views & System Objects", "Incluir Vistas y Objetos del Sistema"),
        ["NoTablesAvailable"] = ("No tables available", "No hay tablas disponibles"),
        ["NoTablesSub"] = ("Open an MDF file to see database tables.", "Abre un archivo MDF para ver las tablas de la base de datos."),
        ["FileMetadataTitle"] = ("FILE METADATA", "METADATOS DEL ARCHIVO"),
        ["NoFileLoaded"] = ("No file loaded", "Ningún archivo cargado"),
        ["SelectMdfHint"] = ("Open or drag an .mdf file", "Abre o arrastra un archivo .mdf"),

        // Center Workspace Header
        ["SelectTablePrompt"] = ("Select a table from the sidebar", "Selecciona una tabla del panel lateral"),
        ["OpenMdfPrompt"] = ("Open an MDF file to inspect records.", "Abre un archivo MDF para inspeccionar registros."),
        ["AllColumns"] = ("(All Columns)", "(Todas las columnas)"),
        ["FilterPlaceholder"] = ("Type value to filter...", "Escribe un valor para filtrar..."),
        ["SearchBtn"] = ("Filter", "Filtrar"),
        ["ClearBtn"] = ("Clear", "Limpiar"),
        ["ColumnsBtn"] = ("Columns", "Columnas"),
        ["RowsBadge"] = ("rows", "filas"),
        ["ColumnsBadge"] = ("columns", "columnas"),
        ["SchemaBadge"] = ("Schema", "Esquema"),

        // Workspace Tabs
        ["TabData"] = ("Data Grid", "Registros"),
        ["TabColumns"] = ("Schema & Columns", "Estructura y Tipos"),
        ["TabQuery"] = ("SQL Studio", "Consola SQL"),
        ["TabHeader"] = ("Binary Header", "Encabezado Binario"),

        // Columns Grid
        ["ColNum"] = ("#", "#"),
        ["ColName"] = ("Column Name", "Nombre de Columna"),
        ["ColType"] = ("Data Type", "Tipo de Dato"),
        ["ColLength"] = ("Max Length", "Longitud Máx"),
        ["ColNullable"] = ("Nullable", "Nulable"),
        ["ColPk"] = ("Primary Key", "Clave Primaria"),
        ["ColIdentity"] = ("Identity", "Autoincrement"),
        ["ColDefault"] = ("Default Value", "Valor por Defecto"),

        // SQL Studio
        ["RunQuery"] = ("Execute Query (F5)", "Ejecutar Consulta (F5)"),
        ["QueryTip"] = ("Run custom SELECT queries against the database", "Ejecuta consultas SELECT sobre la base de datos montada"),
        ["QueryStatusReady"] = ("Ready to execute", "Listo para ejecutar"),

        // Binary Header Tab
        ["HeaderTitle"] = ("Binary MDF File Analysis", "Análisis Binario del Archivo MDF"),
        ["HeaderSub"] = ("Decoded directly from Page 0 (File Header) & Page 9 (DBINFO Boot Page)", "Decodificación directa de Páginas 0 (Encabezado) y 9 (Boot DBINFO)"),
        ["LblFileName"] = ("File Name:", "Nombre de Archivo:"),
        ["LblDbName"] = ("Internal DB Name:", "Nombre Interno BD:"),
        ["LblSqlVersion"] = ("SQL Server Version:", "Versión de SQL Server:"),
        ["LblInternalVersion"] = ("Internal Build Version:", "Versión Interna (Build):"),
        ["LblFileSize"] = ("File Size:", "Tamaño en Disco:"),
        ["LblTotalPages"] = ("Total 8KB Pages:", "Total de Páginas (8 KB):"),
        ["LblCompat"] = ("Engine Compatibility:", "Compatibilidad de Motor:"),
        ["PageNote"] = ("MDF files store data in fixed 8,192-byte pages. This studio extracts their internal structure and utilizes LocalDB for high-speed browsing.", "Los archivos MDF organizan sus datos en páginas fijas de 8.192 bytes. Este visor extrae su arquitectura interna y utiliza LocalDB para consulta interactiva."),

        // Right Inspector Panel
        ["InspectorTitle"] = ("Table Details", "Detalles de tabla"),
        ["InspectorFileTitle"] = ("File / Database Details", "Detalles de archivo/base"),
        ["InspectFileName"] = ("File Name", "Nombre de archivo"),
        ["InspectEngine"] = ("Engine", "Motor"),
        ["InspectDatabase"] = ("Database", "Base de datos"),
        ["InspectFileSize"] = ("File Size", "Tamaño de archivo"),
        ["InspectTables"] = ("Tables", "Tablas"),
        ["InspectRowsTotal"] = ("Total Records", "Registros (total)"),
        ["InspectLastModified"] = ("Last Modified", "Última modificación"),
        ["InspectTableName"] = ("Table Name", "Nombre de tabla"),
        ["InspectSchema"] = ("Schema", "Esquema"),
        ["InspectObjectType"] = ("Object Type", "Tipo de objeto"),
        ["InspectRowCount"] = ("Row Count", "Cantidad de filas"),
        ["InspectColumnsCount"] = ("Columns", "Columnas"),
        ["InspectCreated"] = ("Created", "Creado"),

        // Welcome / Empty State
        ["WelcomeTitle"] = ("Start exploring your database", "Comienza a explorar tus datos"),
        ["WelcomeSubtitle"] = ("Open any .mdf file from your disk or drag & drop it directly into this window.", "Abre cualquier archivo .mdf desde el disco o arrástralo directamente a esta ventana."),
        ["BrowseBtn"] = ("Browse MDF File...", "Examinar Archivo MDF..."),
        ["DropHereHint"] = ("Or drag and drop an .mdf file here\nto open it directly", "O arrastra y suelta aquí un archivo .mdf\npara abrirlo directamente"),

        // Pagination
        ["Page"] = ("Page", "Página"),
        ["Of"] = ("of", "de"),
        ["RowsPerPage"] = ("rows per page", "filas por página"),
        ["ShowingRecords"] = ("Showing {0} - {1} of {2} records", "Mostrando {0} - {1} de {2} registros"),

        // Status Bar
        ["StatusReady"] = ("Ready", "Listo"),
        ["EngineReady"] = ("Microsoft SQL Server LocalDB Active", "Microsoft SQL Server LocalDB Activo"),
        ["EngineInactive"] = ("LocalDB Engine Inactive", "Motor LocalDB Inactivo"),

        // Settings & About Window
        ["SettingsTitle"] = ("Settings & About", "Configuración y Acerca de"),
        ["AppearanceSection"] = ("Appearance & Theme", "Apariencia y Tema"),
        ["ThemeLightDesc"] = ("Crisp, modern light aesthetic", "Interfaz clara, moderna y limpia"),
        ["ThemeDarkDesc"] = ("Deep slate high-contrast dark aesthetic", "Interfaz oscura pizarra de alto contraste"),
        ["LanguageSection"] = ("Language & Localization", "Idioma y Regionalización"),
        ["LangEnDesc"] = ("English (Default)", "Inglés (Por defecto)"),
        ["LangEsDesc"] = ("Spanish (Español)", "Español"),
        ["AboutSection"] = ("About MDF Lens", "Acerca de MDF Lens"),
        ["AboutDesc"] = ("High-performance SQL Server (.mdf) database inspector and studio. Easily open, inspect, query, and export databases without needing a full SQL Server instance.", "Inspector y estudio de alto rendimiento para bases de datos SQL Server (.mdf). Abre, inspecciona, consulta y exporta bases de datos fácilmente sin requerir una instancia completa de SQL Server."),
        ["VersionLabel"] = ("Version:", "Versión:"),
        ["ArchitectureLabel"] = ("Architecture:", "Arquitectura:"),
        ["EngineLabel"] = ("Engine:", "Motor:"),
        ["ChangelogSection"] = ("Recent Changes & Release Notes", "Registro de Cambios y Notas de Versión"),
        ["BtnClose"] = ("Close", "Cerrar"),
        ["BtnOpenChangelogFile"] = ("Open CHANGELOG.md", "Abrir archivo CHANGELOG.md")
    };

    public string Get(string key)
    {
        if (_translations.TryGetValue(key, out var val))
        {
            return CurrentLanguage == AppLanguage.English ? val.En : val.Es;
        }
        return key;
    }

    public void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }

    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == AppLanguage.English ? AppLanguage.Spanish : AppLanguage.English);
    }
}
