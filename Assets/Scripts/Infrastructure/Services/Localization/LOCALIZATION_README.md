# SpiralingStudio Localization System

## Overview

The SpiralingStudio Localization System provides a centralized, scalable solution for managing multilingual text in your Unity project. It integrates MasterMemory for efficient data storage with I2 Localization for advanced text rendering features (RTL support, parameters, emojis).

## Key Features

- **CSV-based translations** - Easy to manage and edit in spreadsheet applications
- **Primary + Fallback language support** - Graceful handling of missing translations
- **I2 Localization integration** - RTL text, parameterized strings, emoji support
- **Reactive UI components** - Automatic text refresh on language change
- **Content file organization** - Separate translations by category (Common, Narration, UI, etc.)
- **Device locale detection** - Automatically selects user's preferred language
- **Comprehensive error handling** - Missing keys, translations, and invalid languages

## Quick Start

### 1. Setup Configuration

1. Create a `LocalizationConfiguration` asset:
   - Right-click in Project → Create → SpiralingStudio → Localization → LocalizationConfiguration
   - Configure supported languages, fallback language, and content files
   - Place in `Assets/Resources/` folder

2. Assign to `GameLifetimeScope`:
   - Select your GameLifetimeScope GameObject
   - Drag the LocalizationConfiguration asset to the inspector field

### 2. Add Translations

Create CSV files in `Assets/Resources/Localization/`:

```csv
Key,en,es,fr
common_button_text_confirm,Confirm,Confirmar,Confirmer
common_text_hello,"Hello, {0}!","¡Hola, {0}!","Bonjour, {0}!"
```

**CSV Format Requirements:**
- UTF-8 encoding (with or without BOM)
- First column: Key (with content prefix, e.g., `common_`, `narration_`, `ui_`)
- Subsequent columns: Language codes (`en`, `es`, `fr`, etc.)
- Use quotes for text containing commas or parameters

### 3. Use in UI

Add `LocalizedTextTMP` component to any GameObject with TextMeshProUGUI:

```csharp
// In Inspector:
// - Localization Key: "common_button_text_confirm"
// - Localize On Start: true
```

Or via code:

```csharp
var localizedText = GetComponent<LocalizedTextTMP>();
localizedText.SetKey("common_text_hello");
localizedText.SetParameters("Player");
```

### 4. Change Language

```csharp
// Inject the service
[Inject] private MFLocalizationService _localizationService;

// Switch language
_localizationService.SetLanguage("es"); // Spanish
_localizationService.SetLanguage("en"); // English
```

## Architecture

### Components

1. **LocalizationConfiguration** (ScriptableObject)
   - Defines supported languages and settings
   - Configures fallback behavior and debug options

2. **MFLocalizationService** (IMFService)
   - Main service managing translations
   - Loads CSV data, handles language switching
   - Provides translation fetching with fallback logic

3. **LocalizationI2Bridge** (Internal)
   - Integrates with I2 Localization
   - Populates I2's LanguageSourceData at runtime
   - Enables RTL, parameters, and advanced features

4. **LocalizedTextTMP** (MonoBehaviour)
   - UI component for reactive localized text
   - Automatically updates on language change
   - Supports parameters and runtime key changes

5. **RefreshLanguageEvent** (Event)
   - Published when language changes
   - All LocalizedTextTMP components auto-refresh

### Data Flow

1. Game starts → MFLocalizationService initializes
2. CSV files loaded → Parsed into memory
3. Device locale detected → Primary language selected
4. I2 populated → Advanced features enabled
5. LocalizedTextTMP components → Subscribe to events
6. Language change → Event published → All UI refreshes

## Key Naming Convention

Use prefixed flat keys for organization:

```
{content_file}_{category}_{type}_{identifier}

Examples:
- common_button_text_confirm
- common_button_text_cancel
- narration_tutorial_text_1
- narration_intro_text_welcome
- ui_menu_main_title
- ui_hud_score
```

**Prefixes:**
- `common_` - Shared UI elements, buttons, common messages
- `narration_` - Story text, dialogue, cutscenes
- `ui_` - Menu screens, HUD elements, settings

## Parameterized Text

Use `{0}`, `{1}`, etc. for runtime value replacement:

```csv
Key,en,es
ui_hud_score,Score: {0},Puntuación: {0}
common_text_welcome,"Welcome, {0}!","¡Bienvenido, {0}!"
ui_hud_stats,"{0} HP | {1} MP","{0} PV | {1} PM"
```

Usage:

```csharp
// Method 1: Via service
string text = _localizationService.GetTranslation("ui_hud_score", playerScore);

// Method 2: Via component
localizedText.LocalizeWithParameters(playerScore);

// Method 3: Set parameters array
localizedText.SetParameters(playerHP.ToString(), playerMP.ToString());
```

## RTL Language Support

I2 Localization automatically handles RTL languages (Arabic, Hebrew, Persian):

1. Add RTL language to configuration:
```csharp
new LanguageInfo { 
    languageName = "Arabic", 
    languageCode = "ar", 
    isRightToLeft = true 
}
```

2. Add translations to CSV
3. I2 automatically:
   - Reverses text rendering
   - Adjusts text alignment (right-to-left)
   - Handles mixed LTR/RTL content

## Advanced Features

### Emoji & Special Characters

- Ensure CSV files use UTF-8 encoding
- Verify TMPro font includes required Unicode ranges
- Use TMPro fallback fonts for missing glyphs

Example CSV with emojis:
```csv
Key,en,es
common_text_success,Success! 🎉,¡Éxito! 🎉
common_text_error,Error ❌,Error ❌
```

### Multiple Content Files

Organize translations by game area:

```
Localization_Common.csv      - UI, buttons, messages
Localization_Narration.csv   - Story, dialogue
Localization_UI.csv          - Menus, HUD
Localization_Items.csv       - Item names, descriptions
Localization_Quests.csv      - Quest text
```

Benefits:
- Easier to manage large translation sets
- Multiple translators can work in parallel
- Faster loading if selective loading is implemented

### Debug Mode

LocalizationConfiguration options:

- **Show Keys Instead of Translations** - Display keys for layout testing
- **Show Missing Translation Warnings** - Log when translations are missing
- **Log Key Lookups** - Verbose logging for debugging
- **Missing Key Prefix** - Prefix for missing keys (default: `[MISSING] `)

### Language Fallback Chain

1. Try primary language (e.g., Spanish)
2. If missing → Try fallback language (e.g., English)
3. If still missing → Return key with prefix: `[MISSING] key_name`

Prevents infinite loops with `maxFallbackDepth` setting.

## API Reference

### MFLocalizationService

```csharp
// Get translation
string text = _localizationService.GetTranslation("key");
string text = _localizationService.GetTranslation("key", param1, param2);

// Language management
_localizationService.SetLanguage("es");
string current = _localizationService.GetCurrentLanguage();
string code = _localizationService.GetCurrentLanguageCode();
List<string> all = _localizationService.GetSupportedLanguages();

// Query
bool supported = _localizationService.IsLanguageSupported("fr");
bool ready = _localizationService.IsInitialized();
```

### LocalizedTextTMP

```csharp
// Set key
localizedText.LocalizationKey = "new_key";
localizedText.SetKey("new_key");

// Parameters
localizedText.SetParameters("value1", "value2");
localizedText.LocalizeWithParameters(value1, value2);

// Manual refresh
localizedText.Localize();

// Clear
localizedText.Clear();
```

## Best Practices

1. **Use consistent key naming** - Follow the prefix convention
2. **Keep translations short** - Consider UI space constraints
3. **Test with longest language** - German/French are typically longest
4. **Use parameters sparingly** - Too many make translation difficult
5. **Provide context** - Add comments in CSV or separate documentation
6. **Handle plurals** - Use I2's plural syntax for number-dependent text
7. **Test RTL languages** - Ensure layout works for Arabic/Hebrew
8. **Version control CSVs** - Track translation changes over time
9. **Separate by release** - Use content files to organize by game version

## Troubleshooting

### Text not localizing

- Check LocalizationConfiguration is assigned in GameLifetimeScope
- Verify CSV file is in Resources/Localization folder
- Confirm key exists in CSV and matches exactly (case-sensitive)
- Check Console for initialization errors

### Missing translation warnings

- Verify language code in CSV matches configuration
- Check for typos in key names
- Ensure fallback language has all keys

### Parameters not working

- Confirm parameter syntax: `{0}`, `{1}`, etc.
- Check parameter count matches in GetTranslation call
- Verify useParameters is enabled on LocalizedTextTMP

### Language not switching

- Ensure RefreshLanguageEvent is registered in EventRegistrationHelper
- Check LocalizedTextTMP components are subscribed to events
- Verify new language code is in supportedLanguages list

## Future Enhancements

Potential improvements:

- **Dynamic loading** - Load languages on-demand to reduce memory
- **Asset bundle support** - Download translations at runtime
- **Plural forms** - Leverage I2's plural system more deeply
- **Gender support** - Use I2 specializations for gendered languages
- **Context variants** - Different translations based on game state
- **String pooling** - Reduce GC pressure with string caching
- **Editor tools** - Visual key browser, missing translation finder
- **Auto-generation** - Generate LocalizationKeys.cs with constants

## Credits

- **I2 Localization** - Advanced text rendering, RTL support
- **MasterMemory** - Efficient in-memory database
- **UniTask** - Async/await for Unity
- **MessagePipe** - Event system
- **VContainer** - Dependency injection

---

**For questions or issues, consult the development team or check the inline code documentation.**




