using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MdfLens.Services;

namespace MdfLens;

public partial class SettingsWindow : Window
{
    private string _changelogPath = "";

    public SettingsWindow()
    {
        InitializeComponent();

        ThemeManager.Instance.ThemeChanged += OnThemeChanged;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;

        Closed += (s, e) =>
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        };

        UpdateThemeUI();
        UpdateLanguageUI();
        LoadChangelog();
    }

    private void OnThemeChanged()
    {
        Dispatcher.Invoke(UpdateThemeUI);
    }

    private void OnLanguageChanged()
    {
        Dispatcher.Invoke(UpdateLanguageUI);
    }

    private void UpdateThemeUI()
    {
        var isLight = ThemeManager.Instance.CurrentTheme == AppTheme.Light;
        var accentBrush = (Brush)FindResource("ThemeAccent");
        var borderBrush = (Brush)FindResource("ThemeBorder");

        CardThemeLight.BorderBrush = isLight ? accentBrush : borderBrush;
        CardThemeLight.BorderThickness = isLight ? new Thickness(2) : new Thickness(1.5);
        BadgeLightActive.Visibility = isLight ? Visibility.Visible : Visibility.Collapsed;

        CardThemeDark.BorderBrush = !isLight ? accentBrush : borderBrush;
        CardThemeDark.BorderThickness = !isLight ? new Thickness(2) : new Thickness(1.5);
        BadgeDarkActive.Visibility = !isLight ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateLanguageUI()
    {
        var loc = LocalizationService.Instance;
        var isEn = loc.CurrentLanguage == AppLanguage.English;
        var accentBrush = (Brush)FindResource("ThemeAccent");
        var borderBrush = (Brush)FindResource("ThemeBorder");

        Title = $"{loc.Get("SettingsTitle")} — MDF Lens";
        TxtSettingsHeader.Text = loc.Get("SettingsTitle");

        TxtSecAppearance.Text = loc.Get("AppearanceSection");
        TxtThemeLightSub.Text = loc.Get("ThemeLightDesc");
        TxtThemeDarkSub.Text = loc.Get("ThemeDarkDesc");

        TxtSecLanguage.Text = loc.Get("LanguageSection");
        TxtLangEnSub.Text = loc.Get("LangEnDesc");
        TxtLangEsSub.Text = loc.Get("LangEsDesc");

        CardLangEn.BorderBrush = isEn ? accentBrush : borderBrush;
        CardLangEn.BorderThickness = isEn ? new Thickness(2) : new Thickness(1.5);
        BadgeLangEnActive.Visibility = isEn ? Visibility.Visible : Visibility.Collapsed;

        CardLangEs.BorderBrush = !isEn ? accentBrush : borderBrush;
        CardLangEs.BorderThickness = !isEn ? new Thickness(2) : new Thickness(1.5);
        BadgeLangEsActive.Visibility = !isEn ? Visibility.Visible : Visibility.Collapsed;

        TxtSecAbout.Text = loc.Get("AboutSection");
        TxtAboutDesc.Text = loc.Get("AboutDesc");
        LblVer.Text = loc.Get("VersionLabel");
        LblArch.Text = loc.Get("ArchitectureLabel");
        LblEngine.Text = loc.Get("EngineLabel");

        TxtSecChangelog.Text = loc.Get("ChangelogSection");
        TxtBtnOpenChangelog.Text = loc.Get("BtnOpenChangelogFile");
        BtnCloseDialog.Content = loc.Get("BtnClose");
    }

    private void LoadChangelog()
    {
        try
        {
            // Search in output directory, application directory, and repository root
            string[] possiblePaths =
            [
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG.md"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "CHANGELOG.md"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "CHANGELOG.md"),
                @"C:\Google-Code\MDF\CHANGELOG.md"
            ];

            foreach (var p in possiblePaths)
            {
                var full = Path.GetFullPath(p);
                if (File.Exists(full))
                {
                    _changelogPath = full;
                    TxtChangelogContent.Text = File.ReadAllText(full);
                    return;
                }
            }

            // Fallback default changelog
            TxtChangelogContent.Text = 
@"# MDF Lens — Release History

## [v1.2.0] — 2026-10-11
- Integrated Settings & About modal window with dual-theme, bilingual language and changelog.
- Replaced default window icon with custom high-resolution MDF Lens icon.
- Restyled all ComboBox dropdowns with dark-mode compatibility and vector icons.
- Added 4-Column Studio Layout (Activity Bar, Database Explorer, Studio Workspace, Table Inspector).
- Added direct binary page 0/9 metadata decoding and full SQL Studio.

## [v1.1.0] — 2026-10-11
- Rebranded to MDF Lens.
- LocalDB auto-detection and instance creation.
- Multi-format data export (CSV, TSV, JSON, SQL).

## [v1.0.0] — 2026-10-10
- Initial release.";
        }
        catch (Exception ex)
        {
            TxtChangelogContent.Text = $"Error reading changelog: {ex.Message}";
        }
    }

    private void CardThemeLight_Click(object sender, MouseButtonEventArgs e)
    {
        ThemeManager.Instance.ApplyTheme(AppTheme.Light);
    }

    private void CardThemeDark_Click(object sender, MouseButtonEventArgs e)
    {
        ThemeManager.Instance.ApplyTheme(AppTheme.Dark);
    }

    private void CardLangEn_Click(object sender, MouseButtonEventArgs e)
    {
        LocalizationService.Instance.SetLanguage(AppLanguage.English);
    }

    private void CardLangEs_Click(object sender, MouseButtonEventArgs e)
    {
        LocalizationService.Instance.SetLanguage(AppLanguage.Spanish);
    }

    private void BtnOpenChangelogFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(_changelogPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _changelogPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("CHANGELOG.md file not found on disk.", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open file: {ex.Message}", "MDF Lens", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCloseDialog_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
