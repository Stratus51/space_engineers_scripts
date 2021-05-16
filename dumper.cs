public void InitDumpers(string name_prefix) {
    var list = new List<IMyShipConnector>();
    Echo("Search for " + name_prefix);
    GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list);
    foreach (var block in list) {
        if(block.CustomName == name_prefix) {
            this.dumpers.Add(block);
        }
    }
    Echo("Found " + this.dumpers.Count);
}

List<IMyShipConnector> dumpers;
List<MyItemType> to_dump;

const float GRAVEL_DENSITY = 0.37f;

public Program() {
    this.dumpers = new List<IMyShipConnector>();
    this.to_dump = new List<MyItemType>(){MyItemType.MakeIngot("Stone")};
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void Dump() {
    List<IMyShipConnector> dumpers = new List<IMyShipConnector>();
    List<float> remaining = new List<float>();

    var nb_throw_out = 0;
    foreach(var dumper in this.dumpers) {
        dumpers.Add(dumper);
        var inventory = dumper.GetInventory();
        if((float)inventory.CurrentVolume/(float)inventory.MaxVolume > 0.90f) {
            dumper.ThrowOut = true;
            nb_throw_out++;
        } else {
            dumper.ThrowOut = false;
        }
    }
    Echo(nb_throw_out + " throwing out.");
}

public void Main(string argument)
{
    var args = argument.Split(' ').ToList();
    if(this.dumpers.Count == 0){
        if (args.Count >= 1 && args[0].Length > 0) {
            var name_prefix = args[0];
            InitDumpers(name_prefix);
        } else {
            Echo("Missing arguments: " + args.Count + " < 1");
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
            return;
        }
    }

    Dump();
}
