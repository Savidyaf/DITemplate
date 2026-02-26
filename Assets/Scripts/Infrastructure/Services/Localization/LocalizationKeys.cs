// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
// Generated from localization CSV files
// This file provides compile-time key checking for localization

namespace SpiralingStudio.Services.Localization
{
    /// <summary>
    /// Auto-generated localization key constants.
    /// Provides compile-time checking and IntelliSense support for localization keys.
    /// </summary>
    public static class LocalizationKeys
    {
        #region Common
        
        public static class Common
        {
            // Buttons
            public const string BUTTON_TEXT_CONFIRM = "common_button_text_confirm";
            public const string BUTTON_TEXT_CANCEL = "common_button_text_cancel";
            public const string BUTTON_TEXT_OK = "common_button_text_ok";
            public const string BUTTON_TEXT_YES = "common_button_text_yes";
            public const string BUTTON_TEXT_NO = "common_button_text_no";
            public const string BUTTON_TEXT_START = "common_button_text_start";
            public const string BUTTON_TEXT_CONTINUE = "common_button_text_continue";
            public const string BUTTON_TEXT_EXIT = "common_button_text_exit";
            
            // General Text
            public const string TEXT_HELLO = "common_text_hello";
            public const string TEXT_WELCOME = "common_text_welcome";
            public const string TEXT_LOADING = "common_text_loading";
            public const string TEXT_ERROR = "common_text_error";
            public const string TEXT_SUCCESS = "common_text_success";
            public const string TEXT_WARNING = "common_text_warning";
            public const string TEXT_INFO = "common_text_info";
        }
        
        #endregion
        
        #region Narration
        
        public static class Narration
        {
            // Tutorial
            public const string TUTORIAL_TEXT_1 = "narration_tutorial_text_1";
            public const string TUTORIAL_TEXT_2 = "narration_tutorial_text_2";
            public const string TUTORIAL_TEXT_3 = "narration_tutorial_text_3";
            
            // Intro
            public const string INTRO_TEXT_1 = "narration_intro_text_1";
            public const string INTRO_TEXT_2 = "narration_intro_text_2";
            
            // Ending
            public const string ENDING_TEXT_1 = "narration_ending_text_1";
            public const string ENDING_TEXT_2 = "narration_ending_text_2";
        }
        
        #endregion
        
        #region UI
        
        public static class UI
        {
            // Menu
            public const string MENU_MAIN_TITLE = "ui_menu_main_title";
            public const string MENU_SETTINGS = "ui_menu_settings";
            public const string MENU_OPTIONS = "ui_menu_options";
            public const string MENU_LANGUAGE = "ui_menu_language";
            public const string MENU_AUDIO = "ui_menu_audio";
            public const string MENU_VIDEO = "ui_menu_video";
            public const string MENU_CONTROLS = "ui_menu_controls";
            
            // Settings
            public const string SETTINGS_VOLUME = "ui_settings_volume";
            public const string SETTINGS_MUSIC = "ui_settings_music";
            public const string SETTINGS_SOUND_EFFECTS = "ui_settings_sound_effects";
            public const string SETTINGS_BRIGHTNESS = "ui_settings_brightness";
            public const string SETTINGS_FULLSCREEN = "ui_settings_fullscreen";
            
            // HUD
            public const string HUD_HEALTH = "ui_hud_health";
            public const string HUD_SCORE = "ui_hud_score";
            public const string HUD_LEVEL = "ui_hud_level";
        }
        
        #endregion
        
        /// <summary>
        /// Helper method to validate if a key exists in this class.
        /// Useful for runtime validation.
        /// </summary>
        public static bool IsValidKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            
            // This is a simple implementation. For production, you might want to use reflection
            // or maintain a HashSet of all keys for faster lookup.
            return key.StartsWith("common_") || 
                   key.StartsWith("narration_") || 
                   key.StartsWith("ui_");
        }
        
        /// <summary>
        /// Gets the content file prefix from a key.
        /// </summary>
        public static string GetContentPrefix(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            
            int underscoreIndex = key.IndexOf('_');
            return underscoreIndex > 0 ? key.Substring(0, underscoreIndex) : string.Empty;
        }
    }
}




