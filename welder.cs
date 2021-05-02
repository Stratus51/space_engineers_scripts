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
    public Vector3D Dst;

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
        this.Dst = pos;
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

public class Welder {
    public Program Program;
    public Arm Arm;
    public IMyShipWelder welder;
    public double BlockSize;

    public Welder(Program program, string name, IMyShipWelder welder) {
        this.Program = program;
        this.Arm = new Arm(program, name);
        this.welder = welder;
        this.BlockSize = this.welder.CubeGrid.GridSize;
    }

    public void Refresh() {
        this.Arm.Refresh();
    }

    double Distance2DSq(Vector3D pos) {
        var dx = this.Arm.Pos.X - pos.X;
        var dy = this.Arm.Pos.Y - pos.Y;
        return dx * dx + dy * dy;
    }

    void Echo(string s) {
        this.Program.Echo(s);
    }

    public void Weld(Vector3D pos, Vector3D speed, Vector3D transport_speed) {
        var in_reach = false;
        var h_dist = this.Distance2DSq(pos);
        Echo("h_dist " + h_dist);
        if(h_dist > 0.1) {
            var transport_height = 0.0;
            // if(h_dist < 1.1) {
            //     transport_height = pos.Z;
            // }
            if(this.Arm.Pos.Z > transport_height) {
                this.Arm.Move(new Vector3D(this.Arm.Pos.X, this.Arm.Pos.Y, 0.0), transport_speed);
            } else {
                this.Arm.Move(new Vector3D(pos.X, pos.Y, 0.0), transport_speed);
            }
        } else if (pos.Z - this.Arm.Pos.Z > 2.0) {
            this.Arm.Move(pos, transport_speed);
        } else if(pos.Z - this.Arm.Pos.Z > 0.1) {
            this.Arm.Move(pos, speed);
        } else {
            in_reach = true;
        }

        if(in_reach) {
            this.welder.Enabled = true;
        } else {
            this.welder.Enabled = false;
        }
    }
}

public Welder GetWelder(string name) {
    var list = new List<IMyShipWelder>();
    GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(list);
    foreach(var welder in list) {
        if(welder.CustomName == name) {
            return new Welder(this, name, welder);
        }
    }
    Echo("Could not find welder of " + name);
    return null;
}

public const int EXTEND_X = 0;
public const int EXTEND_Y = 1;
public const int RETRACT_X = 3;
public const int RETRACT_Y = 4;
public const int PARKING = 5;

public class DepthSensors {
    public Program Program;
    public IMySensorBlock XPos;
    public IMySensorBlock XNeg;
    public IMySensorBlock YPos;
    public IMySensorBlock YNeg;
    public float SensorToWelderDist;
    public float CubeSize;

    List<IMySensorBlock> sensors;

    public DepthSensors(Program program, IMySensorBlock XPos, IMySensorBlock XNeg, IMySensorBlock YPos, IMySensorBlock YNeg) {
        this.Program = program;
        this.XPos = XPos;
        this.XNeg = XNeg;
        this.YPos = YPos;
        this.YNeg = YNeg;
        this.sensors = new List<IMySensorBlock>(){ XPos, XNeg, YPos, YNeg };
        this.CubeSize = XPos.CubeGrid.GridSize;
        this.SensorToWelderDist = this.CubeSize * 1.5f;
        foreach(var sensor in this.sensors) {
            sensor.DetectPlayers = false;
            sensor.DetectFloatingObjects = false;
            sensor.DetectAsteroids = false;

            sensor.DetectSubgrids = true;
            sensor.DetectSmallShips = true;
            sensor.DetectLargeShips = true;
            sensor.DetectStations = true;
            sensor.DetectOwner = true;
            sensor.DetectFriendly = true;
            sensor.DetectNeutral = true;
            sensor.DetectEnemy = true;

            sensor.LeftExtend = 0.0f;
            sensor.RightExtend = 0.0f;
            sensor.TopExtend = 0.0f;
            sensor.BottomExtend = this.SensorToWelderDist + 0.5f;
            sensor.FrontExtend = 0.0f;
            sensor.BackExtend = 0.0f;
        }
    }

    public void SetRange(float range) {
        foreach(var sensor in this.sensors) {
            sensor.FrontExtend = 0.0f;
            sensor.BottomExtend = this.SensorToWelderDist + range;
            sensor.LeftExtend = 0.0f;
            sensor.RightExtend = 0.0f;
        }
    }

    public void SideDetection(int direction, float range) {
        foreach(var s in this.sensors) {
            s.BottomExtend = 0.0f;
            s.FrontExtend = 0.0f;
            s.LeftExtend = 0.0f;
            s.RightExtend = 0.0f;
        }
        IMySensorBlock sensor = null;
        switch(direction) {
            case EXTEND_X:
                sensor = this.XPos;
                break;
            case EXTEND_Y:
                sensor = this.YPos;
                break;
            case RETRACT_X:
                sensor = this.XNeg;
                break;
            case RETRACT_Y:
                sensor = this.YNeg;
                break;
            default:
                break;
        }
        if(sensor != null) {
            sensor.FrontExtend = range;
            sensor.BottomExtend = this.SensorToWelderDist;
            sensor.LeftExtend = this.CubeSize/2;
            sensor.RightExtend = this.CubeSize/2;
        }
    }

    Nullable<double> GetSensorDepth(IMySensorBlock sensor) {
        var list = new List<MyDetectedEntityInfo>();
        sensor.DetectedEntities(list);
        var ret = 10000.0;
        foreach(var entity in list) {
            var target = entity.HitPosition;
            var source_pos = sensor.GetPosition();
            if(target != null) {
                var dist = Vector3D.DistanceSquared((Vector3D)target, source_pos);
                if(dist < ret) {
                    ret = dist;
                }
            }
        }
        if(ret == 10000.0) {
            return null;
        } else {
            return Math.Sqrt(ret);
        }
    }

    public double? XPosDepth() {
        return GetSensorDepth(this.XPos);
    }

    public double? XNegDepth() {
        return GetSensorDepth(this.XNeg);
    }

    public double? YPosDepth() {
        return GetSensorDepth(this.YPos);
    }

    public double? YNegDepth() {
        return GetSensorDepth(this.YNeg);
    }

    public bool IsActive() {
        foreach(var sensor in this.sensors) {
            if(sensor.IsActive) {
                return true;
            }
        }
        return false;
    }
}

public DepthSensors GetDepthSensors(string name) {
    var list = new List<IMySensorBlock>();
    GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(list);
    IMySensorBlock XPos = null;
    IMySensorBlock XNeg = null;
    IMySensorBlock YPos = null;
    IMySensorBlock YNeg = null;
    foreach(var sensor in list) {
        if(sensor.CustomName == name + " X+") {
            XPos = sensor;
        } else if(sensor.CustomName == name + " X-") {
            XNeg = sensor;
        } else if(sensor.CustomName == name + " Y+") {
            YPos = sensor;
        } else if(sensor.CustomName == name + " Y-") {
            YNeg = sensor;
        }
    }
    if(XPos == null || XNeg == null || YPos == null || YNeg == null) {
        return null;
    } else {
        return new DepthSensors(this, XPos, XNeg, YPos, YNeg);
    }
}

public const float LONG_RANGE = 2.0f;
public const float MID_RANGE = 1.0f;
public const float SHORT_RANGE = 0.5f;

public class AutoWelder {
    Program Program;
    public Vector3I PosI;
    public Vector3I MinI;
    public Vector3I MaxI;
    public Vector3D Min;
    public Welder Welder;
    public DepthSensors Sensors;
    public Vector3D Velocity;
    public Vector3D Fast;
    public Vector3D MidVelocity;
    string Name;
    int WeldDuration;
    int RemainingWeld;
    Vector3D NextPos;
    double Step;
    float MidRange;
    float WeldDistance;

    enum State {
        ExtendingFast,
        Extending,
        ExtendingSlow,
        Welding,
        Retracting,
        Moving,
        ParkingRetracting,
        Parking,
        Parked,
    }
    State state;
    State NextState;

    public AutoWelder(Program program, string name, Welder welder, DepthSensors sensors, Vector3D velocity, int duration, double step, double weld_distance) {
        this.Program = program;
        this.Welder = welder;
        this.Sensors = sensors;
        this.Velocity = velocity;
        this.Fast = new Vector3D(5.0, 5.0, 5.0);
        this.MidVelocity = new Vector3D(2.5, 2.5, 2.5);
        this.Name = name;
        this.Step = this.Welder.BlockSize * step;
        var max = this.Welder.Arm.Max;
        this.MaxI.X = (int)((max.X + this.Step - 1.0) / this.Step);
        this.MaxI.Y = (int)((max.Y + this.Step - 1.0) / this.Step);
        this.MaxI.Z = (int)((max.Z + this.Step - 1.0) / this.Step);
        this.state = State.Extending;
        this.WeldDuration = duration;
        this.RemainingWeld = 0;
        this.Min.X = (double)this.MinI.X * this.Step;
        this.Min.Y = (double)this.MinI.Y * this.Step;
        this.Min.Z = (double)this.MinI.Z * this.Step;
        this.Welder.Arm.Start();
        this.Sensors.SetRange(LONG_RANGE);
        this.WeldDistance = (float)weld_distance;
        this.MidRange = Math.Max((float)weld_distance, MID_RANGE);
    }

    public void Refresh() {
        this.Welder.Refresh();
        var pos = this.Welder.Arm.Pos;
        this.PosI.X = (int)(pos.X / this.Step);
        this.PosI.Y = (int)(pos.Y / this.Step);
        this.PosI.Z = (int)(pos.Z / this.Step);
    }

    public int SelectMove() {
        int x = this.PosI.X;
        int y = this.PosI.Y;
        int max_x = this.MaxI.X;
        int max_y = this.MaxI.Y;
        if(y % 2 == 0) {
            if(x != max_x) {
                return EXTEND_X;
            } else {
                if(y == max_y) {
                    return PARKING;
                } else {
                    return EXTEND_Y;
                }
            }
        } else {
            if(x != 0) {
                return RETRACT_X;
            } else {
                if(y == max_y) {
                    return PARKING;
                } else {
                    return EXTEND_Y;
                }
            }
        }
    }

    public Vector3D? NextPosFromMove(int move) {
        var arm = this.Welder.Arm;
        var dst = new Vector3D(arm.Pos.X, arm.Pos.Y, arm.Pos.Z);
        switch(move) {
            case EXTEND_X:
                dst.X = (double)(this.PosI.X + 1) * this.Step;
                break;
            case EXTEND_Y:
                dst.Y = (double)(this.PosI.Y + 1) * this.Step;
                break;
            case RETRACT_X:
                dst.X = (double)(this.PosI.X - 1) * this.Step;
                break;
            case RETRACT_Y:
                dst.Y = (double)(this.PosI.Y - 1) * this.Step;
                break;
            case PARKING:
                return null;
            default:
                break;
        }
        dst.Z = 0.0;
        return dst;
    }

    public void Run() {
        this.Refresh();
        var arm = this.Welder.Arm;

        var dst = new Vector3D(arm.Pos.X, arm.Pos.Y, arm.Pos.Z);
        switch(this.state) {
            case State.ExtendingFast:
                if(!this.Sensors.IsActive()) {
                    dst.Z = arm.Max.Z;
                    arm.Move(dst, this.Fast);
                } else {
                    this.state = State.Extending;
                    this.Sensors.SetRange(this.MidRange);
                    arm.Move(dst, this.MidVelocity);
                }
                break;
            case State.Extending:
                if(!this.Sensors.IsActive()) {
                    dst.Z = arm.Max.Z;
                    arm.Move(dst, this.MidVelocity);
                } else {
                    this.state = State.ExtendingSlow;
                    this.Sensors.SetRange(this.WeldDistance);
                    arm.Move(dst, this.Velocity);
                }
                break;
            case State.ExtendingSlow:
                if(!this.Sensors.IsActive()) {
                    dst.Z = arm.Max.Z;
                    arm.Move(dst, this.Velocity);
                } else {
                    this.state = State.Welding;
                    this.RemainingWeld = this.WeldDuration;
                    this.Welder.welder.Enabled = true;

                    var move = this.SelectMove();
                    var next_pos = this.NextPosFromMove(move);
                    if(next_pos != null) {
                        this.NextPos = (Vector3D)next_pos;
                    }
                    if(move == PARKING) {
                        this.NextState = State.ParkingRetracting;
                    } else {
                        this.NextState = State.Moving;
                    }
                    this.Sensors.SideDetection(move, (float)this.Step);
                    arm.Stop();
                }
                break;
            case State.Welding:
                this.RemainingWeld--;
                if(this.RemainingWeld <= 0) {
                    this.state = State.Retracting;
                    arm.Start();
                    this.Welder.welder.Enabled = false;
                }
                break;
            case State.Retracting:
                if(dst.Z > 0.0 && this.Sensors.IsActive()) {
                    dst.Z = 0.0;
                    arm.Move(dst, this.Fast);
                } else {
                    this.state = this.NextState;
                    this.NextPos.Z = dst.Z;
                }
                break;
            case State.Moving:
                if(arm.Pos == this.NextPos) {
                    this.state = State.Extending;
                } else {
                    arm.Move(this.NextPos, this.Fast);
                    this.Sensors.SetRange(LONG_RANGE);
                }
                break;
            case State.ParkingRetracting:
                if(arm.Pos.Z == 0.0) {
                    this.state = State.Parking;
                } else {
                    dst.Z = Min.Z;
                    Echo("Move " + dst + " " + this.Fast);
                    arm.Move(dst, this.Fast);
                }
                break;
            case State.Parking:
                if(arm.Pos == Min) {
                    this.state = State.Parked;
                } else {
                    arm.Move(this.Min, this.Fast);
                }
                break;
            case State.Parked:
                break;
        }
    }

    public void Print() {
        var arm = this.Welder.Arm;
        Echo("X: " + arm.X.Pos.ToString("0.0") + "/" + arm.X.Max + " | I: " + this.PosI.X + "/" + this.MaxI.X);
        Echo("Y: " + arm.Y.Pos.ToString("0.0") + "/" + arm.Y.Max + " | I: " + this.PosI.Y + "/" + this.MaxI.Y);
        Echo("Z: " + arm.Z.Pos.ToString("0.0") + "/" + arm.Z.Max + " | I: " + this.PosI.Z + "/" + this.MaxI.Z);
        Echo("Sensor: " + this.Sensors.IsActive());
        Echo("State: " + this.state);
    }

    public void Echo(string s) {
        this.Program.Echo(this.Name + ": " + s);
    }

    public bool IsParked() {
        return this.state == State.Parked;
    }
}

AutoWelder GetAutoWelder(string name, double velocity, int weld_duration, double step, double weld_distance) {
    var welder = GetWelder(name);
    var sensors = GetDepthSensors(name);
    if(welder == null || sensors == null) {
        return null;
    }
    return new AutoWelder(this, name, welder, sensors, new Vector3D(velocity, velocity, velocity), weld_duration, step, weld_distance);
}

List<AutoWelder> auto_welders;
public void InitAutoWelders(string name_prefix, double velocity, int weld_duration, double step, double weld_distance) {
    for(var i = 0; i < 100; i++) {
        var auto_welder = GetAutoWelder(name_prefix + " " + i, velocity, weld_duration, step, weld_distance);
        if(auto_welder == null) {
            return;
        }
        this.auto_welders.Add(auto_welder);
    }
}

public Program() {
    Runtime.UpdateFrequency |= UpdateFrequency.Update10;
    this.auto_welders = new List<AutoWelder>();
}

public void Main(string argument) {
    if(this.auto_welders.Count == 0) {
        var args = argument.Split(' ').ToList();
        if (args.Count >= 2) {
            var name_prefix = args[0];
            var velocity = double.Parse(args[1]);
            var weld_duration = int.Parse(args[2]);
            var step = double.Parse(args[3]);
            var weld_distance = double.Parse(args[4]);
            InitAutoWelders(name_prefix, velocity, weld_duration, step, weld_distance);
            if(this.auto_welders.Count > 0) {
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            }
        } else {
            Echo("Missing arguments: " + args.Count + " < 3");
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
            return;
        }
    }

    var all_parked = true;
    var i = 0;
    foreach(var auto_welder in this.auto_welders) {
        auto_welder.Run();
        auto_welder.Print();
        if(!auto_welder.IsParked()) {
            all_parked = false;
        }
        i++;
    }
    if(all_parked) {
        Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
    }
}
