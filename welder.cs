static string VectorToString(Vector3D v) {
    return "X=" + v.X.ToString("0.0") + " Y=" + v.Y.ToString("0.0") + " Z=" + v.Z.ToString("0.0");
}

public enum SliderDirection {
    Positive,
    Negative,
}

public interface Slider {
    float Pos {get;}
    float Min {get;}
    float Max {get;}
    float Speed {get;}

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
    bool IsStable();
}

public class ReverseSlider<T>: Slider where T : Slider {
    public float Pos {get;set;}
    public float Min {get;}
    public float Max {get;}
    public float Speed {get;set;}

    T Slider;

    public ReverseSlider(T slider) {
        this.Slider = slider;
        this.Min = this.Slider.Min;
        this.Max = this.Slider.Max;
    }

    float ReversedPos(float pos) {
        return this.Slider.Max - pos + this.Slider.Min;
    }

    public string Name() {
        return this.Slider.Name();
    }

    public void Refresh() {
        this.Slider.Refresh();
        this.Pos = this.ReversedPos(this.Slider.Pos);
        this.Speed = -this.Slider.Speed;
    }

    public bool Sync() {
        return this.Slider.Sync();
    }

    public void SetSpeed(float speed) {
        this.Slider.SetSpeed(-speed);
        this.Speed = speed;
    }

    public void Reverse() {
        this.Slider.Reverse();
        this.Speed = -this.Speed;
    }

    public void Start() {
        this.Slider.Start();
    }

    public void Stop() {
        this.Slider.Stop();
    }

    public void Run() {
        this.Slider.Run();
    }

    public void MoveTo(float pos, float speed) {
        this.Slider.MoveTo(this.ReversedPos(pos), speed);
    }

    public Vector3D WorldPosition() {
        return this.Slider.WorldPosition();
    }

    public Vector3D WorldDirection() {
        return -this.Slider.WorldDirection();
    }

    public string StateToString() {
        return "[Reversed] " + this.Slider.StateToString();
    }

    public bool IsStable() {
        return this.Slider.IsStable();
    }
}

public class Piston: Slider {
    Program Program;
    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public float Speed {get; set;}
    public IMyPistonBase piston;

    public Piston(Program program, IMyPistonBase piston) {
        this.Program = program;

        this.Min = 0;
        this.Max = piston.HighestPosition;
        this.Max = 10f;
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
        // this.Program.Echo(this.Name() + " Piston[" + this.Pos + "].MoveTo: " + pos + " | " + speed);
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

    public bool IsStable() {
        return true;
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
    public float Speed {get; set;}
    public List<Slider> List;
    bool Enabled;

    public Sliders(Program program, List<Slider> sliders) {
        this.Program = program;
        this.List = sliders;
        this.Min = 0;
        this.Max = (float)sliders[0].Max;

        var size = sliders[0].Max;
        foreach(var slider in sliders) {
            if(slider.Max != size) {
                throw new Exception("Grouped sliders of different size (" + slider.Max + " != " + size + ")!");
            }
        }

        this.Refresh();
    }

    public string Name() {
        return this.List[0].Name();
    }

    public void Refresh() {
        this.List[0].Refresh();
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
        // this.Echo("MoveTo[" + this.List.Count + "](" + pos + ", " + speed + ")");
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

    public bool IsStable() {
        foreach(var slider in this.List) {
            if(!slider.IsStable()) {
                return false;
            }
        }
        return true;
    }
}

public class SliderChain: Slider {
    Program Program;
    List<Slider> List;

    public float Pos {get; set;}
    public float Min {get;}
    public float Max {get;}
    public float Speed {get; set;}
    bool Enabled;
    Slider Last;

    public SliderChain(Program program, List<Slider> sliders) {
        this.Program = program;
        this.List = sliders;
        this.Min = 0.0f;
        this.Max = 0.0f;
        foreach(var slider in sliders) {
            this.Max += slider.Max;
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
        foreach(var slider in this.List) {
            slider.Refresh();
            this.Pos += slider.Pos;
        }
    }

    public void MoveTo(float pos, float speed) {
        var current_pos = this.Pos;
        float needed;
        if(pos > current_pos) {
            needed = pos - current_pos;
            this.Speed = speed;
            foreach(var slider in this.List) {
                if(slider.Pos < slider.Max) {
                    var goal = (float)Math.Min(slider.Max, slider.Pos + needed);
                    slider.MoveTo(goal, speed);
                    this.Last = slider;
                    return;
                }
            }
        } else if(current_pos > pos) {
            needed = current_pos - pos;
            this.Speed = -speed;
            foreach(var slider in this.List) {
                if(slider.Pos > slider.Min) {
                    var goal = (float)Math.Max(slider.Min, slider.Pos - needed);
                    slider.MoveTo(goal, speed);
                    this.Last = slider;
                    return;
                }
            }
        } else {
            return;
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
        foreach(var slider in this.List) {
            if(slider.Pos < slider.Max) {
                slider.SetSpeed(speed);
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
        if(this.List.Count > 0) {
            return this.List[0].WorldPosition();
        } else {
            return new Vector3D(0, 0, 0);
        }
    }

    public Vector3D WorldDirection() {
        if(this.List.Count > 0) {
            return this.List[0].WorldDirection();
        } else {
            return new Vector3D(0, 0, 0);
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

    void Echo(string s) {
        this.Program.Echo("SliderChain: " + s);
    }

    public bool IsStable() {
        foreach(var slider in this.List) {
            if(!slider.IsStable()) {
                return false;
            }
        }
        return true;
    }
}

const float CRAWL_MIN = 2.4f;
const float CRAWL_MAX = 9.8f;
const float CRAWL_RETRACT_SPEED = 0.5f;
const float CRAWL_GRIND_TIME = 2f;
const float CRAWL_SLOW_SPEED = 0.5f;
const float MERGE_BLOCK_MIN_DIST = 1.5f;

public class CrawlSlider: Slider {
    float LastPos;
    public float Pos {get;set;}
    public float Min {get;}
    public float Max {get;}

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
        MergeTopSlideUp,
        MergeTopSlideDown,
        BigSyncTopConnector,
        BigLockingTopConnector,
        UnlockingBottom,
        TranslatingSlider,
        Grind,
        MergeBottomSlideDown,
        MergeBottomSlideUp,
        LockingBottomConnector,
        RewindTopConnector,
        SmallSyncTopConnector,
        SmallLockingTopConnector,
        UnlockingBottomMergeBlock,
        RewindLoad,
    }
    State state;

    public CrawlSlider(Program program, string name, IMyCubeBlock origin, Slider slider, Connectors[] connectors, MergeBlocks[] merge_blocks) {
        this.Program = program;
        this.name = name;
        this.Origin = origin;
        this.Slider = slider;
        this.Connectors = connectors;
        this.MergeBlocks = merge_blocks;

        this.Pos = (float)Vector3D.Dot(this.Connectors[1].List[0].GetPosition() - this.Origin.GetPosition(), this.WorldDirection());
        this.LastPos = this.Pos;

        this.state = this.DetectState();
        this.Refresh();
        this.Min = 0;
        this.Max = 50000;
    }

    State DetectState() {
        if(this.MergeBlocks[0].IsConnected()) {
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
        switch(this.state) {
            case State.TranslatingLoad:
                this.Pos = (float)Vector3D.Dot(this.Connectors[1].List[0].GetPosition() - this.Origin.GetPosition(), this.WorldDirection());
                this.LastPos = this.Pos;
                break;
            default:
                // Make the exterior think we are immobile while transitionning.
                this.Pos = this.LastPos;
                break;
        }
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
        var slider_target = Math.Min(10f, pos - this.Pos + this.Slider.Pos);
        Echo("MoveTo: " + this.Pos + " => " + pos);
        Echo("Target " + slider_target);
        if(pos >= this.Pos) {
            Echo("State: " + this.state);
            this.Speed = speed;
            switch(this.state) {
                case State.TranslatingLoad:
                    if(this.Slider.Pos >= CRAWL_MAX) {
                        this.state = State.MergeTopSlideUp;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(false);
                        this.Slider.MoveTo(slider_target, speed);
                    }
                    break;
                case State.MergeTopSlideUp:
                    if(this.MergeBlocks[1].IsConnected()) {
                        this.state = State.BigSyncTopConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        var target = CRAWL_MAX - MERGE_BLOCK_MIN_DIST;
                        if(this.Slider.Pos > target) {
                            this.MergeBlocks[0].SetEnabled(true);
                            this.Connectors[0].Connect();
                            this.Connectors[1].Connect();
                            this.MergeBlocks[1].SetEnabled(true);

                            this.Slider.MoveTo(target - 0.05f, speed);
                        } else {
                            this.state = State.MergeTopSlideDown;
                            this.MoveTo(pos, CRAWL_SLOW_SPEED);
                        }
                    }
                    break;
                case State.MergeTopSlideDown:
                    if(this.MergeBlocks[1].IsConnected()) {
                        this.state = State.BigSyncTopConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        if(this.Slider.Pos < CRAWL_MAX) {
                            this.MergeBlocks[0].SetEnabled(true);
                            this.Connectors[0].Connect();
                            this.Connectors[1].Connect();
                            this.MergeBlocks[1].SetEnabled(true);

                            this.Slider.MoveTo(CRAWL_MAX + 0.05f, speed);
                        } else {
                            this.state = State.MergeTopSlideUp;
                            this.MoveTo(pos, CRAWL_SLOW_SPEED);
                        }
                    }
                    break;
                case State.BigSyncTopConnector:
                    if(this.Slider.Pos >= CRAWL_MAX) {
                        if(!this.Sync()) {
                            break;
                        }
                        this.state = State.BigLockingTopConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Disconnect();
                        this.MergeBlocks[1].SetEnabled(true);

                        this.Slider.MoveTo(CRAWL_MAX, CRAWL_SLOW_SPEED);
                    }
                    break;
                case State.BigLockingTopConnector:
                    if(this.Connectors[1].Status() == MyShipConnectorStatus.Connected) {
                        this.state = State.UnlockingBottom;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(true);
                    }
                    break;
                case State.UnlockingBottom:
                    if(this.Connectors[0].Status() != MyShipConnectorStatus.Connected && this.MergeBlocks[0].IsDisabled()) {
                        this.state = State.TranslatingSlider;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(false);
                        this.Connectors[0].Disconnect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(true);
                    }
                    break;
                case State.TranslatingSlider:
                    if(this.Slider.Pos <= CRAWL_MIN) {
                        this.state = State.Grind;
                        this.RemainingGrind = new TimeSpan(0, 0, 2);
                    } else {
                        this.MergeBlocks[0].SetEnabled(false);
                        this.Connectors[0].Disconnect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(true);

                        this.Slider.MoveTo(CRAWL_MIN - 0.1f, CRAWL_RETRACT_SPEED);
                    }
                    break;
                case State.Grind:
                    this.RemainingGrind -= this.Program.Runtime.TimeSinceLastRun;
                    if(this.RemainingGrind < TimeSpan.Zero) {
                        this.MergeBlocks[0].SetEnabled(false);
                        this.Connectors[0].Disconnect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(true);

                        this.state = State.MergeBottomSlideDown;
                        this.MoveTo(pos, speed);
                    }
                    break;
                case State.MergeBottomSlideDown:
                    if(this.MergeBlocks[0].IsConnected()) {
                        this.state = State.LockingBottomConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        var target = CRAWL_MIN + MERGE_BLOCK_MIN_DIST;
                        if(this.Slider.Pos < target) {
                            this.MergeBlocks[0].SetEnabled(true);
                            this.Connectors[0].Disconnect();
                            this.Connectors[1].Connect();
                            this.MergeBlocks[1].SetEnabled(true);

                            this.Slider.MoveTo(target + 0.05f, CRAWL_SLOW_SPEED);
                        } else {
                            this.state = State.MergeBottomSlideUp;
                            this.MoveTo(pos, speed);
                        }
                    }
                    break;
                case State.MergeBottomSlideUp:
                    if(this.MergeBlocks[0].IsConnected()) {
                        this.state = State.LockingBottomConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        var target = CRAWL_MIN;
                        if(this.Slider.Pos > target) {
                            this.MergeBlocks[0].SetEnabled(true);
                            this.Connectors[0].Disconnect();
                            this.Connectors[1].Connect();
                            this.MergeBlocks[1].SetEnabled(true);

                            this.Slider.MoveTo(target - 0.05f, CRAWL_SLOW_SPEED);
                        } else {
                            this.state = State.MergeBottomSlideDown;
                            this.MoveTo(pos, speed);
                        }
                    }
                    break;
                case State.LockingBottomConnector:
                    if(this.Connectors[1].Status() == MyShipConnectorStatus.Connected) {
                        this.state = State.RewindTopConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(true);
                    }
                    break;
                case State.RewindTopConnector:
                    if(this.Slider.Pos < CRAWL_MIN - 1f) {
                        if(!this.Sync()) {
                            break;
                        }
                        this.state = State.SmallSyncTopConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Disconnect();
                        this.MergeBlocks[1].SetEnabled(true);

                        this.Slider.MoveTo(CRAWL_MIN - 1.1f, CRAWL_SLOW_SPEED);
                    }
                    break;
                case State.SmallSyncTopConnector:
                    if(this.Slider.Pos >= CRAWL_MIN) {
                        if(!this.Sync()) {
                            break;
                        }
                        this.state = State.SmallLockingTopConnector;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Disconnect();
                        this.MergeBlocks[1].SetEnabled(true);

                        this.Slider.MoveTo(CRAWL_MIN, CRAWL_SLOW_SPEED);
                    }
                    break;
                case State.SmallLockingTopConnector:
                    if(this.Connectors[1].Status() == MyShipConnectorStatus.Connected) {
                        this.state = State.UnlockingBottomMergeBlock;
                        this.MoveTo(pos, speed);
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(true);
                    }
                    break;
                case State.UnlockingBottomMergeBlock:
                    if(this.MergeBlocks[1].IsDisabled()) {
                        this.state = State.RewindLoad;
                        this.MoveTo(pos, speed);
                    } else {
                        this.Slider.MoveTo(CRAWL_MIN, speed);

                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(false);
                    }
                    break;
                case State.RewindLoad:
                    if(this.Slider.Pos <= CRAWL_MIN - 0.1f) {
                        this.state = State.TranslatingLoad;
                    } else {
                        this.MergeBlocks[0].SetEnabled(true);
                        this.Connectors[0].Connect();
                        this.Connectors[1].Connect();
                        this.MergeBlocks[1].SetEnabled(false);

                        this.Slider.MoveTo(CRAWL_MIN - 0.1f, CRAWL_SLOW_SPEED);
                    }
                    break;
            }
        } else {
            // TODO
            switch(this.state) {
                case State.TranslatingLoad:
                    this.Slider.MoveTo(slider_target, speed);
                    break;
                default:
                    Echo("Ignoring negative speeds in state " + this.state);
                    break;
            }
            // throw new Exception("Negative speed not implemented (crawl slider)");
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
                return "Retract: " + this.state;
            } else {
                return "Immobile: " + this.state;
            }
        } else {
            return "Stopped: " + this.state;
        }
    }

    void Echo(string s) {
        this.Program.Echo("Crawl: " + s);
    }

    public bool IsStable() {
        return this.state == State.TranslatingLoad;
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
        Echo("MoveTo:  " + VectorToString(this.Pos));
        Echo("      => " + VectorToString(pos));
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

    public void Echo(string s) {
        this.Program.Echo("Arm: " + s);
    }

    public bool IsStable() {
        return this.X.IsStable() && this.Y.IsStable() && this.Z.IsStable();
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
    var names = new HashSet<string>();
    var all_sliders = new List<Slider>();
    foreach(var entry in sliders_dict) {
        foreach(var slider in entry.Value) {
            var sub_name = slider.Name().Substring(base_name.Length);
            names.Add(sub_name);
            all_sliders.Add(slider);
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
            var sliders = new Sliders(this, kv.Value);
            ret.Add(sliders);
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
                    if(piston.Top == null) {
                        throw new Exception("Piston " + piston.CustomName + " is broken!");
                    }
                    var piston_front = Vector3D.Normalize(piston.Top.GetPosition() - piston.GetPosition());
                    SliderDirection direction;
                    Dictionary<IMyCubeGrid, List<Slider>> per_grid;
                    var type_piston = pistons[i_type];
                    Axis axis;
                    if(Vector3D.Dot(z_axis, piston_front) > 0.9) {
                        axis = Axis.Z;
                        direction = SliderDirection.Positive;
                    } else if(Vector3D.Dot(z_axis, piston_front) < -0.9) {
                        axis = Axis.Z;
                        direction = SliderDirection.Negative;
                    } else if(Vector3D.Dot(y_axis, piston_front) > 0.9) {
                        axis = Axis.Y;
                        direction = SliderDirection.Positive;
                    } else if(Vector3D.Dot(y_axis, piston_front) < -0.9) {
                        axis = Axis.Y;
                        direction = SliderDirection.Negative;
                    } else if(Vector3D.Dot(x_axis, piston_front) > 0.9) {
                        axis = Axis.X;
                        direction = SliderDirection.Positive;
                    } else if(Vector3D.Dot(x_axis, piston_front) < -0.9) {
                        axis = Axis.X;
                        direction = SliderDirection.Negative;
                    } else {
                        throw new Exception("Burp");
                    }
                    // Echo(type + " " + axis + " " + direction);
                    per_grid = type_piston[(int)axis];
                    if(!per_grid.ContainsKey(cube_grid)) {
                        per_grid.Add(cube_grid, new List<Slider>());
                    }
                    if(direction == SliderDirection.Positive) {
                        var slider = new Piston(this, piston);
                        per_grid[cube_grid].Insert(0, slider);
                    } else {
                        var slider = new ReverseSlider<Piston>(new Piston(this, piston));
                        per_grid[cube_grid].Add(slider);
                    }
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
        // TODO: This is a hack. We should check is the sliders are related by either their bottom or top part (each of them could be in a different state).
        if(crawl_sliders.Count > 0) {
            var parallel_crawl_sliders = new Sliders(this, crawl_sliders);
            sliders[i].Add(parallel_crawl_sliders);
        }
    }

    var arm_x = new SliderChain(this, sliders[(int)Axis.X]);
    var arm_y = new SliderChain(this, sliders[(int)Axis.Y]);
    var arm_z = new SliderChain(this, sliders[(int)Axis.Z]);

    return new Arm(this, arm_x, arm_y, arm_z);
}

public const int EXTEND_X = 0;
public const int EXTEND_Y = 1;
public const int EXTEND_Z = 2;
public const int RETRACT_X = 3;
public const int RETRACT_Y = 4;
public const int RETRACT_Z = 5;
public const int CENTERING = 6;

public class GridItemBuffer {
    public class ItemRequirement {
        public MyItemType Type;
        public float[] Thresholds;
        public ItemRequirement(MyItemType type, float[] thresholds) {
            this.Type = type;
            this.Thresholds = thresholds;
        }

        public uint GetState(IMyInventory[] inventories) {
            uint state = 0;
            uint max_state = (uint)this.Thresholds.Length;
            if(state == max_state) {
                return state;
            }

            float total = 0.0f;
            foreach(var inventory in inventories) {
                total += (float)inventory.GetItemAmount(this.Type);
                while(total >= this.Thresholds[state]) {
                    state++;
                    if(state == max_state) {
                        return state;
                    }
                }
            }
            return state;
        }
    }

    public class RequirementState {
        public MyItemType Type;
        public uint State;

        public RequirementState(MyItemType type, uint state) {
            this.Type = type;
            this.State = state;
        }
    }

    public class BufferState {
        RequirementState[] States;

        public BufferState(RequirementState[] states) {
            this.States = states;
        }

        public MyItemType[] ItemsWithStateBelowOrEqualTo(uint limit_state) {
            var ret = new List<MyItemType>();
            foreach(var req in this.States) {
                if(req.State <= limit_state) {
                    ret.Add(req.Type);
                }
            }
            return ret.ToArray();
        }
    }

    Program Program;
    ItemRequirement[] Requirements;
    IMyInventory Destination;

    public GridItemBuffer(Program program, ItemRequirement[] requirements, IMyInventory destination) {
        this.Program = program;
        this.Requirements = requirements;
        this.Destination = destination;
    }

    public BufferState GetState() {
        // TODO Cache result to calculate it at most once per program tick
        var list = new List<IMyTerminalBlock>();
        this.Program.GridTerminalSystem.GetBlocks(list);
        var inventories_list = new List<IMyInventory>();
        foreach(var block in list) {
            for(var i = 0; i < block.InventoryCount; i++) {
                var inv = block.GetInventory(i);
                if(inv.IsConnectedTo(this.Destination)) {
                    inventories_list.Add(inv);
                }
            }
        }

        var inventories = inventories_list.ToArray();
        var req_states = new List<RequirementState>();
        foreach(var req in this.Requirements) {
            var state = req.GetState(inventories);
            req_states.Add(new RequirementState(req.Type, state));
        }

        return new BufferState(req_states.ToArray());
    }
}

static Vector3D VectorInGrid(Vector3D v, IMyCubeGrid grid) {
    var x = Vector3D.Dot(v, grid.WorldMatrix.Up);
    var y = Vector3D.Dot(v, grid.WorldMatrix.Right);
    var z = Vector3D.Dot(v, grid.WorldMatrix.Forward);
    return new Vector3D(x, y, z);
}

public class WeldingPrinter {
    public Arm Arm;
    public Vector3D Dst;
    public Vector3I PosI;
    public Vector3I MaxI;
    public List<IMyShipWelder> Welders;
    Program Program;
    int last_move;
    Vector3D Velocity;
    float WeldRadius;
    float Step;
    float DepthStep;
    string Name;
    bool Welding;
    public MyItemType[] MissingItems;
    public bool Damaged;
    IMySoundBlock[] Sounds;
    TimeSpan RemainingSound;
    HashSet<string> Blacklist;

    GridItemBuffer GridBuffer;
    const uint INVENTORY_STATE_STOP = 0;
    const uint INVENTORY_STATE_CONTINUE = 1;
    const uint INVENTORY_STATE_START = 2;

    TimeSpan RemainingWeldTime = TimeSpan.Zero;
    const int PHANTOM_MIN_WELD_TIME = 2;
    int NbPhantomBlocks = 0;
    TimeSpan? RemainingBlockPosTime;
    const int BLOCK_POSE_TIME = 1;

    public const float WELDER_Z_SHIFT = 0.5f;
    public const float VELOCITY = 3f;

    public const float NATURAL_WELDING_SHIFT = 0f;

    public WeldingPrinter(Program program, string name, List<IMyShipWelder> welders, GridItemBuffer grid_buffer, bool big_grid_target, HashSet<string> blacklist) {
        this.Program = program;
        this.Name = name;
        this.Arm = program.BuildArmFromName(name, welders[0]);
        this.Arm.Z = new ReverseSlider<Slider>(this.Arm.Z);
        this.Welders = welders;
        this.Velocity = new Vector3D(VELOCITY, VELOCITY, VELOCITY);

        var grid = welders[0].CubeGrid;
        if(grid.GridSizeEnum == MyCubeSize.Large) {
            this.WeldRadius = 4.0f*0.5f;
        } else {
            this.WeldRadius = 3.0f*0.5f;
        }
        var excentricity = WELDER_Z_SHIFT + NATURAL_WELDING_SHIFT;
        this.WeldRadius = (float)Math.Sqrt(this.WeldRadius*this.WeldRadius - excentricity*excentricity);
        if(big_grid_target) {
            this.Step = 2.0f * this.WeldRadius;
        } else {
            this.Step = (float)Math.Sqrt(2.0f) * this.WeldRadius;
        }
        this.DepthStep = grid.GridSize;

        this.last_move = 0;
        this.MaxI.X = (int)((this.Arm.Max.X + this.Step - 1.0) / this.Step);
        this.MaxI.Y = (int)((this.Arm.Max.Y + this.Step - 1.0) / this.Step);
        this.MaxI.Z = (int)((this.Arm.Max.Z + this.DepthStep - 1.0) / this.DepthStep);
        this.GridBuffer = grid_buffer;
        this.Welding = false;
        this.Damaged = false;
        this.Blacklist = blacklist;

        this.Refresh();
        this.BuildPosI();

        var list = new List<IMySoundBlock>();
        this.Program.GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(list);
        var sounds = new List<IMySoundBlock>();
        foreach(var sound in list) {
            if(sound.CustomName == name) {
                sounds.Add(sound);
            }
        }
        this.Sounds = sounds.ToArray();
    }

    void RefreshDamaged() {
        this.Damaged = false;
        var first_inv = this.Welders[0].GetInventory();
        foreach(var welder in this.Welders) {
            var slim = welder.CubeGrid.GetCubeBlock(welder.Position);
            var connected = welder.GetInventory().IsConnectedTo(first_inv);
            if(slim.CurrentDamage > 0 || !connected) {
                Echo("Welder: damage: " + slim.CurrentDamage + "; connected: " + connected);
                this.Damaged = true;
                break;
            }
        }
    }

    public Vector3I InitPos(int z) {
        int x;
        int y;
        bool y_extend = (z % 2 == 0);
        bool x_extend;
        if(y_extend) {
            y = 0;
            x_extend = (y % 2 == 0);
        } else {
            y = this.MaxI.Y;
            x_extend = !(y % 2 == 0);
        }
        if(x_extend) {
            x = 0;
        } else {
            x = this.MaxI.X;
        }
        return new Vector3I(x, y, z);
    }

    public Vector3D IntPosToFloat(Vector3I pos_i) {
        double x = Math.Min((float)pos_i.X * this.Step + 0.1f, this.Arm.Max.X);
        double y = Math.Min((float)pos_i.Y * this.Step + 0.1f, this.Arm.Max.Y);
        double z = Math.Min((float)pos_i.Z * this.DepthStep + WELDER_Z_SHIFT, this.Arm.Max.Z);
        return new Vector3D(x, y, z);
    }

    void BuildPosI() {
        int z = (int)((this.Arm.Pos.Z - WELDER_Z_SHIFT) / this.DepthStep);

        this.PosI = this.InitPos(z);
        this.RefreshDst();
    }

    void RefreshDst() {
        this.Dst = this.IntPosToFloat(this.PosI);
    }

    public void Refresh() {
        this.RefreshDamaged();
        this.Arm.Refresh();
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

    public bool TargetIsComplete() {
        var grid = this.Program.Me.CubeGrid;
        var block_size = grid.GridSize;
        foreach(var welder in this.Welders) {
            var head_position = welder.CubeGrid.GridIntegerToWorld(welder.Position);
            var head_perfect_position = head_position;
            var front_position = head_perfect_position + (2 * block_size + WELDER_Z_SHIFT) * welder.WorldMatrix.Forward;

            var shift = this.WeldRadius;
            Echo("Welding radius: " + shift);
            var sq_2 = (float)Math.Sqrt(2.0)/2.0;
            var target_list = new Vector3D[] {
                front_position,
                front_position + shift * welder.WorldMatrix.Up,
                front_position - shift * welder.WorldMatrix.Up,
                front_position + shift * welder.WorldMatrix.Left,
                front_position - shift * welder.WorldMatrix.Left,
            };
            Vector3I min = grid.WorldToGridInteger(front_position);
            Vector3I max = grid.WorldToGridInteger(front_position);
            foreach(var pos in target_list) {
                var pos_i = grid.WorldToGridInteger(pos);
                if(pos_i.X < min.X) {
                    min.X = pos_i.X;
                }
                if(pos_i.Y < min.Y) {
                    min.Y = pos_i.Y;
                }
                if(pos_i.Z < min.Z) {
                    min.Z = pos_i.Z;
                }
                if(pos_i.X > max.X) {
                    max.X = pos_i.X;
                }
                if(pos_i.Y > max.Y) {
                    max.Y = pos_i.Y;
                }
                if(pos_i.Z > max.Z) {
                    max.Z = pos_i.Z;
                }
            }

            var blocks = new List<IMySlimBlock>();
            var phantom_blocks = new HashSet<Vector3D>();
            var nb_incomplete = new HashSet<Vector3D>();
            for(var x = min.X; x < max.X + 1; x++) {
                for(var y = min.Y; y < max.Y + 1; y++) {
                    for(var z = min.Z; z < max.Z + 1; z++) {
                        var pos_i = new Vector3I(x, y, z);
                        var slim = grid.GetCubeBlock(pos_i);
                        if(slim != null) {
                            if(!slim.IsFullIntegrity && !this.Blacklist.Contains(slim.BlockDefinition.SubtypeName)) {
                                nb_incomplete.Add(pos_i);
                            }
                        } else if(grid.CubeExists(pos_i)) {
                            phantom_blocks.Add(pos_i);
                        }
                    }
                }
            }
            if(nb_incomplete.Count > 0) {
                Echo("Incomplete: " + nb_incomplete.Count + " blocks");
                return false;
            }
            int detected_phantom_blocks = phantom_blocks.Count;
            if(detected_phantom_blocks > this.NbPhantomBlocks) {
                int nb_new_blocks = detected_phantom_blocks - this.NbPhantomBlocks;
                this.RemainingWeldTime += new TimeSpan(0, 0, PHANTOM_MIN_WELD_TIME*nb_new_blocks);
                this.NbPhantomBlocks = detected_phantom_blocks;
                return false;
            } else if(detected_phantom_blocks > 0) {
                this.RemainingWeldTime -= this.Program.Runtime.TimeSinceLastRun;
                if(this.RemainingWeldTime > TimeSpan.Zero) {
                    return false;
                }
            }
        }
        return true;
    }

    public void Run() {
        if(this.Damaged) {
            if(this.Welding) {
                this.Stop();
            }
            return;
        }

        var inventory_state = this.GridBuffer.GetState();
        if(this.Welding) {
            this.MissingItems = inventory_state.ItemsWithStateBelowOrEqualTo(INVENTORY_STATE_STOP);
            if(this.MissingItems.Length > 0) {
                this.Stop();
                return;
            }
        } else {
            this.MissingItems = inventory_state.ItemsWithStateBelowOrEqualTo(INVENTORY_STATE_CONTINUE);
            if(this.MissingItems.Length == 0) {
                this.Start();
            } else {
                return;
            }
        }

        if(Vector3D.Distance(this.Arm.Pos, this.Dst) < 0.2) {
            this.SetWelders(true);
            if(this.TargetIsComplete()) {
                if(this.RemainingBlockPosTime == null) {
                    this.RemainingBlockPosTime = new TimeSpan(0, 0, BLOCK_POSE_TIME);
                } else {
                    this.RemainingBlockPosTime -= this.Program.Runtime.TimeSinceLastRun;
                    if(this.RemainingBlockPosTime < TimeSpan.Zero) {
                        var move = this.SelectMove();

                        this.last_move = move;

                        switch(move) {
                            case EXTEND_X:
                                this.PosI.X += 1;
                                break;
                            case EXTEND_Y:
                                this.PosI.Y += 1;
                                break;
                            case EXTEND_Z:
                                this.PosI.Z += 1;
                                break;
                            case RETRACT_X:
                                this.PosI.X -= 1;
                                break;
                            case RETRACT_Y:
                                this.PosI.Y -= 1;
                                break;
                            case RETRACT_Z:
                                this.PosI.Z -= 1;
                                break;
                            default:
                                break;
                        }
                        this.RefreshDst();
                        this.RemainingBlockPosTime = null;
                        this.NbPhantomBlocks = 0;
                        this.SetWelders(false);
                    }
                }
            } else {
                this.RemainingBlockPosTime = null;
            }
        }
        this.Arm.MoveTo(this.Dst, this.Velocity);
    }

    public void SetWelders(bool enabled) {
        foreach(var welder in this.Welders) {
            welder.Enabled = enabled;
        }
    }

    public void Start() {
        this.Arm.Start();
        this.Welding = true;
    }

    public void Stop() {
        this.Arm.Stop();

        this.SetWelders(false);
        this.Welding = false;
        this.NbPhantomBlocks = 0;
        this.RemainingBlockPosTime = null;
    }

    public void StartSound(string name, int duration) {
        if(this.RemainingSound <= TimeSpan.Zero) {
            foreach(var sound in this.Sounds) {
                sound.Enabled = true;
                sound.SelectedSound = name;
                sound.LoopPeriod = duration;
                sound.Range = 500;
                sound.Play();
            }
            this.RemainingSound = new TimeSpan(0, 0, duration);
        }
    }

    public void StopSound() {
        foreach(var sound in this.Sounds) {
            sound.Enabled = false;
            sound.Stop();
        }
        this.RemainingSound = new TimeSpan(0, 0, 0);
    }

    public void Print() {
        Echo("X: " + this.Arm.X.Pos.ToString("0.0") + "/" + this.Arm.X.Max + " | I: " + this.PosI.X + "/" + this.MaxI.X);
        Echo("Y: " + this.Arm.Y.Pos.ToString("0.0") + "/" + this.Arm.Y.Max + " | I: " + this.PosI.Y + "/" + this.MaxI.Y);
        Echo("Z: " + this.Arm.Z.Pos.ToString("0.0") + "/" + this.Arm.Z.Max + " | I: " + this.PosI.Z + "/" + this.MaxI.Z);
        if(this.MissingItems.Length > 0) {
            this.RemainingSound -= this.Program.Runtime.TimeSinceLastRun;
            this.StartSound("Alert 1", 30*60);
        } else {
            this.StopSound();
        }
        if(this.Damaged) {
            Echo("Damaged!");
        } else if(this.MissingItems.Length > 0) {
            foreach(var missing in this.MissingItems) {
                Echo("Missing " + missing.SubtypeId);
            }
        } else if(this.Welding) {
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
            Echo("Stopped welding.");
        }
    }

    public void Echo(string s) {
        this.Program.Echo(this.Name + ": " + s);
    }
}

public WeldingPrinter GetWeldingPrinter(string name, bool big_grid_target) {
    var list = new List<IMyShipWelder>();
    GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(list);
    var welders = new List<IMyShipWelder>();
    foreach(var welder in list) {
        if(welder.CustomName == name) {
            welders.Add(welder);
        }
    }
    if(welders.Count == 0) {
        // Echo("Could not find any drill for " + name);
        return null;
    }

    var component_names = new string[] {
        "SteelPlate",
        "InteriorPlate",
        "Construction",
        "Motor",
        "MetalGrid",
        "LargeTube",
        "SmallTube",
        "Display",
        "Computer",
        // "Medical",
        "SolarCell",
        "PowerCell",
        "Detector",
        "Girder",
        // "Thrust",
        "Reactor",
        "BulletproofGlass",
        "RadioCommunication",
        // "GravityGenerator",
        // "Superconductor",
    };
    var requirements = component_names
        .Select((c) => MyItemType.MakeComponent(c))
        .Select((t) => new GridItemBuffer.ItemRequirement(t, new float[]{25, 50}));
    var grid_buffer = new GridItemBuffer(this, requirements.ToArray(), welders[0].GetInventory());

    var blacklist = new HashSet<string>();
    blacklist.Add("LargeBlockGravityGenerator");
    blacklist.Add("LargeBlockGravityGeneratorSphere");
    blacklist.Add("LargeBlockJumpDrive");
    blacklist.Add("LargeBlockLargeThrust");
    blacklist.Add("LargeBlockSmallThrust");

    var printer = new WeldingPrinter(this, name, welders, grid_buffer, big_grid_target, blacklist);
    if(printer.Arm.Empty()) {
        Echo(name + " has no arm. Not a printer.");
        return null;
    } else {
        return printer;
    }
}

public double Progress() {
    var printer = this.printer;
    var tot = printer.Arm.Max.X * printer.Arm.Max.Y * printer.Arm.Max.Z;
    var consumed = printer.Arm.Pos.Z * printer.Arm.Max.X * printer.Arm.Max.Y;
    return consumed/tot;
}

public void UpdateProgressScreen() {
    var progress = Progress() * 100;
    IMyTextSurface surface = Me.GetSurface(0);
    var lines = new List<string>() {
        "Progress: " + progress.ToString("0.000") + "%",
    };
    var printer = this.printer;
    lines.Add("X: " + printer.Arm.X.Pos.ToString("0.0") + "/" + printer.Arm.X.Max);
    lines.Add("Y: " + printer.Arm.Y.Pos.ToString("0.0") + "/" + printer.Arm.Y.Max);
    lines.Add("Z: " + printer.Arm.Z.Pos.ToString("0.0") + "/" + printer.Arm.Z.Max);
    if(printer.MissingItems.Length > 0) {
        var type = printer.MissingItems[0];
        lines.Add("Missing " + type.SubtypeId);
    }
    var missing_text = String.Join("\n", lines.ToArray());
    surface.WriteText(missing_text);
    Echo(missing_text);
}

Dictionary<string, string> ParseConf() {
    var ret = new Dictionary<string, string>();
    foreach(string line in Me.CustomData.Split('\n')) {
        if(line.Length > 0) {
            var parts = line.Split('=').ToList();
            if(parts.Count != 2) {
                Echo("Bad configuration line: " + line);
                return null;
            }
            ret.Add(parts[0], parts[1]);
        }
    }
    return ret;
}

bool Init() {
    var conf = this.ParseConf();
    if(conf == null) {
        return false;
    }
    if(!conf.ContainsKey("name")) {
        Echo("Configuration missing 'name' field.");
        return false;
    }
    this.printer = GetWeldingPrinter(conf["name"], !conf.ContainsKey("SmallGrid") || conf["SmallGrid"] != "true");
    return true;
}

void PrintSyntax() {
    Echo("Syntax: <command>");
    Echo("  command:");
    Echo("    - 'start'");
    Echo("    - 'stop'");
    Echo("    - 'reset'");
    Echo("    - 'init' (only if welder blocks changed)");
}

enum Command {
    None,
    Run,
    Reset,
    Scan,
}

Command CurrentCommand;
WeldingPrinter printer;
public Program() {
    IMyTextSurface surface = Me.GetSurface(0);
    surface.ContentType = ContentType.TEXT_AND_IMAGE;
    surface.FontSize = 2;
    surface.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
    this.CurrentCommand = Command.None;

    // Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

public void StopProgram() {
    Runtime.UpdateFrequency = UpdateFrequency.None;
}

public void Main(string argument) {
    if(this.printer == null) {
        if(!this.Init()) {
            return;
        }
    }

    var args = argument.Split(' ').ToList();
    if (args.Count >= 1) {
        var cmd = args[0];
        Echo("Command: " + cmd);
        switch(cmd) {
            case "":
                break;
            case "start":
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                this.CurrentCommand = Command.Run;
                break;
            case "scan":
                this.printer.Stop();
                this.printer.Arm.Start();
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                this.CurrentCommand = Command.Scan;
                break;
            case "stop":
                this.printer.Stop();
                StopProgram();
                Me.GetSurface(0).WriteText("Manually stopped.");
                return;
            case "reset":
                this.printer.Stop();
                this.printer.Arm.Start();
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                Me.GetSurface(0).WriteText("Resetting...");
                this.CurrentCommand = Command.Reset;
                break;
            case "init":
                this.Init();
                StopProgram();
                Me.GetSurface(0).WriteText("Reinitialized.");
                return;
            case "help":
                PrintSyntax();
                return;
            default:
                Echo("Unknown command '" + cmd + "'");
                PrintSyntax();
                Me.GetSurface(0).WriteText("Unknown command.");
                return;
        }
    } else {
        Echo("Missing arguments");
        PrintSyntax();
        StopProgram();
        return;
    }

    if(this.printer == null) {
        return;
    }

    switch(this.CurrentCommand) {
        case Command.Run:
            this.printer.Refresh();
            this.printer.Run();
            this.printer.Print();

            UpdateProgressScreen();
            break;
        case Command.Reset:
            this.printer.Arm.Refresh();
            var pos = this.printer.Arm.Pos;
            var target = new Vector3D(pos.X, pos.Y, pos.Z);
            target.X = 0;
            if(pos.X <= 0.1) {
                target.Y = 0;
                if(pos.Y <= 0.1) {
                    target.Z = WeldingPrinter.WELDER_Z_SHIFT;
                    if(Math.Abs(pos.Z - WeldingPrinter.WELDER_Z_SHIFT) < 0.1) {
                        StopProgram();
                        Me.GetSurface(0).WriteText("Resetting Done");
                        this.Init();
                    }
                }
            }
            this.printer.Arm.MoveTo(target, new Vector3D(5, 5, 5));
            break;
        case Command.Scan:
            this.printer.Refresh();
            this.printer.Print();
            if(this.printer.TargetIsComplete()) {
                this.printer.StopSound();
            } else {
                this.printer.StartSound("Alert 1", 30*60);
            }
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            var grid = Me.CubeGrid;
            foreach(var block in blocks) {
                if(!block.IsFunctional) {
                    Echo("Incomplete block: " + VectorToString(VectorInGrid(block.GetPosition(), grid)));
                }
            }
            Me.GetSurface(0).WriteText("Manual scanning.");
            break;
        case Command.None:
            break;
        default:
            break;
    }
}
