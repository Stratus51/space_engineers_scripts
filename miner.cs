class Containers {
    public List<IMyCargoContainer> list;
    public MyFixedPoint MaxVolume;
    public List<string> Ores;
    public Containers(Program program) {
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
        foreach(var ore in this.Ores) {
            var ty = MyItemType.MakeOre(ore);
            ret += this.GetItemAmount(ty);
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

public List<IMyPistonBase> get_pistons(string name) {
    var ret = new List<IMyPistonBase>();
    var list = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(name, list);
    foreach (var block in list) {
        ret.Add(block as IMyPistonBase);
    }
    return ret;
}

public float get_piston_position(IMyPistonBase piston) {
    var pistonInf = piston.DetailedInfo;

    //splits the string into an array by separating by the ':' character
    string[] pistInfArr = (pistonInf.Split(':'));

    // splits the resulting 0.0m into an array with single index of "0.0" by again splitting by character "m"
    string[] pistonDist = (pistInfArr[1].Split('m'));

    //uses double.Parse method to parse the "0.0" into a usable double of value 0.0
    return float.Parse(pistonDist[0]);
}

class Pistons {
    public List<IMyPistonBase> list;
    public float pos;
    public int pos_i;
    public float max;
    public int max_i;
    public Program program;
    public bool precise_position;
    float velocity;
    float step;

    public Pistons(Program program, string name, bool precise_position, float velocity, float step) {
        // program.Echo("Loading " + name);
        this.program = program;
        this.list = program.get_pistons(name);
        this.pos = 0.0f;
        this.precise_position = precise_position;
        this.velocity = velocity;
        this.step = step;
        // program.Echo("pos: " + this.pos + " | max_i: " + this.max);
        // program.Echo("pos_i: " + this.pos_i + " | max_i: " + this.max_i);
        this.pos = 0.0f;
        this.max = 0.0f;
        this.pos_i = 0;
        this.max_i = 0;
        foreach(var piston in this.list) {
            this.max += (float)piston.HighestPosition;
        }
        this.refresh();
        foreach(var piston in this.list) {
            if(this.precise_position) {
                var pos_i = Convert.ToInt32(Math.Floor(piston.CurrentPosition / step));
                piston.MaxLimit = (float)(pos_i + 1) * step;
                piston.MinLimit = (float)pos_i * step;
            } else {
                piston.MaxLimit = 10.0f;
                piston.MinLimit = 0.0f;
            }
        }
    }

    public void refresh() {
        this.pos = 0.0f;
        for(var i = 0; i < this.list.Count; i++) {
            var p = this.list[i];
            // var pos = program.get_piston_position(p);
            var pos = p.CurrentPosition;
            this.pos += pos;
            // p.GetActionWithName("OnOff_Off").Apply(p);
            // program.Echo(name + "[" + i + "] = " + pos + " " + p.CustomName);
        }
        this.pos_i = Convert.ToInt32(Math.Floor(this.pos / step));
        this.max_i = Convert.ToInt32(Math.Floor(this.max / step));
    }

    public void extend() {
        var target = (float)(this.pos_i + 1) * this.step;
        var missing = target - this.pos;
        foreach(var p in this.list) {
            // this.program.Echo("Status: "+ p.Status);
            var pos = this.program.get_piston_position(p);
            if(this.precise_position) {
                p.Velocity = this.velocity;
                p.GetActionWithName("OnOff_On").Apply(p);
                p.MaxLimit = Math.Min(pos + missing, 10.0f);
                this.program.Echo("target: " + target + " | missing: " + missing + " | pos: " + pos + " | set limit: " + p.MaxLimit);
                missing -= p.MaxLimit - pos;
                if(missing < 0.00000001f) {
                    return;
                }
            } else {
                if(pos <= 9.95f) {
                    p.Velocity = this.velocity;
                    return;
                }
            }
        }
    }

    public void retract() {
        var target = (float)(this.pos_i - 1) * this.step;
        var missing = target - this.pos;
        foreach(var p in this.list) {
            // this.program.Echo("Status: "+ p.Status);
            var pos = this.program.get_piston_position(p);
            if(this.precise_position) {
                p.Velocity = -this.velocity;
                p.GetActionWithName("OnOff_On").Apply(p);
                p.MinLimit = Math.Max(pos + missing, 0.0f);
                this.program.Echo("target: " + target + " | missing: " + missing + " | pos: " + pos + " | set limit: " + p.MinLimit);
                missing -= p.MinLimit - pos;
                if(missing > -0.00000001f) {
                    return;
                }
            } else {
                if(pos > 0.05f) {
                    p.Velocity = -this.velocity;
                    return;
                }
            }
        }
    }

    public void stop() {
        foreach(var p in this.list) {
            p.GetActionWithName("OnOff_Off").Apply(p);
        }
    }

    public void start() {
        foreach(var p in this.list) {
            p.GetActionWithName("OnOff_On").Apply(p);
        }
    }
}

public const int EXTEND_X = 0;
public const int EXTEND_Y = 1;
public const int EXTEND_Z = 2;
public const int RETRACT_X = 3;
public const int RETRACT_Y = 4;
public const int RETRACT_Z = 5;

class Miner {
    Pistons x_pistons;
    Pistons y_pistons;
    Pistons z_pistons;
    int last_move;
    public Miner(Program program, string name, float velocity, float step, float depth_step) {
        this.x_pistons = new Pistons(program, name + " X", false, velocity, step);
        this.y_pistons = new Pistons(program, name + " Y", true, velocity, step);
        this.z_pistons = new Pistons(program, name + " Z", true, velocity, depth_step);
        this.last_move = 0;
    }

    public int NbPistons() {
        return this.x_pistons.list.Count + this.y_pistons.list.Count + this.z_pistons.list.Count;
    }

    public void refresh() {
        this.x_pistons.refresh();
        this.y_pistons.refresh();
        this.z_pistons.refresh();
    }

    public int SelectMove() {
        int x = x_pistons.pos_i;
        int y = y_pistons.pos_i;
        int z = z_pistons.pos_i;
        int max_x = x_pistons.max_i;
        int max_y = y_pistons.max_i;
        int max_z = z_pistons.max_i;
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
                if(x != max_y) {
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

    public void run() {
        this.refresh();
        var move = this.SelectMove();
        this.last_move = move;
        switch(move) {
            case EXTEND_X:
                x_pistons.extend();
                break;
            case EXTEND_Y:
                y_pistons.extend();
                break;
            case EXTEND_Z:
                z_pistons.extend();
                break;
            case RETRACT_X:
                x_pistons.retract();
                break;
            case RETRACT_Y:
                y_pistons.retract();
                break;
            case RETRACT_Z:
                z_pistons.retract();
                break;
            default:
                break;
        }
    }

    public void start() {
        this.x_pistons.start();
        this.y_pistons.start();
        this.z_pistons.start();
    }

    public void stop() {
        this.x_pistons.stop();
        this.y_pistons.stop();
        this.z_pistons.stop();
    }

    public void print(Program program) {
        program.Echo("X: " + this.x_pistons.pos + "/" + this.x_pistons.max + " | I: " + this.x_pistons.pos_i + "/" + this.x_pistons.max_i);
        program.Echo("Y: " + this.y_pistons.pos + "/" + this.y_pistons.max + " | I: " + this.y_pistons.pos_i + "/" + this.y_pistons.max_i);
        program.Echo("Z: " + this.z_pistons.pos + "/" + this.z_pistons.max + " | I: " + this.z_pistons.pos_i + "/" + this.z_pistons.max_i);
        switch(this.last_move) {
            case EXTEND_X:
                program.Echo("Moving: X+");
                break;
            case EXTEND_Y:
                program.Echo("Moving: Y+");
                break;
            case EXTEND_Z:
                program.Echo("Moving: Z+");
                break;
            case RETRACT_X:
                program.Echo("Moving: X-");
                break;
            case RETRACT_Y:
                program.Echo("Moving: Y-");
                break;
            case RETRACT_Z:
                program.Echo("Moving: Z-");
                break;
            default:
                break;
        }
    }
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
    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
}

public bool should_start() {
    var fill = this.refineries.CurrentVolume() + this.containers.OreVolume();
    var threshold = (MyFixedPoint)0.4;
    if(this.mining) {
        threshold = (MyFixedPoint)0.6;
    }
    // Echo("Refinery: " + this.refineries.CurrentVolume() + " | containers: " + this.containers.OreVolume());
    // Echo("fill: " + fill + " | threshold: " + this.refineries.MaxVolume * threshold);
    if(fill > this.refineries.MaxVolume * threshold) {
        return false;
    } else {
        return true;
    }
}

public void InitMiners(string name_prefix, float velocity, float step, float depth_step) {
    for(var i = 0; i < 100; i++) {
        var miner = new Miner(this, name_prefix + " " + i, velocity, step, depth_step);
        if(miner.NbPistons() == 0) {
            break;
        }
        this.miners.Add(miner);
    }
}

public void Main(string argument)
{
    var args = argument.Split(' ').ToList();
    if(this.miners.Count == 0){
        if (args.Count >= 3) {
            var name_prefix = args[0];
            var velocity = float.Parse(args[1]);
            var step = float.Parse(args[2]);
            var depth_step = float.Parse(args[3]);
            InitMiners(name_prefix, velocity, step, depth_step);
        } else {
            Echo("Missing arguments: " + args.Count + " < 3");
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
            return;
        }
    }

    var should_start = this.should_start();
    Echo("Should start " + should_start + " | mining: " + this.mining);
    if(this.mining && !should_start) {
        Echo("Auto Paused");
        foreach(var miner in this.miners) {
            miner.stop();
        }
        this.mining = false;
        return;
    } else if(!this.mining && should_start) {
        Echo("Auto Start");
        foreach(var miner in this.miners) {
            miner.start();
        }
        this.mining = true;
    }

    var i = 0;
    foreach(var miner in this.miners) {
        miner.run();
        Echo("Miner " + i + ":");
        miner.print(this);
        i++;
    }
    Echo("Volume: " + this.refineries.CurrentVolume() + this.containers.OreVolume() + "/" + this.refineries.MaxVolume);
}
