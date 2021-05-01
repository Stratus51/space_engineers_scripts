public Program() {
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

List<IMyCubeGrid> ListCubeGrids() {
    var list = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocks(list);

    var ret = new List<IMyCubeGrid>();
    foreach(var block in list) {
        var known = false;
        foreach(var grid in ret) {
            if(block.CubeGrid == grid) {
                known = true;
                break;
            }
        }
        if(!known) {
            ret.Add(block.CubeGrid);
        }
    }
    return ret;
}

class GridStats {
    IMyCubeGrid grid;
    int total;
    int terminal_block;
    int damaged;

    public GridStats(IMyCubeGrid grid) {
        this.grid = grid;
        var tot = 0;
        var termblock = 0;
        var damaged = 0;
        for(var z = grid.Min.Z; z <= grid.Max.Z; z++) {
            for(var y = grid.Min.Y; y <= grid.Max.Y; y++) {
                for(var x = grid.Min.X; x <= grid.Max.X; x++) {
                    if(grid.CubeExists(new Vector3I(x, y, z))) {
                        tot++;
                        var block = grid.GetCubeBlock(new Vector3I(x, y, z));
                        if(block != null) {
                            termblock++;
                            if(!block.IsFullIntegrity) {
                                damaged++;
                            }
                        }
                    }
                }
            }
        }
        this.total = tot;
        this.terminal_block = termblock;
        this.damaged = damaged;
    }

    public void Print(Program program) {
        program.Echo("Grid Z[" + grid.Min.Z + ";" + grid.Max.Z + "] | Y[" + grid.Min.Y + ";" + grid.Max.Y + "] | X[" + grid.Min.X + ";" + grid.Max.X + "]");
        program.Echo("Total: " + this.total);
        program.Echo("Terminal blocks: " + this.terminal_block);
        program.Echo("Damaged: " + this.damaged);
    }
}

void Main(string argument) {
    var list = ListCubeGrids();
    for(var i = 0; i < list.Count; ++i) {
        Echo("Grid " + i + ":");
        new GridStats(list[i]).Print(this);
    }
}
