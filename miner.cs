class Containers {
    Program Program;
    public List<IMyCargoContainer> list;
    public MyFixedPoint MaxVolume;
    public List<string> Ores;
    public List<MyItemType> OresType;
    public Containers(Program program) {
        this.Program = program;
        this.MaxVolume = (MyFixedPoint)0.0;
        this.list = new List<IMyCargoContainer>();
        var list = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(list);
        foreach(var block in list) {
            var refinery = (IMyCargoContainer)block;
            this.list.Add(refinery);
            this.MaxVolume += refinery.GetInventory().MaxVolume;
        }
        this.Ores = new List<string>(){ "Stone", "Iron", "Nickel", "Silicon", "Cobalt", "Magnesium", "Silver","Gold", "Uranium", "Platinum"};
        this.OresType = new List<MyItemType>();
        foreach(var ore in this.Ores) {
            this.OresType.Add(MyItemType.MakeOre(ore));
        }
    }

    public MyFixedPoint GetItemAmount(MyItemType itemType) {
        MyFixedPoint ret = (MyFixedPoint)0.0;
        foreach(var c in list) {
            ret += c.GetInventory().GetItemAmount(itemType);
        }
        return ret;
    }

    public MyFixedPoint OreVolume() {
        MyFixedPoint ret = (MyFixedPoint)0.0;
        foreach(var ore in this.OresType) {
            ret += this.GetItemAmount(ore);
        }
        return ret * (MyFixedPoint)0.37;
    }
}

class Refineries {
    public List<IMyRefinery> list;
    public MyFixedPoint MaxVolume;
    public Refineries(Program program) {
        this.MaxVolume = (MyFixedPoint)0.0;
        this.list = new List<IMyRefinery>();
        var list = new List<IMyTerminalBlock>();
        program.GridTerminalSystem.GetBlocksOfType<IMyRefinery>(list);
        foreach(var block in list) {
            var refinery = (IMyRefinery)block;
            this.list.Add(refinery);
            this.MaxVolume += refinery.InputInventory.MaxVolume;
        }
        this.MaxVolume *= (MyFixedPoint)1000.0;
    }

    public MyFixedPoint CurrentVolume() {
        MyFixedPoint ret = (MyFixedPoint)0.0;
        foreach(var refinery in this.list) {
            ret += refinery.InputInventory.CurrentVolume;
        }
        return ret;
    }
}

public class StraightArm {
    List<IMyPistonBase> positive;
    List<IMyPistonBase> negative;
    public double Max;
    public double Pos;
    public StraightArm(List<IMyPistonBase> positive, List<IMyPistonBase> negative) {
        this.positive = positive;
        this.negative = negative;
        this.Max = 0.0;
        foreach(var piston in this.positive) {
            piston.Enabled = true;
            this.Max += piston.HighestPosition;
        }
        foreach(var piston in this.negative) {
            piston.Enabled = true;
            this.Max += piston.HighestPosition;
        }
    }

    public bool Empty() {
        return this.positive.Count + this.negative.Count == 0;
    }

    public void Refresh() {
        this.Pos = 0.0;
        foreach(var piston in this.positive) {
            this.Pos += piston.CurrentPosition;
        }
        foreach(var piston in this.negative) {
            this.Pos += piston.HighestPosition - piston.CurrentPosition;
        }
    }

    public void Move(double pos, double speed) {
        var current_pos = this.Pos;
        List<IMyPistonBase> to_extend;
        List<IMyPistonBase> to_retract;
        double needed;
        if(pos > current_pos) {
            needed = pos - current_pos;
            to_extend = this.positive;
            to_retract = this.negative;
        } else if(current_pos > pos) {
            needed = current_pos - pos;
            to_extend = this.negative;
            to_retract = this.positive;
        } else {
            return;
        }

        foreach(var piston in to_extend) {
            if(piston.CurrentPosition < piston.HighestPosition) {
                piston.MaxLimit = (float)Math.Min(piston.HighestPosition, piston.CurrentPosition + needed);
                piston.Velocity = (float)speed;
                return;
            }
        }

        foreach(var piston in to_retract) {
            if(piston.CurrentPosition > piston.LowestPosition) {
                piston.MinLimit = (float)Math.Max(piston.LowestPosition, piston.CurrentPosition - needed);
                piston.Velocity = (float)-speed;
                return;
            }
        }
    }

    public void Start() {
        foreach(var piston in this.positive) {
            piston.Enabled = true;
        }
        foreach(var piston in this.negative) {
            piston.Enabled = true;
        }
    }

    public void Stop() {
        foreach(var piston in this.positive) {
            piston.Enabled = false;
        }
        foreach(var piston in this.negative) {
            piston.Enabled = false;
        }
    }
}

public class Arm {
    public StraightArm X;
    public StraightArm Y;
    public StraightArm Z;
    public Vector3D Pos;
    public Vector3D Max;

    public Arm(Program program, string name) {
        var x_pos = new List<IMyPistonBase>();
        var x_neg = new List<IMyPistonBase>();
        var y_pos = new List<IMyPistonBase>();
        var y_neg = new List<IMyPistonBase>();
        var z_pos = new List<IMyPistonBase>();
        var z_neg = new List<IMyPistonBase>();

        var list = new List<IMyPistonBase>();
        program.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(list);
        foreach(var piston in list) {
            if(piston.CustomName == name + " X+") {
                x_pos.Add(piston);
            } else if(piston.CustomName == name + " X-") {
                x_neg.Add(piston);
            } else if(piston.CustomName == name + " Y+") {
                y_pos.Add(piston);
            } else if(piston.CustomName == name + " Y-") {
                y_neg.Add(piston);
            } else if(piston.CustomName == name + " Z+") {
                z_pos.Add(piston);
            } else if(piston.CustomName == name + " Z-") {
                z_neg.Add(piston);
            }
        }

        this.X = new StraightArm(x_pos, x_neg);
        this.Y = new StraightArm(y_pos, y_neg);
        this.Z = new StraightArm(z_pos, z_neg);
        this.Pos = new Vector3D(0.0, 0.0, 0.0);
        this.Max = new Vector3D(this.X.Max, this.Y.Max, this.Z.Max);
    }

    public bool Empty() {
        return this.X.Empty() && this.Y.Empty() && this.Z.Empty();
    }

    public void Refresh() {
        this.X.Refresh();
        this.Y.Refresh();
        this.Z.Refresh();
        this.Pos.X = this.X.Pos;
        this.Pos.Y = this.Y.Pos;
        this.Pos.Z = this.Z.Pos;
    }

    public void Move(Vector3D pos, Vector3D speed) {
        this.X.Move(pos.X, speed.X);
        this.Y.Move(pos.Y, speed.Y);
        this.Z.Move(pos.Z, speed.Z);
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
    public IMyShipDrill Drill;
    Program Program;
    int last_move;
    Vector3D Velocity;
    Vector3D VelocitySlow;
    float Step;
    float DepthStep;
    string Name;
    public Miner(Program program, string name, IMyShipDrill drill, float velocity, float step, float depth_step) {
        this.Program = program;
        this.Name = name;
        this.Arm = new Arm(program, name);
        this.Drill = drill;
        this.Drill.Enabled = true;
        this.Velocity = new Vector3D(velocity, velocity/2.0, velocity);
        this.VelocitySlow = new Vector3D(velocity/2.0, velocity/2.0, velocity);
        this.Step = step;
        this.DepthStep = depth_step;
        this.last_move = 0;
        this.MaxI.X = (int)((this.Arm.Max.X + this.Step - 1.0) / this.Step);
        this.MaxI.Y = (int)((this.Arm.Max.Y + this.Step - 1.0) / this.Step);
        this.MaxI.Z = (int)((this.Arm.Max.Z + this.DepthStep - 1.0) / this.DepthStep);
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
        this.Refresh();
        var move = this.SelectMove();
        this.last_move = move;

        var dst = new Vector3D(this.Arm.Pos.X, this.Arm.Pos.Y, this.Arm.Pos.Z);
        switch(move) {
            case EXTEND_X:
                dst.X = this.Arm.X.Max;
                break;
            case EXTEND_Y:
                dst.Y = (double)(this.PosI.Y + 1) * this.Step;
                break;
            case EXTEND_Z:
                dst.Z = (double)(this.PosI.Z + 1) * this.DepthStep;
                break;
            case RETRACT_X:
                dst.X = 0.0;
                break;
            case RETRACT_Y:
                dst.Y = (double)(this.PosI.Y - 1) * this.Step;
                break;
            case RETRACT_Z:
                dst.Z = (double)(this.PosI.Z - 1) * this.DepthStep;
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
        this.Arm.Move(dst, velocity);
    }

    public void Start() {
        this.Arm.Start();
    }

    public void Stop() {
        this.Arm.Stop();
    }

    public void Print() {
        Echo("X: " + this.Arm.X.Pos.ToString("0.0") + "/" + this.Arm.X.Max + " | I: " + this.PosI.X + "/" + this.MaxI.X);
        Echo("Y: " + this.Arm.Y.Pos.ToString("0.0") + "/" + this.Arm.Y.Max + " | I: " + this.PosI.Y + "/" + this.MaxI.Y);
        Echo("Z: " + this.Arm.Z.Pos.ToString("0.0") + "/" + this.Arm.Z.Max + " | I: " + this.PosI.Z + "/" + this.MaxI.Z);
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
    }

    public void Echo(string s) {
        this.Program.Echo(this.Name + ": " + s);
    }
}

public Miner GetMiner(string name, float velocity, float step, float depth_step) {
    var list = new List<IMyShipDrill>();
    GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(list);
    foreach(var drill in list) {
        if(drill.CustomName == name) {
            var miner = new Miner(this, name, drill, velocity, step, depth_step);
            if(miner.Arm.Empty()) {
                Echo(name + " has no arm. Not a miner.");
                return null;
            } else {
                return miner;
            }
        }
    }
    Echo("Could not find any drill for " + name);
    return null;
}

Containers containers;
Refineries refineries;
List<Miner> miners;
bool mining;
public Program() {
    this.mining = false;

    this.containers = new Containers(this);
    this.refineries = new Refineries(this);
    this.miners = new List<Miner>();

    IMyTextSurface surface = Me.GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.FontSize = 2;
    surface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;

    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

double UsedVolume;
double UsedVolumePercent;
void Refresh() {
    this.UsedVolume = (double)(this.refineries.CurrentVolume() + this.containers.OreVolume());
    this.UsedVolumePercent = this.UsedVolume / (double)this.refineries.MaxVolume;
}

public bool should_start() {
    var threshold = 0.4;
    if(this.mining) {
        threshold = 0.6;
    }
    if(this.UsedVolumePercent > threshold) {
        return false;
    } else {
        return true;
    }
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
        "Refineries: " + (this.UsedVolumePercent * 100.0).ToString("0.000") + "%",
    };
    surface.WriteText(String.Join("\n", lines));
}

public void Main(string argument) {
    if(this.miners.Count == 0){
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
    this.Refresh();

    var should_start = this.should_start();
    Echo("Should start " + should_start + " | mining: " + this.mining);
    if(should_start) {
        if(!this.mining) {
            Echo("Auto Start");
            foreach(var miner in this.miners) {
                miner.Start();
            }
            this.mining = true;
        }
    } else {
        if(this.mining) {
            Echo("Auto Paused");
            foreach(var miner in this.miners) {
                miner.Stop();
            }
            this.mining = false;
        }
    }

    var i = 0;
    foreach(var miner in this.miners) {
        if(this.mining) {
            miner.Run();
        } else {
            miner.Refresh();
        }
        miner.Print();
        i++;
    }
    Echo("Volume: " + this.UsedVolume.ToString("0.0") + "/" + this.refineries.MaxVolume);

    UpdateProgressScreen();
}
