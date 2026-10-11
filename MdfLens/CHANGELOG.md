# MDF Lens — Changelog / Registro de Cambios

## [v1.2.0] — 2026-10-11
### Added & Improved
- **Integrated Settings & About Dialog**:
  - Centralized preferences for Theme (Light / Dark) and Language (English / Spanish).
  - Detailed About view with version details, architecture (.NET 8 WPF, LocalDB engine), and build information.
  - Interactive Changelog viewer showing release history and recent updates.
- **Custom Application Icon**:
  - Replaced default Windows/WPF icon with high-resolution MDF Lens icon (Database & Lens iconography) in title bar, taskbar, and executable.
- **Enhanced Dark Mode UI**:
  - Fully restyled all ComboBox dropdowns (Language, Theme, Columns, Page Size) with high-contrast borders and backgrounds, resolving visibility issues in dark mode.
  - Modernized top bar action indicators with crisp vector icons.
- **4-Column Studio Layout**:
  - Activity Bar (Home, Tables, Query, Binary Header, Export, Settings).
  - Database Explorer with quick search, filter, and file metadata card.
  - Studio Workspace with Data Grid, Schema & Columns, SQL Studio, and Binary Header tabs.
  - Table & File Inspector sidebar with real-time statistics.
- **Dual-Theme Engine**:
  - Crisp modern Light mode (default) and Deep Slate Dark mode with instant runtime switching.
- **Bilingual Interface**:
  - Full English (default) and Spanish localization across all UI elements and messages.

## [v1.1.0] — 2026-10-11
### Rebranding & Engine
- Renamed project to **MDF Lens**.
- Integrated automatic Microsoft SQL Server LocalDB detection and instance provisioning.
- Added direct binary header decoding for Page 0 (File Header) and Page 9 (DBINFO Boot Page).
- Added multi-format data export (CSV, TSV, JSON, SQL).

## [v1.0.0] — 2026-10-10
### Initial Release
- Standalone viewer for Microsoft SQL Server .mdf database files.
- Table schema inspection and row data browsing.
