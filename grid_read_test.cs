string GetBlockType(IMyShipWelder welder, Vector3D direction, float size, string name) {
    var grid = Me.CubeGrid;
    var welder_position = welder.CubeGrid.GridIntegerToWorld(welder.Position);
    var shifted = welder_position + size*direction;

    var pos_i = grid.WorldToGridInteger(shifted);
    var slim = grid.GetCubeBlock(pos_i);

    if(slim == null) {
        return name + ":" + pos_i + ": null";
    } else {
        return name + ":" + pos_i + ": " + slim.BlockDefinition.SubtypeName;
    }
}

class NamedDirection {
    public string Name;
    public Vector3D Direction;
    public NamedDirection(string name, Vector3D direction) {
        this.Name = name;
        this.Direction = direction;
    }
}

void Main() {
    var list = new List<IMyShipWelder>();
    GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(list);
    var welder = list[0];

    var directions = new NamedDirection[] {
        new NamedDirection("Forward", welder.WorldMatrix.Forward),
        new NamedDirection("Backward", welder.WorldMatrix.Backward),
        new NamedDirection("Up", welder.WorldMatrix.Up),
        new NamedDirection("Down", welder.WorldMatrix.Down),
        new NamedDirection("Right", welder.WorldMatrix.Right),
        new NamedDirection("Left", welder.WorldMatrix.Left),
    };
    var size = Me.CubeGrid.GridSize;
    size /= 2;
    size += 0.1f;

    var text = new List<string>();
    foreach(var direction in directions) {
        var s = GetBlockType(welder, direction.Direction, size, direction.Name);
        Echo(s);
        text.Add(s);
    }
    IMyTextSurface surface = Me.GetSurface(0);
    surface.WriteText(String.Join("\n", text.ToArray()));
}
