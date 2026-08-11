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
        ["Common.None"] = "None",
        ["Common.RestartRequired"] = "Restart required",
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
        ["Clicker.Humanized"] = "Human-like clicks",
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
        ["Pattern.Burst"] = "Burst",
        ["Pattern.DoubleClick"] = "Double Click",
        ["Pattern.HoldThenBurst"] = "Hold then Burst",
        ["Pattern.HelpAmplified"] = "Amplified lets the pattern add extra taps above the target CPS.",
        ["Pattern.HelpLocked"] = "Locked keeps the output tied to your target CPS.",
        ["Pattern.HelpStandard"] = "Standard sends one tap per CPS tick.",
        ["Pattern.HelpBurst"] = "Burst sends grouped taps.",
        ["Pattern.HelpDouble"] = "Double Click sends paired taps.",
        ["Pattern.HelpHoldBurst"] = "Hold then Burst starts with a hold, then finishes with a burst pattern.",
        ["Mouse.Title"] = "Mouse Button",
        ["Mouse.Mouse"] = "Mouse",
        ["Mouse.Left"] = "Left",
        ["Mouse.Right"] = "Right",
        ["Mouse.Help"] = "Choose which mouse button Kofge-Clicker presses.\nThis setting is saved with the current profile.",
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
        ["Options.LanguageRestart"] = "Restart Kofge-Clicker to apply the selected language.",
        ["Options.AdminRestart"] = "Restart Kofge-Clicker for this setting to take effect.",
        ["Status.Profile"] = "Profile",
        ["Status.Status"] = "Status",
        ["Status.Click"] = "Click",
        ["Status.Hotkey"] = "Hotkey",
        ["Status.Mode"] = "Mode",
        ["Status.Target"] = "Target",
        ["Status.Humanized"] = "Humanized",
        ["Status.RateLocked"] = "Rate Locked",
        ["Status.RateAmplified"] = "Rate Amplified",
        ["Status.PatternStandard"] = "Std",
        ["Status.PatternDouble"] = "Double",
        ["Status.PatternHoldBurst"] = "Hold+Burst",
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
        ["Validation.DuplicateHotkeys"] = "Clicker, Panic Stop, Show Window, Toggle Enabled and Next Profile hotkeys must be different.",
        ["Validation.UnsafeMouseHotkeys"] = "Panic Stop, Show Window, Toggle Enabled and Next Profile cannot use bare LMB, RMB or MMB. Side mouse buttons are allowed.",
        ["Update.ReadyTitle"] = "Update ready",
        ["Update.ReadyText"] = "Kofge-Clicker {0} has been downloaded and is ready to install.\n\nRestart now to replace the current EXE automatically?",
        ["Update.FailedTitle"] = "Update failed",
        ["Update.FailedText"] = "The automatic updater could not be started. The current version was not changed.",
        ["App.AlreadyRunning"] = "Kofge-Clicker is already running."
    };

    private static readonly Dictionary<string, string> Russian = new(English, StringComparer.OrdinalIgnoreCase)
    {
        ["Common.On"] = "ВКЛ",
        ["Common.Off"] = "ВЫКЛ",
        ["Common.Cancel"] = "Отмена",
        ["Common.None"] = "Нет",
        ["Common.RestartRequired"] = "Требуется перезапуск",
        ["Tabs.Clicker"] = "Кликер",
        ["Tabs.Pattern"] = "Паттерн",
        ["Tabs.Mouse"] = "Мышь",
        ["Tabs.Hotkey"] = "Хоткеи",
        ["Tabs.Profiles"] = "Профили",
        ["Tabs.Options"] = "Опции",
        ["Buttons.Apply"] = "Применить",
        ["Buttons.Close"] = "Закрыть",
        ["Buttons.Bind"] = "Назначить",
        ["Buttons.Refresh"] = "Обновить",
        ["Buttons.ResetHotkeys"] = "Сбросить все хоткеи",
        ["Buttons.New"] = "Создать",
        ["Buttons.Rename"] = "Переимен.",
        ["Buttons.Duplicate"] = "Дублир.",
        ["Buttons.Delete"] = "Удалить",
        ["Buttons.Export"] = "Экспорт",
        ["Buttons.Import"] = "Импорт",
        ["Buttons.SetStartup"] = "Для запуска",
        ["Clicker.Title"] = "Кликер",
        ["Clicker.Enabled"] = "Включён",
        ["Clicker.Hotkey"] = "Хоткей",
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
        ["Pattern.Burst"] = "Серия",
        ["Pattern.DoubleClick"] = "Двойной клик",
        ["Pattern.HoldThenBurst"] = "Удержание и серия",
        ["Pattern.HelpAmplified"] = "Дополнительные клики позволяют паттерну добавлять нажатия сверх заданного CPS.",
        ["Pattern.HelpLocked"] = "Фиксированный режим удерживает частоту на уровне заданного CPS.",
        ["Pattern.HelpStandard"] = "Стандартный паттерн отправляет один клик за такт CPS.",
        ["Pattern.HelpBurst"] = "Серия отправляет сгруппированные клики.",
        ["Pattern.HelpDouble"] = "Двойной клик отправляет клики парами.",
        ["Pattern.HelpHoldBurst"] = "Сначала выполняется удержание, затем серия кликов.",
        ["Mouse.Title"] = "Кнопка мыши",
        ["Mouse.Mouse"] = "Мышь",
        ["Mouse.Left"] = "Левая",
        ["Mouse.Right"] = "Правая",
        ["Mouse.Help"] = "Выберите кнопку мыши, которую будет нажимать Kofge-Clicker.\nНастройка сохраняется в текущем профиле.",
        ["Hotkeys.Title"] = "Служебные хоткеи",
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
        ["Options.LanguageRestart"] = "Перезапустите Kofge-Clicker, чтобы применить выбранный язык.",
        ["Options.AdminRestart"] = "Перезапустите Kofge-Clicker, чтобы применить эту настройку.",
        ["Status.Profile"] = "Профиль",
        ["Status.Status"] = "Статус",
        ["Status.Click"] = "Клик",
        ["Status.Hotkey"] = "Хоткей",
        ["Status.Mode"] = "Режим",
        ["Status.Target"] = "Цель",
        ["Status.Humanized"] = "Естеств. клики",
        ["Status.RateLocked"] = "CPS фиксирован",
        ["Status.RateAmplified"] = "CPS усилен",
        ["Status.PatternStandard"] = "Станд.",
        ["Status.PatternDouble"] = "Двойной",
        ["Status.PatternHoldBurst"] = "Удерж.+серия",
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
        ["Validation.DuplicateHotkeys"] = "Хоткеи кликера, аварийной остановки, окна, включения и профилей должны отличаться.",
        ["Validation.UnsafeMouseHotkeys"] = "Служебные хоткеи нельзя назначать только на ЛКМ, ПКМ или СКМ. Боковые кнопки мыши разрешены.",
        ["Update.ReadyTitle"] = "Обновление готово",
        ["Update.ReadyText"] = "Kofge-Clicker {0} загружен и готов к установке.\n\nПерезапустить приложение и автоматически заменить текущий EXE?",
        ["Update.FailedTitle"] = "Ошибка обновления",
        ["Update.FailedText"] = "Не удалось запустить автоматическое обновление. Текущая версия не изменена.",
        ["App.AlreadyRunning"] = "Kofge-Clicker уже запущен."
    };

    private static readonly IReadOnlyDictionary<string, string> LegacyEnglishLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clicker.Humanized"] = "Humanized",
            ["Options.StartHidden"] = "Start hidden to tray",
            ["Options.MinimizeToTray"] = "Minimize button to tray"
        };

    private static readonly IReadOnlyDictionary<string, string> LegacyRussianLabels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Buttons.Rename"] = "Переименовать",
            ["Buttons.Duplicate"] = "Дублировать",
            ["Clicker.Humanized"] = "Гуманизация",
            ["Pattern.Locked"] = "Фиксированный",
            ["Pattern.Amplified"] = "Усиленный",
            ["Pattern.HelpAmplified"] = "Усиленный режим позволяет паттерну добавлять клики сверх заданного CPS.",
            ["Hotkeys.PanicStop"] = "Аварийная остановка",
            ["Hotkeys.ToggleEnabled"] = "Включить кликер",
            ["Hotkeys.NextProfile"] = "Следующий профиль",
            ["Options.RunAsAdministrator"] = "От имени администратора",
            ["Status.Humanized"] = "Гуманизация",
            ["Options.CloseToTray"] = "Закрывать в трей"
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
