﻿static MiningArm[] mining_arms;

public Program() {
    Echo("Hello" + Me.SurfaceCount);
    for (int i = 0; i < Me.SurfaceCount; i++) {
        IMyTextSurface surface = Me.GetSurface(i);
        surface.WriteText("Hello " + i + "");
    }
    Echo("Grip blocks: " + CoundGridBlocks(Me.CubeGrid));
    Echo("Joints: " + GetGridJoints().Count);
    // Echo("Branches: " + GetGridGraph(Me.CubeGrid).GetBranches(null).Count);
    mining_arms = GetMiningArms(Me.CubeGrid);
    Echo("Mining arms: " + mining_arms.Length);
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {
}

public void Main(string argument, UpdateType updateSource) {
    foreach(MiningArm arm in mining_arms) {
        arm.Run();
    }
}

int CoundGridBlocks(IMyCubeGrid grid) {
    int count = 0;
    Echo("Min: " + grid.Min);
    Echo("Max: " + grid.Max);
    foreach(Vector3I pos in Vector3I.EnumerateRange(grid.Min, grid.Max + new Vector3I(1, 1, 1))) {
        if(grid.CubeExists(pos) ) {
            count++;
        }
    }
    return count;
}

const float VERTICAL_STEP_SIZE = 1.0f;
const float LATERAL_STEP_SIZE = 2.0f;
const float SPEED_NORMAL = 0.2f;
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
                    positive_pistons[0].Add(piston);
                    break;
                case Base6Directions.Direction.Backward:
                    negative_pistons[0].Add(piston);
                    break;
                case Base6Directions.Direction.Up:
                    positive_pistons[1].Add(piston);
                    break;
                case Base6Directions.Direction.Down:
                    negative_pistons[1].Add(piston);
                    break;
                case Base6Directions.Direction.Left:
                    positive_pistons[2].Add(piston);
                    break;
                case Base6Directions.Direction.Right:
                    negative_pistons[2].Add(piston);
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
}

class MiningArm {
    Program Program;
    public List<IMyShipDrill> Drills;
    public List<IMyTextPanel> Screens;
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

        program.Echo("Found " + grids.Count + " grids");
        Screens = new List<IMyTextPanel>();
        List<IMyTextPanel> all_screens = new List<IMyTextPanel>();
        Program.GridTerminalSystem.GetBlocksOfType(all_screens);
        foreach(IMyTextPanel screen in all_screens) {
            if (grids.Contains(screen.CubeGrid)) {
                screen.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER;
                Screens.Add(screen);
            }
        }
        program.Echo("Found " + Screens.Count + " screens");
    }

    bool ShouldStop() {
        MyFixedPoint max_volume = 0;
        MyFixedPoint current_volume = 0;
        foreach(IMyShipDrill drill in Drills) {
            max_volume += drill.GetInventory(0).MaxVolume;
            current_volume += drill.GetInventory(0).CurrentVolume;
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
        foreach(IMyTextPanel screen in Screens) {
            var remaining_time = RemainingTime();
            screen.WriteText("Progression: " + progression.ToString("0.00") + "%" + "\nRemaining time: " + remaining_time);
        }
    }

    public void Run() {
        UpdateScreens();
        if (ShouldStop()) {
            Arm.Stop();;
            foreach(IMyShipDrill drill in Drills) {
                drill.Enabled = false;
            }
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
}

MiningArm[] GetMiningArms(IMyCubeGrid grid) {
    List<IMyShipDrill> drills = new List<IMyShipDrill>();
    GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(drills);

    for (int i = drills.Count - 1; i >= 0; i--) {
        IMyShipDrill drill = drills[i];
        if (!drill.CubeGrid.IsSameConstructAs(grid)) {
            drills.RemoveAt(i);
        }
    }
    if (drills.Count == 0) {
        Echo("No drills found");
        return new MiningArm[0];
    }

    List<GridBranch> branches = GetGridGraph(grid).GetBranches(null);
    Echo("Found " + branches.Count + " branches");
    List<MiningArm> arms = new List<MiningArm>();
    foreach(GridBranch branch in branches) {
        Echo("Branch " + branch.End.CustomName);
        List<IMyShipDrill> branchDrills = new List<IMyShipDrill>();
        foreach(IMyShipDrill drill in drills) {
            if (drill.CubeGrid.IsSameConstructAs(branch.End)) {
                branchDrills.Add(drill);
            }
        }
        Echo("Found " + branchDrills.Count + " drills on branch");
        if (branchDrills.Count > 0) {
            IMyShipDrill drill = branchDrills[0];
            MatrixD toolMatrix = drill.WorldMatrix;
            Matrix omat;
            drill.Orientation.GetMatrix(out omat);
            toolMatrix = MatrixD.Multiply(omat, toolMatrix);
            Echo("Drill " + toolMatrix.Forward.ToString("0.0"));
            PistonArm piston_arm = PistonArm.FromGridBranch(this, branch, toolMatrix);
            arms.Add(new MiningArm(this, branchDrills, piston_arm));
        }
    }

    return arms.ToArray();
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

    public List<GridBranch> GetBranches(List<GridGraphNode> parents = null) {
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
            List<GridBranch> neighborBranches = neighbor.Node.GetBranches(newParents);
            foreach(GridBranch branch in neighborBranches) {
                branch.Joints.Insert(0, neighbor.Joint);
                branch.Start = Grid;
                branches.Add(branch);
            }
        }
        return branches;
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
