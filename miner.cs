public Program() {
    // Runtime.UpdateFrequency |= UpdateFrequency.Update100;
    this.name_prefix = "Miner Piston";
    this.velocity = 0.5f;
    this.step = 2.0f;
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

    public Pistons(Program program, string name, bool precise_position) {
        program.Echo("Loading " + name);
        this.program = program;
        this.list = program.get_pistons(name);
        this.pos = 0.0f;
        this.precise_position = precise_position;
        for(var i = 0; i < this.list.Count; i++) {
            var p = this.list[i];
            var pos = program.get_piston_position(p);
            this.pos += pos;
            p.GetActionWithName("OnOff_Off").Apply(p);
            program.Echo(name + "[" + i + "] = " + pos + " " + p.CustomName);
            if(!this.precise_position) {
                p.MaxLimit = 10.0f;
                p.MinLimit = 0.0f;
            }
        }
        var step = program.step;
        this.pos_i = Convert.ToInt32(Math.Floor(this.pos / step));
        this.max = (float)this.list.Count * 10.0f;
        this.max_i = Convert.ToInt32(Math.Floor(this.max / step));
        program.Echo("pos: " + this.pos + " | max_i: " + this.max);
        program.Echo("pos_i: " + this.pos_i + " | max_i: " + this.max_i);
    }

    public void extend() {
        var target = (float)(this.pos_i + 1) * this.program.step;
        var missing = target - this.pos;
        foreach(var p in this.list) {
            this.program.Echo("Status: "+ p.Status);
            var pos = this.program.get_piston_position(p);
            if(pos <= 9.9f) {
                p.Velocity = this.program.velocity;
                if(this.precise_position) {
                    p.MaxLimit = pos + missing;
                }
                this.program.Echo("target: " + target + " | missing: " + missing + " | pos: " + pos + " | set limit: " + p.MaxLimit);
                p.GetActionWithName("OnOff_On").Apply(p);
                return;
            }
        }
    }

    public void retract() {
        var target = (float)(this.pos_i - 1) * this.program.step;
        var missing = target - this.pos;
        foreach(var p in this.list) {
            this.program.Echo("Status: "+ p.Status);
            var pos = this.program.get_piston_position(p);
            if(pos >= 0.1f) {
                p.Velocity = -this.program.velocity;
                if(this.precise_position) {
                    p.MinLimit = pos + missing;
                }
                this.program.Echo("target: " + target + " | missing: " + missing + " | pos: " + pos + " | set limit: " + p.MaxLimit);
                p.GetActionWithName("OnOff_On").Apply(p);
                return;
            }
        }
    }
}

public const int EXTEND_X = 0;
public const int EXTEND_Y = 1;
public const int EXTEND_Z = 2;
public const int RETRACT_X = 3;
public const int RETRACT_Y = 4;
public const int RETRACT_Z = 5;

public int SelectMove(int x, int y, int z, int max_x, int max_y, int max_z) {
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

string name_prefix;
float velocity;
float step;

public void Main(string argument)
{
    var args = argument.Split(' ');
    if(args.Length >= 3) {
        this.name_prefix = args[0];
        this.velocity = float.Parse(args[1]);
        this.step = float.Parse(args[2]);
    }
    // if(SelectMove(0, 0, 0, 0, 0, 0) != 6) {
    //     throw new Exception("Len: " + args.Length + " | " + this.name_prefix + " | " + this.velocity + " | " + this.step);
    // }

    var x_pistons = new Pistons(this, name_prefix + " X", false);
    var y_pistons = new Pistons(this, name_prefix + " Y", true);
    var z_pistons = new Pistons(this, name_prefix + " Z", true);

    var move = SelectMove(x_pistons.pos_i, y_pistons.pos_i, z_pistons.pos_i, x_pistons.max_i, y_pistons.max_i, z_pistons.max_i);
    Echo("Selected move " + move);
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
