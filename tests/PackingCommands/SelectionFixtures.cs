using System.Windows;
using System.Windows.Controls;
using Designer.Modules.BetterPacking;

// Minimal host boundary fixtures, not vendor implementation or Designer acceptance.
// The production reader, planner, preferences UI and WPF router run against these.
internal sealed class FixtureCanvas : Canvas
{
    internal PackingEditor Editor { get; set; } = PackingEditor.Schematic;
    public bool IsUciPage => Editor == PackingEditor.Uci;
}
namespace QSC.QSys.Commands
{
    public static class Align { public static bool Can { get; set; } = true; }
}
namespace QSC.QSys.Design
{
    public class Object
    {
        public Point Position { get; set; }
        public Size Size { get; set; } = new(80, 30);
        public object Binding { get; } = new();
    }
    public class Component : Object { }
    public class DerivedComponent : Component { }
    public class Container : Object { }
    public class ChannelGroup : Object { }
    public class ContainerPin : Object { }
    public class ControlStyle : Object { }
    public class ControllerControlStyle : ControlStyle { }
    public class ControlSurface : Object { }
    public class GraphicImage : Object { }
    public class Group : Object { }
}
namespace QSC.QSys.Design.Graphics
{
    public class Popup : Design.Object { }
    public class Header : Design.Object { }
    public class TextBlock : Design.Object { }
    public class GroupBox : Design.Object { }
    public class Region : Design.Object { }
    public class Flexbox : Design.Object { }
}
namespace QSC.QSys.Design.Mux
{
    public class Muxer : Design.Object { }
    public class Demuxer : Design.Object { }
}
namespace QSC.QSys.Graphic
{
    public class Object
    {
        public QSC.QSys.Design.Object obj { get; init; } = null!;
        public object GraphicCanvas { get; set; } = null!;
        public bool IsCanvasLocked { get; set; }
        public bool IsLocked { get; set; }
        public bool IsVisible { get; set; } = true;
        public Vector Offset { get; set; }
        public Vector BoundsOffset { get; set; }
        public Point Position => obj.Position + Offset;
        public Size Size { get; set; } = new(80, 30);
        public Rect rc => new(Position + BoundsOffset, Size);
    }
}
namespace QsysMods.Core
{
    public static class HostSelection
    {
        public static QSC.QSys.Graphic.Object[] Graphics { get; set; } = [];
        public static int Operations { get; set; }
        public static bool FailOperation { get; set; }
        public static object GlobalSelection => Graphics;
        public static object[] SelectedModels() => Graphics.Select(g => (object)g.obj).Distinct().ToArray();
        public static Type HostType(string name) => typeof(HostSelection).Assembly.GetType(name, true)!;
        public static object RequiredProperty(object instance, string name) => instance.GetType().GetProperty(name)!.GetValue(instance)!;
        public static bool IsSchematicCanvas(object canvas) => ((FixtureCanvas)canvas).Editor == PackingEditor.Schematic;
        public static string ApplyPositions(IReadOnlyList<(object Model, Point After)> targets,
            string kind, object parameters, string message, Func<string, object, string> receipt)
        {
            if (FailOperation) throw new InvalidOperationException("Fixture operation rejected");
            Operations++;
            foreach (var (model, after) in targets) ((QSC.QSys.Design.Object)model).Position = after;
            return receipt(kind, parameters);
        }
    }
}
