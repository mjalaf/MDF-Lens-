using System;
using System.Windows;
using System.Windows.Media;

namespace MdfLens.Services;

public enum AppTheme
{
    Light,
    Dark
}

public class ThemeManager
{
    public static ThemeManager Instance { get; } = new();

    // Default to Light as requested
    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public event Action? ThemeChanged;

    public void ApplyTheme(AppTheme theme)
    {
        CurrentTheme = theme;
        var dict = Application.Current.Resources;

        if (theme == AppTheme.Light)
        {
            // Crisp, modern white aesthetic matching mockups
            dict["ThemeWindowBg"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));    // #F8FAFC
            dict["ThemeHeaderBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));    // #FFFFFF
            dict["ThemeActivityBarBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // #FFFFFF
            dict["ThemeSidebarBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));   // #FFFFFF
            dict["ThemeCardBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));      // #FFFFFF
            dict["ThemeContentBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));   // #FFFFFF
            dict["ThemeBorder"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));      // #E2E8F0
            dict["ThemeBorderSubtle"] = new SolidColorBrush(Color.FromRgb(241, 245, 249)); // #F1F5F9
            dict["ThemeTextPrimary"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));    // #0F172A
            dict["ThemeTextSecondary"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));  // #475569
            dict["ThemeTextMuted"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));   // #94A3B8
            
            // Primary vibrant blue
            dict["ThemeAccent"] = new SolidColorBrush(Color.FromRgb(0, 102, 255));        // #0066FF
            dict["ThemeAccentSoft"] = new SolidColorBrush(Color.FromArgb(28, 0, 102, 255)); // #EBF3FF
            dict["ThemeAccentHover"] = new SolidColorBrush(Color.FromRgb(0, 82, 204));
            dict["ThemeAccentText"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            
            // Danger button
            dict["ThemeDangerBg"] = new SolidColorBrush(Color.FromRgb(254, 242, 242));    // #FEF2F2
            dict["ThemeDangerBorder"] = new SolidColorBrush(Color.FromRgb(254, 202, 202));// #FECACA
            dict["ThemeDangerText"] = new SolidColorBrush(Color.FromRgb(239, 68, 68));    // #EF4444

            dict["ThemeGridHeaderBg"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
            dict["ThemeGridRowBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));   // #FFFFFF
            dict["ThemeGridRowAltBg"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
            dict["ThemeGridLines"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));   // #F1F5F9
            dict["ThemeInputBg"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));     // #FFFFFF
            dict["ThemeCodeBg"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));      // #F8FAFC
            dict["ThemeBadgeBg"] = new SolidColorBrush(Color.FromRgb(239, 246, 255));     // #EFF6FF
            dict["ThemeBadgeText"] = new SolidColorBrush(Color.FromRgb(0, 102, 255));
        }
        else
        {
            // Dark Mode - Deep Slate / Studio Theme
            dict["ThemeWindowBg"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));       // #0F172A
            dict["ThemeHeaderBg"] = new SolidColorBrush(Color.FromRgb(2, 6, 23));         // #020617
            dict["ThemeActivityBarBg"] = new SolidColorBrush(Color.FromRgb(2, 6, 23));    // #020617
            dict["ThemeSidebarBg"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));      // #0F172A
            dict["ThemeCardBg"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));         // #1E293B
            dict["ThemeContentBg"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));      // #0F172A
            dict["ThemeBorder"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));         // #334155
            dict["ThemeBorderSubtle"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));   // #1E293B
            dict["ThemeTextPrimary"] = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC
            dict["ThemeTextSecondary"] = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
            dict["ThemeTextMuted"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));   // #64748B
            dict["ThemeAccent"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));       // #38BDF8
            dict["ThemeAccentSoft"] = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248));
            dict["ThemeAccentHover"] = new SolidColorBrush(Color.FromRgb(14, 165, 233));
            dict["ThemeAccentText"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            
            dict["ThemeDangerBg"] = new SolidColorBrush(Color.FromRgb(69, 10, 10));       // #450A0A
            dict["ThemeDangerBorder"] = new SolidColorBrush(Color.FromRgb(153, 27, 27));  // #991B1B
            dict["ThemeDangerText"] = new SolidColorBrush(Color.FromRgb(248, 113, 113));  // #F87171

            dict["ThemeGridHeaderBg"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));   // #1E293B
            dict["ThemeGridRowBg"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));      // #0F172A
            dict["ThemeGridRowAltBg"] = new SolidColorBrush(Color.FromRgb(20, 30, 50));   // #141E32
            dict["ThemeGridLines"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));      // #1E293B
            dict["ThemeInputBg"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));        // #1E293B
            dict["ThemeCodeBg"] = new SolidColorBrush(Color.FromRgb(2, 6, 23));           // #020617
            dict["ThemeBadgeBg"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));        // #1E293B
            dict["ThemeBadgeText"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        }

        ThemeChanged?.Invoke();
    }

    public void ToggleTheme()
    {
        ApplyTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
    }
}
