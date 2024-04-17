using System;
using System.Collections.Generic;
using Mars.Interfaces.Environments;
using Newtonsoft.Json;

public class KnownState
{
    [JsonProperty]
    public Position SelfPosition { get; set; }
    [JsonProperty]
    public List<Position> NearbyAgents { get; set; }
    [JsonProperty]
    public List<Position> Walls { get; set; }
    [JsonProperty]
    public List<Position> Doors { get; set; }
    [JsonProperty]
    public Position Exit { get; set; }
    public KnownState()
    {
        NearbyAgents = new List<Position>();
        Walls = new List<Position>();
        Doors = new List<Position>();
        
    }
    public override bool Equals(object obj)
    {
        if (obj is KnownState other)
        {
            return Equals(SelfPosition, other.SelfPosition) &&
                   Equals(Walls, other.Walls) &&
                   Equals(Doors, other.Doors) &&
                   Equals(Exit, other.Exit);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SelfPosition, Walls, Doors, Exit);
    }

    public override string ToString()
    {
        string str = $"SelfPosition: {SelfPosition}~ NearbyAgents: [";
        // List<(double,double)> nearbyAgentsLocations = new List<(double, double)>();
        // foreach (var agent in NearbyAgents)
        // {
        //     nearbyAgentsLocations.Add((agent.X, agent.Y));
        //     str=$"{str}{agent}; ";
        // }
        str = $"{str}]~ Walls: [";
        List<(double, double)> wallsLocations = new List<(double, double)>();
        foreach (var wall in Walls)
        {
            wallsLocations.Add((wall.X, wall.Y));
            str = $"{str}{wall}; ";
        }
        List<(double, double)> doorsLocations = new List<(double, double)>();
        str = $"{str}]~ Doors: [";
        foreach (var door in Doors)
        {
            doorsLocations.Add((door.X, door.Y));
            str = $"{str}{door}; ";
        }
        str = $"{str}]~ Exit: {Exit}!";
        return str;
        // return $"SelfPosition: ({SelfPosition}), NearbyAgents: {nearbyAgentsLocations}, Walls: {wallsLocations}, Doors: {doorsLocations}, Exit: (Exit)";
    }

    public static KnownState FromString(string str)
    {
        KnownState knownState=new KnownState();
        // SelfPosition: (double, double)~ NearbyAgents: \[(double,double); (double,double); ...\]~ walls: \[(double,double); (double,double); ...\]~ Doors: \[(double,double); (double,double); ...\]~ Exit: (double,double)
        string[] parts = str.Split('~');

        // get the self position from the string 
        string selfPositionString = parts[0].Split(": ")[1];
        (double x, double y) = GetPositionFromString(selfPositionString);
        knownState.SelfPosition = new Position(x, y);
        // get the nearby agents from the string
        string nearbyAgentsString = parts[1].Split(": ")[1];
        string[] nearbyAgentsParts = nearbyAgentsString.Split("; ");
        if (nearbyAgentsParts.Length > 1)
        {
            foreach (var agent in nearbyAgentsParts)
            {
                if (agent.Contains("]"))
                    continue;
                (x, y) = GetPositionFromString(agent);
                knownState.NearbyAgents.Add(new Position(x, y));
            }
        }
        // get the walls from the string
        string wallsString = parts[2].Split(": ")[1];
        string[] wallsParts = wallsString.Split("; ");
        if (wallsParts.Length > 1)
        {
            foreach (var wall in wallsParts)
            {
                if (wall.Contains("]"))
                    continue;
                (x, y) = GetPositionFromString(wall);
                knownState.Walls.Add(new Position(x, y));
            }
        }
        // get the doors from the string
        string doorsString = parts[3].Split(": ")[1];
        string[] doorsParts = doorsString.Split("; ");
        if (doorsParts.Length > 1)
        {
            foreach (var door in doorsParts)
            {
                if (door.Contains("]"))
                    continue;
                (x, y) = GetPositionFromString(door);
                knownState.Doors.Add(new Position(x, y));
            }
        }
        // get the exit from the string
        string exitString = parts[4].Split(": ")[1];
        if (exitString != "")
        {
            (x, y) = GetPositionFromString(exitString);
            knownState.Exit = new Position(x, y);
        }
        return knownState;
    }
    private static (double,double) GetPositionFromString(string str)
    {
        string[] parts = str.Split(", ");
        double x = double.Parse(parts[0].Split("(")[1]);
        double y = double.Parse(parts[1].Split(")")[0]);
        return (x, y);
    }
}
