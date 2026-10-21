using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using KofgeClicker;

internal static class AuditChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static |
        BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly App = typeof(HotkeyHelper).Assembly;

    internal static void JournalEditIsolation()
    {
        var source = New("MacroDefinition");
        Set(source, "Name", "Audit recording");
        var sourceEvents = (IList)Get(source, "Events");
        for (var index = 0; index < 8; index++)
        {
            var item = New("MacroEvent");
            Set(item, "Type", Enum.Parse(Type("MacroEventType"), "MouseMove"));
            Set(item, "X", index);
            Set(item, "OffsetMicroseconds", (index + 1) * 1_000L);
            sourceEvents.Add(item);
        }

        var storage = New("MacroStorage", Path.Combine(Path.GetTempPath(), $"Kofge-Audit-{Guid.NewGuid():N}"));
        var formatType = typeof(Func<,>).MakeGenericType(Type("MacroEvent"), typeof(string));
        var format = Expression.Lambda(formatType, Expression.Constant("Audit action"),
            Expression.Parameter(Type("MacroEvent"))).Compile();
        using var dialog = (Form)New("MacroJournalDialog", source, storage, format);
        _ = dialog.Handle;
        _ = ((Control)Field(dialog, "_timeline")).Handle;
        Call(dialog, "SelectAllVisibleEvents");
        Assert(((int[])Field(dialog, "_selectedIndices")).Length == 8, "Select-all missed journal rows.");
        ((TextBox)Field(dialog, "_sleepInput")).Text = "5";
        Assert((bool)Call(dialog, "ApplyCurrentSleep")!, "Batch sleep edit failed.");
        Assert(((long[])Field(dialog, "_offsets")).SequenceEqual(
            Enumerable.Range(1, 8).Select(index => index * 5_000L)), "Batch sleep edit changed the wrong pauses.");
        ((TextBox)Field(dialog, "_xInput")).Text = "42";
        Assert((bool)Call(dialog, "ApplyCurrentCoordinates")!, "Batch coordinate edit failed.");
        Assert(((IList)Field(dialog, "_events")).Cast<object>().All(item => (int)Get(item, "X") == 42),
            "Batch coordinate edit missed rows.");
        Assert(sourceEvents.Cast<object>().Select(item => (int)Get(item, "X")).SequenceEqual(Enumerable.Range(0, 8)) &&
            sourceEvents.Cast<object>().Select(item => (long)Get(item, "OffsetMicroseconds")).SequenceEqual(
                Enumerable.Range(1, 8).Select(index => index * 1_000L)),
            "Unsaved journal edits mutated the source recording.");
        Call(dialog, "UndoJournalChange");
        Assert(((IList)Field(dialog, "_events")).Cast<object>().Select(item => (int)Get(item, "X"))
            .SequenceEqual(Enumerable.Range(0, 8)), "Undo did not restore coordinates.");
        Call(dialog, "RedoJournalChange");
        Assert(((IList)Field(dialog, "_events")).Cast<object>().All(item => (int)Get(item, "X") == 42),
            "Redo did not restore edited coordinates.");
        Call(dialog, "SelectAllVisibleEvents");
        Call(dialog, "DeleteSelectedEvents");
        Assert(((IList)Field(dialog, "_events")).Count == 0, "Deleting all rows left events behind.");
        Call(dialog, "UndoJournalChange");
        Assert(((IList)Field(dialog, "_events")).Count == 8, "Undo did not restore deleted rows.");
        Call(dialog, "ResetChanges");
        Assert(!(bool)Call(dialog, "HasAppliedChanges")!, "Reset left applied changes behind.");
        Assert(((long[])Field(dialog, "_offsets")).SequenceEqual(
            Enumerable.Range(1, 8).Select(index => index * 1_000L)), "Reset did not restore original timing.");
    }

    internal static void VersionAndHotkeySafety()
    {
        bool Newer(string candidate, string current) =>
            (bool)Static("UpdateChecker", "IsNewerVersion", candidate, current)!;
        Assert(!Newer("v1.0.8", "1.0.8.0") && !Newer("v1.0.7", "v1.0.8") &&
            Newer("v1.0.9", "v1.0.8") && Newer("v1.0.10", "v1.0.9") &&
            !Newer("not-a-version", "v1.0.8"), "Update version comparison permits an invalid upgrade.");
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unassigned = Static("MainForm", "GetSafeUniqueHotkey", "None", new[] { "F12" }, used, true);
        Assert(Equals(unassigned, "None") && used.Count == 0, "Empty hotkeys were assigned during repair.");
        var first = Static("MainForm", "GetSafeUniqueHotkey", "F8", new[] { "F8", "F9" }, used, true);
        var second = Static("MainForm", "GetSafeUniqueHotkey", "^F8", new[] { "F8", "F9" }, used, true);
        Assert(Equals(first, "F8") && Equals(second, "F9"), "Hotkey repair left conflicting primary keys.");
    }

    private static Type Type(string name) => App.GetType($"KofgeClicker.{name}", true)!;
    private static object New(string name, params object[] args) =>
        Activator.CreateInstance(Type(name), Members, null, args, null)!;
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;
    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;
    private static void Set(object target, string name, object value) => target.GetType().GetProperty(name, Members)!.SetValue(target, value);
    private static object? Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Members)!.Invoke(target, args);
    private static object? Static(string type, string name, params object[] args) =>
        Type(type).GetMethod(name, Members)!.Invoke(null, args);
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
