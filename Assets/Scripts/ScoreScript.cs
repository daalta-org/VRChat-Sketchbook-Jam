
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ScoreScript : UdonSharpBehaviour
{
    [SerializeField] private int[][] pointsGuess = null;
    [SerializeField] private int[][] pointsBonus = null;

    /// <summary>
    /// Gets the points a player should receive
    /// </summary>
    /// <param name="playerCount">Absolute player count. Min 3, max 8</param>
    /// <param name="placement">Placement. 0 is first, 1 is second, 2 is third...</param>
    /// <returns>Points that player would receive, or -1 if the parameters were invalid.</returns>
    public int GetGuessPoints(int playerCount, int placement)
    {
        if (!IsPlayerCountValid(playerCount))
        {
            return -1;
        }

        return pointsGuess[playerCount][placement];
    }

    public int[] GetGuessPointsArray(int playerCount)
    {
        if (!IsPlayerCountValid(playerCount)) return null;
        return pointsGuess[playerCount];
    }

    private bool IsPlayerCountValid(int playerCount)
    {
        return 3 <= playerCount && playerCount <= 8;
    }
    
    /// <summary>
    /// Gets the points a player should receive
    /// </summary>
    /// <param name="playerCount">Absolute player count. Min 3, max 8</param>
    /// <param name="placement">Placement. 0 is first, 1 is second, 2 is third...</param>
    /// <returns>Points that player would receive, or -1 if the parameters were invalid.</returns>
    public int GetBonusPoints(int playerCount, int placement)
    {
        if (!IsPlayerCountValid(playerCount))
        {
            return -1;
        }

        return pointsBonus[playerCount][placement];
    }
}
