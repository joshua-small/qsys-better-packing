using System.Collections;
using System.ComponentModel;
using System.Windows;

// Synthetic boundary only. These types exercise the production reader and Core
// transaction helper without loading Designer or any vendor assemblies.
namespace QSC.QSys
{
    public static class App { public static FixtureManager Manager = new(); }
    public sealed class FixtureDocument { public List<FixturePage> Pages { get; } = []; }
    public sealed class FixturePage { public List<object> AllChildren { get; } = []; }
    public sealed class FixtureManager
    {
        public FixtureDocument Document { get; set; } = new();
        public bool Dirty { get; set; }
        public bool Undoable => undo.Count > 0;
        public bool Redoable => redo.Count > 0;
        public bool IsLoading { get; set; }
        public bool IsInUndo { get; set; }
        public bool IsInEndOperation { get; set; }
        public bool Busy { get; set; }
        public bool AdjustOnEnd { get; set; }
        public int Starts { get; private set; }
        public int Ends { get; private set; }
        public int Cancels { get; private set; }
        public int Subscribers => Models.Sum(m => m.SubscriberCount);
        public List<Design.Object> Models { get; } = [];
        private Dictionary<Design.Object, Point>? pending;
        private readonly Stack<Dictionary<Design.Object, Point>> undo = new(), redo = new();
        private Dictionary<Design.Object, Point> Capture() => Models.ToDictionary(m => m, m => m.Position);
        private static void Restore(Dictionary<Design.Object, Point> states) { foreach (var (model, position) in states) model.Position = position; }
        public Guid StartOperation() { if (Busy) return Guid.Empty; pending = Capture(); Starts++; return Guid.NewGuid(); }
        public void CancelOperation() { Restore(pending!); Cancels++; pending = null; }
        public void EndOperation(Guid operation) { Ends++; if (pending is not null) { undo.Push(pending); redo.Clear(); pending = null; Dirty = true;
            if (AdjustOnEnd) Models[0].Position = new(Models[0].Position.X + 1, Models[0].Position.Y); } }
        public void Undo() { redo.Push(Capture()); Restore(undo.Pop()); }
        public void Redo() { undo.Push(Capture()); Restore(redo.Pop()); }
    }
}
namespace QSC.QSys.Commands { public static class Align { public static bool Can { get; set; } = true; } }
namespace QSC.QSys.ControlConnection
{
    public sealed class ConnectionStatus { public bool IsConnected { get; set; } }
    public static class Manager { public static ConnectionStatus Status { get; } = new(); public static bool IsEmulator => false; }
}
namespace QSC.QSys.Util
{
    public sealed class Selection : ArrayList
    {
        public static Selection Global = new();
        public object[] UnlockedObjects => this.Cast<object>().OfType<Graphic.Object>().Where(g => !g.IsLocked && !g.obj.IsLocked)
            .Select(g => (object)g.obj).Distinct().ToArray();
    }
}
namespace QSC.QSys.Design
{
    public class Notify : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? changed;
        public event PropertyChangedEventHandler? PropertyChanged { add => changed += value; remove => changed -= value; }
        public int SubscriberCount => changed?.GetInvocationList().Length ?? 0;
        public void Change(string name) => changed?.Invoke(this, new(name));
    }
    public class Object : Notify
    {
        private Point position;
        private Size size = new(80, 30);
        private string label = "";
        public bool FailNextMove { get; set; }
        public Point Position { get => position; set { if (FailNextMove) { FailNextMove = false; throw new Exception("setter failed"); } position = value; Change(nameof(Position)); } }
        public Size Size { get => size; set { size = value; Change(nameof(Size)); } }
        public string Label { get => label; set { label = value; Change(nameof(Label)); } }
        public bool IsAttached { get; set; } = true;
        public bool IsLocked { get; set; }
        public string Style { get; set; } = "original-style";
        public double Orientation { get; set; } = 90;
    }
    public sealed class ControlValue : Notify
    {
        private string name = "";
        public Guid Id { get; set; }
        public string Name { get => name; set { name = value; Change(nameof(Name)); } }
    }
    public class ControlStyle : Object { public ControlValue Value { get; set; } = new(); }
    public class ControllerControlStyle : ControlStyle { }
    public class Component : Object
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string ClassName { get; set; } = "device_controller_script";
        public string Specification { get; set; } = "";
    }
    public class Container : Object { public object[] Canvases { get; set; } = []; }
    public class ChannelGroup : Container { }
    public static class ComponentCache
    {
        public static Dictionary<Guid, Component> Components { get; } = [];
        public static Component? GetComponent(Guid id) => Components.GetValueOrDefault(id);
    }
    public static class ComponentHelper
    {
        public static T GetComponentProperty<T>(Component component, string codeName, T fallback) =>
            codeName == "control-config" ? (T)(object)component.Specification : fallback;
    }
}
namespace QSC.QSys.Design.Graphics { public class TextBlock : Design.Object { } }
namespace QSC.QSys.Graphic
{
    public sealed class GraphicCanvas { public QSC.QSys.FixturePage Root { get; set; } = new(); }
    public class Object
    {
        public Design.Object obj { get; set; } = null!;
        public GraphicCanvas GraphicCanvas { get; set; } = null!;
        public bool IsCanvasLocked { get; set; }
        public bool IsLocked { get; set; }
        public bool IsVisible { get; set; } = true;
        public Vector Offset { get; set; }
        public Vector BoundsOffset { get; set; }
        public Point Position => obj.Position + Offset;
        public Size Size { get; set; }
        public Rect rc => new(Position + BoundsOffset, Size);
    }
}
namespace QSC.QSysComponents { public static class DeviceControl { public static string ControlPropertyName => "control-config"; } }
namespace QsysMods.Core { public static class Host { public static bool Enabled { get; set; } = true; } }
