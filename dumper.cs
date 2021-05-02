class Sources {
    public List<IMyInventory> list;
    public Sources(Program program) {
        this.list = new List<IMyInventory>();
        var list = new List<IMyTerminalBlock>();

        program.GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(list);
        foreach(var block in list) {
            var source = (IMyCargoContainer)block;
            this.list.Add(source.GetInventory());
        }

        program.GridTerminalSystem.GetBlocksOfType<IMyRefinery>(list);
        foreach(var block in list) {
            var source = (IMyRefinery)block;
            this.list.Add(source.OutputInventory);
            var items = new List<MyInventoryItem>();
            source.OutputInventory.GetItems(items);
        }
    }
}

public void InitDumpers(string name_prefix) {
    var list = new List<IMyTerminalBlock>();
    Echo("Search for " + name_prefix);
    GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list);
    foreach (var block in list) {
        if(block.CustomName == name_prefix) {
            this.dumpers.Add(block);
        }
    }
    Echo("Found " + list.Count);
}

Sources containers;
List<IMyShipConnector> dumpers;
List<MyItemType> to_dump;

const float GRAVEL_DENSITY = 0.37f;

public Program() {
    this.containers = new Sources(this);
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
        var rem = (float)(inventory.MaxVolume - inventory.CurrentVolume);
        Echo("rem " + rem + " / " + (float)inventory.MaxVolume);
        if(rem/(float)inventory.MaxVolume < 0.10f) {
            dumper.ThrowOut = true;
            nb_throw_out++;
        } else {
            dumper.ThrowOut = false;
        }
        remaining.Add(rem * 1000.0f);
    }
    Echo(nb_throw_out + " throwing out.");

    var last = dumpers.Count - 1;
    Echo("last " + last);
    if(remaining.Count == 0) {
        Echo("Dumpers full!");
        return;
    }
    while(remaining[last] <= 0.0f) {
        Echo("rem["+ last+"]" + remaining[last]);
        dumpers.RemoveAt(last);
        remaining.RemoveAt(last);
        last = dumpers.Count - 1;
        Echo("last " + last);
        if(last == 0) {
            Echo("Dumpers full!");
            return;
        }
    }

    var transfering = false;
    foreach(var source in this.containers.list) {
        foreach(var type in to_dump) {
            var list = new List<MyInventoryItem>();
            source.GetItems(list, (item) => {
                return item.Type.Equals(type);
            });
            foreach(var item in list) {
                source.TransferItemTo(dumpers[last].GetInventory(), item);
                remaining[last] -= (float)item.Amount * GRAVEL_DENSITY;

                while(remaining[last] <= 0.0f) {
                    dumpers.RemoveAt(last);
                    remaining.RemoveAt(last);
                    if(last == 0) {
                        Echo("Too much to transfer!");
                        return;
                    }
                    last = dumpers.Count - 1;
                    Echo("last " + last);
                }
                transfering = true;
            }
        }
    }
    if(transfering) {
        Echo("Transfering");
    }
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
