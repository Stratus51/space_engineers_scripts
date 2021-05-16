public enum SliderDirection {
    Positive,
    Negative,
}

public interface Slider {
    float Pos {get;}
    float Min {get;}
    float Max {get;}
    float Speed {get;}

    SliderDirection Direction {get;}

    string Name();
    void Refresh();
    bool Sync();
    void SetSpeed(float speed);
    void Reverse();
    void Start();
    void Stop();
    void Run();
    void MoveTo(float pos, float speed);
    Vector3D WorldPosition();
    Vector3D WorldDirection();
    string StateToString();
}

public class Piston: Slider {
    Program Program;
    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public SliderDirection Direction {get;}
    public float Speed {get; set;}
    public IMyPistonBase piston;

    public Piston(Program program, IMyPistonBase piston, SliderDirection direction) {
        this.Program = program;

        this.Min = 0;
        this.Max = piston.HighestPosition;
        this.Direction = direction;
        this.piston = piston;
        this.Refresh();
    }

    public string Name() {
        return this.piston.CustomName;
    }

    public void Refresh() {
        this.Pos = this.piston.CurrentPosition;
        this.Speed = this.piston.Velocity;
    }

    public bool Sync() {
        return true;
    }

    public void SetSpeed(float speed) {
        // this.Program.Echo("Piston.SetSpeed: " + speed);
        this.piston.Velocity = speed;
        this.Speed = speed;
    }

    public void Reverse() {
        this.piston.Reverse();
        this.Speed = -this.Speed;
    }

    public void Start() {
        this.piston.Enabled = true;
    }

    public void Stop() {
        this.piston.Enabled = false;
    }

    public void Run() {}

    public void MoveTo(float pos, float speed) {
        this.Program.Echo("Piston[" + this.Pos + "].MoveTo: " + pos + " | " + speed);
        this.piston.MinLimit = pos;
        this.piston.MaxLimit = pos;

        if(pos > this.Pos) {
            this.SetSpeed(speed);
        } else if(pos < this.Pos) {
            this.SetSpeed(-speed);
        }
    }

    public Vector3D WorldPosition() {
        return piston.CubeGrid.GridIntegerToWorld(piston.Position);
    }

    public Vector3D WorldDirection() {
        return this.piston.WorldMatrix.Up;
    }

    public string StateToString() {
        if(this.piston.Enabled) {
            if(this.Speed > 0.0f) {
                return "Expand";
            } else if(this.Speed < 0.0f) {
                return "Retract";
            } else {
                return "Immobile";
            }
        } else {
            return "Stopped";
        }
    }
}

public class Connectors {
    public IMyShipConnector[] List;
    public Connectors(IMyShipConnector[] connectors) {
        this.List = connectors;
    }

    public void Connect() {
        foreach(var connector in this.List) {
            connector.Connect();
        }
    }

    public void Disconnect() {
        foreach(var connector in this.List) {
            connector.Disconnect();
        }
    }

    public MyShipConnectorStatus Status() {
        var status = MyShipConnectorStatus.Connected;
        foreach(var connector in this.List) {
            switch(status) {
                case MyShipConnectorStatus.Connected:
                    if(connector.Status != MyShipConnectorStatus.Connected) {
                        status = connector.Status;
                    }
                    break;
                case MyShipConnectorStatus.Connectable:
                    if(connector.Status == MyShipConnectorStatus.Unconnected) {
                        status = connector.Status;
                    }
                    break;
                case MyShipConnectorStatus.Unconnected:
                    break;
                default:
                    break;
            }
            if(status == MyShipConnectorStatus.Unconnected) {
                break;
            }
        }
        return status;
    }
}

public class MergeBlocks {
    public IMyShipMergeBlock[] List;
    public bool Enabled;

    public MergeBlocks(IMyShipMergeBlock[] connectors) {
        this.List = connectors;
    }

    public bool IsEnabled() {
        foreach(var mblock in this.List) {
            if(!mblock.Enabled) {
                return false;
            }
        }
        return true;
    }

    public bool IsDisabled() {
        foreach(var mblock in this.List) {
            if(mblock.Enabled) {
                return false;
            }
        }
        return true;
    }

    public bool IsConnected() {
        foreach(var mblock in this.List) {
            if(!mblock.IsConnected) {
                return false;
            }
        }
        return true;
    }

    public void SetEnabled(bool enabled) {
        foreach(var mblock in this.List) {
            mblock.Enabled = enabled;
        }
    }
}

public class Sliders: Slider {
    Program Program;
    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public SliderDirection Direction {get;}
    public float Speed {get; set;}
    public List<Slider> List;
    bool Enabled;

    public Sliders(Program program, List<Slider> sliders) {
        this.Program = program;
        this.List = sliders;
        this.Min = 0;
        this.Max = (float)sliders[0].Max;
        this.Direction = sliders[0].Direction;
        this.Refresh();

        var size = sliders[0].Max;
        foreach(var slider in sliders) {
            if(slider.Max != size) {
                throw new Exception("Grouped sliders of different size!");
            }
        }
    }

    public string Name() {
        return this.List[0].Name();
    }

    public void Refresh() {
        var pos = this.List[0].Pos;
        foreach(var slider in this.List) {
            slider.Refresh();
            if(slider.Pos > pos) {
                pos = slider.Pos;
            }
        }
        this.Pos = pos;
    }

    public bool Sync() {
        var ref_pos = this.List[0].Pos;
        var is_synced = true;
        foreach(var slider in this.List) {
            if(slider.Sync()) {
                if(Math.Abs(slider.Pos - ref_pos) > 0.1) {
                    is_synced = false;
                    slider.MoveTo(ref_pos, 0.5f);
                }
            }
        }
        return is_synced;
    }

    public void SetSpeed(float speed) {
        foreach(var slider in this.List) {
            slider.SetSpeed(speed);
        }
        this.Speed = speed;
    }

    public void Reverse() {
        foreach(var slider in this.List) {
            slider.Reverse();
        }
        this.Speed = -this.Speed;
    }

    public void Start() {
        foreach(var slider in this.List) {
            slider.Start();
        }
        this.Enabled = true;
    }

    public void Stop() {
        foreach(var slider in this.List) {
            slider.Stop();
        }
        this.Enabled = false;
    }

    public void Run() {
        foreach(var slider in this.List) {
            slider.Run();
        }
    }

    public void MoveTo(float pos, float speed) {
        this.Echo("MoveTo[" + this.List.Count + "](" + pos + ", " + speed + ")");
        foreach(var slider in this.List) {
            slider.MoveTo(pos, speed);
        }
        this.Speed = this.List[0].Speed;
    }

    public Vector3D WorldPosition() {
        return this.List[0].WorldPosition();
    }

    public Vector3D WorldDirection() {
        return this.List[0].WorldDirection();
    }

    public string StateToString() {
        var state = this.List[0].StateToString();
        if(this.Enabled) {
            if(this.Speed > 0.0f) {
                return "Expand " + state;
            } else if(this.Speed < 0.0f) {
                return "Retract " + state;
            } else {
                return "Immobile " + state;
            }
        } else {
            return "Stopped " + state;
        }
    }

    void Echo(string s) {
        this.Program.Echo("Sliders: " + s);
    }
}

public class SliderChain: Slider {
    List<Slider> List;
    List<Slider> Positive;
    List<Slider> Negative;

    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public float Speed {get; set;}
    public SliderDirection Direction {get;}
    bool Enabled;
    Slider Last;

    public SliderChain(Program program, List<Slider> sliders, SliderDirection direction) {
        this.List = sliders;
        this.Positive = new List<Slider>();
        this.Negative = new List<Slider>();
        this.Min = 0.0f;
        this.Max = 0.0f;
        this.Direction = direction;
        program.Echo("SliderChain " + sliders.Count);
        foreach(var slider in sliders) {
            this.Max += slider.Max;
            switch(slider.Direction) {
                case SliderDirection.Positive:
                    this.Positive.Add(slider);
                    break;
                case SliderDirection.Negative:
                    this.Negative.Add(slider);
                    break;
            }
        }
        this.Refresh();
    }

    public string Name() {
        return this.List[0].Name();
    }

    public bool Empty() {
        return this.List.Count == 0;
    }

    public void Refresh() {
        this.Pos = 0.0f;
        foreach(var slider in this.Positive) {
            slider.Refresh();
            this.Pos += slider.Pos;
        }
        foreach(var slider in this.Negative) {
            slider.Refresh();
            this.Pos += slider.Max - slider.Pos;
        }
    }

    public void MoveTo(float pos, float speed) {
        var current_pos = this.Pos;
        List<Slider> to_extend;
        List<Slider> to_retract;
        float needed;
        if(pos > current_pos) {
            needed = pos - current_pos;
            to_extend = this.Positive;
            to_retract = this.Negative;
            this.Speed = speed;
        } else if(current_pos > pos) {
            needed = current_pos - pos;
            to_extend = this.Negative;
            to_retract = this.Positive;
            this.Speed = -speed;
        } else {
            return;
        }

        foreach(var slider in to_extend) {
            if(slider.Pos < slider.Max) {
                var goal = (float)Math.Min(slider.Max, slider.Pos + needed);
                slider.MoveTo(goal, speed);
                this.Last = slider;
                return;
            }
        }

        foreach(var slider in to_retract) {
            if(slider.Pos > slider.Min) {
                var goal = (float)Math.Max(slider.Min, slider.Pos - needed);
                slider.MoveTo(goal, speed);
                this.Last = slider;
                return;
            }
        }
    }

    public bool Sync() {
        var synced = true;
        foreach(var slider in this.List) {
            synced = synced && slider.Sync();
        }
        return synced;
    }

    public void Reverse() {
        this.SetSpeed(-this.Speed);
    }

    public void Start() {
        this.Enabled = true;
        foreach(var slider in this.List) {
            slider.Start();
        }
    }

    public void Stop() {
        this.Enabled = false;
        foreach(var slider in this.List) {
            slider.Stop();
        }
    }

    public void SetSpeed(float speed) {
        foreach(var slider in this.Positive) {
            if(slider.Pos < slider.Max) {
                slider.SetSpeed(speed);
                return;
            }
        }

        foreach(var slider in this.Negative) {
            if(slider.Pos > slider.Min) {
                slider.SetSpeed(-speed);
                return;
            }
        }
    }

    public void Run() {
        foreach(var slider in this.List) {
            slider.Run();
        }
    }

    public Vector3D WorldPosition() {
        if(this.Positive.Count > 0) {
            return this.Positive[0].WorldPosition();
        } else {
            return -this.Negative[0].WorldPosition();
        }
    }

    public Vector3D WorldDirection() {
        if(this.Positive.Count > 0) {
            return this.Positive[0].WorldDirection();
        } else {
            return -this.Negative[0].WorldDirection();
        }
    }

    public string StateToString() {
        var last_state = "";
        if(this.Last != null) {
            last_state = this.Last.StateToString();
        }
        if(this.Enabled) {
            if(this.Speed > 0.0f) {
                return "Expand " + last_state;
            } else if(this.Speed < 0.0f) {
                return "Retract " + last_state;
            } else {
                return "Immobile " + last_state;
            }
        } else {
            return "Stopped " + last_state;
        }
    }
}

const float CRAWL_MIN = 2.6f;
const float CRAWL_MAX = 9.8f;
const float CRAWL_RETRACT_SPEED = 3f;
const float CRAWL_GRIND_TIME = 2f;

public class CrawlSlider: Slider {
    public float Pos {get;set;}
    public float Min {get;}
    public float Max {get;}

    public SliderDirection Direction {get;}

    Program Program;
    string name;
    IMyCubeBlock Origin;
    Slider Slider;
    Connectors[] Connectors;
    MergeBlocks[] MergeBlocks;
    public float Speed {get; set;}
    bool Enabled;

    TimeSpan RemainingGrind;

    enum State {
        TranslatingLoad,
        SyncBottomConnector,
        LockingBottomConnector,
        UnlockingTop,
        TranslatingSlider,
        Grind,
        LockingTop,
        UnlockingBottomMergeBlock,
    }
    State state;

    public CrawlSlider(Program program, string name, IMyCubeBlock origin, Slider slider, Connectors[] connectors, MergeBlocks[] merge_blocks) {
        this.Program = program;
        this.name = name;
        this.Origin = origin;
        this.Slider = slider;
        this.Connectors = connectors;
        this.MergeBlocks = merge_blocks;

        this.state = this.DetectState();
        this.Refresh();
        this.Min = 0;
        this.Max = 50000;
    }

    State DetectState() {
        if(this.Slider.Pos <= CRAWL_MIN) {
            this.Connectors[1].Connect();
            this.MergeBlocks[1].SetEnabled(true);
            return State.TranslatingSlider;
        } else if(this.Slider.Pos >= CRAWL_MAX) {
            this.Connectors[1].Connect();
            this.MergeBlocks[1].SetEnabled(true);
            return State.TranslatingLoad;
        } else if(this.MergeBlocks[0].IsConnected()) {
            return State.TranslatingLoad;
        } else if(this.MergeBlocks[1].IsConnected()) {
            return State.TranslatingSlider;
        } else {
            throw new Exception("Unknown crawl slider state.");
        }
    }

    public string Name() {
        return this.name;
    }

    public void Refresh() {
        this.Slider.Refresh();
        this.Pos = (float)Vector3D.Dot(this.Slider.WorldPosition() - this.Origin.GetPosition(), this.WorldDirection()) + this.Slider.Pos;
    }

    public bool Sync() {
        return this.Slider.Sync();
    }

    public void SetSpeed(float speed) {
        if(speed > 0) {
            this.MoveTo(this.Max, speed);
        } else {
            this.MoveTo(this.Min, -speed);
        }
        this.Speed = speed;
    }

    public void Reverse() {
        this.SetSpeed(-this.Speed);
    }

    public void Start() {
        this.Slider.Start();
        this.Enabled = true;
    }

    public void Stop() {
        this.Slider.Stop();
        this.Enabled = false;
    }

    public void Run() {
    }

    public void MoveTo(float pos, float speed) {
        if(pos > this.Pos) {
            var slider_target = Math.Min(CRAWL_MAX, pos - this.Pos + this.Slider.Pos);
            this.Speed = speed;
            switch(this.state) {
                case State.TranslatingLoad:
                    if(this.Slider.Pos > 7) {
                        this.MergeBlocks[1].SetEnabled(true);
                    }

                    if(this.MergeBlocks[1].IsConnected()) {
                        this.state = State.SyncBottomConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Slider.MoveTo(slider_target, speed);
                    }
                    break;
                case State.SyncBottomConnector:
                    if(this.Slider.Pos >= CRAWL_MAX) {
                        this.state = State.LockingBottomConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Connectors[1].Disconnect();
                        this.Slider.MoveTo(CRAWL_MAX, speed);
                    }
                    break;
                case State.LockingBottomConnector:
                    if(this.Connectors[1].Status() == MyShipConnectorStatus.Connected) {
                        this.state = State.UnlockingTop;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Connectors[1].Connect();
                    }
                    break;
                case State.UnlockingTop:
                    Echo("State.UnlockingTop: " + this.Connectors[0].Status() + " | " + this.MergeBlocks[0].IsDisabled());
                    if(this.Connectors[0].Status() == MyShipConnectorStatus.Connectable && this.MergeBlocks[0].IsDisabled()) {
                        this.state = State.TranslatingSlider;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Connectors[0].Disconnect();
                        this.MergeBlocks[0].SetEnabled(false);
                    }
                    break;
                case State.TranslatingSlider:
                    if(this.Slider.Pos < 7) {
                        this.MergeBlocks[0].SetEnabled(true);
                    }
                    if(this.MergeBlocks[0].IsConnected()) {
                        this.state = State.Grind;
                        this.RemainingGrind = new TimeSpan(0, 0, 2);
                    } else {
                        this.Slider.MoveTo(CRAWL_MIN, CRAWL_RETRACT_SPEED);
                    }
                    break;
                case State.Grind:
                    this.RemainingGrind -= this.Program.Runtime.TimeSinceLastRun;
                    if(this.RemainingGrind < TimeSpan.Zero) {
                        this.state = State.LockingTop;
                        this.MoveTo(pos, speed);
                    }
                    break;
                case State.LockingTop:
                    if(this.Connectors[0].Status() == MyShipConnectorStatus.Connected) {
                        this.state = State.UnlockingBottomMergeBlock;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Connectors[0].Connect();
                    }
                    break;
                case State.UnlockingBottomMergeBlock:
                    if(this.MergeBlocks[1].IsDisabled()) {
                        this.state = State.TranslatingLoad;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(false);
                    }
                    break;
            }
        } else {
            throw new Exception("Negative speed not implemented (crawl slider)");
        }
    }

    public Vector3D WorldPosition() {
        return this.Origin.GetPosition();
    }

    public Vector3D WorldDirection() {
        return this.Slider.WorldDirection();
    }

    public string StateToString() {
        if(this.Enabled) {
            if(this.Speed > 0.0f) {
                return "Expand: " + this.state;
            } else if(this.Speed < 0.0f) {
                return "Retract" + this.state;
            } else {
                return "Immobile" + this.state;
            }
        } else {
            return "Stopped";
        }
    }

    void Echo(string s) {
        this.Program.Echo("Crawl: " + s);
    }
}

public class Arm {
    Program Program;
    public Slider X;
    public Slider Y;
    public Slider Z;
    public Vector3D Pos;
    public Vector3D Max;

    public Arm(Program program, Slider x, Slider y, Slider z) {
        this.Program = program;
        this.X = x;
        this.Y = y;
        this.Z = z;
        this.Pos = new Vector3D(0.0, 0.0, 0.0);
        this.Max = new Vector3D(this.X.Max, this.Y.Max, this.Z.Max);
    }

    public bool Empty() {
        return this.X.Max + this.Y.Max + this.Z.Max == 0;
    }

    public void Refresh() {
        this.X.Refresh();
        this.Y.Refresh();
        this.Z.Refresh();
        this.Pos.X = this.X.Pos;
        this.Pos.Y = this.Y.Pos;
        this.Pos.Z = this.Z.Pos;
    }

    public void MoveTo(Vector3D pos, Vector3D speed) {
        this.Program.Echo("MoveTo: " + pos + " | " + speed);
        this.X.MoveTo((float)pos.X, (float)speed.X);
        this.Y.MoveTo((float)pos.Y, (float)speed.Y);
        this.Z.MoveTo((float)pos.Z, (float)speed.Z);
    }

    public void Start() {
        this.X.Start();
        this.Y.Start();
        this.Z.Start();
    }

    public void Stop() {
        this.X.Stop();
        this.Y.Stop();
        this.Z.Stop();
    }
}

enum SliderType {
    Direct,
    Crawl,
}

Nullable<SliderType> SliderTypeFromName(string name) {
    if(name.StartsWith("Direct")) {
        return SliderType.Direct;
    } else if(name.StartsWith("Crawl")) {
        return SliderType.Crawl;
    } else {
        return SliderType.Direct;
    }
}

enum Axis {
    X,
    Y,
    Z

}
public List<Slider> BuildCrawlSliders(string root_name, Dictionary<IMyCubeGrid, List<Slider>> sliders_dict, List<IMyShipConnector> all_connectors, List<IMyShipMergeBlock> all_merge_blocks) {
    string base_name = root_name + " Crawl";
    var names = new List<string>();
    var all_sliders = new List<Slider>();
    Echo("Building Crawl sliders");
    foreach(var entry in sliders_dict) {
        Echo("entry: " + entry.Value.Count);
        foreach(var slider in entry.Value) {
            var sub_name = slider.Name().Substring(base_name.Length);
            names.Add(sub_name);
            all_sliders.Add(slider);
            Echo("Found slider '" + sub_name + "'");
        }
    }

    var ret = new List<Slider>();
    foreach(var name in names) {
        var sliders = new List<Slider>();
        var connectors = new List<IMyShipConnector>[2];
        var merge_blocks = new List<IMyShipMergeBlock>[2];
        for(var i = 0; i < 2; i++) {
            connectors[i] = new List<IMyShipConnector>();
            merge_blocks[i] = new List<IMyShipMergeBlock>();
        }

        foreach(var slider in all_sliders) {
            var sub_name = slider.Name().Substring(base_name.Length);
            if(sub_name == name) {
                sliders.Add(slider);
            }
        }

        foreach(var connector in all_connectors) {
            var sub_name = connector.CustomName.Substring(base_name.Length);
            if(sub_name.StartsWith(name + " Bottom")) {
                connectors[0].Add(connector);
            } else if(sub_name.StartsWith(name + " Top")) {
                connectors[1].Add(connector);
            }
        }

        foreach(var merge_block in all_merge_blocks) {
            var sub_name = merge_block.CustomName.Substring(base_name.Length);
            if(sub_name.StartsWith(name + " Bottom")) {
                merge_blocks[0].Add(merge_block);
            } else if(sub_name.StartsWith(name + " Top")) {
                merge_blocks[1].Add(merge_block);
            }
        }

        var origin = this.GridTerminalSystem.GetBlockWithName(root_name + " Crawl Origin");
        if(sliders.Count > 0 && connectors[0].Count > 0 && connectors[1].Count > 0 && merge_blocks[0].Count > 0 && merge_blocks[1].Count > 0 && origin != null) {
            var slider = new Sliders(this, sliders);
            var built_connectors = new Connectors[2];
            var built_merge_blocks = new MergeBlocks[2];
            for(var i = 0; i < 2; i++) {
                built_connectors[i] = new Connectors(connectors[i].ToArray());
                built_merge_blocks[i] = new MergeBlocks(merge_blocks[i].ToArray());
            }
            Echo("Found crawl slider " + sliders.Count + " | "  + connectors[0].Count + " " + connectors[1].Count + " | " + merge_blocks[0].Count + " " + merge_blocks[1].Count);
            ret.Add(new CrawlSlider(this, root_name, origin, slider, built_connectors, built_merge_blocks));
        } else {
            Echo("Dropped crawl slider " + name);
            if(sliders.Count == 0) {
                Echo("Missing sliders");
            }
            if(connectors[0].Count == 0) {
                Echo("Missing bottom connectors");
            }
            if(connectors[1].Count == 0) {
                Echo("Missing top connectors");
            }
            if(merge_blocks[0].Count == 0) {
                Echo("Missing bottom merge blocks");
            }
            if(merge_blocks[1].Count == 0) {
                Echo("Missing top merge blocks");
            }
            if(origin == null) {
                Echo("Missing origin block");
            }
        }
    }
    return ret;
}

public List<Slider> BuildDirectPistons(Dictionary<IMyCubeGrid, List<Slider>> pistons) {
    var ret = new List<Slider>();
    foreach(var kv in pistons) {
        if(kv.Value.Count == 0) {
            ret.Add(kv.Value[0]);
        } else {
            ret.Add(new Sliders(this, kv.Value));
        }
    }
    return ret;
}

public Arm BuildArmFromName(string name, IMyCubeBlock top_block) {
    var pistons = new Dictionary<IMyCubeGrid, List<Slider>>[2][];
    for(var i = 0; i < 2; i++) {
        var plist = new Dictionary<IMyCubeGrid, List<Slider>>[3];
        for(var j = 0; j < 3; j++) {
            plist[j] = new Dictionary<IMyCubeGrid, List<Slider>>();
        }
        pistons[i] = plist;
    }

    var top_w_matrix = top_block.CubeGrid.WorldMatrix;
    var z_axis = top_w_matrix.GetDirectionVector(top_block.Orientation.Forward);
    var y_axis = top_w_matrix.GetDirectionVector(top_block.Orientation.Up);
    var x_axis = top_w_matrix.GetDirectionVector(top_block.Orientation.Left);

    Echo("Scanning pistons");
    {
        var list = new List<IMyPistonBase>();
        this.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(list);
        foreach(var piston in list) {
            if(piston.CustomName.StartsWith(name)) {
                var name_spec = "";
                if(piston.CustomName.Length > name.Length) {
                    name_spec = piston.CustomName.Substring(name.Length + 1);
                }

                var type = SliderTypeFromName(name_spec);
                if(type != null) {
                    var i_type = (int)type;
                    var cube_grid = piston.CubeGrid;
                    var piston_front = Vector3D.Normalize(piston.Top.GetPosition() - piston.GetPosition());
                    SliderDirection direction;
                    Dictionary<IMyCubeGrid, List<Slider>> per_grid;
                    var type_piston = pistons[i_type];
                    if(Vector3D.Dot(z_axis, piston_front) > 0.9) {
                        Echo(type + " Z+");
                        per_grid = type_piston[(int)Axis.Z];
                        direction = SliderDirection.Positive;
                    } else if(Vector3D.Dot(z_axis, piston_front) < -0.9) {
                        Echo(type + " Z-");
                        per_grid = type_piston[(int)Axis.Z];
                        direction = SliderDirection.Negative;
                    } else if(Vector3D.Dot(y_axis, piston_front) > 0.9) {
                        Echo(type + " Y+");
                        per_grid = type_piston[(int)Axis.Y];
                        direction = SliderDirection.Positive;
                    } else if(Vector3D.Dot(y_axis, piston_front) < -0.9) {
                        Echo(type + " Y-");
                        per_grid = type_piston[(int)Axis.Y];
                        direction = SliderDirection.Negative;
                    } else if(Vector3D.Dot(x_axis, piston_front) > 0.9) {
                        Echo(type + " X+");
                        per_grid = type_piston[(int)Axis.X];
                        direction = SliderDirection.Positive;
                    } else if(Vector3D.Dot(x_axis, piston_front) < -0.9) {
                        Echo(type + " X-");
                        per_grid = type_piston[(int)Axis.X];
                        direction = SliderDirection.Negative;
                    } else {
                        throw new Exception("Burp");
                    }
                    if(!per_grid.ContainsKey(cube_grid)) {
                        per_grid.Add(cube_grid, new List<Slider>());
                    }
                    per_grid[cube_grid].Add(new Piston(this, piston, direction));
                }
            }
        }
    }

    Echo("Scanning connectors");
    var all_connectors = new List<IMyShipConnector>();
    {
        var list = new List<IMyShipConnector>();
        this.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list);
        foreach(var connector in list) {
            if(connector.CustomName.StartsWith(name)) {
                all_connectors.Add(connector);
            }
        }
    }

    Echo("Scanning merge blocks");
    var all_merge_blocks = new List<IMyShipMergeBlock>();
    {
        var list = new List<IMyShipMergeBlock>();
        this.GridTerminalSystem.GetBlocksOfType<IMyShipMergeBlock>(list);
        foreach(var mb in list) {
            if(mb.CustomName.StartsWith(name)) {
                all_merge_blocks.Add(mb);
            }
        }
    }

    Echo("Building complex sliders");
    var sliders = new List<Slider>[3];
    for(var i = 0; i < 3; i++) {
        Echo("Axis " + (Axis)i);
        sliders[i] = new List<Slider>();
        var direct_sliders = BuildDirectPistons(pistons[(int)SliderType.Direct][i]);
        Echo(direct_sliders.Count + " direct sliders segments");
        sliders[i].AddRange(direct_sliders);
        var crawl_sliders = BuildCrawlSliders(name, pistons[(int)SliderType.Crawl][i], all_connectors, all_merge_blocks);
        Echo(crawl_sliders.Count + " crawl sliders segments");
        sliders[i].AddRange(crawl_sliders);
    }

    var arm_x = new SliderChain(this, sliders[(int)Axis.X], SliderDirection.Positive);
    var arm_y = new SliderChain(this, sliders[(int)Axis.Y], SliderDirection.Positive);
    var arm_z = new SliderChain(this, sliders[(int)Axis.Z], SliderDirection.Positive);

    return new Arm(this, arm_x, arm_y, arm_z);
}

public const int EXTEND_X = 0;
public const int EXTEND_Y = 1;
public const int EXTEND_Z = 2;
public const int RETRACT_X = 3;
public const int RETRACT_Y = 4;
public const int RETRACT_Z = 5;
public const int CENTERING = 6;

public class Miner {
    public Arm Arm;
    public Vector3I PosI;
    public Vector3I MaxI;
    public List<IMyShipDrill> Drills;
    Program Program;
    int last_move;
    Vector3D Velocity;
    Vector3D VelocitySlow;
    float Step;
    float DepthStep;
    string Name;
    float MaxVolume;
    float MaxFill;
    float MinFill;
    bool Mining;

    public Miner(Program program, string name, List<IMyShipDrill> drills, float velocity, float step, float depth_step, float max_fill, float min_fill) {
        this.Program = program;
        this.Name = name;
        this.Arm = program.BuildArmFromName(name, drills[0]);
        this.Drills = drills;
        this.Velocity = new Vector3D(velocity, velocity/2.0, velocity);
        this.VelocitySlow = new Vector3D(velocity/2.0, velocity/2.0, velocity);
        this.Step = step;
        this.DepthStep = depth_step;
        this.last_move = 0;
        this.MaxI.X = (int)((this.Arm.Max.X + this.Step - 1.0) / this.Step);
        this.MaxI.Y = (int)((this.Arm.Max.Y + this.Step - 1.0) / this.Step);
        this.MaxI.Z = (int)((this.Arm.Max.Z + this.DepthStep - 1.0) / this.DepthStep);
        this.MaxFill = max_fill;
        this.MinFill = min_fill;
        this.Mining = false;

        this.MaxVolume = 0.0f;
        foreach(var drill in this.Drills) {
            MaxVolume += (float)drill.GetInventory().MaxVolume;
        }
    }

    public float CurrentVolume() {
        var ret = 0.0f;
        foreach(var drill in this.Drills) {
            ret += (float)drill.GetInventory().CurrentVolume;
        }
        return ret;
    }

    public void Refresh() {
        this.Arm.Refresh();
        this.PosI.X = (int)(this.Arm.Pos.X / this.Step);
        this.PosI.Y = (int)(this.Arm.Pos.Y / this.Step);
        this.PosI.Z = (int)(this.Arm.Pos.Z / this.DepthStep);
    }

    public int SelectMove() {
        int x = this.PosI.X;
        int y = this.PosI.Y;
        int z = this.PosI.Z;
        int max_x = this.MaxI.X;
        int max_y = this.MaxI.Y;
        int max_z = this.MaxI.Z;
        Echo(x + ";" + y + ";" + z);
        Echo(max_x + ";" + max_y + ";" + max_z);
        if(z % 2 == 0) {
            if(y % 2 == 0) {
                if(x != max_x) {
                    return EXTEND_X;
                } else {
                    if(y != max_y) {
                        return EXTEND_Y;
                    } else {
                        return EXTEND_Z;
                    }
                }
            } else {
                if(x != 0) {
                    return RETRACT_X;
                } else {
                    if(y != max_y) {
                        return EXTEND_Y;
                    } else {
                        return EXTEND_Z;
                    }
                }
            }
        } else {
            if(y % 2 == 0) {
                if(x != 0) {
                    return RETRACT_X;
                } else {
                    if(y != 0) {
                        return RETRACT_Y;
                    } else {
                        return EXTEND_Z;
                    }
                }
            } else {
                if(x != max_x) {
                    return EXTEND_X;
                } else {
                    if(y != 0) {
                        return RETRACT_Y;
                    } else {
                        return EXTEND_Z;
                    }
                }
            }
        }
    }

    public void Run() {
        if(this.Mining) {
            if(this.CurrentVolume() >= this.MaxVolume * this.MaxFill) {
                this.Stop();
                this.Mining = false;
                return;
            }
        } else {
            if(this.CurrentVolume() <= this.MaxVolume * this.MinFill) {
                this.Start();
                this.Mining = true;
            } else {
                return;
            }
        }

        var move = this.SelectMove();
        this.last_move = move;

        var dst = new Vector3D(this.Arm.Pos.X, this.Arm.Pos.Y, this.Arm.Pos.Z);
        switch(move) {
            case EXTEND_X:
                dst.X = this.Arm.X.Max;
                break;
            case EXTEND_Y:
                dst.Y = (float)(this.PosI.Y + 1) * this.Step;
                break;
            case EXTEND_Z:
                dst.Z = (float)(this.PosI.Z + 1) * this.DepthStep;
                break;
            case RETRACT_X:
                dst.X = 0.0;
                break;
            case RETRACT_Y:
                dst.Y = (float)(this.PosI.Y - 1) * this.Step;
                break;
            case RETRACT_Z:
                dst.Z = (float)(this.PosI.Z - 1) * this.DepthStep;
                break;
            default:
                break;
        }
        var velocity = this.Velocity;
        if(move == EXTEND_X || move == RETRACT_X) {
            if(this.PosI.Y == 0 || this.PosI.Y == this.MaxI.Y) {
                velocity = this.VelocitySlow;
            }
        }
        this.Arm.MoveTo(dst, velocity);
    }

    public void Start() {
        this.Arm.Start();

        foreach(var drill in this.Drills) {
            drill.Enabled = true;
        }
    }

    public void Stop() {
        this.Arm.Stop();

        foreach(var drill in this.Drills) {
            drill.Enabled = false;
        }
    }

    public void Print() {
        Echo("X: " + this.Arm.X.Pos.ToString("0.0") + "/" + this.Arm.X.Max + " | I: " + this.PosI.X + "/" + this.MaxI.X);
        Echo("Y: " + this.Arm.Y.Pos.ToString("0.0") + "/" + this.Arm.Y.Max + " | I: " + this.PosI.Y + "/" + this.MaxI.Y);
        Echo("Z: " + this.Arm.Z.Pos.ToString("0.0") + "/" + this.Arm.Z.Max + " | I: " + this.PosI.Z + "/" + this.MaxI.Z);
        if(this.Mining) {
            switch(this.last_move) {
                case EXTEND_X:
                    Echo("Moving: X+ " + this.Arm.X.StateToString());
                    break;
                case EXTEND_Y:
                    Echo("Moving: Y+ " + this.Arm.Y.StateToString());
                    break;
                case EXTEND_Z:
                    Echo("Moving: Z+ " + this.Arm.Z.StateToString());
                    break;
                case RETRACT_X:
                    Echo("Moving: X- " + this.Arm.X.StateToString());
                    break;
                case RETRACT_Y:
                    Echo("Moving: Y- " + this.Arm.Y.StateToString());
                    break;
                case RETRACT_Z:
                    Echo("Moving: Z- " + this.Arm.Z.StateToString());
                    break;
                default:
                    break;
            }
        } else {
            Echo("Stopped mining");
        }
    }

    public void Echo(string s) {
        this.Program.Echo(this.Name + ": " + s);
    }
}

const float MaxFill = 0.20f;
const float MinFill = 0.0f;
public Miner GetMiner(string name, float velocity, float step, float depth_step) {
    var list = new List<IMyShipDrill>();
    GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(list);
    var drills = new List<IMyShipDrill>();
    foreach(var drill in list) {
        if(drill.CustomName == name) {
            drills.Add(drill);
        }
    }
    if(drills.Count == 0) {
        // Echo("Could not find any drill for " + name);
        return null;
    }

    var miner = new Miner(this, name, drills, velocity, step, depth_step, MaxFill, MinFill);
    if(miner.Arm.Empty()) {
        Echo(name + " has no arm. Not a miner.");
        return null;
    } else {
        return miner;
    }
}

List<Miner> miners;
public Program() {
    this.miners = new List<Miner>();

    IMyTextSurface surface = Me.GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.FontSize = 2;
    surface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;

    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void InitMiners(string name_prefix, float velocity, float step, float depth_step) {
    for(var i = 0; i < 100; i++) {
        var miner = GetMiner(name_prefix + " " + i, velocity, step, depth_step);
        if(miner == null) {
            return;
        }
        this.miners.Add(miner);
    }
}

public double Progress() {
    var tot = 0.0;
    var consumed = 0.0;
    foreach(var miner in this.miners) {
        tot += miner.Arm.Max.X * miner.Arm.Max.Y * miner.Arm.Max.Z;
        consumed += miner.Arm.Pos.Z * miner.Arm.Max.X * miner.Arm.Max.Y;
    }
    return consumed/tot;
}

public void UpdateProgressScreen() {
    var progress = Progress() * 100;
    IMyTextSurface surface = Me.GetSurface(0);
    var lines = new string[] {
        "Progress: " + progress.ToString("0.000") + "%",
    };
    surface.WriteText(String.Join("\n", lines));
}

public void Main(string argument) {
    if(this.miners.Count == 0) {
        var args = argument.Split(' ').ToList();
        if (args.Count >= 3) {
            var name_prefix = args[0];
            var velocity = float.Parse(args[1]);
            var step = float.Parse(args[2]);
            var depth_step = float.Parse(args[3]);
            InitMiners(name_prefix, velocity, step, depth_step);
            if(this.miners.Count == 0) {
                Echo("Could not find any miner");
                return;
            }
        } else {
            Echo("Missing arguments: " + args.Count + " < 3");
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
            return;
        }
    }
    // this.Refresh();

    var i = 0;
    foreach(var miner in this.miners) {
        miner.Refresh();
        miner.Run();
        miner.Print();
        i++;
    }

    UpdateProgressScreen();
}
