public Program() {
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

void Main(string argument) {
    var args = argument.Split(' ').ToList();
    string name;
    if (args.Count < 1 && args[0].Length == 0) {
        return;
    }
    name = args[0];

    var sensors = new List<IMySensorBlock>();
    GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors);
    foreach(var sensor in sensors) {
        if(sensor.CustomName == name) {
            Echo("Sensor '" + sensor.Name + "' " + sensor.EntityId);

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

            sensor.LeftExtend = 0.0f;
            sensor.RightExtend = 0.0f;
            sensor.TopExtend = 0.0f;
            sensor.BottomExtend = 0.0f;
            sensor.FrontExtend = 50.0f;
            sensor.BackExtend = 0.0f;
            sensor.DetectEnemy = true;

            var list = new List<MyDetectedEntityInfo>();
            sensor.DetectedEntities(list);
            foreach(var entity in list) {
                switch(entity.Type) {
                    case MyDetectedEntityType.SmallGrid:
                    case MyDetectedEntityType.LargeGrid:
                        Echo("  Detected " + entity + " | " + entity.Type + " | " + entity.Position);
                        break;
                }
            }
        }
    }
}
