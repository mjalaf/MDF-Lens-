using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using MdfLens.Models;
using MdfLens.Services;

namespace MdfLens;

public partial class MainWindow : Window
{
    private readonly SqlServerService _sqlService = new();
    private MdfHeaderInfo? _currentHeader;
    private List<TableItem> _allTables = new();
    private TableItem? _selectedTable;
    private List<ColumnInfo> _currentColumns = new();
    private DataTable? _currentTableData;

    private int _currentPage = 1;
    private int _pageSize = 50;
    private long _totalRows = 0;
    private int _totalPages = 1;

    public MainWindow()
    {
        InitializeComponent();

        // 1. Defaults: English language and Light (white) theme
        ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        ThemeManager.Instance.ApplyTheme(AppTheme.Light);

        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        LocalizationService.Instance.SetLanguage(AppLanguage.English);

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            bool localDbReady = false;
            try
            {
                localDbReady = await SqlServerService.IsLocalDbAvailableAsync();
            }
            catch { }

            Dispatcher.Invoke(() =>
            {
                var loc = LocalizationService.Instance;
                if (localDbReady)
                {
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // green
                    TxtEngineStatus.Text = loc.Get("EngineReady");
                }
                else
                {
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // amber
                    TxtEngineStatus.Text = loc.Get("EngineInactive");
                }
            });
        });

        if (!string.IsNullOrEmpty(App.LoadedFileOnStartup))
        {
            _ = LoadMdfFileAsync(App.LoadedFileOnStartup);
        }
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        await _sqlService.DetachCurrentDatabaseAsync();
        _sqlService.Dispose();
    }

    #region Theme & Language

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbLanguage.SelectedIndex == 0)
        {
            LocalizationService.Instance.SetLanguage(AppLanguage.English);
        }
        else if (CmbLanguage.SelectedIndex == 1)
        {
            LocalizationService.Instance.SetLanguage(AppLanguage.Spanish);
        }
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTheme.SelectedIndex == 0)
        {
            ThemeManager.Instance.ApplyTheme(AppTheme.Light);
        }
        else if (CmbTheme.SelectedIndex == 1)
        {
            ThemeManager.Instance.ApplyTheme(AppTheme.Dark);
        }
    }

    private void OnThemeChanged()
    {
        var isLight = ThemeManager.Instance.CurrentTheme == AppTheme.Light;
        if (PathThemeIcon != null)
        {
            PathThemeIcon.Data = Geometry.Parse(isLight 
                ? "M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" 
                : "M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z");
        }
        if (CmbTheme != null)
        {
            var expectedIndex = isLight ? 0 : 1;
            if (CmbTheme.SelectedIndex != expectedIndex)
            {
                CmbTheme.SelectedIndex = expectedIndex;
            }
        }
    }

    private void OnLanguageChanged()
    {
        UpdateLanguageUI();
        if (_selectedTable != null)
        {
            UpdatePaginationUI();
        }
    }

    private void UpdateLanguageUI()
    {
        var loc = LocalizationService.Instance;

        // Title and Subtitle
        Title = $"MDF Lens — {loc.Get("AppTagline")}";
        TxtAppSubtitle.Text = $" — {loc.Get("AppTagline")}";

        if (TxtTopSettingsBtn != null)
        {
            TxtTopSettingsBtn.Text = loc.Get("NavSettings");
        }
        if (CmbLanguage != null)
        {
            var expectedLangIndex = loc.CurrentLanguage == AppLanguage.English ? 0 : 1;
            if (CmbLanguage.SelectedIndex != expectedLangIndex)
            {
                CmbLanguage.SelectedIndex = expectedLangIndex;
            }
        }

        // Top Breadcrumb Pill
        if (_currentHeader == null)
        {
            TxtTopBreadcrumb.Text = loc.Get("NoFileLoadedPill");
        }
        else
        {
            TxtTopBreadcrumb.Text = $"{loc.Get("DatabaseLoadedPill")} | {_currentHeader.FileName} | {_currentHeader.SqlVersionName}";
        }

        // Sub-Toolbar Group Headers
        TxtGroupDatabaseFile.Text = loc.Get("GroupDatabaseFile");
        TxtGroupQueryExport.Text = loc.Get("GroupQueryExport");
        TxtGroupConnection.Text = loc.Get("GroupConnection");

        // Sub-Toolbar Buttons
        TxtBtnOpenMdf.Text = $"{loc.Get("OpenMdf")} ▾";
        TxtBtnAttachDetect.Text = $"{loc.Get("AttachDetectDbs")} ▾";
        TxtBtnRefresh.Text = loc.Get("Refresh");
        TxtBtnRunQuery.Text = loc.Get("RunQueryBtn");
        TxtBtnExport.Text = $"{loc.Get("Export")} ▾";
        TxtBtnDetachClose.Text = loc.Get("DetachCloseDb");

        MenuCsvItem.Header = loc.Get("ExportCsv");
        MenuTsvItem.Header = loc.Get("ExportTsv");
        MenuJsonItem.Header = loc.Get("ExportJson");
        MenuSqlItem.Header = loc.Get("ExportSql");

        // Activity Bar
        TxtNavHome.Text = loc.Get("NavHome");
        TxtNavTables.Text = loc.Get("NavTables");
        TxtNavQuery.Text = loc.Get("NavQuery");
        TxtNavHeader.Text = loc.Get("NavHeader");
        TxtNavExport.Text = loc.Get("NavExport");
        TxtNavSettings.Text = loc.Get("NavSettings");

        // Left Sidebar: Explorer
        TxtExplorerTitle.Text = loc.Get("ExplorerTitle");
        PlaceholderSearchTables.Text = loc.Get("SearchTables");
        ChkIncludeViews.Content = loc.Get("ShowViews");
        TxtNoTablesAvailable.Text = loc.Get("NoTablesAvailable");
        TxtNoTablesSub.Text = loc.Get("NoTablesSub");
        TxtFileMetaHeader.Text = loc.Get("FileMetadataTitle");

        if (_currentHeader == null)
        {
            TxtDbName.Text = loc.Get("NoFileLoaded");
            TxtFileSubInfo.Text = loc.Get("SelectMdfHint");
        }

        // Central Workspace Header
        if (_selectedTable == null)
        {
            TxtCurrentTableName.Text = loc.Get("SelectTablePrompt");
            TxtCurrentTablePrompt.Text = loc.Get("OpenMdfPrompt");
        }
        PillRowCount.Text = $"{_totalRows:N0} {loc.Get("RowsBadge")}";
        PillColCount.Text = $"{_currentColumns.Count} {loc.Get("ColumnsBadge")}";

        // Tabs
        TxtTabBtnData.Text = loc.Get("TabData");
        TxtTabBtnColumns.Text = loc.Get("TabColumns");
        TxtTabBtnQuery.Text = loc.Get("TabQuery");
        TxtTabBtnBinary.Text = loc.Get("TabHeader");

        // Filter Bar
        PlaceholderFilterValue.Text = loc.Get("FilterPlaceholder");
        TxtBtnFilter.Text = loc.Get("SearchBtn");
        TxtBtnClear.Text = loc.Get("ClearBtn");
        TxtBtnColumnsDropdown.Text = $"{loc.Get("ColumnsBtn")} ▾";

        // Columns Grid
        ColGridNum.Header = loc.Get("ColNum");
        ColGridName.Header = loc.Get("ColName");
        ColGridType.Header = loc.Get("ColType");
        ColGridLength.Header = loc.Get("ColLength");
        ColGridNullable.Header = loc.Get("ColNullable");
        ColGridPk.Header = loc.Get("ColPk");
        ColGridIdentity.Header = loc.Get("ColIdentity");
        ColGridDefault.Header = loc.Get("ColDefault");

        // SQL View
        TxtBtnExecuteQuery.Text = loc.Get("RunQuery");
        TxtQueryTip.Text = loc.Get("QueryTip");

        // Binary Header View
        TxtHeaderTitle.Text = loc.Get("HeaderTitle");
        TxtHeaderSub.Text = loc.Get("HeaderSub");
        LblHdrFile.Text = loc.Get("LblFileName");
        LblHdrDb.Text = loc.Get("LblDbName");
        LblHdrSql.Text = loc.Get("LblSqlVersion");
        LblHdrBuild.Text = loc.Get("LblInternalVersion");
        LblHdrSize.Text = loc.Get("LblFileSize");
        LblHdrPages.Text = loc.Get("LblTotalPages");
        LblHdrCompat.Text = loc.Get("LblCompat");
        TxtPageNote.Text = loc.Get("PageNote");

        // Welcome / Empty Screen
        TxtWelcomeTitle.Text = loc.Get("WelcomeTitle");
        TxtWelcomeSub.Text = loc.Get("WelcomeSubtitle");
        TxtBtnBrowsePrompt.Text = loc.Get("BrowseBtn");
        TxtDropHereHint.Text = loc.Get("DropHereHint");

        // Inspector Labels
        TxtInspectorHeader.Text = loc.Get("InspectorTitle");
        LblInspectTableName.Text = loc.Get("InspectTableName");
        LblInspectSchema.Text = loc.Get("InspectSchema");
        LblInspectObjectType.Text = loc.Get("InspectObjectType");
        LblInspectRowCount.Text = loc.Get("InspectRowCount");
        LblInspectColumns.Text = loc.Get("InspectColumnsCount");
        LblInspectEngine.Text = loc.Get("InspectEngine");
        LblInspectDb.Text = loc.Get("InspectDatabase");
        LblInspectFile.Text = loc.Get("InspectFileName");
        LblInspectSize.Text = loc.Get("InspectFileSize");
        LblInspectCreated.Text = loc.Get("InspectCreated");
        LblInspectModified.Text = loc.Get("InspectLastModified");

        // Pagination
        TxtLblPage.Text = loc.Get("Page");
        TxtStatusBar.Text = loc.Get("StatusReady");
    }

    #endregion

    #region Tab Navigation Switching

    private void SetActiveTab(int tabIndex)
    {
        ViewDataGrid.Visibility = tabIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        ViewColumnsGrid.Visibility = tabIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        ViewQueryStudio.Visibility = tabIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        ViewBinaryHeader.Visibility = tabIndex == 3 ? Visibility.Visible : Visibility.Collapsed;

        TabBtnData.Style = (Style)FindResource(tabIndex == 0 ? "ActivityBarBtnActive" : "ActivityBarBtn");
        TabBtnColumns.Style = (Style)FindResource(tabIndex == 1 ? "ActivityBarBtnActive" : "ActivityBarBtn");
        TabBtnQuery.Style = (Style)FindResource(tabIndex == 2 ? "ActivityBarBtnActive" : "ActivityBarBtn");
        TabBtnBinary.Style = (Style)FindResource(tabIndex == 3 ? "ActivityBarBtnActive" : "ActivityBarBtn");
    }

    private void TabBtnData_Click(object sender, RoutedEventArgs e) => SetActiveTab(0);
    private void TabBtnColumns_Click(object sender, RoutedEventArgs e) => SetActiveTab(1);
    private void TabBtnQuery_Click(object sender, RoutedEventArgs e) => SetActiveTab(2);
    private void TabBtnBinary_Click(object sender, RoutedEventArgs e) => SetActiveTab(3);

    private void NavBtnHome_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(0);
        HighlightNavBtn(NavBtnHome);
    }

    private void NavBtnTables_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(0);
        HighlightNavBtn(NavBtnTables);
    }

    private void NavBtnQuery_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(2);
        HighlightNavBtn(NavBtnQuery);
        TxtSqlQuery.Focus();
    }

    private void NavBtnHeader_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(3);
        HighlightNavBtn(NavBtnHeader);
    }

    private void NavBtnExport_Click(object sender, RoutedEventArgs e)
    {
        BtnExportTable_Click(sender, e);
    }

    private void NavBtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWin = new SettingsWindow
        {
            Owner = this
        };
        settingsWin.ShowDialog();
    }

    private void BtnRunQueryTop_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(2);
        HighlightNavBtn(NavBtnQuery);
        TxtSqlQuery.Focus();
    }

    private void HighlightNavBtn(Button activeBtn)
    {
        NavBtnHome.Style = (Style)FindResource("ActivityBarBtn");
        NavBtnTables.Style = (Style)FindResource("ActivityBarBtn");
        NavBtnQuery.Style = (Style)FindResource("ActivityBarBtn");
        NavBtnHeader.Style = (Style)FindResource("ActivityBarBtn");
        NavBtnExport.Style = (Style)FindResource("ActivityBarBtn");

        activeBtn.Style = (Style)FindResource("ActivityBarBtnActive");
    }

    #endregion

    #region Drag & Drop & Open File

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                await LoadMdfFileAsync(files[0]);
            }
        }
    }

    private async void BtnOpenMdf_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "SQL Server Database (*.mdf)|*.mdf|All files (*.*)|*.*",
            Title = LocalizationService.Instance.CurrentLanguage == AppLanguage.English ? "Select MDF Database File" : "Seleccionar archivo de base de datos MDF"
        };

        if (dlg.ShowDialog(this) == true)
        {
            await LoadMdfFileAsync(dlg.FileName);
        }
    }

    private async void BtnDetectedMdfs_Click(object sender, RoutedEventArgs e)
    {
        MenuDetectedDbs.Items.Clear();

        var dbs = await _sqlService.GetRegisteredDatabasesAsync();
        var loc = LocalizationService.Instance;

        if (dbs.Count == 0)
        {
            var emptyItem = new MenuItem { Header = loc.CurrentLanguage == AppLanguage.English ? "(No databases detected in LocalDB)" : "(No hay bases detectadas en LocalDB)", IsEnabled = false };
            MenuDetectedDbs.Items.Add(emptyItem);
        }
        else
        {
            foreach (var (dbName, filePath) in dbs.Take(30))
            {
                var item = new MenuItem
                {
                    Header = $"{dbName} ({Path.GetFileName(filePath)})",
                    ToolTip = filePath
                };
                item.Click += async (_, _) => await LoadMdfFileAsync(filePath);
                MenuDetectedDbs.Items.Add(item);
            }
        }

        MenuDetectedDbs.PlacementTarget = BtnDetectedMdfs;
        MenuDetectedDbs.IsOpen = true;
    }

    public async Task LoadMdfFileAsync(string filePath)
    {
        OverlayLoading.Visibility = Visibility.Visible;
        var loc = LocalizationService.Instance;
        TxtStatusBar.Text = $"{loc.Get("StatusReady")} — {Path.GetFileName(filePath)}";

        try
        {
            // 1. Binary Header Parse
            _currentHeader = MdfHeaderParser.ParseFile(filePath);

            // Update Header Tab
            TxtInfoFileName.Text = _currentHeader.FileName;
            TxtInfoDbName.Text = _currentHeader.DatabaseName;
            TxtInfoSqlVersion.Text = $"{_currentHeader.SqlVersionName} (Build {_currentHeader.InternalVersion})";
            TxtInfoInternalVersion.Text = _currentHeader.InternalVersion.ToString();
            TxtInfoFileSize.Text = _currentHeader.FileSizeFormatted;
            TxtInfoTotalPages.Text = $"{_currentHeader.TotalPages:N0}";
            TxtInfoCompatible.Text = _currentHeader.IsSupportedByLocalDb ? "LocalDB 2008+" : "Offline Header Mode (< SQL 2008)";

            // Update Sidebar Card
            TxtDbName.Text = _currentHeader.DatabaseName;
            TxtSqlVersionBadge.Text = _currentHeader.SqlVersionName;
            TxtFileSubInfo.Text = $"{_currentHeader.FileName} • {_currentHeader.FileSizeFormatted}";

            // Update Top Breadcrumb
            TopPillDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // green dot
            TxtTopBreadcrumb.Text = $"{loc.Get("DatabaseLoadedPill")} | {_currentHeader.FileName} | {_currentHeader.SqlVersionName}";

            // Update Right Inspector Panel with File details
            ValInspectDb.Text = _currentHeader.DatabaseName;
            ValInspectFile.Text = _currentHeader.FileName;
            ValInspectSize.Text = _currentHeader.FileSizeFormatted;
            ValInspectEngine.Text = "Microsoft SQL Server";

            try
            {
                var fi = new FileInfo(filePath);
                ValInspectCreated.Text = fi.CreationTime.ToString("yyyy-MM-dd HH:mm");
                ValInspectModified.Text = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            }
            catch { }

            // Check if legacy SQL Server version (< 655)
            if (!_currentHeader.IsSupportedByLocalDb)
            {
                string notice = loc.CurrentLanguage == AppLanguage.English
                    ? $"File parsed successfully:\n\n• Database: {_currentHeader.DatabaseName}\n• Version: {_currentHeader.SqlVersionName} (Build {_currentHeader.InternalVersion})\n• Size: {_currentHeader.FileSizeFormatted} ({_currentHeader.TotalPages:N0} pages)\n\nCompatibility Notice:\nMicrosoft SQL Server 2022 (installed on this PC) does not support direct attach for databases older than SQL Server 2008 (build 655).\n\nYou can review all file architecture and metadata in the 'Binary Header' tab."
                    : $"Archivo analizado con éxito:\n\n• Base de Datos: {_currentHeader.DatabaseName}\n• Versión: {_currentHeader.SqlVersionName} (Build {_currentHeader.InternalVersion})\n• Tamaño: {_currentHeader.FileSizeFormatted} ({_currentHeader.TotalPages:N0} páginas)\n\nDetalle de compatibilidad:\nMicrosoft SQL Server 2022 (instalado en este equipo) no permite montar directamente bases de datos anteriores a SQL Server 2008 (versión 655).\n\nPuedes revisar todos los detalles técnicos en la pestaña 'Encabezado Binario'.";

                MessageBox.Show(notice, "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
                SetActiveTab(3);
                BtnClose.IsEnabled = true;
                return;
            }

            // 2. Connect / Attach to LocalDB
            var (success, dbName, message) = await _sqlService.AttachOrConnectAsync(filePath);

            if (success)
            {
                _currentHeader.IsAttached = true;
                _currentHeader.AttachedDatabaseName = dbName;

                _allTables = await _sqlService.GetTablesAsync(dbName);
                SidebarEmptyState.Visibility = _allTables.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                ApplyTableFilter();

                BtnRefresh.IsEnabled = true;
                BtnClose.IsEnabled = true;
                BtnExportTable.IsEnabled = true;
                BtnRunQueryTop.IsEnabled = true;

                TxtStatusBar.Text = $"{dbName} ({_allTables.Count} objects)";

                var firstTable = _allTables.FirstOrDefault(t => !t.IsView) ?? _allTables.FirstOrDefault();
                if (firstTable != null)
                {
                    ListTables.SelectedItem = firstTable;
                }
            }
            else
            {
                TxtStatusBar.Text = message;
                MessageBox.Show($"Header: {_currentHeader.SqlVersionName} ({_currentHeader.DatabaseName})\n\n{message}", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
                SetActiveTab(3);
                BtnClose.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading MDF file: {ex.Message}", "Error Opening MDF", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatusBar.Text = $"Error: {ex.Message}";
        }
        finally
        {
            OverlayLoading.Visibility = Visibility.Collapsed;
        }
    }

    #endregion

    #region Tables & Filter

    private void ApplyTableFilter()
    {
        string filterText = TxtSearchTables.Text.Trim();
        bool showViews = ChkIncludeViews.IsChecked == true;

        var query = _allTables.AsEnumerable();

        if (!showViews)
        {
            query = query.Where(t => !t.IsView);
        }

        if (!string.IsNullOrWhiteSpace(filterText))
        {
            query = query.Where(t => t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                                     t.Schema.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();
        ListTables.ItemsSource = list;
    }

    private void TxtSearchTables_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyTableFilter();
    }

    private void ChkIncludeViews_Changed(object sender, RoutedEventArgs e)
    {
        ApplyTableFilter();
    }

    private async void ListTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListTables.SelectedItem is TableItem item)
        {
            _selectedTable = item;
            _currentPage = 1;
            TxtFilterValue.Text = string.Empty;

            TxtCurrentTableName.Text = item.Name;
            TxtCurrentTableSub.Text = $"[{item.Schema}].[{item.Name}]";
            TxtCurrentTablePrompt.Text = string.Empty;

            // Stat pills
            PillRowCount.Text = item.RowCountFormatted;
            ContainerPillSchema.Visibility = Visibility.Visible;
            PillSchemaName.Text = $"{LocalizationService.Instance.Get("SchemaBadge")}: {item.Schema}";

            // Update Right Inspector Panel
            ValInspectTableName.Text = item.Name;
            ValInspectSchema.Text = item.Schema;
            ValInspectObjectType.Text = item.IsView ? "VIEW" : "USER_TABLE";
            ValInspectRowCount.Text = $"{item.RowCount:N0}";

            TxtSqlQuery.Text = $"SELECT TOP 100 * FROM [{item.Schema}].[{item.Name}];";

            await LoadTableSchemaAndDataAsync();
        }
    }

    private async Task LoadTableSchemaAndDataAsync()
    {
        if (_selectedTable == null || string.IsNullOrEmpty(_sqlService.ActiveDatabaseName))
            return;

        OverlayLoading.Visibility = Visibility.Visible;
        OverlayEmpty.Visibility = Visibility.Collapsed;

        try
        {
            _currentColumns = await _sqlService.GetColumnsAsync(
                _sqlService.ActiveDatabaseName,
                _selectedTable.Schema,
                _selectedTable.Name);

            GridColumns.ItemsSource = _currentColumns;

            // Update Column counts
            ContainerPillCols.Visibility = Visibility.Visible;
            PillColCount.Text = $"{_currentColumns.Count} {LocalizationService.Instance.Get("ColumnsBadge")}";
            ValInspectColumns.Text = _currentColumns.Count.ToString();

            var loc = LocalizationService.Instance;
            var comboItems = new List<string> { loc.Get("AllColumns") };
            comboItems.AddRange(_currentColumns.Select(c => c.ColumnName));
            CmbColumns.ItemsSource = comboItems;
            CmbColumns.SelectedIndex = 0;

            await LoadPageDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading table: {ex.Message}", "Query Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            OverlayLoading.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadPageDataAsync()
    {
        if (_selectedTable == null || string.IsNullOrEmpty(_sqlService.ActiveDatabaseName))
            return;

        OverlayLoading.Visibility = Visibility.Visible;

        try
        {
            string? filterCol = CmbColumns.SelectedItem as string;
            string? filterVal = string.IsNullOrWhiteSpace(TxtFilterValue.Text) ? null : TxtFilterValue.Text.Trim();

            var (dt, total) = await _sqlService.GetTableDataAsync(
                _sqlService.ActiveDatabaseName,
                _selectedTable.Schema,
                _selectedTable.Name,
                _currentPage,
                _pageSize,
                filterCol,
                filterVal,
                _currentColumns);

            _currentTableData = dt;
            _totalRows = total;

            GridData.ItemsSource = _currentTableData.DefaultView;

            _totalPages = (int)Math.Max(1, Math.Ceiling((double)_totalRows / _pageSize));
            if (_currentPage > _totalPages) _currentPage = _totalPages;

            UpdatePaginationUI();

            var loc = LocalizationService.Instance;
            TxtStatusBar.Text = $"[{_selectedTable.Schema}].[{_selectedTable.Name}] — {_totalRows:N0} {loc.Get("RowsBadge")}";
            PillRowCount.Text = $"{_totalRows:N0} {loc.Get("RowsBadge")}";
            ValInspectRowCount.Text = $"{_totalRows:N0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error fetching records: {ex.Message}", "Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            OverlayLoading.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdatePaginationUI()
    {
        var loc = LocalizationService.Instance;
        long startRecord = _totalRows == 0 ? 0 : ((_currentPage - 1) * _pageSize) + 1;
        long endRecord = Math.Min(_currentPage * _pageSize, _totalRows);

        string template = loc.Get("ShowingRecords");
        TxtPaginationSummary.Text = string.Format(template, $"{startRecord:N0}", $"{endRecord:N0}", $"{_totalRows:N0}") + $" | {loc.Get("Page")} {_currentPage} {loc.Get("Of")} {_totalPages}";

        TxtCurrentPageNumber.Text = _currentPage.ToString();
        TxtTotalPagesCount.Text = $"{loc.Get("Of")} {_totalPages}";

        BtnPageFirst.IsEnabled = _currentPage > 1;
        BtnPagePrev.IsEnabled = _currentPage > 1;
        BtnPageNext.IsEnabled = _currentPage < _totalPages;
        BtnPageLast.IsEnabled = _currentPage < _totalPages;
    }

    private async void BtnSearchFilter_Click(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        await LoadPageDataAsync();
    }

    private async void BtnClearFilter_Click(object sender, RoutedEventArgs e)
    {
        TxtFilterValue.Text = string.Empty;
        CmbColumns.SelectedIndex = 0;
        _currentPage = 1;
        await LoadPageDataAsync();
    }

    private async void TxtFilterValue_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _currentPage = 1;
            await LoadPageDataAsync();
        }
    }

    private async void BtnPageFirst_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1) { _currentPage = 1; await LoadPageDataAsync(); }
    }

    private async void BtnPagePrev_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1) { _currentPage--; await LoadPageDataAsync(); }
    }

    private async void BtnPageNext_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages) { _currentPage++; await LoadPageDataAsync(); }
    }

    private async void BtnPageLast_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages) { _currentPage = _totalPages; await LoadPageDataAsync(); }
    }

    private async void TxtCurrentPageNumber_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && int.TryParse(TxtCurrentPageNumber.Text.Trim(), out int page))
        {
            _currentPage = Math.Clamp(page, 1, _totalPages);
            await LoadPageDataAsync();
        }
    }

    private async void CmbPageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPageSize.SelectedItem is ComboBoxItem item && item.Content is string text)
        {
            var numStr = new string(text.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numStr, out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                if (_selectedTable != null)
                {
                    await LoadPageDataAsync();
                }
            }
        }
    }

    private void GridData_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = (e.Row.GetIndex() + 1 + (_currentPage - 1) * _pageSize).ToString();
    }

    private void GridData_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyType == typeof(DateTime) && e.Column is DataGridTextColumn dateCol)
        {
            dateCol.Binding.StringFormat = "yyyy-MM-dd HH:mm:ss";
        }
        else if ((e.PropertyType == typeof(decimal) || e.PropertyType == typeof(double)) && e.Column is DataGridTextColumn numCol)
        {
            numCol.Binding.StringFormat = "N2";
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTable != null)
        {
            await LoadPageDataAsync();
        }
    }

    private async void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        await _sqlService.DetachCurrentDatabaseAsync();

        _allTables.Clear();
        ListTables.ItemsSource = null;
        GridData.ItemsSource = null;
        GridColumns.ItemsSource = null;
        GridQueryResult.ItemsSource = null;

        var loc = LocalizationService.Instance;
        TxtDbName.Text = loc.Get("NoFileLoaded");
        TxtFileSubInfo.Text = loc.Get("SelectMdfHint");
        TxtSqlVersionBadge.Text = "SQL Server";
        TopPillDot.Fill = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        TxtTopBreadcrumb.Text = loc.Get("NoFileLoadedPill");

        TxtCurrentTableName.Text = loc.Get("SelectTablePrompt");
        TxtCurrentTableSub.Text = string.Empty;
        TxtCurrentTablePrompt.Text = loc.Get("OpenMdfPrompt");
        PillRowCount.Text = $"0 {loc.Get("RowsBadge")}";
        ContainerPillCols.Visibility = Visibility.Collapsed;
        ContainerPillSchema.Visibility = Visibility.Collapsed;

        ValInspectTableName.Text = "—";
        ValInspectSchema.Text = "—";
        ValInspectRowCount.Text = "0";
        ValInspectColumns.Text = "0";
        ValInspectDb.Text = "—";
        ValInspectFile.Text = "—";
        ValInspectSize.Text = "—";

        BtnRefresh.IsEnabled = false;
        BtnClose.IsEnabled = false;
        BtnExportTable.IsEnabled = false;
        BtnRunQueryTop.IsEnabled = false;

        SidebarEmptyState.Visibility = Visibility.Visible;
        OverlayEmpty.Visibility = Visibility.Visible;
        SetActiveTab(0);

        TxtStatusBar.Text = loc.Get("StatusReady");
    }

    #endregion

    #region Export Handlers

    private void BtnExportTable_Click(object sender, RoutedEventArgs e)
    {
        BtnExportTable.ContextMenu.PlacementTarget = BtnExportTable;
        BtnExportTable.ContextMenu.IsOpen = true;
    }

    private async void MenuExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTableData == null || _selectedTable == null) return;
        var dlg = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = $"{_selectedTable.Name}_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
        if (dlg.ShowDialog(this) == true)
        {
            await ExportService.ExportToCsvAsync(_currentTableData, dlg.FileName);
            MessageBox.Show($"Exported '{_selectedTable.Name}' to CSV!", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void MenuExportTsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTableData == null || _selectedTable == null) return;
        var dlg = new SaveFileDialog { Filter = "Tab Separated / Excel (*.tsv)|*.tsv", FileName = $"{_selectedTable.Name}_{DateTime.Now:yyyyMMdd_HHmm}.tsv" };
        if (dlg.ShowDialog(this) == true)
        {
            await ExportService.ExportToTsvAsync(_currentTableData, dlg.FileName);
            MessageBox.Show($"Exported '{_selectedTable.Name}' to TSV!", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void MenuExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTableData == null || _selectedTable == null) return;
        var dlg = new SaveFileDialog { Filter = "JSON File (*.json)|*.json", FileName = $"{_selectedTable.Name}_{DateTime.Now:yyyyMMdd_HHmm}.json" };
        if (dlg.ShowDialog(this) == true)
        {
            await ExportService.ExportToJsonAsync(_currentTableData, dlg.FileName);
            MessageBox.Show($"Exported '{_selectedTable.Name}' to JSON!", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void MenuExportSql_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTableData == null || _selectedTable == null) return;
        var dlg = new SaveFileDialog { Filter = "SQL Script (*.sql)|*.sql", FileName = $"{_selectedTable.Name}_insert.sql" };
        if (dlg.ShowDialog(this) == true)
        {
            await ExportService.ExportToSqlScriptAsync(_currentTableData, _selectedTable.FullName, dlg.FileName);
            MessageBox.Show($"Exported '{_selectedTable.Name}' as SQL script!", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region SQL Query Execution

    private async void BtnExecuteQuery_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCustomQueryAsync();
    }

    private async void TxtSqlQuery_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            await ExecuteCustomQueryAsync();
        }
    }

    private async Task ExecuteCustomQueryAsync()
    {
        if (string.IsNullOrEmpty(_sqlService.ActiveDatabaseName))
        {
            var loc = LocalizationService.Instance;
            MessageBox.Show(loc.CurrentLanguage == AppLanguage.English ? "Please open an MDF database first." : "Por favor abre una base de datos MDF primero.", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string sql = TxtSqlQuery.Text.Trim();
        if (string.IsNullOrWhiteSpace(sql)) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        TxtQueryStatus.Text = "Executing query...";

        try
        {
            var dt = await _sqlService.ExecuteQueryAsync(_sqlService.ActiveDatabaseName, sql);
            sw.Stop();

            GridQueryResult.ItemsSource = dt.DefaultView;
            TxtQueryStatus.Text = $"Success in {sw.ElapsedMilliseconds} ms ({dt.Rows.Count:N0} rows returned).";
        }
        catch (Exception ex)
        {
            sw.Stop();
            TxtQueryStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show(ex.Message, "SQL Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}