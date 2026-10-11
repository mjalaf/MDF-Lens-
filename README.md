# MDF Lens Studio for Windows

**MDF Lens** is a high-performance, modern Microsoft SQL Server Primary Data File (`.mdf`) inspector and database viewer built with **WPF (.NET 8)** for Windows.

Designed with a developer-first, studio layout inspired by modern database tools (DataGrip, TablePlus), MDF Lens allows you to inspect, browse, and export SQL Server database files without friction.

---

## Key Features

### 1. Studio Multi-Panel Layout
* **Activity Bar (Far-Left)**: Fast switching between `Home`, `Tables`, `Query Studio`, `Binary Header`, `Export`, and `Settings`.
* **Database Explorer (Left Sidebar)**: Searchable list of database tables and views with row count indicators, plus an integrated `FILE METADATA` summary.
* **Central Studio Workspace**:
  * **Header Breadcrumbs & Stat Pills**: Table name, schema, record counts, and column counts.
  * **Unified Filtering**: Filter by specific columns or across `(All Columns)` with real-time feedback.
  * **Segmented Tabs**:
    * **Data Grid**: Virtualized high-speed table browsing with formatted dates, numbers, and pagination.
    * **Schema & Columns**: Full column definitions, SQL data types, max lengths, nullability, primary key, identity, and defaults.
    * **SQL Studio**: Custom `SELECT` query console with syntax execution and execution time profiling (F5).
    * **Binary Header**: Low-level 8KB page architecture inspection.
* **Inspector Panel (Right Sidebar)**: Real-time metadata for selected tables and databases (Object type, Row count, Engine, Database, File size, Timestamps).

### 2. Grouped Action Toolbar
* **Database & File**: `Open MDF ▾` (file picker and quick access), `Attach / Detect DBs ▾` (one-click detection of databases on the PC), and `Refresh`.
* **Query & Export**: `Run Query` shortcut and `Export ▾` (CSV, TSV/Excel, JSON, SQL INSERT scripts).
* **Connection**: `Detach / Close DB` (safely detaches temporary instances and cleans up locks).

### 3. Dual Theme & Internationalization (Bilingual)
* **Themes**: Instant toggle between **Light** (crisp white modern UI) and **Dark** (deep slate studio mode) without reloading.
* **Languages**: Seamless on-the-fly switching between **English (EN)** and **Spanish (ES)** across all menus, tooltips, dialogs, and headers.
* **Default Settings**: Starts by default in **English** with the **Light (White)** theme.

### 4. Direct Binary Page Inspection (Offline)
* Directly parses **Page 0 (File Header)** and **Page 9 (Boot DBINFO Page)** from the raw MDF binary without requiring an active SQL Server instance:
  * Detects internal SQL Server edition (SQL 7.0, 2000, 2005, 2008, 2008 R2, 2012, 2014, 2016, 2017, 2019, 2022).
  * Extracts internal database name and total 8KB page counts.
  * Evaluates engine compatibility before mounting.

### 5. Microsoft SQL Server LocalDB Integration
* Leverages the native **LocalDB** instance (`MSSQLLocalDB`) installed on Windows.
* Auto-detects databases already attached or dynamically mounts detached `.mdf` files via safe attach procedures (`FOR ATTACH` / `FOR ATTACH_REBUILD_LOG`).

---

## Project Structure

```
C:\Google-Code\MDF\
├── README.md                      # English documentation
├── Run_MdfLens.cmd                # Double-click launcher
├── MDF Lens.lnk                   # Desktop / Workspace shortcut
└── MdfLens/
    ├── MdfLens.csproj             # .NET 8 WPF project targeting win-x64
    ├── App.xaml / App.xaml.cs     # Startup configuration & unhandled exception logging
    ├── MainWindow.xaml / .cs      # Modern 4-panel studio UI and business logic
    ├── Models/
    │   ├── MdfHeaderInfo.cs       # Binary header metadata model
    │   ├── TableItem.cs           # Database table entity & formatted row counter
    │   └── ColumnInfo.cs          # Column definitions & SQL constraints model
    ├── SettingsWindow.xaml / .cs  # Centralized Settings, About & Changelog dialog
    ├── app_icon.ico / app_icon.png# High-resolution application icon
    ├── CHANGELOG.md               # Version release notes and change history
    └── Services/
        ├── MdfHeaderParser.cs     # Direct binary page decoder (Page 0 & Page 9)
        ├── SqlServerService.cs    # LocalDB attachment manager & paginated query engine
        ├── ThemeManager.cs        # Dynamic Light & Dark theme manager
        ├── LocalizationService.cs # Real-time English & Spanish dictionary provider
        └── ExportService.cs       # Multi-format data exporter (CSV, TSV, JSON, SQL)
```

---

## How to Run MDF Lens

### Option 1: Desktop Shortcut
Double-click the **MDF Lens** shortcut on your Desktop:
```
C:\Users\mjala\Desktop\MDF Lens.lnk
```

### Option 2: Run Launcher
Double-click `Run_MdfLens.cmd` in the project root:
```powershell
start C:\Google-Code\MDF\Run_MdfLens.cmd
```

### Option 3: Direct Executable
```powershell
C:\Google-Code\MDF\MdfLens\bin\Release\net8.0-windows\win-x64\publish\MdfLens.exe
```

### Option 4: From Command Line (.NET CLI)
```powershell
dotnet run --project C:\Google-Code\MDF\MdfLens
```

Or open a specific file directly on launch:
```powershell
C:\Google-Code\MDF\MdfLens\bin\Release\net8.0-windows\win-x64\publish\MdfLens.exe "C:\Path\To\Database.mdf"
```
