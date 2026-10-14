using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QsysMods.Core;

namespace Designer.Modules.BetterPacking;

public sealed record PackingSettings(int Gap = 20, bool ReplaceDefaultPackBehavior = true);
public sealed class PackingModule : ModModule
{
    private ModSettings<PackingSettings>? settings;
    private PackingCommandRouter? router;
    private Window? operation;
    public PackingModule() => Mods.Register("better-packing", Initialize);
    private void Initialize(Window main)
    {
        settings = Preferences.Register("better-packing", "Better Packing", () => new PackingSettings(), value =>
        {
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "Gap (schematic units)", Margin = new Thickness(0,0,0,6) });
            var gap = new TextBox { Text = value.Gap.ToString(), Width = 100, HorizontalAlignment = HorizontalAlignment.Left };
            var replace = new CheckBox { Content = "Replace Default Pack Behavior", IsChecked = value.ReplaceDefaultPackBehavior, Margin = new Thickness(0,16,0,8) };
            panel.Children.Add(gap); panel.Children.Add(replace);
            panel.Children.Add(new TextBlock { Text = "Use improved packing for schematic keyboard shortcuts, Tools > Pack, and right-click Pack actions. UCI packing keeps its native behavior.", TextWrapping = TextWrapping.Wrap });
            return new PreferenceEditor<PackingSettings>(panel, () => int.TryParse(gap.Text, out var number) ? new(number, replace.IsChecked == true) : throw new ArgumentException("Gap must be a whole number."));
        }, value => { if (value.Gap is < 0 or > 10000) throw new ArgumentException("Gap must be between 0 and 10000."); });
        var natives = new Dictionary<RoutedCommand, PackAlignment>();
        foreach (var alignment in new[] { PackAlignment.Left, PackAlignment.Right, PackAlignment.Top, PackAlignment.Bottom })
        {
            var command = Commands.Native("Pack" + alignment);
            if (main.CommandBindings.Cast<CommandBinding>().Count(b => ReferenceEquals(b.Command, command)) != 1)
                throw new InvalidOperationException("Expected one native packing binding: " + command.Name);
            natives.Add(command, alignment);
        }
        void Configure()
        {
            router?.Dispose(); router = null;
            if (settings.Value.ReplaceDefaultPackBehavior)
                router = new(main, natives, () => ReadContext(main), canvas => Host.Enabled && SelectionPacker.CanPackOn(canvas),
                    (alignment, canvas, name) => Pack(alignment, canvas, "native-command: " + name), error => Host.Error("better-packing", error, true));
        }
        Configure(); settings.Changed += Configure;
        main.Closed += (_, _) => { router?.Dispose(); settings.Changed -= Configure; };
        Commands.Register(new("better-packing/window", "Better Packing…", "Better Packing", Show, () => true, InMenu: true));
        foreach (var alignment in Enum.GetValues<PackAlignment>())
        {
            var selected = alignment;
            Commands.Register(new("better-packing/" + alignment.ToString().ToLowerInvariant(), Title(alignment), "Better Packing",
                () => Pack(selected, ReadContext(main).Canvas, "registered-command"),
                () => ReadContext(main) is { Focus: PackingFocus.Schematic, Canvas: { } canvas } && SelectionPacker.CanPackOn(canvas)));
        }
    }
    private static string Title(PackAlignment value) => value == PackAlignment.VerticalCenter ? "Vertical Pack Center" : "Pack " + value;
    private void Pack(PackAlignment alignment, object? canvas = null, string invocation = "operation-window")
        => SelectionPacker.Pack(alignment, settings!.Value.Gap, (kind, detail) => Host.RequiredReceipt("better-packing", kind, detail), canvas, invocation);
    private void Show()
    {
        if (operation is not null) { operation.Activate(); return; }
        operation = new Window { Title = "Better Packing", Owner = Application.Current.MainWindow, Width = 480, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        Host.Theme(operation);
        var body = new StackPanel { Margin = new Thickness(16) };
        body.Children.Add(new TextBlock { Text = "Pack the selected schematic components and Layout items.", TextWrapping = TextWrapping.Wrap });
        foreach (var alignment in Enum.GetValues<PackAlignment>())
        {
            var button = new Button { Content = Title(alignment), Margin = new Thickness(0,8,0,0), Padding = new Thickness(8) };
            button.Click += (_, _) => { try { Pack(alignment); } catch (Exception ex) { Host.Error("better-packing", ex, true); } }; body.Children.Add(button);
        }
        var preferences = new Button { Content = "Packing preferences…", Margin = new Thickness(0,16,0,0), Padding = new Thickness(8) };
        preferences.Click += (_, _) => Preferences.Open("better-packing"); body.Children.Add(preferences);
        operation.Content = body; operation.Closed += (_, _) => operation = null; operation.Show();
    }
    private static PackingCommandContext ReadContext(Window window)
    {
        var focused = Keyboard.FocusedElement as DependencyObject;
        for (var node = focused; node is not null; node = node is Visual ? VisualTreeHelper.GetParent(node) : (node as FrameworkContentElement)?.Parent)
            if (node is MenuItem or ContextMenu) { focused = FocusManager.GetFocusedElement(window) as DependencyObject; break; }
        if (focused is not UIElement element || !Host.Type("QSC.QSys.Graphic.GraphicCanvas").IsInstanceOfType(focused) || !element.IsVisible || Window.GetWindow(focused) != window)
            return new(PackingFocus.Blocked);
        return HostSelection.IsSchematicCanvas(focused) ? new(PackingFocus.Schematic, focused) : new(PackingFocus.OtherCanvas, focused);
    }
}
