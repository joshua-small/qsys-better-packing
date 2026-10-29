using QsysMods.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Designer.Modules.BetterPacking;

internal sealed class TransposePreview : Window
{
    internal TransposePreview(TransposeSession session, Window owner)
    {
        Owner = owner;
        Title = "Preview Text Controller Transpose";
        Width = 840; Height = 560; MinWidth = 540; MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Host.Theme(this);
        var root = new DockPanel { Margin = new Thickness(16) };
        var heading = new TextBlock
        {
            Text = $"{session.Plan.Families.Count} families × {session.Plan.Count} controls · Gap {session.Plan.Gap}\n"
                + "Family labels become column headers; control indices become rows. Only positions change. Review both layouts, then Apply once or Cancel.\n"
                + "Only the selected grid is shown. Check surrounding space before applying.",
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12)
        };
        DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 90, Padding = new Thickness(8) };
        var apply = new Button { Content = "Apply transpose", MinWidth = 140, Padding = new Thickness(8), Margin = new Thickness(8, 0, 0, 0) };
        cancel.Click += (_, _) => Close();
        apply.Click += (_, _) =>
        {
            apply.IsEnabled = false;
            try { session.Apply((kind, detail) => Host.RequiredReceipt("better-packing", kind, detail)); }
            catch (Exception error) { Host.Error("better-packing", error, true); }
            finally { Close(); }
        };
        footer.Children.Add(cancel); footer.Children.Add(apply);
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var views = new Grid();
        views.ColumnDefinitions.Add(new()); views.ColumnDefinitions.Add(new());
        for (int column = 0; column < 2; column++)
        {
            var panel = new DockPanel { Margin = new Thickness(column == 0 ? 0 : 8, 0, column == 0 ? 8 : 0, 0) };
            var title = new TextBlock { Text = column == 0 ? "Before" : "After", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(title, Dock.Top); panel.Children.Add(title);
            panel.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = Drawing(session.Plan, column == 1), VerticalAlignment = VerticalAlignment.Top });
            Grid.SetColumn(panel, column); views.Children.Add(panel);
        }
        root.Children.Add(views); Content = root;
        Closed += (_, _) => session.Dispose();
    }

    private static Canvas Drawing(TransposePlan plan, bool after)
    {
        var cells = plan.Moves.Select(m => new { Item = m.Before, X = after ? m.X : m.Before.X, Y = after ? m.Y : m.Before.Y }).ToArray();
        double minX = cells.Min(c => c.X), minY = cells.Min(c => c.Y);
        var canvas = new Canvas { Width = cells.Max(c => c.X + c.Item.Width) - minX + 2, Height = cells.Max(c => c.Y + c.Item.Height) - minY + 2 };
        foreach (var cell in cells)
        {
            string family = plan.Families.Single(f => f.Id == cell.Item.FamilyId).Name;
            var box = new Border
            {
                Width = cell.Item.Width, Height = cell.Item.Height, BorderThickness = new Thickness(1), BorderBrush = SystemColors.WindowTextBrush,
                Background = cell.Item.Index == 0 ? SystemColors.ControlLightBrush : SystemColors.WindowBrush,
                ToolTip = family + (cell.Item.Index == 0 ? " — label" : $" — index {cell.Item.Index}")
                    + $" ({cell.X}, {cell.Y}) · {cell.Item.Width} × {cell.Item.Height}",
                Child = new TextBlock { Text = cell.Item.Index == 0 ? family : cell.Item.Index.ToString(), Foreground = SystemColors.WindowTextBrush,
                    TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            Canvas.SetLeft(box, cell.X - minX + 1); Canvas.SetTop(box, cell.Y - minY + 1); canvas.Children.Add(box);
        }
        return canvas;
    }
}
