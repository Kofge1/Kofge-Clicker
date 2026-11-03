using System.Runtime.CompilerServices;

namespace KofgeClicker;

internal static class ClassicMenuTheme
{
    private static readonly ConditionalWeakTable<ToolStripDropDown, object> StyledMenus = new();

    internal static void Apply(ToolStripDropDown menu)
    {
        if (PixelUiPreview.Enabled) return;
        StyledMenus.GetValue(menu, static owner =>
        {
            owner.Opening += (_, _) => StyleItems(owner.Items);
            return new object();
        });
        menu.BackColor = UiTheme.Surface;
        menu.ForeColor = UiTheme.TextPrimary;
        if (menu.Renderer is not PillDropdown.PillMenuRenderer)
            menu.Renderer = new PillDropdown.PillMenuRenderer();
        StyleItems(menu.Items);
    }

    private static void StyleItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = item.Enabled ? UiTheme.TextPrimary : UiTheme.TextMuted;
            if (item is ToolStripMenuItem { HasDropDownItems: true } branch) Apply(branch.DropDown);
        }
    }
}
