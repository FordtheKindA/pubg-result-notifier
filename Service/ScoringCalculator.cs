namespace ServicePractice.Services;

public class ScoringCalculator
{
    private const int KillPointMultiplier = 1;

    public int GetKillPoints(int kills)
    {
        return kills * KillPointMultiplier;
    }
    public int GetPlacementPoints(int placement)
{
    return placement switch
    {
        1 => 10,
        2 => 6,
        3 => 5,
        4 => 4,  
        5 => 3,
        6 => 2,
        7 or 8 => 1,
        _ => 0
    };
}
}