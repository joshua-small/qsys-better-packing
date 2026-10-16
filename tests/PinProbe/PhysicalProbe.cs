using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Designer.Modules.PinPackingLabProbe;

// Guarded native-fixture setup and observation only. Actual drags are external
// physical input, never a call to Designer's movement implementation.
public sealed partial class PinProbe
{
    private object physicalManager = null!, physicalRoot = null!, physicalCanvas = null!;
    private object[] physicalOwners = [], physicalChosen = [];
    private readonly List<object> physicalChecks = [];
    private readonly Dictionary<object, (object A, object B)> wireIdentity = [];
    private string lastCommand = "";
    private bool busy;
    private object Field(object o, string n) => o.GetType().GetField(n, Instance)!.GetValue(o)!;
    private object GraphicOf(object model) => Graphics().Single(g => T("Graphic.Object").IsInstanceOfType(g) && ReferenceEquals(P(g, "obj"), model));
    private object Mapped(string kind, object model) => Call(T("Graphic.Maps").GetField(kind, Static)!.GetValue(null)!, "Get", model)!;
    private object[] Wires() => Descendants(physicalRoot).Where(T("Design.Wire").IsInstanceOfType).ToArray();
    private void Operation(Action action)
    {
        var token = Call(physicalManager, "StartOperation")!;
        try { action(); } finally { Call(physicalManager, "EndOperation", token); }
    }
    private void Choose(object[] models)
    {
        var selection = T("Util.Selection").GetField("Global", Static)!.GetValue(null)!;
        Call(selection, "Clear");
        var add = selection.GetType().GetMethod("Add", Instance, null, [T("Util.ISelectable"), typeof(bool)], null)!;
        foreach (var model in models) add.Invoke(selection, [GraphicOf(model), false]);
        Keyboard.Focus((IInputElement)physicalCanvas);
    }
    private async Task OpenPhysical(int index)
    {
        if (index < 0) { physicalCanvas = P(GraphicOf(Items(P(physicalRoot,"AllChildren")).First(T("Design.Component").IsInstanceOfType)),"GraphicCanvas"); return; }
        var owner = physicalOwners[index]; var graphic = GraphicOf(owner);
        var open = T("Tools.TraverseTool").GetMethods(Static).Single(m => m.Name == "OpenWindow" && m.GetParameters().Length == 6);
        Require((bool)open.Invoke(null, [P(graphic,"GraphicCanvas"), graphic, false, null, null, null])!, "Could not open fixture.");
        await Idle();
        var page = Items(P(owner,"Canvases"))[0];
        physicalChosen = Items(P(page,"AllChildren")).Where(m => T("Design.ContainerPin").IsInstanceOfType(m) || T("Design.Component").IsInstanceOfType(m)).ToArray();
        physicalCanvas = P(GraphicOf(physicalChosen[0]),"GraphicCanvas");
        Set(physicalCanvas,"CanvasScaleFactor",2.0);
        Choose([]);
    }
    private static async Task Idle() => await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
    private async Task StartPhysical(object manager, object root)
    {
        using (var isolation=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"profile","isolation.json"))))
            foreach(var property in isolation.RootElement.EnumerateObject())
                Require(property.Value.GetString()!.StartsWith(Path.Combine(AppContext.BaseDirectory,"profile")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Native preferences were not isolated.");
        physicalManager=manager; physicalRoot=root;
        physicalOwners=Descendants(root).Where(m => T("Design.Container").IsInstanceOfType(m) || T("Design.ChannelGroup").IsInstanceOfType(m)).ToArray();
        Require(physicalOwners.Length == 3, "Requires the saved three-scope geometry fixture.");
        if(config!.PhysicalReopen || config.PhysicalResume)
        {
            for(int scope=0;scope<physicalOwners.Length;scope++) await OpenPhysical(scope);
            foreach(var wire in Wires()) wireIdentity.Add(wire,(P(wire,"PinA"),P(wire,"PinB")));
            Require(wireIdentity.Count==9,"Reopened wire count mismatch.");
            if(config.PhysicalReopen) { WritePhysical("reopened"); return; }
            await OpenPhysical(0); WritePhysical("ready"); BeginPhysicalCommands(); return;
        }
        for (int scope=0; scope<physicalOwners.Length; scope++)
        {
            await OpenPhysical(scope);
            var page=P(physicalCanvas,"Root");
            var gains=physicalChosen.Where(T("Design.Component").IsInstanceOfType).ToArray();
            var connectors=physicalChosen.Where(T("Design.ContainerPin").IsInstanceOfType).ToArray();
            Require(gains.Length==2, "Expected two Gain components per scope.");
            Operation(() =>
            {
                for(int i=0;i<gains.Length;i++) { Set(gains[i],"Position",new Point(200+200*i,180+120*i)); Set(gains[i],"Label","Drag Gain "+(i+1)); }
                int ins=0,outs=0;
                foreach(var connector in connectors)
                {
                    var pin=Items(P(connector,"Pins")).Single();
                    bool source=Field(pin,"Direction").ToString()=="Output";
                    int order=source?ins++:outs++;
                    Set(connector,"Position",new Point(source?50:600,100+order*80));
                    Set(connector,"Label",(source?"Input ":"Output ")+(order+1));
                    if(order!=0) continue;
                    var target=Items(P(gains[source?0:1],"Pins")).First(p => Field(p,"Direction").ToString()!=(source?"Output":"Input")
                        && (P(pin,"WiringDomain").ToString()=="Unknown" || P(p,"WiringDomain").Equals(P(pin,"WiringDomain"))));
                    if(P(pin,"WiringDomain").ToString()=="Unknown") Set(pin,"WiringDomain",P(target,"WiringDomain"));
                    T("Tools.WireTool").GetMethod("CreateWire",Static,null,[T("Design.Canvas"),T("Design.Pin"),T("Design.Pin")],null)!.Invoke(null,[page,pin,target]);
                }
                // A component-to-component audio connection gives packing both
                // moved-to-moved and moved-to-stationary attached-wire cases.
                var a=Items(P(gains[0],"Pins")).First(p=>Field(p,"Direction").ToString()=="Output" && !(bool)Field(p,"UserCreated"));
                var b=Items(P(gains[1],"Pins")).First(p=>Field(p,"Direction").ToString()=="Input" && !(bool)Field(p,"UserCreated"));
                T("Tools.WireTool").GetMethod("CreateWire",Static,null,[T("Design.Canvas"),T("Design.Pin"),T("Design.Pin")],null)!.Invoke(null,[page,a,b]);
            });
            await Idle();
        }
        foreach(var wire in Wires()) wireIdentity.Add(wire,(P(wire,"PinA"),P(wire,"PinB")));
        Require(wireIdentity.Count==9,"Expected nine attached fixture wires.");
        await OpenPhysical(0);
        WritePhysical("ready");
        BeginPhysicalCommands();
    }
    private void BeginPhysicalCommands()
    {
        timer=new DispatcherTimer(DispatcherPriority.ContextIdle) { Interval=TimeSpan.FromMilliseconds(500) };
        timer.Tick+=async (_,_)=>await PhysicalTick(); timer.Start();
    }
    private object[] CheckWires()
    {
        Require(Wires().Length==wireIdentity.Count,"Wire count changed.");
        return wireIdentity.Select(pair =>
        {
            var wire=pair.Key; var (a,b)=pair.Value;
            Require(ReferenceEquals(P(wire,"PinA"),a) && ReferenceEquals(P(wire,"PinB"),b),"Endpoint identity changed.");
            Require((bool)P(wire,"IsAttached") && Items(P(a,"Wires")).Contains(wire) && Items(P(b,"Wires")).Contains(wire),"Wire detached.");
            var gr=Mapped("Wires",wire);
            Point NativeCenter(object pin)
            {
                var center=(Point)P(pin,"CenterWithProvider"); var offset=(Point)P(Field(pin,"Provider"),"CanvasOffset");
                return new(center.X+offset.X,center.Y+offset.Y);
            }
            var pa=NativeCenter(a); var pb=NativeCenter(b);
            bool rendered=gr is not null;
            if(Items(P(P(physicalCanvas,"Root"),"AllChildren")).Contains(wire))
                Require(rendered,"A wire on the observed canvas has no rendered path.");
            if(rendered)
            {
                Require((Point)P(gr!,"P1")==pa && (Point)P(gr!,"P2")==pb,"Rendered wire endpoint stale.");
                Require(P(gr!,"FocusGeometry") is Geometry geometry && !geometry.Bounds.IsEmpty,"Empty wire path.");
            }
            return (object)new { a=P(a,"Id").ToString(), b=P(b,"Id").ToString(), start=pa.ToString(), end=pb.ToString(), attached=true, renderedEndpointsMatch=rendered };
        }).ToArray();
    }
    private void WritePhysical(string status)
    {
        var models=Items(P(P(physicalCanvas,"Root"),"AllChildren")).Where(T("Design.Object").IsInstanceOfType).ToArray();
        var dpi=VisualTreeHelper.GetDpi((Visual)physicalCanvas);
        var transform=((Visual)physicalCanvas).TransformToAncestor(Application.Current.MainWindow);
        var origin=transform.Transform(new Point(0,0)); var axis=transform.Transform(new Point(1,0));
        File.WriteAllText(config!.Receipt,JsonSerializer.Serialize(new { status, lastCommand, pid=Environment.ProcessId,
            vendorHash=Hash(host.Location), modules=AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.GetName().Name!.StartsWith("Designer.Modules.") || a.GetName().Name=="Qmm.mods-core.Runtime").Select(a=>new {name=a.GetName().Name,hash=Hash(a.Location)}),
            scale=P(physicalCanvas,"CanvasScaleFactor"), effectiveScale=(axis-origin).Length, dpiX=dpi.DpiScaleX,dpiY=dpi.DpiScaleY,
            objects=models.Select(m=>new { type=m.GetType().Name,label=P(m,"Label"),position=P(m,"Position").ToString(), body=P(m,"Size").ToString(),drawn=P(GraphicOf(m),"Size").ToString(),
                pins=T("Design.PinProvider").IsInstanceOfType(m)?Items(P(GraphicOf(m),"pins")).Select(p=>new {center=P(p,"CanvasCenter").ToString(),direction=Field(P(p,"InnerPin"),"Direction").ToString(),domain=P(P(p,"InnerPin"),"WiringDomain").ToString(),exposed=Field(P(p,"InnerPin"),"UserCreated")}).ToArray():null }),
            wires=CheckWires(), checks=physicalChecks, originalUnchanged=Hash(config.Input)==config.InputHash }, new JsonSerializerOptions{WriteIndented=true}));
    }
    private async Task PhysicalTick()
    {
        if(busy)return; busy=true;
        try
        {
            var commandPath=Path.Combine(AppContext.BaseDirectory,"physical-command.json");
            if(!File.Exists(commandPath))return;
            using var doc=JsonDocument.Parse(File.ReadAllText(commandPath)); var command=doc.RootElement;
            string id=command.GetProperty("id").GetString()!; if(id==lastCommand)return;
            lastCommand=id; string action=command.GetProperty("action").GetString()!;
            if(action=="scope") await OpenPhysical(command.GetProperty("index").GetInt32());
            else if(action=="pack") await PackAttached();
            else if(action=="save")
            {
                Require(config!.SaveAs is not null && !File.Exists(config.SaveAs),"Output must be fresh.");
                Require(await (Task<bool>)T("Commands.Save").GetMethod("SaveFile",Static)!.Invoke(null,[config.SaveAs])!,"Save failed.");
            }
            else Require(action=="snapshot","Unknown test action.");
            await Idle();
            physicalChecks.Add(new {id,action,wireEndpointsPreserved=CheckWires().Length});
            WritePhysical("ready");
        }
        catch(Exception e) {timer?.Stop();File.WriteAllText(config!.Receipt,JsonSerializer.Serialize(new {status="error",lastCommand,error=e.ToString(),checks=physicalChecks},new JsonSerializerOptions{WriteIndented=true}));}
        finally {busy=false;}
    }
    private async Task PackAttached()
    {
        var pack=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Designer.Modules.BetterPacking").GetType("Designer.Modules.BetterPacking.SelectionPacker")!;
        var allChosen=physicalChosen.Where(m=>T("Design.Component").IsInstanceOfType(m)||Items(P(m,"Pins")).Any(p=>Items(P(p,"Wires")).Any(w=>!((bool)Field(w,"IgnoreForWiring"))))).ToArray();
        var partialChosen=allChosen.Where(m=>P(m,"Label").ToString() is "Input 1" or "Drag Gain 1").ToArray();
        Require(partialChosen.Length==2,"Partial selection fixture mismatch.");
        foreach(var chosen in new[]{allChosen,partialChosen})
        {
        var before=chosen.Select(m=>(Point)P(m,"Position")).ToArray();
        var labels=chosen.Select(m=>P(m,"Label")).ToArray();
        var sizes=chosen.Select(m=>(Size)P(m,"Size")).ToArray();
        foreach(bool exposed in new[]{false,true}) foreach(bool bottom in new[]{false,true})
        {
            Choose(chosen);
            Func<string,object,string> receipt=(kind,detail)=>kind;
            pack.GetMethod("PackPins",Static)!.Invoke(null,[bottom,exposed,30,receipt,physicalCanvas]); await Idle();
            var anchors=chosen.Select(m=>Items(P(GraphicOf(m),"pins")).Where(p=>T("Design.ContainerPin").IsInstanceOfType(m)||(bool)Field(P(p,"InnerPin"),"UserCreated")==exposed).Select(p=>((Point)P(p,"CanvasCenter")).Y).ToArray()).Select(ys=>bottom?ys.Max():ys.Min()).ToArray();
            Require(anchors.All(y=>Math.Abs(y-anchors[0])<0.000001),"Attached pins did not align."); CheckWires();
            var after=chosen.Select(m=>(Point)P(m,"Position")).ToArray();
            if(after.SequenceEqual(before))
            {
                physicalChecks.Add(new {kind="attached-pin-pack",bottom,exposed,selection=chosen==allChosen?"all-endpoints":"stationary-peer",anchors,wireCount=wireIdentity.Count,noOp=true});
                continue; // No operation was recorded; Undo would reach an unrelated prior edit.
            }
            Call(physicalManager,"Undo"); await Idle(); Require(chosen.Select(m=>(Point)P(m,"Position")).SequenceEqual(before),"Attached packing undo failed.");CheckWires();
            Call(physicalManager,"Redo"); await Idle(); Require(chosen.Select(m=>(Point)P(m,"Position")).SequenceEqual(after),"Attached packing redo failed.");CheckWires();
            Call(physicalManager,"Undo"); await Idle();CheckWires();
            Require(chosen.Select(m=>P(m,"Label")).SequenceEqual(labels) && chosen.Select(m=>(Size)P(m,"Size")).SequenceEqual(sizes),"Packing changed labels or sizes.");
            physicalChecks.Add(new {kind="attached-pin-pack",bottom,exposed,selection=chosen==allChosen?"all-endpoints":"stationary-peer",anchors,wireCount=wireIdentity.Count,undoRedo=true,labelsSizesPreserved=true});
        }
        }
        Choose([]);
    }
}
