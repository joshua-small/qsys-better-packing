using System.Windows;
using System.Windows.Controls;

namespace Designer.Modules.BetterPacking;

internal sealed class PackingOptionsEditor
{
    private readonly Dictionary<(PackingEditor, PackingElement), ComboBox> choices = new();
    internal Grid View { get; } = new() { Margin = new Thickness(0, 12, 0, 12) };

    internal PackingOptionsEditor(PackingSettings value)
    {
        View.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        View.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        View.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        View.RowDefinitions.Add(new() { Height = GridLength.Auto });
        Add(new TextBlock { Text = "Element type", FontWeight = FontWeights.SemiBold }, 0, 0);
        Add(new TextBlock { Text = "Schematic", FontWeight = FontWeights.SemiBold }, 0, 1);
        Add(new TextBlock { Text = "UCI", FontWeight = FontWeights.SemiBold }, 0, 2);
        int row = 1;
        foreach (var (element, name) in PackingPolicy.Choices)
        {
            View.RowDefinitions.Add(new() { Height = GridLength.Auto });
            Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center }, row, 0);
            foreach (var editor in new[] { PackingEditor.Schematic, PackingEditor.Uci })
            {
                int column = editor == PackingEditor.Schematic ? 1 : 2;
                if (editor == PackingEditor.Uci && element == PackingElement.Component)
                {
                    Add(new TextBlock { Text = "—", VerticalAlignment = VerticalAlignment.Center, ToolTip = "Components are placed on schematic pages." }, row, column);
                    continue;
                }
                var options = editor == PackingEditor.Schematic ? value.Schematic : value.Uci;
                var choice = new ComboBox { MinWidth = 125, ItemsSource = new[] { "Native", "Better Packing" }, SelectedIndex = options.Enabled(element) ? 1 : 0,
                    ToolTip = editor + " · " + name };
                System.Windows.Automation.AutomationProperties.SetName(choice, editor + " " + name + " packing");
                choices.Add((editor, element), choice);
                Add(choice, row, column);
            }
            row++;
        }
    }

    private void Add(FrameworkElement element, int row, int column)
    {
        element.Margin = new Thickness(0, 3, column == 2 ? 0 : 12, 3);
        Grid.SetRow(element, row); Grid.SetColumn(element, column); View.Children.Add(element);
    }

    internal PackingElementOptions Read(PackingEditor editor)
    {
        bool On(PackingElement element) => choices.TryGetValue((editor, element), out var choice) && choice.SelectedIndex == 1;
        return new(On(PackingElement.Component), On(PackingElement.Layout), On(PackingElement.Control), On(PackingElement.Header),
            On(PackingElement.TextBlock), On(PackingElement.GroupBox), On(PackingElement.Image), On(PackingElement.Shape));
    }
}
