namespace GridBlueprint.Model;

public abstract class rewardsQLearning
{
    public static readonly double exitReward = 100;
    public static readonly double inTheRoomReward = -5;
    // public static readonly double tryToLeaveWorldReward = -50;
    public static readonly double nextToWallReward = -2; //-5? first find wall and then walk next to it until the exit
    public static readonly double collisionReward = -10;
    public static readonly double findDoorReward = -1;
    
    
}