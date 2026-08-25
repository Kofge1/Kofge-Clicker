using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace KofgeClicker;

internal static class LocalizationService
{
    internal const string DefaultLanguageCode = "en";
    internal const string RussianLanguageCode = "ru";

    private static readonly Dictionary<string, string> English = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Common.On"] = "ON",
        ["Common.Off"] = "OFF",
        ["Common.Ok"] = "OK",
        ["Common.Cancel"] = "Cancel",
        ["Common.Yes"] = "Yes",
        ["Common.No"] = "No",
        ["Common.None"] = "None",
        ["Common.RestartRequired"] = "Restart required",
        ["Common.RestartFailed"] = "Restart failed",
        ["Tabs.Clicker"] = "Clicker",
        ["Tabs.Pattern"] = "Pattern",
        ["Tabs.Mouse"] = "Mouse",
        ["Tabs.Hotkey"] = "Hotkey",
        ["Tabs.Profiles"] = "Profiles",
        ["Tabs.Options"] = "Options",
        ["Buttons.Apply"] = "Apply",
        ["Buttons.Close"] = "Close",
        ["Buttons.Bind"] = "Bind",
        ["Buttons.Refresh"] = "Refresh",
        ["Buttons.ResetHotkeys"] = "Reset All Hotkeys",
        ["Buttons.New"] = "New",
        ["Buttons.Rename"] = "Rename",
        ["Buttons.Duplicate"] = "Duplicate",
        ["Buttons.Delete"] = "Delete",
        ["Buttons.Export"] = "Export",
        ["Buttons.Import"] = "Import",
        ["Buttons.SetStartup"] = "Set Startup",
        ["Clicker.Title"] = "Clicker",
        ["Clicker.Enabled"] = "Enabled",
        ["Clicker.Hotkey"] = "Hotkey",
        ["Clicker.Mode"] = "Mode",
        ["Clicker.Hold"] = "Hold",
        ["Clicker.Toggle"] = "Toggle",
        ["Clicker.Humanized"] = "Human clicks",
        ["Clicker.Stable"] = "Stable",
        ["Clicker.Natural"] = "Natural",
        ["Clicker.Aggressive"] = "Aggressive",
        ["Pattern.Title"] = "Click Pattern",
        ["Pattern.Pattern"] = "Pattern",
        ["Pattern.Clicks"] = "Clicks",
        ["Pattern.GapMs"] = "Gap ms",
        ["Pattern.HoldMs"] = "Hold ms",
        ["Pattern.PressMs"] = "Press ms",
        ["Pattern.ReleaseMs"] = "Release ms",
        ["Pattern.RateBehavior"] = "Rate behavior",
        ["Pattern.Locked"] = "Locked",
        ["Pattern.Amplified"] = "Amplified",
        ["Pattern.Standard"] = "Standard",
        ["Pattern.Burst"] = "Triple Click",
        ["Pattern.DoubleClick"] = "Double Click",
        ["Pattern.HoldThenBurst"] = "Custom",
        ["Pattern.HelpAmplified"] = "Amplified lets the pattern add extra taps above the target CPS.",
        ["Pattern.HelpLocked"] = "Locked keeps the output tied to your target CPS.",
        ["Pattern.HelpStandard"] = "Standard sends one tap per CPS tick.",
        ["Pattern.HelpBurst"] = "Triple Click sends three clicks per pattern cycle.",
        ["Pattern.HelpDouble"] = "Double Click sends paired taps.",
        ["Pattern.HelpHoldBurst"] = "Custom lets you configure the click count, gap, hold, press and release timing.",
        ["Mouse.Title"] = "Mouse Button",
        ["Mouse.Mouse"] = "Mouse",
        ["Mouse.Left"] = "Left",
        ["Mouse.Right"] = "Right",
        ["Mouse.Help"] = "Choose which mouse button Kofge-Clicker presses.\nThis setting is saved with the current profile.",
        ["Mouse.TestTitle"] = "Built-in click test",
        ["Mouse.TestBothButtonsInstruction"] = "Click here with LMB or RMB, or activate the clicker with its hotkey.",
        ["Mouse.TestClicks"] = "Clicks received",
        ["Mouse.TestCps"] = "Live CPS",
        ["Mouse.TestReset"] = "Reset test",
        ["Hotkeys.Title"] = "Service Hotkeys",
        ["Hotkeys.PanicStop"] = "Panic Stop",
        ["Hotkeys.ShowWindow"] = "Show Window",
        ["Hotkeys.ToggleEnabled"] = "Toggle Enabled",
        ["Hotkeys.NextProfile"] = "Next Profile",
        ["Profiles.Title"] = "Profiles",
        ["Profiles.Current"] = "Current profile",
        ["Profiles.Startup"] = "Startup profile: {0}",
        ["Profiles.Remember"] = "Remember profile",
        ["Profiles.LastUsed"] = "Last used profile",
        ["Profiles.DataLocation"] = "Profiles, settings and logs:   {0}",
        ["Profiles.NewTitle"] = "New profile",
        ["Profiles.NewPrompt"] = "Enter a name for the new profile.",
        ["Profiles.RenameTitle"] = "Rename profile",
        ["Profiles.RenamePrompt"] = "Enter a new profile name.",
        ["Profiles.EmptyName"] = "Profile name cannot be empty.",
        ["Profiles.AlreadyExists"] = "A profile with this name already exists.",
        ["Profiles.MustRemain"] = "At least one profile must remain.",
        ["Profiles.DeleteTitle"] = "Delete profile",
        ["Profiles.DeleteQuestion"] = "Delete profile '{0}'?",
        ["Profiles.Exported"] = "Profile exported successfully.",
        ["Profiles.InvalidFile"] = "This file is not a valid exported profile.",
        ["Profiles.StartupUpdated"] = "Startup profile updated.",
        ["Options.WindowAndTray"] = "Window and Tray",
        ["Options.RunAsAdministrator"] = "Run as administrator",
        ["Options.StartHidden"] = "Launch hidden in tray",
        ["Options.RunOnStartup"] = "Run on startup",
        ["Options.RememberProfile"] = "Remember profile",
        ["Options.MinimizeToTray"] = "Minimize (-) to tray",
        ["Options.CloseToTray"] = "Close window to tray",
        ["Options.WindowTarget"] = "Window Target",
        ["Options.RestrictWindow"] = "Only while selected window is focused",
        ["Options.AnyWindow"] = "Any window",
        ["Options.TargetWindow"] = "Target window: {0}",
        ["Options.SavedTarget"] = "Saved target (not running): {0}",
        ["Options.NotSelected"] = "Not selected",
        ["Options.UnknownWindow"] = "Unknown window",
        ["Options.SelectTargetFirst"] = "Select a target window from the list first.",
        ["Options.Language"] = "Language",
        ["Options.LanguageEnglish"] = "English",
        ["Options.LanguageRussian"] = "Russian",
        ["Options.LanguageRestartQuestion"] = "Restart Kofge-Clicker now to apply the selected language?",
        ["Options.LanguageRestartFailed"] = "Kofge-Clicker could not restart automatically. Restart it manually to apply the selected language.",
        ["Options.AdminRestart"] = "Restart Kofge-Clicker for this setting to take effect.",
        ["Options.StartupErrorTitle"] = "Windows startup could not be updated",
        ["Options.StartupEnableErrorText"] = "Kofge-Clicker could not register itself to start with Windows. The option was turned off. Check Windows permissions or security software and try again.",
        ["Options.StartupDisableErrorText"] = "Kofge-Clicker could not remove its Windows startup registration. The option was restored. Check Windows permissions or security software and try again.",
        ["Status.Profile"] = "Profile",
        ["Status.Status"] = "Status",
        ["Status.Click"] = "Click",
        ["Status.Hotkey"] = "Hotkey",
        ["Status.Mode"] = "Mode",
        ["Status.Target"] = "Target",
        ["Status.Humanized"] = "Human clicks",
        ["Status.RateLocked"] = "Rate Locked",
        ["Status.RateAmplified"] = "Rate Amplified",
        ["Status.PatternStandard"] = "Std",
        ["Status.PatternDouble"] = "Double",
        ["Status.PatternHoldBurst"] = "Custom",
        ["Status.StateDisabled"] = "Clicker disabled",
        ["Status.StateRecording"] = "Press a key...",
        ["Hotkeys.RecordingPrompt"] = "Waiting for input...",
        ["Status.StateClicking"] = "Clicking - {0} CPS",
        ["Status.StateWaitingWindow"] = "Waiting for {0}",
        ["Status.StateReadyHold"] = "Ready - hold {0}",
        ["Status.StateReadyToggle"] = "Ready - press {0}",
        ["Tooltips.TabClicker"] = "Configure activation, click speed, operating mode and human timing.",
        ["Tooltips.TabPattern"] = "Configure the click sequence and its timing.",
        ["Tooltips.TabMouse"] = "Choose which mouse button Kofge-Clicker will press.",
        ["Tooltips.TabHotkey"] = "Configure service hotkeys for controlling Kofge-Clicker.",
        ["Tooltips.TabProfiles"] = "Create and manage separate sets of clicker settings.",
        ["Tooltips.TabOptions"] = "Configure startup, tray, target-window and language behavior.",
        ["Tooltips.ClickerEnabled"] = "Allows or prevents the clicker from being activated by its hotkey.",
        ["Tooltips.ClickerMode"] = "Choose how the assigned hotkey starts and stops clicking.",
        ["Tooltips.ClickerModeHold"] = "Clicking continues only while you hold the assigned hotkey. Release it to stop.",
        ["Tooltips.ClickerModeToggle"] = "Press the hotkey once to start continuous clicking. Press it again to stop.",
        ["Tooltips.ClickerHotkey"] = "The keyboard key or mouse button that starts and stops clicking.",
        ["Tooltips.BindHotkey"] = "Wait for a new keyboard key or mouse button and assign it to this action.",
        ["Tooltips.ClickerCps"] = "Sets the target number of clicks per second, from 1 to 100.",
        ["Tooltips.ClickerHumanized"] = "Adds natural timing variation and occasional short pauses between clicks.",
        ["Tooltips.PatternType"] = "Selects how one click cycle is performed: standard, triple click, double click or a fully configurable custom pattern.",
        ["Tooltips.PatternClicks"] = "Sets the number of clicks used by the Custom pattern.",
        ["Tooltips.PatternGap"] = "Sets the delay between clicks in Triple Click, Double Click and Custom patterns.",
        ["Tooltips.PatternHold"] = "Sets how long the first mouse press is held in the Custom pattern.",
        ["Tooltips.PatternPress"] = "Adds a delay after pressing the selected mouse button.",
        ["Tooltips.PatternRelease"] = "Adds a delay after releasing the selected mouse button.",
        ["Tooltips.PatternRate"] = "Locked keeps output near the target CPS. Amplified allows patterns to add extra clicks.",
        ["Tooltips.MouseButton"] = "Selects the mouse button that Kofge-Clicker will press.",
        ["Tooltips.MouseTestBothButtons"] = "Counts both LMB and RMB clicks received in this area and measures their live CPS. It does not change your settings.",
        ["Tooltips.MouseTestReset"] = "Clears the click count and current CPS measurement.",
        ["Tooltips.HotkeyPanic"] = "Stops clicking, releases held mouse buttons and closes Kofge-Clicker immediately.",
        ["Tooltips.HotkeyWindow"] = "Shows the minimized app or hides the visible app in the tray.",
        ["Tooltips.HotkeyEnabled"] = "Turns the main Enabled setting on or off without opening the app.",
        ["Tooltips.HotkeyProfile"] = "Switches to the next profile in the profile list.",
        ["Tooltips.ResetHotkeys"] = "Restores all service and clicker hotkeys to their safe defaults.",
        ["Tooltips.ProfileCurrent"] = "Selects the active profile and loads all settings saved in it.",
        ["Tooltips.ProfileNew"] = "Creates a new profile with its own clicker settings.",
        ["Tooltips.ProfileRename"] = "Changes the name of the selected profile.",
        ["Tooltips.ProfileDuplicate"] = "Creates a copy of the selected profile and all its settings.",
        ["Tooltips.ProfileDelete"] = "Permanently deletes the selected profile.",
        ["Tooltips.ProfileExport"] = "Saves the selected profile to a file for backup or sharing.",
        ["Tooltips.ProfileImport"] = "Loads a profile from a previously exported profile file.",
        ["Tooltips.ProfileStartup"] = "Chooses which profile Kofge-Clicker loads when it starts.",
        ["Tooltips.OptionAdministrator"] = "Restarts future launches with administrator rights for elevated games and apps.",
        ["Tooltips.OptionStartHidden"] = "Starts Kofge-Clicker hidden in the system tray instead of opening its window.",
        ["Tooltips.OptionWindowsStartup"] = "Starts Kofge-Clicker automatically after signing in to Windows.",
        ["Tooltips.OptionMinimizeTray"] = "Sends the app to the tray when the window minimize button is pressed.",
        ["Tooltips.OptionCloseTray"] = "Sends the app to the tray instead of exiting when the window close button is pressed.",
        ["Tooltips.OptionTargetOnly"] = "Allows generated clicks only while the selected target window is focused.",
        ["Tooltips.OptionTargetList"] = "Selects the window or process in which generated clicks are allowed.",
        ["Tooltips.OptionRefreshWindows"] = "Refreshes the list of currently available windows and processes.",
        ["Tooltips.OptionLanguage"] = "Selects the interface language. Restart the app to apply a change.",
        ["Tooltips.LanguageToggle"] = "Switches between English and Russian. The selected language applies after restarting.",
        ["Tooltips.Apply"] = "Validates and saves the current settings.",
        ["Tooltips.Close"] = "Closes the window or sends it to the tray when that option is enabled.",
        ["Tooltips.Status"] = "The first line shows the current state. The arrow below shows activation hotkey → clicked mouse button.",
        ["Tray.Running"] = "Running in tray",
        ["Tray.Hide"] = "Hide Kofge-Clicker",
        ["Tray.Open"] = "Open Kofge-Clicker",
        ["Tray.Disable"] = "Disable Clicker",
        ["Tray.Enable"] = "Enable Clicker",
        ["Tray.StopClicking"] = "Stop Clicking",
        ["Tray.StartClicking"] = "Start Clicking",
        ["Tray.HoldMode"] = "Hold Mode Active",
        ["Tray.Profiles"] = "Profiles",
        ["Tray.Exit"] = "Exit",
        ["Tray.ClickerOff"] = "CLICKER OFF",
        ["Tray.ClickerOn"] = "CLICKER ON",
        ["Tray.ProfileChanged"] = "PROFILE:",
        ["Tour.WelcomeBadge"] = "FIRST LAUNCH",
        ["Tour.WelcomeTitle"] = "Welcome to Kofge-Clicker",
        ["Tour.WelcomeText"] = "Take a short tour of the essential controls, or skip it permanently and start using the app right away.",
        ["Tour.WelcomeHotkeys"] = "Choose the hotkeys you will use every day.",
        ["Tour.WelcomeBehavior"] = "Learn how click modes and patterns change the result.",
        ["Tour.WelcomeOptions"] = "Configure startup, tray behavior and a target window.",
        ["Tour.Step"] = "STEP {0} OF {1}",
        ["Tour.Start"] = "Start tour",
        ["Tour.Skip"] = "Skip permanently",
        ["Tour.Back"] = "Back",
        ["Tour.Next"] = "Next",
        ["Tour.Finish"] = "Finish",
        ["Tour.MainHotkeyTitle"] = "Choose your activation hotkey",
        ["Tour.MainHotkeyText"] = "This hotkey starts the clicker in Hold or Toggle mode. Choose a key or mouse button that is comfortable and does not conflict with the current app or game.",
        ["Tour.CurrentBinding"] = "Current binding",
        ["Tour.ChangeBinding"] = "Change binding",
        ["Tour.MainHotkeyHint"] = "Click Change binding, then press the keyboard key or mouse button you want to use. Modifier combinations such as Ctrl + key are supported.",
        ["Tour.ModesTitle"] = "Choose how the clicker starts and stops",
        ["Tour.ModesText"] = "Click a mode below to select it. The choice is applied immediately and saved separately in each profile.",
        ["Tour.HoldText"] = "Clicks only while the activation hotkey is held. Release the hotkey to stop immediately.",
        ["Tour.ToggleText"] = "One press starts clicking; the next press stops it. Useful when you do not want to keep holding a key.",
        ["Tour.PatternsTitle"] = "Control click behavior with patterns",
        ["Tour.PatternsText"] = "Patterns define what happens on every CPS tick. Delays and rate behavior let you fine-tune how the selected pattern is sent.",
        ["Tour.PatternStandard"] = "Sends one regular click per CPS tick.",
        ["Tour.PatternBurst"] = "Sends three clicks as one action.",
        ["Tour.PatternDouble"] = "Sends two clicks as one action.",
        ["Tour.PatternHoldBurst"] = "Lets you configure every available pattern value.",
        ["Tour.PatternRateText"] = "Fixed keeps the selected CPS limit. Extra clicks allows a pattern to add actions above that base rate.",
        ["Tour.ServiceHotkeysTitle"] = "Set up service hotkeys",
        ["Tour.ServiceHotkeysText"] = "These shortcuts remain available while Kofge-Clicker is in the tray. You can change each one now or keep the safe defaults.",
        ["Tour.StartupTrayTitle"] = "Configure startup and tray behavior",
        ["Tour.StartupTrayText"] = "These options control how Kofge-Clicker launches and what the window buttons do.",
        ["Tour.StartupAdminText"] = "Future launches run Kofge-Clicker as administrator.",
        ["Tour.StartupHiddenText"] = "Kofge-Clicker starts directly in the system tray.",
        ["Tour.StartupWindowsText"] = "Kofge-Clicker starts after you sign in to Windows.",
        ["Tour.StartupMinimizeText"] = "The minimize button hides the app in the tray.",
        ["Tour.StartupCloseText"] = "The close button hides the app instead of exiting.",
        ["Tour.TargetWindowTitle"] = "Limit clicking to a selected window",
        ["Tour.TargetWindowText"] = "Choose where clicking is allowed and keep other applications available for normal mouse input.",
        ["Tour.TargetFocusTitle"] = "Window focus",
        ["Tour.TargetFocusText"] = "Clicking pauses outside the selected window and becomes available again when you return to it.",
        ["Tour.OptionsTitle"] = "Finish with application options",
        ["Tour.OptionsText"] = "The Options tab controls startup and tray behavior. Target Window limits clicking to the selected application while that window is focused.",
        ["Tour.OptionAdmin"] = "Restarts future launches with administrator privileges for elevated applications.",
        ["Tour.OptionTray"] = "Starts without opening the main window and remains available in the tray.",
        ["Tour.OptionStartup"] = "Launches Kofge-Clicker automatically when you sign in to Windows.",
        ["Tour.OptionTarget"] = "Select an application and enable this option so clicks only run while that window is focused.",
        ["Validation.DuplicateHotkeys"] = "Clicker, Panic Stop, Show Window, Toggle Enabled and Next Profile hotkeys must be different.",
        ["Validation.UnsafeMouseHotkeys"] = "Panic Stop, Show Window, Toggle Enabled and Next Profile cannot use bare LMB, RMB or MMB. Side mouse buttons are allowed.",
        ["Validation.ClickerActivation"] = "Clicker activation",
        ["Validation.HotkeyConflict"] = "{0} cannot be assigned to {1} because it overlaps with {2} ({3}). Additional Ctrl, Shift or Alt keys can make both actions run at once. Choose another key.",
        ["Validation.UnsafeServiceAssignment"] = "A service hotkey cannot use bare LMB, RMB or MMB. Add Ctrl, Shift or Alt, or choose a side mouse button.",
        ["Settings.Saved"] = "Settings saved",
        ["ReviewPrompt.Title"] = "Enjoying Kofge-Clicker?",
        ["ReviewPrompt.Text"] = "If Kofge-Clicker has been useful, you can leave a short review. It helps other users and supports the project.",
        ["ReviewPrompt.LeaveReview"] = "Leave a review",
        ["ReviewPrompt.Later"] = "Later",
        ["ReviewPrompt.Never"] = "Don't show again",
        ["Update.ReadyTitle"] = "Update ready",
        ["Update.ReadyText"] = "Kofge-Clicker {0} has been downloaded and is ready to install.\n\nRestart now to replace the current EXE automatically?",
        ["Update.FailedTitle"] = "Update failed",
        ["Update.FailedText"] = "The automatic updater could not be started. The current version was not changed.",
        ["WhatsNew.Title"] = "What's new in {0}",
        ["WhatsNew.Subtitle"] = "A quick look at the main improvements in this version.",
        ["WhatsNew.HoverTitle"] = "Hover explanations",
        ["WhatsNew.HoverText"] = "Hover over a setting for one second to see a clear explanation.",
        ["WhatsNew.TestTitle"] = "Built-in click test",
        ["WhatsNew.TestText"] = "Test mouse clicks and watch the live CPS counter on the Mouse tab.",
        ["WhatsNew.HotkeysTitle"] = "Safer hotkeys",
        ["WhatsNew.HotkeysText"] = "Conflicting hotkey combinations are now detected before they are saved.",
        ["WhatsNew.FeedbackTitle"] = "Clearer feedback",
        ["WhatsNew.FeedbackText"] = "Warnings and save confirmations now match the application style.",
        ["WhatsNew.WindowTitle"] = "What's New window",
        ["WhatsNew.WindowText"] = "The highlights of each new version are shown once after updating.",
        ["WhatsNew.LanguageButtonTitle"] = "Language on the main screen",
        ["WhatsNew.LanguageButtonText"] = "Switch between English and Russian using the new EN/RU button.",
        ["WhatsNew.LanguageDetectionTitle"] = "Automatic first-launch language",
        ["WhatsNew.LanguageDetectionText"] = "Russian keyboard layouts select Russian; all other layouts default to English.",
        ["WhatsNew.BothButtonsTitle"] = "LMB and RMB click testing",
        ["WhatsNew.BothButtonsText"] = "The built-in test now counts both primary mouse buttons regardless of the selected click button.",
        ["WhatsNew.AtomicTabsTitle"] = "Clean tab switching",
        ["WhatsNew.AtomicTabsText"] = "Tabs now switch as one complete frame without remnants of the previous page.",
        ["WhatsNew.SmoothTabsTitle"] = "Smoother visual feedback",
        ["WhatsNew.SmoothTabsText"] = "The active tab highlight now changes with a short, subtle transition.",
        ["WhatsNew.ClearLabelsTitle"] = "Clearer Russian labels",
        ["WhatsNew.ClearLabelsText"] = "The Russian interface now uses simpler names for the clicker state and keyboard shortcuts.",
        ["WhatsNew.ConsistentTermsTitle"] = "Consistent terminology",
        ["WhatsNew.ConsistentTermsText"] = "Tooltips, onboarding, validation messages and the status panel now use the same wording.",
        ["WhatsNew.TargetAppsTitle"] = "Clearer application list",
        ["WhatsNew.TargetAppsText"] = "Target windows now show application names and icons instead of technical process names.",
        ["WhatsNew.WindowsStartupTitle"] = "Reliable Windows startup",
        ["WhatsNew.WindowsStartupText"] = "Windows startup now uses the system Startup folder, with the registry kept as a fallback.",
        ["WhatsNew.ProfileSafetyTitle"] = "Safer profile imports",
        ["WhatsNew.ProfileSafetyText"] = "Invalid or unsupported profile files are now rejected before they can change clicker settings.",
        ["WhatsNew.ReliabilityTitle"] = "Improved runtime reliability",
        ["WhatsNew.ReliabilityText"] = "Windows startup failures are now reported correctly, and click sessions release their resources safely.",
        ["WhatsNew.DisplayScaleTitle"] = "Consistent display scaling",
        ["WhatsNew.DisplayScaleText"] = "The interface now stays stable and readable at 100%, 125% and 150% Windows scaling.",
        ["WhatsNew.WindowPlacementTitle"] = "Safe window placement",
        ["WhatsNew.WindowPlacementText"] = "Main and dialog windows remain fully visible on resolutions down to 1280x720.",
        ["WhatsNew.OnboardingLayoutTitle"] = "Refined onboarding layout",
        ["WhatsNew.OnboardingLayoutText"] = "Welcome text and hotkey rows are now evenly aligned with balanced spacing and no clipping.",
        ["WhatsNew.ClickTestAccuracyTitle"] = "More accurate click testing",
        ["WhatsNew.ClickTestAccuracyText"] = "Live CPS now uses the original mouse message time for reliable measurements during UI load.",
        ["WhatsNew.ProfileNotificationTitle"] = "Profile switch notifications",
        ["WhatsNew.ProfileNotificationText"] = "A notification now highlights the newly selected profile after every profile switch.",
        ["WhatsNew.NotificationTextTitle"] = "Cleaner notification text",
        ["WhatsNew.NotificationTextText"] = "Notification headings now render cleanly without clipped letters.",
        ["WhatsNew.ReviewPromptTitle"] = "Optional review reminder",
        ["WhatsNew.ReviewPromptText"] = "A quiet, delayed prompt can invite active users to leave a review without interrupting startup or clicking.",
        ["WhatsNew.ReviewLinkTitle"] = "Localized review page",
        ["WhatsNew.ReviewLinkText"] = "The review button opens the English or Russian website page based on the selected app language.",
        ["WhatsNew.Continue"] = "Got it",
        ["App.AlreadyRunning"] = "Kofge-Clicker is already running.",
        ["App.InputHookFailedTitle"] = "Input unavailable",
        ["App.InputHookFailedText"] = "Kofge-Clicker could not connect to the global keyboard or mouse input. Restart the app. If the problem persists, check whether security software is blocking it."
    };

    private static readonly Dictionary<string, string> Russian = new(English, StringComparer.OrdinalIgnoreCase)
    {
        ["WhatsNew.AtomicTabsTitle"] = "Чистое переключение вкладок",
        ["WhatsNew.AtomicTabsText"] = "Вкладки теперь меняются одним готовым кадром без остатков предыдущей страницы.",
        ["WhatsNew.SmoothTabsTitle"] = "Плавная визуальная реакция",
        ["WhatsNew.SmoothTabsText"] = "Подсветка активной вкладки теперь меняется с коротким и мягким переходом.",
        ["Common.On"] = "ВКЛ",
        ["Common.Off"] = "ВЫКЛ",
        ["Common.Cancel"] = "Отмена",
        ["Common.Yes"] = "Да",
        ["Common.No"] = "Нет",
        ["Common.None"] = "Нет",
        ["Common.RestartRequired"] = "Требуется перезапуск",
        ["Common.RestartFailed"] = "Ошибка перезапуска",
        ["Tabs.Clicker"] = "Кликер",
        ["Tabs.Pattern"] = "Паттерн",
        ["Tabs.Mouse"] = "Мышь",
        ["Tabs.Hotkey"] = "Клавиши",
        ["Tabs.Profiles"] = "Профили",
        ["Tabs.Options"] = "Опции",
        ["Buttons.Apply"] = "Применить",
        ["Buttons.Close"] = "Закрыть",
        ["Buttons.Bind"] = "Назначить",
        ["Buttons.Refresh"] = "Обновить",
        ["Buttons.ResetHotkeys"] = "Сбросить все клавиши",
        ["Buttons.New"] = "Создать",
        ["Buttons.Rename"] = "Переимен.",
        ["Buttons.Duplicate"] = "Дублир.",
        ["Buttons.Delete"] = "Удалить",
        ["Buttons.Export"] = "Экспорт",
        ["Buttons.Import"] = "Импорт",
        ["Buttons.SetStartup"] = "Для запуска",
        ["Clicker.Title"] = "Кликер",
        ["Clicker.Enabled"] = "Работа кликера",
        ["Clicker.Hotkey"] = "Клавиша запуска",
        ["Clicker.Mode"] = "Режим",
        ["Clicker.Hold"] = "Удержание",
        ["Clicker.Toggle"] = "Переключение",
        ["Clicker.Humanized"] = "Естеств. клики",
        ["Clicker.Stable"] = "Стабильный",
        ["Clicker.Natural"] = "Естественный",
        ["Clicker.Aggressive"] = "Агрессивный",
        ["Pattern.Title"] = "Паттерн кликов",
        ["Pattern.Pattern"] = "Паттерн",
        ["Pattern.Clicks"] = "Клики",
        ["Pattern.GapMs"] = "Интервал",
        ["Pattern.HoldMs"] = "Удержание",
        ["Pattern.PressMs"] = "Нажатие",
        ["Pattern.ReleaseMs"] = "Отпускание",
        ["Pattern.RateBehavior"] = "Поведение CPS",
        ["Pattern.Locked"] = "Фиксир.",
        ["Pattern.Amplified"] = "Доп. клики",
        ["Pattern.Standard"] = "Стандартный",
        ["Pattern.Burst"] = "Тройной клик",
        ["Pattern.DoubleClick"] = "Двойной клик",
        ["Pattern.HoldThenBurst"] = "Настраиваемый",
        ["Pattern.HelpAmplified"] = "Дополнительные клики позволяют паттерну добавлять нажатия сверх заданного CPS.",
        ["Pattern.HelpLocked"] = "Фиксированный режим удерживает частоту на уровне заданного CPS.",
        ["Pattern.HelpStandard"] = "Стандартный паттерн отправляет один клик за такт CPS.",
        ["Pattern.HelpBurst"] = "Тройной клик отправляет три клика за один цикл паттерна.",
        ["Pattern.HelpDouble"] = "Двойной клик отправляет клики парами.",
        ["Pattern.HelpHoldBurst"] = "Настраиваемый паттерн позволяет задать количество кликов, интервал, удержание, нажатие и отпускание.",
        ["Mouse.Title"] = "Кнопка мыши",
        ["Mouse.Mouse"] = "Мышь",
        ["Mouse.Left"] = "Левая",
        ["Mouse.Right"] = "Правая",
        ["Mouse.Help"] = "Выберите кнопку мыши, которую будет нажимать Kofge-Clicker.\nНастройка сохраняется в текущем профиле.",
        ["Mouse.TestTitle"] = "Встроенная проверка",
        ["Mouse.TestBothButtonsInstruction"] = "Нажимайте здесь ЛКМ или ПКМ либо включите кликер назначенной клавишей запуска.",
        ["Mouse.TestClicks"] = "Получено кликов",
        ["Mouse.TestCps"] = "Текущий CPS",
        ["Mouse.TestReset"] = "Сбросить тест",
        ["Hotkeys.Title"] = "Горячие клавиши",
        ["Hotkeys.PanicStop"] = "Аварийное зак.",
        ["Hotkeys.ShowWindow"] = "Показать окно",
        ["Hotkeys.ToggleEnabled"] = "ВКЛ/ВЫКЛ кликера",
        ["Hotkeys.NextProfile"] = "След. профиль",
        ["Profiles.Title"] = "Профили",
        ["Profiles.Current"] = "Текущий профиль",
        ["Profiles.Startup"] = "Стартовый профиль: {0}",
        ["Profiles.Remember"] = "Запоминать профиль",
        ["Profiles.LastUsed"] = "Последний профиль",
        ["Profiles.DataLocation"] = "Профили, настройки и логи:   {0}",
        ["Profiles.NewTitle"] = "Новый профиль",
        ["Profiles.NewPrompt"] = "Введите название нового профиля.",
        ["Profiles.RenameTitle"] = "Переименование профиля",
        ["Profiles.RenamePrompt"] = "Введите новое название профиля.",
        ["Profiles.EmptyName"] = "Название профиля не может быть пустым.",
        ["Profiles.AlreadyExists"] = "Профиль с таким названием уже существует.",
        ["Profiles.MustRemain"] = "Должен остаться хотя бы один профиль.",
        ["Profiles.DeleteTitle"] = "Удаление профиля",
        ["Profiles.DeleteQuestion"] = "Удалить профиль «{0}»?",
        ["Profiles.Exported"] = "Профиль успешно экспортирован.",
        ["Profiles.InvalidFile"] = "Файл не является корректным экспортированным профилем.",
        ["Profiles.StartupUpdated"] = "Стартовый профиль обновлён.",
        ["Options.WindowAndTray"] = "Окно и трей",
        ["Options.RunAsAdministrator"] = "Запуск от админа",
        ["Options.StartHidden"] = "Запуск скрытым в трее",
        ["Options.RunOnStartup"] = "Автозапуск Windows",
        ["Options.RememberProfile"] = "Запоминать профиль",
        ["Options.MinimizeToTray"] = "Кнопка «−» в трей",
        ["Options.CloseToTray"] = "Кнопка «×» в трей",
        ["Options.WindowTarget"] = "Целевое окно",
        ["Options.RestrictWindow"] = "Только выбранное окно",
        ["Options.AnyWindow"] = "Любое окно",
        ["Options.TargetWindow"] = "Целевое окно: {0}",
        ["Options.SavedTarget"] = "Сохранённая цель (не запущена): {0}",
        ["Options.NotSelected"] = "Не выбрано",
        ["Options.UnknownWindow"] = "Неизвестное окно",
        ["Options.SelectTargetFirst"] = "Сначала выберите целевое окно из списка.",
        ["Options.Language"] = "Язык",
        ["Options.LanguageEnglish"] = "Английский",
        ["Options.LanguageRussian"] = "Русский",
        ["Options.LanguageRestartQuestion"] = "Перезапустить Kofge-Clicker сейчас, чтобы применить выбранный язык?",
        ["Options.LanguageRestartFailed"] = "Не удалось автоматически перезапустить Kofge-Clicker. Перезапустите приложение вручную, чтобы применить выбранный язык.",
        ["Options.AdminRestart"] = "Перезапустите Kofge-Clicker, чтобы применить эту настройку.",
        ["Options.StartupErrorTitle"] = "Не удалось изменить автозапуск",
        ["Options.StartupEnableErrorText"] = "Kofge-Clicker не удалось добавить себя в автозапуск Windows. Настройка была выключена. Проверьте разрешения Windows или защитное ПО и попробуйте снова.",
        ["Options.StartupDisableErrorText"] = "Kofge-Clicker не удалось удалить из автозапуска Windows. Настройка была восстановлена. Проверьте разрешения Windows или защитное ПО и попробуйте снова.",
        ["Status.Profile"] = "Профиль",
        ["Status.Status"] = "Статус",
        ["Status.Click"] = "Клик",
        ["Status.Hotkey"] = "Клавиша",
        ["Status.Mode"] = "Режим",
        ["Status.Target"] = "Цель",
        ["Status.Humanized"] = "Естеств. клики",
        ["Status.RateLocked"] = "CPS фиксирован",
        ["Status.RateAmplified"] = "CPS усилен",
        ["Status.PatternStandard"] = "Станд.",
        ["Status.PatternDouble"] = "Двойной",
        ["Status.PatternHoldBurst"] = "Настр.",
        ["Status.StateDisabled"] = "Кликер выключен",
        ["Status.StateRecording"] = "Нажмите клавишу...",
        ["Hotkeys.RecordingPrompt"] = "Ожидание ввода...",
        ["Status.StateClicking"] = "Кликает - {0} CPS",
        ["Status.StateWaitingWindow"] = "Ожидает {0}",
        ["Status.StateReadyHold"] = "Готов - удерживайте {0}",
        ["Status.StateReadyToggle"] = "Готов - нажмите {0}",
        ["Tooltips.TabClicker"] = "Настройка включения, скорости кликов, режима работы и естественных задержек.",
        ["Tooltips.TabPattern"] = "Настройка последовательности кликов и её временных параметров.",
        ["Tooltips.TabMouse"] = "Выбор кнопки мыши, которую будет нажимать Kofge-Clicker.",
        ["Tooltips.TabHotkey"] = "Настройка горячих клавиш для управления Kofge-Clicker.",
        ["Tooltips.TabProfiles"] = "Создание и управление отдельными наборами настроек кликера.",
        ["Tooltips.TabOptions"] = "Настройка запуска, трея, целевого окна и языка интерфейса.",
        ["Tooltips.ClickerEnabled"] = "Разрешает или запрещает запуск кликера назначенной клавишей.",
        ["Tooltips.ClickerMode"] = "Выбирает, как назначенная клавиша запускает и останавливает клики.",
        ["Tooltips.ClickerModeHold"] = "Кликер работает только пока вы удерживаете назначенную клавишу. Отпустите её — клики остановятся.",
        ["Tooltips.ClickerModeToggle"] = "Одно нажатие назначенной клавиши запускает непрерывные клики. Повторное нажатие останавливает их.",
        ["Tooltips.ClickerHotkey"] = "Клавиша клавиатуры или кнопка мыши, которая запускает и останавливает клики.",
        ["Tooltips.BindHotkey"] = "Ожидает новую клавишу или кнопку мыши и назначает её выбранному действию.",
        ["Tooltips.ClickerCps"] = "Задаёт целевое количество кликов в секунду от 1 до 100.",
        ["Tooltips.ClickerHumanized"] = "Добавляет естественные отклонения времени и редкие короткие паузы между кликами.",
        ["Tooltips.PatternType"] = "Выбирает выполнение одного цикла: стандартный, тройной клик, двойной клик или полностью настраиваемый паттерн.",
        ["Tooltips.PatternClicks"] = "Задаёт количество кликов в настраиваемом паттерне.",
        ["Tooltips.PatternGap"] = "Задаёт паузу между кликами в паттернах «Тройной клик», «Двойной клик» и «Настраиваемый».",
        ["Tooltips.PatternHold"] = "Задаёт длительность первого удержания в настраиваемом паттерне.",
        ["Tooltips.PatternPress"] = "Добавляет задержку после нажатия выбранной кнопки мыши.",
        ["Tooltips.PatternRelease"] = "Добавляет задержку после отпускания выбранной кнопки мыши.",
        ["Tooltips.PatternRate"] = "Фиксир. удерживает частоту около заданного CPS. Доп. клики разрешают паттерну добавлять нажатия.",
        ["Tooltips.MouseButton"] = "Выбирает кнопку мыши, которую будет нажимать Kofge-Clicker.",
        ["Tooltips.MouseTestBothButtons"] = "Считает нажатия ЛКМ и ПКМ в этой области и измеряет их текущий CPS. Настройки кликера не изменяются.",
        ["Tooltips.MouseTestReset"] = "Обнуляет количество кликов и текущее измерение CPS.",
        ["Tooltips.HotkeyPanic"] = "Немедленно останавливает клики, отпускает кнопки мыши и закрывает Kofge-Clicker.",
        ["Tooltips.HotkeyWindow"] = "Показывает свёрнутое приложение или скрывает открытое приложение в трей.",
        ["Tooltips.HotkeyEnabled"] = "Включает или выключает параметр «Работа кликера» без открытия приложения.",
        ["Tooltips.HotkeyProfile"] = "Переключает на следующий профиль в списке профилей.",
        ["Tooltips.ResetHotkeys"] = "Возвращает всем горячим клавишам безопасные значения по умолчанию.",
        ["Tooltips.ProfileCurrent"] = "Выбирает активный профиль и загружает все сохранённые в нём настройки.",
        ["Tooltips.ProfileNew"] = "Создаёт новый профиль с отдельными настройками кликера.",
        ["Tooltips.ProfileRename"] = "Изменяет название выбранного профиля.",
        ["Tooltips.ProfileDuplicate"] = "Создаёт копию выбранного профиля вместе со всеми его настройками.",
        ["Tooltips.ProfileDelete"] = "Безвозвратно удаляет выбранный профиль.",
        ["Tooltips.ProfileExport"] = "Сохраняет выбранный профиль в файл для резервной копии или передачи.",
        ["Tooltips.ProfileImport"] = "Загружает профиль из ранее экспортированного файла.",
        ["Tooltips.ProfileStartup"] = "Выбирает профиль, который Kofge-Clicker загрузит при запуске.",
        ["Tooltips.OptionAdministrator"] = "Следующие запуски будут выполняться от имени администратора для игр и приложений с повышенными правами.",
        ["Tooltips.OptionStartHidden"] = "Запускает Kofge-Clicker скрытым в системном трее без открытия окна.",
        ["Tooltips.OptionWindowsStartup"] = "Автоматически запускает Kofge-Clicker после входа в Windows.",
        ["Tooltips.OptionMinimizeTray"] = "Скрывает приложение в трей при нажатии кнопки сворачивания окна.",
        ["Tooltips.OptionCloseTray"] = "Скрывает приложение в трей вместо выхода при нажатии крестика окна.",
        ["Tooltips.OptionTargetOnly"] = "Разрешает созданные клики только тогда, когда выбранное целевое окно находится в фокусе.",
        ["Tooltips.OptionTargetList"] = "Выбирает окно или процесс, внутри которого разрешены созданные клики.",
        ["Tooltips.OptionRefreshWindows"] = "Обновляет список доступных окон и процессов.",
        ["Tooltips.OptionLanguage"] = "Выбирает язык интерфейса. Для применения изменения нужен перезапуск.",
        ["Tooltips.LanguageToggle"] = "Переключает интерфейс между русским и английским языком. Выбранный язык применится после перезапуска.",
        ["Tooltips.Apply"] = "Проверяет и сохраняет текущие настройки.",
        ["Tooltips.Close"] = "Закрывает окно или скрывает его в трей, если включена соответствующая настройка.",
        ["Tooltips.Status"] = "Первая строка показывает текущее состояние. Стрелка ниже показывает клавишу запуска → нажатая кнопка мыши.",
        ["Tray.Running"] = "Приложение работает в трее",
        ["Tray.Hide"] = "Скрыть Kofge-Clicker",
        ["Tray.Open"] = "Открыть Kofge-Clicker",
        ["Tray.Disable"] = "Выключить кликер",
        ["Tray.Enable"] = "Включить кликер",
        ["Tray.StopClicking"] = "Остановить клики",
        ["Tray.StartClicking"] = "Запустить клики",
        ["Tray.HoldMode"] = "Режим удержания активен",
        ["Tray.Profiles"] = "Профили",
        ["Tray.Exit"] = "Выход",
        ["Tray.ClickerOff"] = "КЛИКЕР ВЫКЛЮЧЕН",
        ["Tray.ClickerOn"] = "КЛИКЕР ВКЛЮЧЁН",
        ["Tray.ProfileChanged"] = "ПРОФИЛЬ:",
        ["Tour.WelcomeBadge"] = "ПЕРВЫЙ ЗАПУСК",
        ["Tour.WelcomeTitle"] = "Добро пожаловать в Kofge-Clicker",
        ["Tour.WelcomeText"] = "Пройдите короткий экскурс по основным функциям или навсегда пропустите его и сразу начните пользоваться приложением.",
        ["Tour.WelcomeHotkeys"] = "Выберите горячие клавиши, которыми будете пользоваться каждый день.",
        ["Tour.WelcomeBehavior"] = "Узнайте, как режимы и паттерны меняют поведение кликов.",
        ["Tour.WelcomeOptions"] = "Настройте запуск, трей и работу только в выбранном окне.",
        ["Tour.Step"] = "ШАГ {0} ИЗ {1}",
        ["Tour.Start"] = "Начать экскурс",
        ["Tour.Skip"] = "Больше не показывать",
        ["Tour.Back"] = "Назад",
        ["Tour.Next"] = "Далее",
        ["Tour.Finish"] = "Завершить",
        ["Tour.MainHotkeyTitle"] = "Выберите клавишу запуска кликов",
        ["Tour.MainHotkeyText"] = "Эта клавиша запускает кликер в режиме удержания или переключения. Выберите удобную клавишу клавиатуры или кнопку мыши, которая не конфликтует с приложением или игрой.",
        ["Tour.CurrentBinding"] = "Текущая клавиша",
        ["Tour.ChangeBinding"] = "Сменить клавишу",
        ["Tour.MainHotkeyHint"] = "Нажмите «Сменить клавишу», затем нажмите нужную клавишу клавиатуры или кнопку мыши. Сочетания с Ctrl, Shift и Alt также поддерживаются.",
        ["Tour.ModesTitle"] = "Выберите способ запуска и остановки",
        ["Tour.ModesText"] = "Нажмите на нужный режим ниже, чтобы выбрать его. Изменение применяется сразу и сохраняется отдельно в каждом профиле.",
        ["Tour.HoldText"] = "Клики идут только пока зажата клавиша запуска. Отпустите её, чтобы сразу остановить кликер.",
        ["Tour.ToggleText"] = "Одно нажатие запускает клики, следующее останавливает. Клавишу запуска не нужно удерживать.",
        ["Tour.PatternsTitle"] = "Настройте поведение с помощью паттернов",
        ["Tour.PatternsText"] = "Паттерн определяет действие на каждом такте CPS. Задержки и поведение CPS позволяют точно настроить отправку выбранного паттерна.",
        ["Tour.PatternStandard"] = "Отправляет один обычный клик за такт CPS.",
        ["Tour.PatternBurst"] = "Отправляет три клика как одно действие.",
        ["Tour.PatternDouble"] = "Отправляет два клика как одно действие.",
        ["Tour.PatternHoldBurst"] = "Позволяет настроить все доступные значения паттерна.",
        ["Tour.PatternRateText"] = "Фиксир. сохраняет ограничение выбранного CPS. Доп. клики позволяют паттерну добавлять действия сверх базовой частоты.",
        ["Tour.ServiceHotkeysTitle"] = "Настройте горячие клавиши",
        ["Tour.ServiceHotkeysText"] = "Эти команды работают, даже когда Kofge-Clicker находится в трее. Их можно изменить сейчас или оставить безопасные значения по умолчанию.",
        ["Tour.StartupTrayTitle"] = "Настройте запуск и трей",
        ["Tour.StartupTrayText"] = "Эти опции управляют запуском Kofge-Clicker и поведением кнопок окна.",
        ["Tour.StartupAdminText"] = "Следующие запуски Kofge-Clicker будут от имени администратора.",
        ["Tour.StartupHiddenText"] = "Kofge-Clicker сразу запускается в системном трее.",
        ["Tour.StartupWindowsText"] = "Kofge-Clicker запускается после входа в Windows.",
        ["Tour.StartupMinimizeText"] = "Кнопка «−» скрывает окно приложения в трей.",
        ["Tour.StartupCloseText"] = "Кнопка «×» скрывает окно в трей вместо выхода.",
        ["Tour.TargetWindowTitle"] = "Ограничьте клики выбранным окном",
        ["Tour.TargetWindowText"] = "Выберите, где разрешены клики, чтобы в остальных приложениях мышь работала обычно.",
        ["Tour.TargetFocusTitle"] = "Фокус окна",
        ["Tour.TargetFocusText"] = "Вне выбранного окна клики приостанавливаются и снова становятся доступны после возврата.",
        ["Tour.OptionsTitle"] = "Завершите настройкой приложения",
        ["Tour.OptionsText"] = "Вкладка «Опции» управляет запуском и треем. Целевое окно разрешает клики только тогда, когда выбранное приложение находится в фокусе.",
        ["Tour.OptionAdmin"] = "Следующие запуски будут выполняться с правами администратора для приложений с повышенными правами.",
        ["Tour.OptionTray"] = "Приложение запускается без главного окна и остаётся доступным в трее.",
        ["Tour.OptionStartup"] = "Kofge-Clicker автоматически запускается после входа в Windows.",
        ["Tour.OptionTarget"] = "Выберите приложение и включите функцию, чтобы клики работали только пока его окно находится в фокусе.",
        ["Validation.DuplicateHotkeys"] = "Клавиши кликера, аварийной остановки, окна, включения и профилей должны отличаться.",
        ["Validation.UnsafeMouseHotkeys"] = "Горячие клавиши нельзя назначать только на ЛКМ, ПКМ или СКМ. Боковые кнопки мыши разрешены.",
        ["Validation.ClickerActivation"] = "Запуск кликера",
        ["Validation.HotkeyConflict"] = "Нельзя назначить {0} для функции «{1}»: она пересекается с функцией «{2}» ({3}). Дополнительные Ctrl, Shift или Alt могут запустить обе функции одновременно. Выберите другую клавишу.",
        ["Validation.UnsafeServiceAssignment"] = "Горячей клавише нельзя назначить только ЛКМ, ПКМ или СКМ. Добавьте Ctrl, Shift или Alt либо выберите боковую кнопку мыши.",
        ["Settings.Saved"] = "Настройки сохранены",
        ["ReviewPrompt.Title"] = "Нравится Kofge-Clicker?",
        ["ReviewPrompt.Text"] = "Если Kofge-Clicker оказался полезным, вы можете оставить короткий отзыв. Это поможет другим пользователям и развитию проекта.",
        ["ReviewPrompt.LeaveReview"] = "Оставить отзыв",
        ["ReviewPrompt.Later"] = "Позже",
        ["ReviewPrompt.Never"] = "Больше не показывать",
        ["Update.ReadyTitle"] = "Обновление готово",
        ["Update.ReadyText"] = "Kofge-Clicker {0} загружен и готов к установке.\n\nПерезапустить приложение и автоматически заменить текущий EXE?",
        ["Update.FailedTitle"] = "Ошибка обновления",
        ["Update.FailedText"] = "Не удалось запустить автоматическое обновление. Текущая версия не изменена.",
        ["WhatsNew.Title"] = "Что нового в {0}",
        ["WhatsNew.Subtitle"] = "Кратко о главных улучшениях этой версии.",
        ["WhatsNew.HoverTitle"] = "Подсказки при наведении",
        ["WhatsNew.HoverText"] = "Наведите курсор на настройку и подождите секунду, чтобы увидеть понятное объяснение.",
        ["WhatsNew.TestTitle"] = "Встроенная проверка кликера",
        ["WhatsNew.TestText"] = "Во вкладке «Мышь» можно проверить клики и увидеть текущий CPS.",
        ["WhatsNew.HotkeysTitle"] = "Безопасные клавиши",
        ["WhatsNew.HotkeysText"] = "Конфликтующие сочетания теперь обнаруживаются до сохранения.",
        ["WhatsNew.FeedbackTitle"] = "Понятные уведомления",
        ["WhatsNew.FeedbackText"] = "Предупреждения и подтверждение сохранения теперь оформлены в стиле приложения.",
        ["WhatsNew.WindowTitle"] = "Окно «Что нового»",
        ["WhatsNew.WindowText"] = "Главные изменения каждой новой версии показываются один раз после обновления.",
        ["WhatsNew.LanguageButtonTitle"] = "Язык на главном экране",
        ["WhatsNew.LanguageButtonText"] = "Переключайтесь между русским и английским новой кнопкой RU/EN.",
        ["WhatsNew.LanguageDetectionTitle"] = "Автовыбор языка при первом запуске",
        ["WhatsNew.LanguageDetectionText"] = "Русская раскладка включает русский язык, а любая другая — английский.",
        ["WhatsNew.BothButtonsTitle"] = "Проверка ЛКМ и ПКМ",
        ["WhatsNew.BothButtonsText"] = "Встроенный тест теперь считает обе основные кнопки независимо от настройки Mouse.",
        ["WhatsNew.ClearLabelsTitle"] = "Понятные названия",
        ["WhatsNew.ClearLabelsText"] = "Элементы интерфейса получили понятные названия: «Клавиши», «Работа кликера» и «Клавиша запуска».",
        ["WhatsNew.ConsistentTermsTitle"] = "Единая терминология",
        ["WhatsNew.ConsistentTermsText"] = "Подсказки, обучение, проверки и строка состояния теперь используют те же понятные названия.",
        ["WhatsNew.TargetAppsTitle"] = "Понятный список приложений",
        ["WhatsNew.TargetAppsText"] = "Целевые окна теперь показываются с названиями и иконками приложений вместо технических имён процессов.",
        ["WhatsNew.WindowsStartupTitle"] = "Надёжный автозапуск Windows",
        ["WhatsNew.WindowsStartupText"] = "Автозапуск теперь использует системную папку автозагрузки, а реестр остаётся резервным вариантом.",
        ["WhatsNew.ProfileSafetyTitle"] = "Безопасный импорт профилей",
        ["WhatsNew.ProfileSafetyText"] = "Некорректные и неподдерживаемые файлы профилей теперь отклоняются до изменения настроек кликера.",
        ["WhatsNew.ReliabilityTitle"] = "Повышена стабильность",
        ["WhatsNew.ReliabilityText"] = "Ошибки автозапуска теперь отображаются правильно, а ресурсы сессий кликов освобождаются безопасно.",
        ["WhatsNew.DisplayScaleTitle"] = "Стабильный масштаб интерфейса",
        ["WhatsNew.DisplayScaleText"] = "Интерфейс остаётся стабильным и читаемым при масштабе Windows 100%, 125% и 150%.",
        ["WhatsNew.WindowPlacementTitle"] = "Безопасное размещение окон",
        ["WhatsNew.WindowPlacementText"] = "Главное окно и диалоги полностью видны на разрешениях вплоть до 1280x720.",
        ["WhatsNew.OnboardingLayoutTitle"] = "Улучшенное выравнивание обучения",
        ["WhatsNew.OnboardingLayoutText"] = "Текст приветствия и строки клавиш получили ровные интервалы, одинаковые отступы и больше не обрезаются.",
        ["WhatsNew.ClickTestAccuracyTitle"] = "Более точная проверка кликов",
        ["WhatsNew.ClickTestAccuracyText"] = "Текущий CPS теперь использует исходное время события мыши и точнее измеряется при нагрузке интерфейса.",
        ["WhatsNew.ProfileNotificationTitle"] = "Уведомления о смене профиля",
        ["WhatsNew.ProfileNotificationText"] = "После переключения уведомление выделяет название нового активного профиля.",
        ["WhatsNew.NotificationTextTitle"] = "Аккуратный текст уведомлений",
        ["WhatsNew.NotificationTextText"] = "Заголовки уведомлений теперь отображаются полностью без обрезанных букв.",
        ["WhatsNew.ReviewPromptTitle"] = "Ненавязчивое напоминание об отзыве",
        ["WhatsNew.ReviewPromptText"] = "Активным пользователям может показываться отложенное предложение оставить отзыв, не мешающее запуску и работе кликера.",
        ["WhatsNew.ReviewLinkTitle"] = "Страница отзыва на нужном языке",
        ["WhatsNew.ReviewLinkText"] = "Кнопка отзыва открывает русскую или английскую версию сайта в зависимости от языка приложения.",
        ["WhatsNew.Continue"] = "Понятно",
        ["App.AlreadyRunning"] = "Kofge-Clicker уже запущен.",
        ["App.InputHookFailedTitle"] = "Ввод недоступен",
        ["App.InputHookFailedText"] = "Kofge-Clicker не смог подключиться к глобальному вводу клавиатуры или мыши. Перезапустите приложение. Если ошибка повторится, проверьте, не блокирует ли его защитное ПО."
    };

    private static readonly IReadOnlyDictionary<string, string> LegacyEnglishLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clicker.Humanized"] = "Human-like clicks",
            ["Pattern.Burst"] = "Burst",
            ["Pattern.HoldThenBurst"] = "Hold then Burst",
            ["Pattern.HelpBurst"] = "Burst sends grouped taps.",
            ["Pattern.HelpHoldBurst"] = "Hold then Burst starts with a hold, then finishes with a burst pattern.",
            ["Status.PatternHoldBurst"] = "Hold+Burst",
            ["Tooltips.PatternType"] = "Selects how one click cycle is performed: standard, burst, double click or hold then burst.",
            ["Tooltips.PatternClicks"] = "Sets how many clicks are sent by burst-based patterns.",
            ["Tooltips.PatternGap"] = "Sets the pause in milliseconds between clicks inside a burst.",
            ["Tooltips.PatternHold"] = "Sets how long the first mouse press is held in Hold then Burst.",
            ["Tour.ModesText"] = "The Clicker tab contains two activation modes. You can switch between them at any time and save a different choice in each profile.",
            ["Tour.PatternBurst"] = "Sends exactly three clicks per pattern cycle.",
            ["Tour.PatternHoldBurst"] = "Holds first, then sends a click series.",
            ["Status.Humanized"] = "Humanized",
            ["Options.StartHidden"] = "Start hidden to tray",
            ["Options.MinimizeToTray"] = "Minimize button to tray",
            ["Tooltips.ClickerMode"] = "Hold clicks only while the hotkey is held. Toggle starts and stops clicking with each press.",
            ["Tray.ProfileChanged"] = "PROFILE SWITCHED: {0}"
        };

    private static readonly IReadOnlyDictionary<string, string> LegacyRussianLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Tabs.Hotkey"] = "Хоткеи",
            ["Buttons.ResetHotkeys"] = "Сбросить все хоткеи",
            ["Clicker.Enabled"] = "Включён",
            ["Clicker.Hotkey"] = "Хоткей",
            ["Mouse.TestBothButtonsInstruction"] = "Нажимайте здесь ЛКМ или ПКМ либо включите кликер назначенным хоткеем.",
            ["Hotkeys.Title"] = "Служебные хоткеи",
            ["Status.Hotkey"] = "Хоткей",
            ["Tooltips.TabHotkey"] = "Настройка служебных хоткеев для управления Kofge-Clicker.",
            ["Tooltips.ClickerEnabled"] = "Разрешает или запрещает запуск кликера назначенным хоткеем.",
            ["Tooltips.ClickerModeHold"] = "Кликер работает только пока вы удерживаете назначенный хоткей. Отпустите хоткей — клики остановятся.",
            ["Tooltips.ClickerModeToggle"] = "Одно нажатие хоткея запускает непрерывные клики. Повторное нажатие останавливает их.",
            ["Tooltips.HotkeyEnabled"] = "Включает или выключает основной параметр «Включён» без открытия приложения.",
            ["Tooltips.ResetHotkeys"] = "Возвращает всем хоткеям безопасные значения по умолчанию.",
            ["Tooltips.Status"] = "Первая строка показывает текущее состояние. Стрелка ниже показывает хоткей запуска → нажатая кнопка мыши.",
            ["Tour.WelcomeHotkeys"] = "Выберите хоткеи, которыми будете пользоваться каждый день.",
            ["Tour.MainHotkeyTitle"] = "Выберите хоткей запуска кликов",
            ["Tour.MainHotkeyText"] = "Этот хоткей запускает кликер в режиме удержания или переключения. Выберите удобную клавишу или кнопку мыши, которая не конфликтует с приложением или игрой.",
            ["Tour.CurrentBinding"] = "Текущий хоткей",
            ["Tour.ChangeBinding"] = "Сменить хоткей",
            ["Tour.MainHotkeyHint"] = "Нажмите «Сменить хоткей», затем нажмите нужную клавишу клавиатуры или кнопку мыши. Сочетания с Ctrl, Shift и Alt также поддерживаются.",
            ["Tour.HoldText"] = "Клики идут только пока зажат хоткей запуска. Отпустите его, чтобы сразу остановить кликер.",
            ["Tour.ToggleText"] = "Одно нажатие запускает клики, следующее останавливает. Хоткей не нужно удерживать.",
            ["Tour.ServiceHotkeysTitle"] = "Настройте служебные хоткеи",
            ["Validation.DuplicateHotkeys"] = "Хоткеи кликера, аварийной остановки, окна, включения и профилей должны отличаться.",
            ["Validation.UnsafeMouseHotkeys"] = "Служебные хоткеи нельзя назначать только на ЛКМ, ПКМ или СКМ. Боковые кнопки мыши разрешены.",
            ["Validation.UnsafeServiceAssignment"] = "Служебному хоткею нельзя назначить только ЛКМ, ПКМ или СКМ. Добавьте Ctrl, Shift или Alt либо выберите боковую кнопку мыши.",
            ["WhatsNew.HotkeysTitle"] = "Безопасные хоткеи",
            ["Buttons.Rename"] = "Переименовать",
            ["Buttons.Duplicate"] = "Дублировать",
            ["Clicker.Humanized"] = "Гуманизация",
            ["Pattern.Burst"] = "Серия",
            ["Pattern.HoldThenBurst"] = "Удержание и серия",
            ["Pattern.HelpBurst"] = "Серия отправляет сгруппированные клики.",
            ["Pattern.HelpHoldBurst"] = "Сначала выполняется удержание, затем серия кликов.",
            ["Status.PatternHoldBurst"] = "Удерж.+серия",
            ["Tooltips.PatternType"] = "Выбирает выполнение одного цикла: стандартный, серия, двойной клик или удержание с серией.",
            ["Tooltips.PatternClicks"] = "Задаёт количество кликов в паттернах с серией.",
            ["Tooltips.PatternGap"] = "Задаёт паузу в миллисекундах между кликами внутри серии.",
            ["Tooltips.PatternHold"] = "Задаёт длительность первого удержания в паттерне «Удержание с серией».",
            ["Tour.ModesText"] = "На вкладке «Кликер» доступны два режима активации. Их можно менять в любое время и сохранять отдельно в каждом профиле.",
            ["Tour.PatternBurst"] = "Отправляет ровно три клика за один цикл паттерна.",
            ["Tour.PatternHoldBurst"] = "Сначала удерживает, затем отправляет серию.",
            ["Pattern.Locked"] = "Фиксированный",
            ["Pattern.Amplified"] = "Усиленный",
            ["Pattern.HelpAmplified"] = "Усиленный режим позволяет паттерну добавлять клики сверх заданного CPS.",
            ["Hotkeys.PanicStop"] = "Аварийная остановка",
            ["Hotkeys.ToggleEnabled"] = "Включить кликер",
            ["Hotkeys.NextProfile"] = "Следующий профиль",
            ["Options.RunAsAdministrator"] = "От имени администратора",
            ["Status.Humanized"] = "Гуманизация",
            ["Options.CloseToTray"] = "Закрывать в трей",
            ["Tooltips.ClickerMode"] = "Выбирает, как назначенный хоткей запускает и останавливает клики.",
            ["Tray.ProfileChanged"] = "ПРОФИЛЬ ПЕРЕКЛЮЧЁН: {0}"
        };

    private static IReadOnlyDictionary<string, string> _current = English;

    internal static string CurrentLanguageCode { get; private set; } = DefaultLanguageCode;

    internal static void Initialize(string? languageCode)
    {
        var normalized = NormalizeLanguageCode(languageCode);
        EnsureLanguageFile(DefaultLanguageCode, English, LegacyEnglishLabels);
        EnsureLanguageFile(RussianLanguageCode, Russian, LegacyRussianLabels);

        var selectedDefaults = normalized == RussianLanguageCode ? Russian : English;
        var merged = new Dictionary<string, string>(English, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in selectedDefaults)
        {
            merged[entry.Key] = entry.Value;
        }

        foreach (var entry in LoadLanguageFile(normalized))
        {
            if (!string.IsNullOrWhiteSpace(entry.Value))
            {
                merged[entry.Key] = entry.Value;
            }
        }

        CurrentLanguageCode = normalized;
        _current = merged;
    }

    internal static string Get(string key, params object[] args)
    {
        var template = _current.TryGetValue(key, out var translated)
            ? translated
            : English.TryGetValue(key, out var fallback) ? fallback : key;
        if (args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    internal static string NormalizeLanguageCode(string? languageCode)
    {
        return languageCode?.Trim().StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
            ? RussianLanguageCode
            : DefaultLanguageCode;
    }

    private static string GetLanguagePath(string languageCode) =>
        Path.Combine(AppPaths.LanguagesDirectory, $"{languageCode}.json");

    private static Dictionary<string, string> LoadLanguageFile(string languageCode)
    {
        try
        {
            var path = GetLanguagePath(languageCode);
            if (!File.Exists(path))
            {
                return [];
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void EnsureLanguageFile(
        string languageCode,
        IReadOnlyDictionary<string, string> defaults,
        IReadOnlyDictionary<string, string> legacyDefaults)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LanguagesDirectory);
            var path = GetLanguagePath(languageCode);
            if (!File.Exists(path))
            {
                WriteLanguageFile(path, defaults);
                return;
            }

            Dictionary<string, string>? existing;
            try
            {
                existing = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            }
            catch
            {
                return;
            }

            if (existing is null)
            {
                return;
            }

            var changed = false;
            foreach (var entry in defaults)
            {
                if (existing.TryGetValue(entry.Key, out var existingValue))
                {
                    if (legacyDefaults.TryGetValue(entry.Key, out var legacyValue)
                        && string.Equals(existingValue, legacyValue, StringComparison.Ordinal))
                    {
                        existing[entry.Key] = entry.Value;
                        changed = true;
                    }

                    continue;
                }

                existing[entry.Key] = entry.Value;
                changed = true;
            }

            if (changed)
            {
                WriteLanguageFile(path, existing);
            }
        }
        catch
        {
        }
    }

    private static void WriteLanguageFile(string path, IReadOnlyDictionary<string, string> values)
    {
        var ordered = values.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        var json = JsonSerializer.Serialize(ordered, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, path, true);
    }
}
