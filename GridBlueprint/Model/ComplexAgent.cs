using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mars.Common;
using Mars.Components.Agents;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using Mars.Numerics;

using System.IO;
using Newtonsoft.Json;

namespace GridBlueprint.Model;

public class ComplexAgent : IAgent<GridLayer>, IPositionable
{
    #region Init

    /// <summary>
    ///     The initialization method of the ComplexAgent which is executed once at the beginning of a simulation.
    ///     It sets an initial Position and an initial State and generates a list of movement directions.
    /// </summary>
    /// <param name="layer">The GridLayer that manages the agents</param>
    public void Init(GridLayer layer)
    {
        _layer = layer;
        Position = new Position(StartX, StartY);
        _state = AgentState.MoveQLearning; // my code
        _directions = CreateMovementDirectionsList();
        _layer.ComplexAgentEnvironment.Insert(this);
        _QLearningRewards = CreateQLearningRewards();
        _Qstate = GetCurrentState();
        LoadQTable(_fileName);
    }
    
    public void SaveQTable(string filePath)
    {
        if (train)
        {
            Console.WriteLine("Saving QTable");
            string str = "";
            foreach (var key in _Qtable.Keys)
            {

                str += $"?{key} %{_Qtable[key]}\n";
            }

            // save as txt file
            File.WriteAllText(filePath, str);
        }
    }
    public void LoadQTable(string filePath)
    {
        if (File.Exists(filePath))
        {
            Console.WriteLine("Loading QTable");
            var json = File.ReadAllText(filePath);
            // split by new line
            string[] lines = json.Split("\n");
            foreach (var line in lines)
            {
                if (line != "")
                {
                    string stateStringFromKey = line.Split("?(")[1].Split("!")[0];
                    KnownState state = KnownState.FromString(stateStringFromKey);
                    string actionStringFromKey = line.Split("!")[1].Split(")")[0].Split("(")[1];
                    // get action from string in the format x,y
                    string[] actionString = actionStringFromKey.Split(",");
                    Position action = new Position(int.Parse(actionString[0]), int.Parse(actionString[1]));
                    double value = double.Parse(line.Split("%")[1]);
                    if (_Qtable.ContainsKey((state.ToString(), action)))
                    {
                        Console.WriteLine("weired you alredy have this key in the QTable");
                        Console.WriteLine($"key: ({state} {action})");
                        Console.WriteLine($"with this value: {_Qtable[(state.ToString(), action)]}");
                        Console.WriteLine($"you tried to enter the new value: {value}");
                        
                    }
                    _Qtable[(state.ToString(), action)] = value;
                }
            }
        }
    }
    #endregion

    #region Tick

    /// <summary>
    ///     The tick method of the ComplexAgent which is executed during each time step of the simulation.
    ///     A ComplexAgent can move randomly along straight lines. It must stay within the bounds of the GridLayer
    ///     and cannot move onto grid cells that are not routable.
    /// </summary>
    public void Tick()
    {
        if (_layer.IsExit((int)Position.X, (int)Position.Y))
        {
            SaveQTable(_fileName);
            RemoveFromSimulation();
        }
        _currentTick++;
        if (_currentTick % 100 == 0)
        {
            SaveQTable(_fileName);
        }
        var oldState = _Qstate;
        KnownState newState = GetCurrentState();
        _Qstate = learnFromHistory(oldState, newState);
        
        MoveQLearning(train);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Generates a list of eight movement directions that the agent uses for random movement.
    /// </summary>
    /// <returns>The list of movement directions</returns>
    private static List<Position> CreateMovementDirectionsList()
    {
        return new List<Position>
        {
            MovementDirections.North,
            MovementDirections.Northeast,
            MovementDirections.East,
            MovementDirections.Southeast,
            MovementDirections.South,
            MovementDirections.Southwest,
            MovementDirections.West,
            MovementDirections.Northwest
        };
    }

    private void UpdateTable(Position action)
    {
        double stateActionReward= getReward(_Qstate, action);
        KnownState nextState = expectedState(_Qstate, action);
        double nextStateReward = maxActionQtable(nextState).Item1;
        double qValue = stateActionReward + (_gamma * nextStateReward);
        _Qtable[(_Qstate.ToString(), action)] = qValue;
    }
    /// <summary>
    ///     Performs one random move, if possible, using the movement directions list.
    /// </summary>
    private void MoveRandomly()
    {
        var validActions = getValidActions(Position);
        if (validActions.Count == 0)
        {
            return;
        }
        var randomIndex = _random.Next(validActions.Count);
        var action = validActions[randomIndex];
        UpdateTable(action);
        moveTo(action);
    }
    private static List<Double> CreateQLearningRewards()
    {
        return new List<Double>
        {
            rewardsQLearning.exitReward,
            rewardsQLearning.inTheRoomReward,
            rewardsQLearning.nextToWallReward,
            rewardsQLearning.collisionReward,
            rewardsQLearning.findDoorReward
        };
    }

    private void MoveQLearning(bool train)
    {
        if (train)
            MoveRandomly(); // train session
        else
        {
            //get best action given the current state
            var (maxValue, bestAction) = maxActionQtable(_Qstate);
            if (bestAction == null)
            {
                return;
            }
            if (maxValue <= -100)
            {
                MoveRandomly();
                return;
            }
            UpdateTable(bestAction);
            Console.WriteLine($"agent {ID} moved to {bestAction}");
            moveTo(bestAction);            
        }

    }

    private List<Position> getValidActions(Position curPosition)
    {
        List<Position> validActions = new List<Position>();
        foreach (var direction in _directions)
        {
            var newX = curPosition.X + direction.X;
            var newY = curPosition.Y + direction.Y;
            Position nextPosition = new Position(newX, newY);
            if (tryMoveTo(nextPosition))
            {
                validActions.Add(nextPosition);
            }
        }
        return validActions;
    }
    private Double getReward(KnownState state, Position action)
    {
        if (state.Exit!=null && state.Exit.Equals(action))
        {
            return rewardsQLearning.exitReward;
        }
        if (state.Walls.Contains(action))
        {
            return rewardsQLearning.nextToWallReward;
        }
        if (state.NearbyAgents.Contains(action))
        {
            return rewardsQLearning.collisionReward;
        }
        if (state.Doors.Contains(action))
        {
            return rewardsQLearning.findDoorReward;
        }
        return rewardsQLearning.inTheRoomReward;
    }
    
    private List<Position> lookForAgentsLocations()
    {
        // Get all nearby Agents instances
        var simpleAgents = _layer.SimpleAgentEnvironment.Explore(Position, radius: AgentExploreRadius);
        var complexAgents = _layer.ComplexAgentEnvironment.Explore(Position, radius: AgentExploreRadius);
        
        List<Position> simpleAgentsPositions =simpleAgents.Select(agent => agent.Position).ToList();
        List<Position> complexAgentsPositions =complexAgents.Select(agent => agent.Position).ToList();
        List<Position> agentPositons = new List<Position>();
        agentPositons.AddRange(simpleAgentsPositions);
        agentPositons.AddRange(complexAgentsPositions);
        // Remove the current agent's position
        agentPositons.Remove(Position);
        
        // Select and return their positions
        return agentPositons;
    }

    private bool aboutToCollide(Position nextPosition)
    {
        var agentsPositions = lookForAgentsLocations();
        return agentsPositions.Contains(nextPosition);
    }
    
    private bool tryMoveTo(Position nextPosition)
    {
        var newX = nextPosition.X;
        var newY = nextPosition.Y;
        Position oldPosition = Position;
        if (0 <= newX && newX < _layer.Width && 0 <= newY && newY < _layer.Height)
        {
            // Check if chosen move goes to a cell that is routable
            if (_layer.IsRoutable(newX, newY))
            {
                if (aboutToCollide(nextPosition))
                {
                    // Console.WriteLine($"{GetType().Name} tried to collide at: {Position}");
                    return false;
                }
                // Console.WriteLine($"{GetType().Name} moved to a new cell: {Position}");
                return true;
            }
            // Console.WriteLine($"{GetType().Name} tried to move to a blocked cell: ({newX}, {newY})");
            return false;
        }
        // Console.WriteLine($"{GetType().Name} tried to leave the world: ({newX}, {newY})");
        return false;
    }
    private void moveTo(Position nextPosition)
    {
        Position = nextPosition;
        _layer.ComplexAgentEnvironment.MoveTo(this, nextPosition);
    }

    private (double,Position) maxActionQtable(KnownState qState)
    {
        double maxValue = -100;
        Position bestAction = null;
        var expectedReward = 0.0;
        var newPos = qState.SelfPosition;
        var validActions = getValidActions(newPos);
        foreach (var action in validActions)
        {
            // expectedReward = getReward(qState, action);
            // if (expectedReward > maxValue)
            // {
            //     maxValue = expectedReward;
            //     bestAction = action;
            // }
            if (!_Qtable.ContainsKey((qState.ToString(), action)))
            {
                _Qtable[(qState.ToString(), action)] = getReward(qState, action);
            }
            if (_Qtable[(qState.ToString(), action)]>maxValue)
            {
                maxValue = _Qtable[(qState.ToString(), action)];
                bestAction = action;
            }
        }

        return (maxValue, bestAction);
    }
    
    // end of my code

    /// <summary>
    ///     Removes this agent from the simulation and, by extension, from the visualization.
    /// </summary>
    private void RemoveFromSimulation()
    {
        Console.WriteLine($"ComplexAgent {ID} is removing itself from the simulation.");
        _layer.ComplexAgentEnvironment.Remove(this);
        UnregisterAgentHandle.Invoke(_layer, this);
    }

    public KnownState GetCurrentState()
    {
        var (walls, doors, exit) = Explore();
        var currentState = new KnownState
        {
            SelfPosition = Position,
            NearbyAgents = lookForAgentsLocations(),
            Walls = walls,
            Doors = doors,
            Exit = exit
        };

        return currentState;
    }
    // explore the environment: walls, doors, exit
    private (List<Position>,List<Position>, Position) Explore()
    {
        List<Position> walls = new List<Position>();
        List<Position> doors = new List<Position>();
        Position exit = null;
        var currX= (int) Position.X;
        var currY= (int) Position.Y;
        
        for (int i = -1; i <= AgentExploreRadius; i++)
        {
            for (int j = -1; j <= AgentExploreRadius; j++)
            {
                var newX = currX + i;
                var newY = currY + j;
                if (0 <= newX && newX < _layer.Width && 0 <= newY && newY < _layer.Height)
                {
                    if (_layer.IsDoor(newX, newY))
                    {
                        doors.Add(new Position(newX, newY));
                    }
                    else if (_layer.IsExit(newX, newY))
                    {
                        exit = new Position(newX, newY);
                    }
                    else if (!_layer.IsRoutable(newX, newY))
                    {
                        walls.Add(new Position(newX, newY));
                    }
                }
            }
        }
        return (walls, doors, exit);
    }
    
    
    private KnownState learnFromHistory(KnownState oldState, KnownState newState)
    {
        // Extend SelfPosition
        if (newState.SelfPosition == null)
        {
            newState.SelfPosition = oldState.SelfPosition;
        }

        // Extend Walls
        if (newState.Walls == null)
        {
            newState.Walls = oldState.Walls;
        }
        else
        {
            foreach (var wall in oldState.Walls)
            {
                if (!newState.Walls.Contains(wall))
                {
                    newState.Walls.Add(wall);
                }
            }
        }

        // Extend Doors
        if (newState.Doors == null)
        {
            newState.Doors = oldState.Doors;
        }
        else
        {
            foreach (var door in oldState.Doors)
            {
                if (!newState.Doors.Contains(door))
                {
                    newState.Doors.Add(door);
                }
            }
        }

        // Extend Exit
        if (newState.Exit == null)
        {
            newState.Exit = oldState.Exit;
        }

        return newState;
    }
    
    private KnownState expectedState(KnownState oldState, Position action)
    {
        KnownState newState = new KnownState
        {
            SelfPosition = action,
            NearbyAgents = oldState.NearbyAgents,
            Walls = oldState.Walls,
            Doors = oldState.Doors,
            Exit = oldState.Exit
        };
        return newState;
    }
    #endregion

    #region Fields and Properties

    public Guid ID { get; set; }
    
    public Position Position { get; set; }

    [PropertyDescription(Name = "StartX")]
    public int StartX { get; set; }
    
    [PropertyDescription(Name = "StartY")]
    public int StartY { get; set; }
    
    [PropertyDescription(Name = "MaxTripDistance")]
    public double MaxTripDistance { get; set; }
    
    [PropertyDescription(Name = "AgentExploreRadius")]
    public double AgentExploreRadius { get; set; }
    
    public UnregisterAgent UnregisterAgentHandle { get; set; }
    
    private GridLayer _layer;
    private List<Position> _directions;
    private readonly Random _random = new();
    private Position _goal;
    private bool _tripInProgress;
    private AgentState _state;
    private List<Position>.Enumerator _path;
    // my code
    private List<Double> _QLearningRewards;
    private KnownState _Qstate;
    private ConcurrentDictionary<(string, Position),Double> _Qtable = new ConcurrentDictionary<(string, Position), Double>();
    private double _gamma = 0.8;
    private int _currentTick=1;
    private string _fileName = "C:\\Users\\loonn\\RiderProjects\\blueprint-grid\\GridBlueprint\\Resources\\QTable.txt";
    private bool train = false;
    #endregion
}