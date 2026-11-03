using System.Runtime.CompilerServices;

namespace KofgeClicker;

internal static partial class PixelUiPreview
{
    private static readonly ConditionalWeakTable<Control, object> Observed = new();
    private static readonly ConditionalWeakTable<ToolStripDropDown, object> StyledMenus = new();

    internal static Color MapColor(Color value) => UiPalette.MapColor(value);

    internal static void ApplyTree(Control root)
    {
        if (!Enabled || root.IsDisposed) return;
        Observed.GetValue(root, static control =>
        {
            control.ControlAdded += (_, e) =>
            {
                if (e.Control is not null) ApplyTree(e.Control);
            };
            return new object();
        });
        if (root.Font.FontFamily.Name != "Monocraft")
        {
            var pixels = root.Font.Unit == GraphicsUnit.Pixel ? root.Font.Size : root.Font.SizeInPoints * 96f / 72f;
            root.Font = (Font)Font(pixels).Clone();
        }
        if (root is Form) root.BackColor = Background;
        else if (root.BackColor.A != 0)
            root.BackColor = root.BackColor == SystemColors.Control || root.BackColor == SystemColors.Window
                ? Surface : MapColor(root.BackColor);
        root.ForeColor = root.ForeColor == Color.Black || root.ForeColor == SystemColors.ControlText ? Text : MapColor(root.ForeColor);
        switch (root)
        {
            case AccentButton button:
                button.TextOffset = Point.Empty;
                button.UseFilledBorderRing = true;
                break;
            case RoundedPanel panel:
                panel.FillColor = MapColor(panel.FillColor);
                panel.BackColor = panel.FillColor;
                panel.BorderColor = Border;
                panel.DrawShadow = false;
                break;
            case InfoPill pill:
                pill.FillColor = Surface;
                pill.BorderColor = Border;
                break;
            case PillValueEditor editor:
                editor.FillColor = Surface;
                editor.BorderColor = Border;
                break;
            case PillDropdown dropdown:
                dropdown.FillColor = Surface;
                dropdown.BorderColor = Border;
                break;
            case ToggleSwitchCheckBox toggle:
                toggle.CheckedFillColor = Accent;
                toggle.CheckedBorderColor = AccentEdge;
                toggle.UncheckedFillColor = UiPalette.ToggleOff;
                toggle.UncheckedBorderColor = Border;
                break;
            case TextBox input:
                input.BackColor = Surface;
                input.ForeColor = Text;
                break;
            case ListView list:
                list.BackColor = Surface;
                list.ForeColor = Text;
                break;
        }
        foreach (Control child in root.Controls) ApplyTree(child);
        root.Invalidate();
    }

    internal static void ApplyMenu(ContextMenuStrip menu)
    {
        if (!Enabled) return;
        StyleDropDown(menu, UiTheme.SmallFont);
        StyleMenuItems(menu.Items, menu.Font);
    }

    private static void StyleDropDown(ToolStripDropDown menu, Font font)
    {
        StyledMenus.GetValue(menu, static owner =>
        {
            owner.Opening += (_, _) => StyleMenuItems(owner.Items, owner.Font);
            return new object();
        });
        menu.Font = font;
        menu.BackColor = Surface;
        menu.ForeColor = Text;
        menu.DropShadowEnabled = false;
        if (menu.Renderer is not PixelMenuRenderer) menu.Renderer = new PixelMenuRenderer();
    }

    private static void StyleMenuItems(ToolStripItemCollection items, Font font)
    {
        foreach (ToolStripItem item in items)
        {
            item.Font = font;
            item.ForeColor = item.Enabled ? Text : UiTheme.TextMuted;
            if (item is ToolStripMenuItem { HasDropDownItems: true } branch)
            {
                StyleDropDown(branch.DropDown, font);
                StyleMenuItems(branch.DropDownItems, font);
            }
        }
    }

    private sealed class PixelMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(Surface);
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Configure(e.Graphics);
            var isChecked = e.Item is ToolStripMenuItem { Checked: true };
            using var fill = new SolidBrush(e.Item.Selected && e.Item.Enabled ? Accent : isChecked ? Frame : Surface);
            e.Graphics.FillRectangle(fill, new Rectangle(Point.Empty, e.Item.Size));
        }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // The selected value is marked by the row fill, not a native checkbox.
        }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            => DrawText(e.Graphics, e.Text ?? string.Empty, e.TextFont ?? UiTheme.SmallFont, e.TextRectangle,
                e.Item.Enabled ? Text : UiTheme.TextMuted, ContentAlignment.MiddleLeft);
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, 6, e.Item.Height / 2, e.Item.Width - 6, e.Item.Height / 2);
        }
    }
}
