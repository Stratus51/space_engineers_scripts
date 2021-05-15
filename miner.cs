public enum SliderDirection {
    Positive,
    Negative,
}

public interface Slider {
    float Pos {get;}
    float Min {get;}
    float Max {get;}

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
}

public class Piston: Slider {
    Program Program;
    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public SliderDirection Direction {get;}
    public float Speed;
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
        return this.List[0].Status;
    }
}

public class Sliders: Slider {
    Program Program;
    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public SliderDirection Direction {get;}
    public float Speed;
    public List<Slider> List;

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
    }

    public void Stop() {
        foreach(var slider in this.List) {
            slider.Stop();
        }
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
    }

    public Vector3D WorldPosition() {
        return this.List[0].WorldPosition();
    }

    public Vector3D WorldDirection() {
        return this.List[0].WorldDirection();
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
    public float Speed;
    public SliderDirection Direction {get;}

    public SliderChain(List<Slider> sliders, SliderDirection direction) {
        this.List = sliders;
        this.Positive = new List<Slider>();
        this.Negative = new List<Slider>();
        this.Min = 0.0f;
        this.Max = 0.0f;
        this.Direction = direction;
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
        } else if(current_pos > pos) {
            needed = current_pos - pos;
            to_extend = this.Negative;
            to_retract = this.Positive;
        } else {
            return;
        }

        foreach(var slider in to_extend) {
            if(slider.Pos < slider.Max) {
                var goal = (float)Math.Min(slider.Max, slider.Pos + needed);
                slider.MoveTo(goal, speed);
                return;
            }
        }

        foreach(var slider in to_retract) {
            if(slider.Pos > slider.Min) {
                var goal = (float)Math.Max(slider.Min, slider.Pos - needed);
                slider.MoveTo(goal, speed);
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
        foreach(var slider in this.List) {
            slider.Start();
        }
    }

    public void Stop() {
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
    if(name == "") {
        return SliderType.Direct;
    } else if(name.StartsWith("Direct")) {
        return SliderType.Direct;
    } else if(name.StartsWith("Crawl")) {
        return SliderType.Crawl;
    } else {
        return null;
    }
}

enum Axis {
    X,
    Y,
    Z
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
    var list = new List<IMyPistonBase>();
    this.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(list);
    foreach(var piston in list) {
        if(piston.CustomName.StartsWith(name)) {
            var name_spec = "";
            if(piston.CustomName.Length > name.Length) {
                name_spec = piston.CustomName.Substring(name.Length + 1);
            }

            var type = SliderTypeFromName(name_spec);
            type = SliderType.Direct;
            if(type != null) {
                var i_type = (int)type;
                var cube_grid = piston.CubeGrid;
                var piston_front = Vector3D.Normalize(piston.Top.GetPosition() - piston.GetPosition());
                SliderDirection direction;
                Dictionary<IMyCubeGrid, List<Slider>> per_grid;
                if(Vector3D.Dot(z_axis, piston_front) > 0.9) {
                    Echo("Z+");
                    per_grid = pistons[i_type][(int)Axis.Z];
                    direction = SliderDirection.Positive;
                } else if(Vector3D.Dot(z_axis, piston_front) < -0.9) {
                    Echo("Z-");
                    per_grid = pistons[i_type][(int)Axis.Z];
                    direction = SliderDirection.Negative;
                } else if(Vector3D.Dot(y_axis, piston_front) > 0.9) {
                    Echo("Y+ " + Vector3D.Dot(y_axis, piston_front));
                    per_grid = pistons[i_type][(int)Axis.Y];
                    direction = SliderDirection.Positive;
                } else if(Vector3D.Dot(y_axis, piston_front) < -0.9) {
                    Echo("Y-");
                    per_grid = pistons[i_type][(int)Axis.Y];
                    direction = SliderDirection.Negative;
                } else if(Vector3D.Dot(x_axis, piston_front) > 0.9) {
                    Echo("X+");
                    per_grid = pistons[i_type][(int)Axis.X];
                    direction = SliderDirection.Positive;
                } else if(Vector3D.Dot(x_axis, piston_front) < -0.9) {
                    Echo("X-");
                    per_grid = pistons[i_type][(int)Axis.X];
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

    Echo("Building complex sliders");
    var sliders = new List<Slider>[3];
    for(var i = 0; i < 3; i++) {
        sliders[i] = new List<Slider>();
        sliders[i].AddRange(BuildDirectPistons(pistons[(int)SliderType.Direct][i]));
    }

    var arm_x = new SliderChain(sliders[(int)Axis.X], SliderDirection.Positive);
    var arm_y = new SliderChain(sliders[(int)Axis.Y], SliderDirection.Positive);
    var arm_z = new SliderChain(sliders[(int)Axis.Z], SliderDirection.Positive);

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
                    Echo("Moving: X+");
                    break;
                case EXTEND_Y:
                    Echo("Moving: Y+");
                    break;
                case EXTEND_Z:
                    Echo("Moving: Z+");
                    break;
                case RETRACT_X:
                    Echo("Moving: X-");
                    break;
                case RETRACT_Y:
                    Echo("Moving: Y-");
                    break;
                case RETRACT_Z:
                    Echo("Moving: Z-");
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
        Echo("Could not find any drill for " + name);
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
