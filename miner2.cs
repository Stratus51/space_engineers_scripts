static MiningArmsFinder miningArmsFinder;
static MiningArm[] mining_arms;
static bool running = false;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    miningArmsFinder = new MiningArmsFinder(this);
}

public void Save() {
}

public void Main(string argument, UpdateType updateSource) {
    string[] args = argument.Split(' ');

    if (args.Length == 0 && (running || mining_arms == null)) {
        Echo("Running");
        Run();
        return;
    }

    string cmd = args[0];
    Echo("Command: " + cmd);
    if (cmd == "reset") {
        foreach(MiningArm arm in mining_arms) {
            arm.Reset();
        }
        Runtime.UpdateFrequency = UpdateFrequency.None;
        running = false;
        return;
    } else if (cmd == "run") {
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
        running = true;
        return;
    } else if (cmd == "stop") {
        Runtime.UpdateFrequency = UpdateFrequency.None;
        running = false;
        return;
    } else if (cmd == "" && (running || mining_arms == null)) {
        Echo("Running");
        Run();
        return;
    }
}

public void Run() {
    if (mining_arms == null) {
        mining_arms = miningArmsFinder.Continue();
        if (mining_arms != null) {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Echo("Found " + mining_arms.Length + " mining arms");
        }
        return;
    }
    foreach(MiningArm arm in mining_arms) {
        arm.Run();
    }
}

const float VERTICAL_STEP_SIZE = 1.0f;
const float LATERAL_STEP_SIZE = 2.0f;
const float SPEED_NORMAL = 0.3f;
const float SPEED_FAST = 2f;

class PistonArmAxis {
    public IMyPistonBase[] PositivePistons;
    public IMyPistonBase[] NegativePistons;
    public float HighestPosition;
    Program Program;

    public PistonArmAxis(Program program, IMyPistonBase[] positivePistons, IMyPistonBase[] negativePistons) {
        Program = program;
        PositivePistons = positivePistons;
        NegativePistons = negativePistons;

        float position = 0;
        foreach(IMyPistonBase piston in PositivePistons) {
            position += piston.HighestPosition;
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            position += piston.HighestPosition;
        }
        HighestPosition = position;
    }

    public float CurrentPosition() {
        float position = 0;
        foreach(IMyPistonBase piston in PositivePistons) {
            position += piston.CurrentPosition;
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            position += piston.HighestPosition - piston.CurrentPosition;
        }
        return position;
    }

    public int NbSteps(float step_size) {
        int count = 0;
        foreach(IMyPistonBase piston in PositivePistons) {
            float progression = piston.CurrentPosition / step_size;
            if (piston.Velocity > 0) {
                count += (int)Math.Floor(progression);
            } else {
                count += (int)Math.Ceiling(progression);
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            float progression = (piston.HighestPosition - piston.CurrentPosition) / step_size;
            if (piston.Velocity < 0) {
                count += (int)Math.Floor(progression);
            } else {
                count += (int)Math.Ceiling(progression);
            }
        }
        return count;
    }

    public bool IsMaxed() {
        foreach(IMyPistonBase piston in PositivePistons) {
            if (piston.CurrentPosition < piston.HighestPosition) {
                return false;
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            if (piston.CurrentPosition > piston.LowestPosition) {
                return false;
            }
        }
        return true;
    }

    public bool IsMined() {
        foreach(IMyPistonBase piston in PositivePistons) {
            if (piston.CurrentPosition > piston.LowestPosition) {
                return false;
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            if (piston.CurrentPosition < piston.HighestPosition) {
                return false;
            }
        }
        return true;
    }

    public void Inc(float speed, float step_size) {
        foreach(IMyPistonBase piston in PositivePistons) {
            if(piston.CurrentPosition < piston.HighestPosition) {
                piston.Velocity = speed;
                float nb_steps = (float)Math.Floor(piston.CurrentPosition / step_size);
                float nextPos = (nb_steps + 1) * step_size;
                piston.MaxLimit = nextPos;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + nextPos);
                return;
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            if(piston.CurrentPosition > piston.LowestPosition) {
                piston.Velocity = -speed;
                float progression = piston.HighestPosition - piston.CurrentPosition;
                float nb_steps = (float)Math.Floor(progression / step_size);
                float nextPos = piston.HighestPosition - (nb_steps + 1) * step_size;
                piston.MinLimit = nextPos;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + nextPos);
                return;
            }
        }
    }

    public void Dec(float speed, float step_size) {
        foreach(IMyPistonBase piston in PositivePistons) {
            if(piston.CurrentPosition > piston.LowestPosition) {
                piston.Velocity = -speed;
                float progression = piston.HighestPosition - piston.CurrentPosition;
                float nb_steps = (float)Math.Floor(progression / step_size);
                float nextPos = piston.HighestPosition - (nb_steps + 1) * step_size;
                piston.MinLimit = nextPos;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + nextPos);
                return;
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            if(piston.CurrentPosition < piston.HighestPosition) {
                piston.Velocity = speed;
                float nb_steps = (float)Math.Floor(piston.CurrentPosition / step_size);
                float nextPos = (nb_steps + 1) * step_size;
                piston.MaxLimit = nextPos;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + nextPos);
                return;
            }
        }
    }

    public void IncToMax(float speed) {
        foreach(IMyPistonBase piston in PositivePistons) {
            if(piston.CurrentPosition < piston.HighestPosition) {
                piston.Velocity = speed;
                piston.MaxLimit = piston.HighestPosition;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + piston.HighestPosition);
                return;
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            if(piston.CurrentPosition > piston.LowestPosition) {
                piston.Velocity = -speed;
                piston.MinLimit = piston.LowestPosition;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + piston.LowestPosition);
                return;
            }
        }
    }

    public void DecToMin(float speed) {
        foreach(IMyPistonBase piston in PositivePistons) {
            if(piston.CurrentPosition > piston.LowestPosition) {
                piston.Velocity = -speed;
                piston.MinLimit = piston.LowestPosition;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + piston.LowestPosition);
                return;
            }
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            if(piston.CurrentPosition < piston.HighestPosition) {
                piston.Velocity = speed;
                piston.MaxLimit = piston.HighestPosition;
                // this.Program.Echo("Moving " + piston.CustomName + " to " + piston.HighestPosition);
                return;
            }
        }
    }

    public void Stop() {
        foreach(IMyPistonBase piston in PositivePistons) {
            piston.Velocity = 0;
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            piston.Velocity = 0;
        }
    }

    public void Reset() {
        foreach(IMyPistonBase piston in PositivePistons) {
            piston.Velocity = -1f;
            piston.MinLimit = 0.0f;
        }
        foreach(IMyPistonBase piston in NegativePistons) {
            piston.Velocity = 1f;
            piston.MaxLimit = piston.HighestPosition;
        }
    }
}

class PistonArm {
    Program Program;
    public PistonArmAxis[] Axes;
    public float HighestPosition;

    public PistonArm(Program program, PistonArmAxis[] axes) {
        Program = program;
        Axes = axes;
        HighestPosition = axes[0].HighestPosition * axes[1].HighestPosition * axes[2].HighestPosition;
    }

    public static PistonArm FromGridBranch(Program program, GridBranch branch, MatrixD toolMatrix) {
        List<IMyPistonBase> all_pistons = new List<IMyPistonBase>();
        program.GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(all_pistons);
        // program.Echo("Found " + all_pistons.Count + " pistons");

        Dictionary<long, IMyPistonBase> pistons_by_id = new Dictionary<long, IMyPistonBase>();
        foreach(IMyPistonBase piston in all_pistons) {
            pistons_by_id[piston.EntityId] = piston;
        }

        List<IMyPistonBase> pistons = new List<IMyPistonBase>();
        foreach(IMyMechanicalConnectionBlock joint in branch.Joints) {
            IMyPistonBase piston = joint as IMyPistonBase;
            if (piston != null) {
                pistons.Add(piston);
            }
        }
        // program.Echo("Found " + pistons.Count + " relevant pistons");

        List<IMyPistonBase>[] positive_pistons = new List<IMyPistonBase>[3];
        List<IMyPistonBase>[] negative_pistons = new List<IMyPistonBase>[3];
        for (int i = 0; i < 3; i++) {
            positive_pistons[i] = new List<IMyPistonBase>();
            negative_pistons[i] = new List<IMyPistonBase>();
        }

        foreach(IMyPistonBase piston in pistons) {
            Vector3D dir_vec = Vector3D.TransformNormal(piston.WorldMatrix.Up, MatrixD.Transpose(toolMatrix));
            Base6Directions.Direction dir = Base6Directions.GetClosestDirection(dir_vec);
            // program.Echo(piston.CustomName + ": " + dir);

            switch (dir) {
                case Base6Directions.Direction.Forward:
                    positive_pistons[2].Add(piston);
                    break;
                case Base6Directions.Direction.Backward:
                    negative_pistons[2].Add(piston);
                    break;
                case Base6Directions.Direction.Up:
                    positive_pistons[1].Add(piston);
                    break;
                case Base6Directions.Direction.Down:
                    negative_pistons[1].Add(piston);
                    break;
                case Base6Directions.Direction.Left:
                    positive_pistons[0].Add(piston);
                    break;
                case Base6Directions.Direction.Right:
                    negative_pistons[0].Add(piston);
                    break;
            }
        }

        program.Echo("Found " + positive_pistons[0].Count + " positive x pistons");
        program.Echo("Found " + negative_pistons[0].Count + " negative x pistons");
        program.Echo("Found " + positive_pistons[1].Count + " positive y pistons");
        program.Echo("Found " + negative_pistons[1].Count + " negative y pistons");
        program.Echo("Found " + positive_pistons[2].Count + " positive z pistons");
        program.Echo("Found " + negative_pistons[2].Count + " negative z pistons");

        PistonArmAxis[] axes = new PistonArmAxis[3];
        for (int i = 0; i < 3; i++) {
            axes[i] = new PistonArmAxis(program, positive_pistons[i].ToArray(), negative_pistons[i].ToArray());
        }

        return new PistonArm(program, axes);
    }

    public float CurrentPosition() {
        float position = 0;
        bool y_inc = Axes[2].NbSteps(VERTICAL_STEP_SIZE) % 2 == 0;
        bool x_inc = Axes[1].NbSteps(LATERAL_STEP_SIZE) % 2 == 0;

        if (x_inc) {
            position += Axes[0].CurrentPosition();
        } else {
            position += Axes[0].HighestPosition - Axes[0].CurrentPosition();
        }

        if (y_inc) {
            position += Axes[1].CurrentPosition() * Axes[0].HighestPosition;
        } else {
            position += (Axes[1].HighestPosition - Axes[1].CurrentPosition()) * Axes[0].HighestPosition;
        }
        position += Axes[2].CurrentPosition() * Axes[0].HighestPosition * Axes[1].HighestPosition;

        return position;
    }

    public void Stop() {
        foreach(PistonArmAxis axis in Axes) {
            axis.Stop();
        }
    }

    public void Run(float speed) {
        bool y_inc = Axes[2].NbSteps(VERTICAL_STEP_SIZE) % 2 == 0;
        bool x_inc = Axes[1].NbSteps(LATERAL_STEP_SIZE) % 2 == 0;

        if (x_inc && !Axes[0].IsMaxed()) {
            Axes[0].IncToMax(speed);
        } else if (!x_inc && !Axes[0].IsMined()) {
            Axes[0].DecToMin(speed);
        } else if (y_inc && !Axes[1].IsMaxed()) {
            Axes[1].Inc(speed, LATERAL_STEP_SIZE);
        } else if (!y_inc && !Axes[1].IsMined()) {
            Axes[1].Dec(speed, LATERAL_STEP_SIZE);
        } else {
            Axes[2].Inc(speed, VERTICAL_STEP_SIZE);
        }
    }

    public void Reset() {
        foreach(PistonArmAxis axis in Axes) {
            axis.Reset();
        }
    }
}

class MiningArm {
    Program Program;
    public List<IMyShipDrill> Drills;
    public List<IMyTextSurface> Screens;
    PistonArm Arm;

    public MiningArm(Program program, List<IMyShipDrill> drills, PistonArm arm) {
        Program = program;
        Drills = drills;
        Arm = arm;

        List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
        grids.Add(drills[0].CubeGrid);
        foreach(PistonArmAxis axe in arm.Axes) {
            foreach(IMyPistonBase piston in axe.PositivePistons) {
                grids.Add(piston.CubeGrid);
            }
            foreach(IMyPistonBase piston in axe.NegativePistons) {
                grids.Add(piston.CubeGrid);
            }
        }

        Screens = new List<IMyTextSurface>();
        for (int i = 0; i < Program.Me.SurfaceCount; i++) {
            IMyTextSurface screen = Program.Me.GetSurface(i);
            screen.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
            Screens.Add(screen);
        }
    }

    bool ShouldStop() {
        MyFixedPoint max_volume = 0;
        MyFixedPoint current_volume = 0;
        foreach(IMyShipDrill drill in Drills) {
            max_volume += drill.GetInventory(0).MaxVolume;
            current_volume += drill.GetInventory(0).CurrentVolume;
            if (!drill.IsFunctional) {
                return true;
            }
        }
        return (float)current_volume / (float)max_volume > 0.25;
    }

    string RemainingTime() {
        float position = Arm.CurrentPosition();

        float remaining = Arm.HighestPosition - position;
        int remaining_seconds = (int)(remaining / SPEED_NORMAL);
        int remaining_minutes = remaining_seconds / 60;
        remaining_seconds = remaining_seconds % 60;
        int remaining_hours = remaining_minutes / 60;
        remaining_minutes = remaining_minutes % 60;
        int remaining_days = remaining_hours / 24;
        remaining_hours = remaining_hours % 24;

        return remaining_days + "d " + remaining_hours + "h " + remaining_minutes + "m " + remaining_seconds + "s";
    }

    void UpdateScreens() {
        float position = Arm.CurrentPosition();

        float progression = position * 100 / Arm.HighestPosition;
        foreach(IMyTextSurface screen in Screens) {
            var remaining_time = RemainingTime();
            screen.WriteText("Progression: " + progression.ToString("0.00") + "%");
            screen.WriteText("\nRemaining time: " + remaining_time, true);
            screen.WriteText("\nX: " + Arm.Axes[0].CurrentPosition().ToString("0.0") + "/" + Arm.Axes[0].HighestPosition, true);
            screen.WriteText("\nY: " + Arm.Axes[1].CurrentPosition().ToString("0.0") + "/" + Arm.Axes[1].HighestPosition, true);
            screen.WriteText("\nZ: " + Arm.Axes[2].CurrentPosition().ToString("0.0") + "/" + Arm.Axes[2].HighestPosition, true);
            screen.WriteText("\nDrills: " + Drills.Count, true);
        }
    }

    void EnableDrills(bool enable) {
        foreach(IMyShipDrill drill in Drills) {
            drill.Enabled = enable;
        }
    }

    public void Run() {
        UpdateScreens();
        if (ShouldStop()) {
            Arm.Stop();;
            EnableDrills(false);
            return;
        }

        float speed = SPEED_FAST;
        foreach(IMyShipDrill drill in Drills) {
            drill.Enabled = true;
            if (drill.IsWorking) {
                speed = SPEED_NORMAL;
            }
        }
        Arm.Run(speed);
    }

    public void Reset() {
        EnableDrills(false);
        Arm.Reset();
    }
}

class MiningArmsFinder {
    List<IMyShipDrill> Drills;
    List<IMyShipDrill> FilteredDrills;
    GridGraphNode Graph;
    GridBranch[] Branches;
    int CurrentBranch = 0;
    List<MiningArm> MiningArms;
    Program Program;

    public MiningArmsFinder(Program program) {
        Program = program;
    }

    public MiningArm[] Continue() {
        if (Drills == null) {
            Drills = new List<IMyShipDrill>();
            Program.GridTerminalSystem.GetBlocksOfType(Drills);
            if (Drills.Count == 0) {
                Program.Echo("No drills found");
                return new MiningArm[0];
            } else {
                return null;
            }
        }

        if (FilteredDrills == null) {
            FilteredDrills = new List<IMyShipDrill>();
            foreach(IMyShipDrill drill in Drills) {
                if (drill.CubeGrid.IsSameConstructAs(Program.Me.CubeGrid)) {
                    FilteredDrills.Add(drill);
                }
            }
            if (FilteredDrills.Count == 0) {
                Program.Echo("No drills found");
                return new MiningArm[0];
            }
            return null;
        }

        if (Graph == null) {
            Graph = Program.GetGridGraph(Program.Me.CubeGrid);
            if (Graph == null) {
                Program.Echo("No graph found");
                return new MiningArm[0];
            }
            return null;
        }

        if (Branches == null) {
            Branches = Graph.GetBranchesToDrills(Program).ToArray();
            if (Branches.Length == 0) {
                Program.Echo("No branches found");
                return new MiningArm[0];
            }
            return null;
        }

        if (MiningArms == null) {
            MiningArms = new List<MiningArm>();
            return null;
        }

        if (CurrentBranch < Branches.Length) {
            GridBranch branch = Branches[CurrentBranch];
            CurrentBranch++;
            List<IMyShipDrill> branchDrills = new List<IMyShipDrill>();
            foreach(IMyShipDrill drill in FilteredDrills) {
                if (drill.CubeGrid == branch.End) {
                    branchDrills.Add(drill);
                }
            }
            Program.Echo("Found " + branchDrills.Count + " drills on branch");
            if (branchDrills.Count > 0) {
                IMyShipDrill drill = branchDrills[0];
                MatrixD toolMatrix = drill.WorldMatrix;
                PistonArm piston_arm = PistonArm.FromGridBranch(Program, branch, toolMatrix);
                MiningArms.Add(new MiningArm(Program, branchDrills, piston_arm));
            }
            return null;
        }

        return MiningArms.ToArray();
    }
}

class GridBranch {
    public IMyCubeGrid Start;
    public IMyCubeGrid End;
    public List<IMyMechanicalConnectionBlock> Joints = new List<IMyMechanicalConnectionBlock>();

    public GridBranch(IMyCubeGrid start, IMyCubeGrid end) {
        Start = start;
        End = end;
    }
}

class GridGraphNeighbor {
    public IMyMechanicalConnectionBlock Joint;
    public GridGraphNode Node;

    public GridGraphNeighbor(IMyMechanicalConnectionBlock joint, GridGraphNode node) {
        Joint = joint;
        Node = node;
    }
}

class GridGraphNode {
    public IMyCubeGrid Grid;
    public List<GridGraphNeighbor> Neighbors = new List<GridGraphNeighbor>();

    public GridGraphNode(IMyCubeGrid grid) {
        Grid = grid;
    }

    public List<GridBranch> GetBranches(Program Program, List<GridGraphNode> parents = null) {
        if (parents != null && Neighbors.Count == 1) {
            return new List<GridBranch>(new GridBranch[] { new GridBranch(Grid, Grid) });
        }
        parents = parents ?? new List<GridGraphNode>();

        List<GridBranch> branches = new List<GridBranch>();
        foreach(GridGraphNeighbor neighbor in Neighbors) {
            if (parents.Contains(neighbor.Node)) {
                continue;
            }
            List<GridGraphNode> newParents = new List<GridGraphNode>(parents);
            newParents.Add(this);
            List<GridBranch> neighborBranches = neighbor.Node.GetBranches(Program, newParents);
            foreach(GridBranch branch in neighborBranches) {
                if (branch.End == null) {
                    continue;
                }
                branch.Joints.Insert(0, neighbor.Joint);
                branch.Start = Grid;
                branches.Add(branch);
            }
        }
        return branches;
    }

    public List<GridBranch> GetBranchesToTargets(Program Program, List<IMyCubeGrid> targets, List<GridGraphNode> parents = null) {
        if (targets.Contains(Grid)) {
            return new List<GridBranch>(new GridBranch[] { new GridBranch(Grid, Grid) });
        }
        if (parents != null && Neighbors.Count == 1) {
            return new List<GridBranch>();
        }
        parents = parents ?? new List<GridGraphNode>();

        List<GridBranch> branches = new List<GridBranch>();
        foreach(GridGraphNeighbor neighbor in Neighbors) {
            if (parents.Contains(neighbor.Node)) {
                continue;
            }
            List<GridGraphNode> newParents = new List<GridGraphNode>(parents);
            newParents.Add(this);
            List<GridBranch> neighborBranches = neighbor.Node.GetBranchesToTargets(Program, targets, newParents);
            foreach(GridBranch branch in neighborBranches) {
                if (branch.End == null) {
                    continue;
                }
                branch.Joints.Insert(0, neighbor.Joint);
                branch.Start = Grid;
                branches.Add(branch);
            }
        }
        return branches;
    }

    public List<GridBranch> GetBranchesToDrills(Program Program) {
        List<IMyShipDrill> drills = new List<IMyShipDrill>();
        Program.GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);
        HashSet<IMyCubeGrid> drillGrids = new HashSet<IMyCubeGrid>();
        foreach(IMyShipDrill drill in drills) {
            drillGrids.Add(drill.CubeGrid);
        }

        return GetBranchesToTargets(Program, drillGrids.ToList());
    }
}

List<IMyMechanicalConnectionBlock> GetGridJoints() {
    List<IMyMechanicalConnectionBlock> joints = new List<IMyMechanicalConnectionBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyMechanicalConnectionBlock>(joints);

    for (int i = joints.Count - 1; i >= 0; i--) {
        IMyMechanicalConnectionBlock joint = joints[i];
        if (!joint.CubeGrid.IsSameConstructAs(Me.CubeGrid)) {
            joints.RemoveAt(i);
        }
    }
    return joints;
}

GridGraphNode GetGridGraph(IMyCubeGrid grid) {
    List<IMyMechanicalConnectionBlock> joints = GetGridJoints();

    HashSet<IMyCubeGrid> grids = new HashSet<IMyCubeGrid>();
    foreach(IMyMechanicalConnectionBlock joint in joints) {
        grids.Add(joint.CubeGrid);
        grids.Add(joint.TopGrid);
    }
    List<GridGraphNode> nodes = new List<GridGraphNode>();
    foreach(IMyCubeGrid otherGrid in grids) {
        nodes.Add(new GridGraphNode(otherGrid));
    }
    GridGraphNode root = new GridGraphNode(grid);
    foreach(GridGraphNode node in nodes) {
        if (node.Grid == grid) {
            root = node;
            break;
        }
    }

    HashSet<GridGraphNode> done = new HashSet<GridGraphNode>();
    List<GridGraphNode> todo = new List<GridGraphNode>();
    todo.Add(root);

    while(todo.Count > 0) {
        GridGraphNode node = todo[0];
        todo.RemoveAt(0);
        done.Add(node);

        foreach(IMyMechanicalConnectionBlock joint in joints) {
            if (joint.TopGrid == node.Grid) {
                GridGraphNode otherNode = nodes.Find(n => n.Grid == joint.CubeGrid);
                if (otherNode != null) {
                    node.Neighbors.Add(new GridGraphNeighbor(joint, otherNode));
                    if (!done.Contains(otherNode) && !todo.Contains(otherNode)) {
                        todo.Add(otherNode);
                    }
                }
            }
            if (joint.CubeGrid == node.Grid) {
                GridGraphNode otherNode = nodes.Find(n => n.Grid == joint.TopGrid);
                if (otherNode != null) {
                    node.Neighbors.Add(new GridGraphNeighbor(joint, otherNode));
                    if (!done.Contains(otherNode) && !todo.Contains(otherNode)) {
                        todo.Add(otherNode);
                    }
                }
            }
        }
    }

    return root;
}
