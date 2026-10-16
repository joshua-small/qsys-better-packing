using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QsysMods.Core;

namespace Designer.Modules.BetterPacking;

public sealed class PackingModule : ModModule
{
    private const string OverrideGroup = "better-packing/default-controls";
    private ModSettings<PackingSettings>? settings;
    private Window? operation;
    public PackingModule() => Mods.Register("better-packing", Initialize);
    private void Initialize(Window main)
    {
        ModControls.RegisterReplacementGroup(OverrideGroup, store =>
        {
            var old = store.Read("better-packing", () => new PackingSettings());
            var controls = store.Read(ModControls.SettingsId, ControlSettings.Empty);
            return PackingPolicy.LegacyOverridesEnabled(old, controls.Decisions.ToDictionary(p => p.Key, p => p.Value.Accepted));
        });
        settings = Preferences.RegisterWithControls("better-packing", "Better Packing", () => new PackingSettings(ControlsManagedByCore: true), (value, draft) =>
        {
            var panel = new StackPanel { Margin = new Thickness(16) };
            panel.Children.Add(new TextBlock { Text = "Gap (canvas units)", Margin = new Thickness(0,0,0,6) });
            var gap = new TextBox { Text = value.Gap.ToString(), Width = 100, HorizontalAlignment = HorizontalAlignment.Left };
            panel.Children.Add(gap);
            var choices = new PackingOptionsEditor(value);
            panel.Children.Add(choices.View);
            panel.Children.Add(new TextBlock { Text = "Choose which element types use the gap above. If a selection includes a type set to Native, the entire selection uses native packing. Layout includes Containers, Channel Groups, muxes, popup buttons and internal connectors; adjacent internal connectors remain flush.", TextWrapping = TextWrapping.Wrap });
            var controls = ControlGroupPreference.Create(draft, OverrideGroup, "Disable default Q-SYS control overrides");
            controls.Margin = new Thickness(0, 20, 0, 0); panel.Children.Add(controls);
            panel.Children.Add(new TextBlock { Text = "The master switch above restores every native Pack action. Your shortcut assignments are kept. Groups, Flexboxes, other unlisted types and component control panels always use native packing. The Better Packing window explicitly packs supported items regardless of these routing choices.", TextWrapping = TextWrapping.Wrap });
            return new PreferenceEditor<PackingSettings>(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                () => int.TryParse(gap.Text, out var number) ? new PackingSettings(number, value.ReplaceDefaultPackBehavior, true)
                { Schematic = choices.Read(PackingEditor.Schematic), Uci = choices.Read(PackingEditor.Uci) } : throw new ArgumentException("Gap must be a whole number."));
        }, value => value.Validate());
        settings.Changed += CommandManager.InvalidateRequerySuggested;
        foreach (var alignment in Enum.GetValues<PackAlignment>())
        {
            var command = Commands.Native(alignment == PackAlignment.Center ? "HorizontalPackCenter"
                : alignment == PackAlignment.VerticalCenter ? "VerticalPackCenter" : "Pack" + alignment);
            if (main.CommandBindings.Cast<CommandBinding>().Count(b => ReferenceEquals(b.Command, command)) != 1)
                throw new InvalidOperationException("Expected one native packing binding: " + command.Name);
            Commands.RegisterNativeAlias("better-packing/" + alignment.ToString().ToLowerInvariant(), command);
            var selected = alignment;
            ModControls.RegisterReplacement(new("better-packing/replace-" + alignment.ToString().ToLowerInvariant(),
                "better-packing", "Better Packing", "1", Commands.NativeId(command), Title(alignment),
                "Schematic/UCI element choices in Better Packing Preferences; mixed selections use Native if any selected type uses Native.", Scope: ControlScope.Any)
                { ReplacementGroupId = OverrideGroup }, command,
                () =>
                {
                    var context = ReadContext(main);
                    return new(context.Focus == PackingFocus.OtherCanvas ? ReplacementAvailability.OutsideScope
                        : (context.Focus is PackingFocus.Schematic or PackingFocus.Uci) && context.Canvas is { } canvas && Host.Enabled
                            ? SelectionPacker.Availability(canvas, settings.Value) : ReplacementAvailability.Blocked, context.Canvas);
                }, canvas => Pack(selected, canvas, "native-command: " + command.Name));
        }
        Commands.Register(new("better-packing/window", "Better Packing…", "Better Packing", Show, () => true, InMenu: true));
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
        body.Children.Add(new TextBlock { Text = "Apply Better Packing to the selected supported schematic or UCI items. These buttons always use Better Packing and the saved gap. Tools > Pack follows your element preferences.", TextWrapping = TextWrapping.Wrap });
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
        return new(SelectionPacker.EditorForCanvas(focused) switch
        { PackingEditor.Schematic => PackingFocus.Schematic, PackingEditor.Uci => PackingFocus.Uci, _ => PackingFocus.OtherCanvas }, focused);
    }
}
