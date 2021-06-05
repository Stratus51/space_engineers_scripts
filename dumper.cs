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
    InitDumpers(conf["name"]);
    return true;
}

public void Main(string argument)
{
    var args = argument.Split(' ').ToList();
    if(this.dumpers.Count == 0){
        this.Init();
    }

    Dump();
}
