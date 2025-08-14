# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)

## [1.3.0] - 2025-08-01
### Added
- A dedicated setting to toggle between displaying shortcut numbers or full scene names.
- Proper section headers in the settings menu for better organization.

### Changed
- **BREAKING:** The package namespace has been changed to `Ineedmypills.Utils` to reflect the fork and prevent conflicts.
- **BREAKING:** The `EditorPrefs` key for settings has been updated to avoid conflicts with the original package.
- The toolbar integration has been completely refactored to use the modern UIElements API for Unity 2021.1+, ensuring future compatibility and stability.
- The "Show shortcut names" option is now enabled by default for a better user experience.
- The settings menu layout has been improved for clarity.
- Updated `README.md` with modern installation instructions and a full feature list.
- Updated `package.json` with a more descriptive summary, license, and relevant keywords.

### Removed
- All non-essential code comments have been removed for a cleaner codebase.

## [1.2.0] - 2025-07-31
### Fixed
- Support for Unity 2021.

## [1.1.1] - 2021-03-16
### Fixed
- Clean scene name after restore.

## [1.1.0] - 2020-08-29
### Added
- Restore scene after exit play mode.
- Support for Unity 2020.1.

### Changed
- Shortcut name is now displayed as tooltip.

## [1.0.1] - 2019-06-12
This is the first release of *Scene Inspector* package via Unity Package Manager.

### Added
- Create new scene option.
- Pin scene shortcut to toolbar.

### Changed
- UX tweaks
