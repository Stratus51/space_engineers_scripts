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

public class WeldingSlider: Slider {
    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public SliderDirection Direction {get;}

    Program Program;
    Slider[] Arms;
    Connectors[] Links;
    IMyCubeBlock Front;
    Vector3D FrontDirection;
    State state;
    float Speed;

    enum State {
        Holding0,
        Holding1,
        SwitchingTo0,
        SwitchingTo1,
    }

    public WeldingSlider(Program program, Slider[] arms, Connectors[] links, IMyCubeBlock front, SliderDirection direction, float min, float max) {
        this.Program = program;
        this.Direction = direction;
        this.Arms = arms;
        this.Links = links;
        this.Front = front;
        this.Min = min;
        this.Max = max;
        this.Speed = 0;

        if(links[0].Status() == MyShipConnectorStatus.Connected) {
            this.state = State.Holding0;
        } else if(links[1].Status() == MyShipConnectorStatus.Connected) {
            this.state = State.Holding1;
        } else {
            throw new Exception("WeldingSlider not holding a central bar.");
        }

        this.FrontDirection = this.Arms[0].WorldDirection();

        this.Refresh();
    }

    public string Name() {
        return this.Arms[0].Name();
    }

    public Connectors HoldingLink() {
        if(this.Links[0].Status() == MyShipConnectorStatus.Connected) {
            return this.Links[0];
        } else {
            return this.Links[1];
        }
    }

    public void Refresh() {
        this.Arms[0].Refresh();
        this.Arms[1].Refresh();

        var anchor_connector = this.HoldingLink().List[0].OtherConnector;
        var bar_grid = anchor_connector.CubeGrid;

        var bar_max = bar_grid.Max;
        Echo("Bar max: " + bar_max + " | Anchor: " + anchor_connector.Position);
        var bar_max_dim = Math.Max(Math.Max(bar_max.X, bar_max.Y), bar_max.Z);
        float bar_size;
        // TODO: That's a shady way of getting the bar direction...
        if(bar_max.X == bar_max_dim) {
            bar_size = bar_max.X - anchor_connector.Position.X;
        } else if(bar_max.Y == bar_max_dim) {
            bar_size = bar_max.Y - anchor_connector.Position.Y;
        } else {
            bar_size = bar_max.Z - anchor_connector.Position.Z;
        }
        this.Pos = this.HoldingArm().Pos + bar_size * bar_grid.GridSize;
    }

    public bool Sync() {
        var synced = true;
        synced = synced && this.Arms[0].Sync();
        synced = synced && this.Arms[1].Sync();
        return synced;
    }

    public void SetSpeed(float speed) {
        this.Speed = speed;
        switch(this.state) {
            case State.Holding0:
            case State.SwitchingTo1:
                this.Arms[0].SetSpeed(speed);
                this.Arms[1].SetSpeed(-speed);
                break;
            case State.Holding1:
            case State.SwitchingTo0:
                this.Arms[0].SetSpeed(-speed);
                this.Arms[1].SetSpeed(speed);
                break;
        }
    }

    public void Reverse() {
        switch(this.state) {
            case State.Holding0:
            case State.Holding1:
                this.Arms[0].Reverse();
                this.Arms[1].Reverse();
                break;
            case State.SwitchingTo0:
                this.Links[1].Connect();
                this.state = State.SwitchingTo1;
                break;
            case State.SwitchingTo1:
                this.Links[0].Connect();
                this.state = State.SwitchingTo0;
                break;
        }
    }

    // TODO Run should be removed and merged into SetSpeed and MoveTo with the current API usage
    public void Run() {
        var arm_0_sync = this.Arms[0].Sync();
        var arm_1_sync = this.Arms[1].Sync();
        if(!arm_0_sync || !arm_1_sync) {
            return;
        }
        switch(this.state) {
            case State.Holding0:
                if(this.Speed > 0) {
                    if(this.Arms[0].Pos == this.Arms[0].Max && this.Arms[1].Pos == 0) {
                        this.Links[1].Connect();
                        this.state = State.SwitchingTo1;
                    }
                } else {
                    if(this.Arms[1].Pos == this.Arms[1].Max && this.Arms[0].Pos == 0) {
                        this.Links[1].Connect();
                        this.state = State.SwitchingTo1;
                    }
                }
                break;
            case State.SwitchingTo1:
                if(this.Links[1].Status() == MyShipConnectorStatus.Connected) {
                    switch(this.Links[0].Status()) {
                        case MyShipConnectorStatus.Connected:
                            this.Links[0].Disconnect();
                            break;
                        default:
                            this.state = State.Holding1;
                            this.Arms[0].Reverse();
                            this.Arms[1].Reverse();
                            break;
                    }
                } else {
                    throw new Exception("Link 1 failed to connect");
                }
                break;
            case State.Holding1:
                if(this.Speed > 0) {
                    if(this.Arms[1].Pos == this.Arms[1].Max && this.Arms[0].Pos == 0) {
                        Echo("Arm Maxed");
                        this.Links[0].Connect();
                        this.state = State.SwitchingTo0;
                    }
                } else {
                    if(this.Arms[0].Pos == this.Arms[0].Max && this.Arms[1].Pos == 0) {
                        this.Links[0].Connect();
                        this.state = State.SwitchingTo0;
                    }
                }
                break;
            case State.SwitchingTo0:
                if(this.Links[0].Status() == MyShipConnectorStatus.Connected) {
                    switch(this.Links[1].Status()) {
                        case MyShipConnectorStatus.Connected:
                            Echo("Connected");
                            this.Links[1].Disconnect();
                            break;
                        default:
                            this.state = State.Holding0;
                            this.Arms[0].Reverse();
                            this.Arms[1].Reverse();
                            break;
                    }
                } else {
                    throw new Exception("Link 1 failed to connect");
                }
                break;
        }
    }

    Slider HoldingArm() {
        switch(this.state) {
            case State.Holding0:
            case State.SwitchingTo1:
                return this.Arms[0];
            case State.Holding1:
            case State.SwitchingTo0:
                return this.Arms[1];
            default:
                throw new Exception("Bad WeldingArm state: " + this.state);
        }
    }

    Slider NonHoldingArm() {
        switch(this.state) {
            case State.Holding0:
            case State.SwitchingTo1:
                return this.Arms[1];
            case State.Holding1:
            case State.SwitchingTo0:
                return this.Arms[0];
            default:
                throw new Exception("Bad WeldingArm state: " + this.state);
        }
    }

    float RemainingPistonLength() {
        var arm = this.HoldingArm();

        if(this.Speed > 0) {
            return arm.Max - arm.Pos;
        } else {
            return arm.Pos;
        }
    }

    public void Start() {
        this.Arms[0].Start();
        this.Arms[1].Start();
    }

    public void Stop() {
        this.Arms[0].Stop();
        this.Arms[1].Stop();
    }

    void Echo(string s) {
        this.Program.Echo("Welding Arm: " + s);
    }

    public void MoveTo(float pos, float speed) {
        Echo("MoveTo(" + pos + ", " + speed + ")");
        if(pos > this.Pos) {
            var holding_arm = this.HoldingArm();
            if(holding_arm.Pos == holding_arm.Max) {
                holding_arm.MoveTo(holding_arm.Max, speed);
                this.NonHoldingArm().MoveTo(0, speed);
                this.SetSpeed(speed);
                this.Run();
                return;
            }

            var missing = pos - this.Pos;
            var arm_pos = (float)Math.Min(holding_arm.Pos + missing, holding_arm.Max);
            // Echo("pos " + pos + " | this.Pos " + this.Pos + " | missing " + missing + " | arm_pos " + arm_pos + " | holding_arm.Max " + holding_arm.Max + " | holding_arm.Pos " + holding_arm.Pos);
            // Echo("holding_arm.MoveTo(" + arm_pos + ", " + speed + ")");
            holding_arm.MoveTo(arm_pos, speed);
            // Echo("non_holding_arm.MoveTo(" + (holding_arm.Max - arm_pos) + ", " + speed + ")");
            this.NonHoldingArm().MoveTo(holding_arm.Max - arm_pos, speed);
        } else if(pos < this.Pos) {
            var holding_arm = this.HoldingArm();
            if(holding_arm.Pos == holding_arm.Min) {
                holding_arm.MoveTo(0, speed);
                this.NonHoldingArm().MoveTo(holding_arm.Max, speed);
                this.SetSpeed(-speed);
                this.Run();
                return;
            }

            var missing = pos - this.Pos;
            var arm_pos = (float)Math.Max(holding_arm.Pos - missing, holding_arm.Min);
            holding_arm.MoveTo(arm_pos, speed);
            this.NonHoldingArm().MoveTo(holding_arm.Max - arm_pos, speed);
        }
    }

    public Vector3D WorldPosition() {
        return this.Arms[0].WorldPosition();
    }

    public Vector3D WorldDirection() {
        return this.Arms[0].WorldDirection();
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

public Arm BuildArmFromName(string name, IMyCubeBlock top_block) {
    var x_by_name = new Dictionary<string, List<Slider>>();
    var y_by_name = new Dictionary<string, List<Slider>>();
    var z_by_name = new Dictionary<string, List<Slider>>();
    var welding_x = new List<Slider>();
    var welding_y = new List<Slider>();
    var welding_z = new List<Slider>();

    var list = new List<IMyPistonBase>();
    this.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(list);
    foreach(var piston in list) {
        var piston_name_length = (name + " X+").Length;
        var piston_name = piston.CustomName;
        var group_name = "";
        if(piston.CustomName.Length > piston_name_length) {
            piston_name = piston.CustomName.Substring(0, piston_name_length);
            group_name = piston.CustomName.Substring(piston_name_length);
            // Echo("piston_name " + piston_name + " | " + name + " X+");
        }
        var welding_name_length = (name + " Welding X+").Length;
        var welding_name = piston.CustomName;
        if(piston.CustomName.Length > welding_name_length) {
            welding_name = piston.CustomName.Substring(0, welding_name_length);
        }
        Piston piston_obj;
        if(piston.CustomName.Contains('+')) {
            piston_obj = new Piston(this, piston, SliderDirection.Positive);
        } else {
            piston_obj = new Piston(this, piston, SliderDirection.Negative);
        }

        if(piston_name == name + " X+" || piston_name == name + " X-") {
            if(!x_by_name.ContainsKey(group_name)) {
                x_by_name.Add(group_name, new List<Slider>());
            }
            x_by_name[group_name].Add(piston_obj);
        } else if(piston_name == name + " Y+" || piston_name == name + " Y-") {
            if(!y_by_name.ContainsKey(group_name)) {
                y_by_name.Add(group_name, new List<Slider>());
            }
            y_by_name[group_name].Add(piston_obj);
        } else if(piston_name == name + " Z+" || piston_name == name + " Z-") {
            if(!z_by_name.ContainsKey(group_name)) {
                z_by_name.Add(group_name, new List<Slider>());
            }
            z_by_name[group_name].Add(piston_obj);
        } else if(welding_name == name + " Welding X+" || welding_name == name + " Welding X-") {
            welding_x.Add(piston_obj);
        } else if(welding_name == name + " Welding Y+" || welding_name == name + " Welding Y-") {
            welding_y.Add(piston_obj);
        } else if(welding_name == name + " Welding Z+" || welding_name == name + " Welding Z-") {
            welding_z.Add(piston_obj);
        }
    }

    // Build slider groups
    var x = new List<Slider>();
    var y = new List<Slider>();
    var z = new List<Slider>();
    foreach(var kv in x_by_name) {
        x.Add(new Sliders(this, kv.Value));
    }
    foreach(var kv in y_by_name) {
        y.Add(new Sliders(this, kv.Value));
    }
    foreach(var kv in z_by_name) {
        z.Add(new Sliders(this, kv.Value));
    }

    // Build Welding Sliders
    var axes_sliders = new List<List<Slider>>() {welding_x, welding_y, welding_z};
    var axes_name = new List<string>() {"X", "Y", "Z"};
    var axes_chain = new List<List<Slider>>() {x, y, z};
    for(var i = 0; i < axes_sliders.Count; i++) {
        var sliders = axes_sliders[i];
        var direction = axes_name[i];
        var chain = axes_chain[i];
        if(sliders.Count >= 2) {
            // Fetch connectors
            var links = new List<Connectors>();
            for(var j = 0; j < 2; j++) {
                var connectors = new List<IMyShipConnector>();
                var tmp = new List<IMyTerminalBlock>();
                this.GridTerminalSystem.SearchBlocksOfName(name + " Welding Connector " + direction + " " + j, tmp);
                foreach(var connector in tmp) {
                    connectors.Add((IMyShipConnector)connector);
                }
                links.Add(new Connectors(connectors.ToArray()));
            }

            // Refetch sliders
            var arms_sliders = new List<Slider>[]{ new List<Slider>(), new List<Slider>() };
            foreach(var slider in sliders) {
                if(slider.Name().Substring((name + " Welding " + direction).Length + 1) == " 0") {
                    arms_sliders[0].Add(slider);
                } else {
                    arms_sliders[1].Add(slider);
                }
            }
            var arms = new Sliders[] {
                new Sliders(this, arms_sliders[0]),
                new Sliders(this, arms_sliders[1]),
            };

            Echo("Found a Welding Arm!");
            chain.Add(new WeldingSlider(this, arms, links.ToArray(), top_block, arms[0].Direction, 0, 10000));
        }
    }

    var arm_x = new SliderChain(x, SliderDirection.Positive);
    var arm_y = new SliderChain(y, SliderDirection.Positive);
    var arm_z = new SliderChain(z, SliderDirection.Positive);

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
    public Miner(Program program, string name, List<IMyShipDrill> drills, float velocity, float step, float depth_step) {
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

    var miner = new Miner(this, name, drills, velocity, step, depth_step);
    if(miner.Arm.Empty()) {
        Echo(name + " has no arm. Not a miner.");
        return null;
    } else {
        return miner;
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
