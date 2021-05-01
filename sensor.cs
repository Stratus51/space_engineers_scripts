public Program() {
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

void Main(string argument) {
    var sensors = new List<IMySensorBlock>();
    GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors);
    foreach(var sensor in sensors) {
        Echo("Sensor '" + sensor.Name + "' " + sensor.EntityId);

        sensor.DetectPlayers = false;
        sensor.DetectFloatingObjects = false;
        sensor.DetectAsteroids = false;
        sensor.DetectEnemy = false;

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
        sensor.FrontExtend = 10.0f;
        sensor.BackExtend = 0.0f;

        var list = new List<MyDetectedEntityInfo>();
        sensor.DetectedEntities(list);
        foreach(var entity in list) {
            switch(entity.Type) {
                case MyDetectedEntityType.SmallGrid:
                case MyDetectedEntityType.LargeGrid:
                    Echo("  Detected " + entity + " | " + entity.Type);
                    break;
            }
        }
    }
}
