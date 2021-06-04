using UdonSharp;
using UnityEngine;

public class Prompts : UdonSharpBehaviour
{
    [SerializeField] private TextAsset textAsset = null;

    private string[] prompts = null;

    private bool cacheUsedMessageShown = false;

    public string[] LoadPromptsFromFile()
    {
        if (prompts != null)
        {
            if (!cacheUsedMessageShown) Debug.Log($"Cached prompts loaded. There are {prompts.Length} prompts.\n" +
                                                  "This message will not be shown again.");
            cacheUsedMessageShown = true;
            return prompts;
        }
        
        var input = textAsset.text;
        var inputLines = input.Split('\n');
        var result =  new string[(inputLines.Length-1) * 6];
        for (var y = 0; y < inputLines.Length - 1; y++)
        {
            var line = inputLines[y + 1].Split(',');
            for (var x = 0; x < 6; x++)
            {
                var index = y * 6 + x;
                result[index] = line[x + 1];
            }
        }

        prompts = result;
        Debug.Log($"Prompts loaded from file. There are {prompts.Length}");
        
        return prompts;
    }

    /// <summary>
    /// Returns six prompts for a given index
    /// </summary>
    /// <param name="index">Index of the prompt group</param>
    /// <returns>Six similar prompts</returns>
    public string[] GetPrompt(int index)
    {
        var result = new string[6];
        for (var i = 0; i < 6; i++)
        {
            result[i] = LoadPromptsFromFile()[index * 6 + i];
        }

        return result;
    }

    private int GetNumPrompts()
    {
        return LoadPromptsFromFile().Length / 6;
    }

    /// <summary>
    /// Returns a number random prompts, based on a seed and a given amount. Indices do not repeat
    /// </summary>
    /// <param name="seed">Seed to use for randomization</param>
    /// <param name="amount">Number of prompts to return</param>
    /// <returns>An array of unique indices, referring to different prompts</returns>
    public int[] GetPrompts(int seed, int amount)
    {
        UnityEngine.Random.InitState(seed);
        var allIndicesShuffled = GetAllIndicesShuffled();
        var result = new int[amount];
        
        for (var i = 0; i < amount; i++)
        {
            result[i] = allIndicesShuffled[i];
        }

        return result;
    }

    public int[] GetPromptSequenceForRound(int[] promptSequence, int round)
    {
        Debug.Log("Prompt sequence length: " + promptSequence.Length);
        
        var numPromptsPerRound = 3;
        var numPlayersPerPrompt = 3;
        var numPromptsTotal = 8;
        var numTotalPerRound = numPromptsPerRound * numPlayersPerPrompt;

        var result = new int[numTotalPerRound];
        for (var p = 0; p < numPromptsPerRound; p++)
        {
            result[p * numPlayersPerPrompt] = promptSequence[p + round * numPromptsPerRound]; // ERROR index outside array
            result[p * numPlayersPerPrompt + 1] = result[p * numPlayersPerPrompt];
            numPromptsTotal--;
            if (numPromptsTotal == 0) continue;
            result[p * numPlayersPerPrompt + 2] = result[p * numPlayersPerPrompt];

        }

        return ShuffleIndices(result);
    }
    
    private int[] GetAllIndicesShuffled()
    {
        var allIndices = new int [GetNumPrompts()];

        for (var i = 0; i < allIndices.Length; i++)
        {
            allIndices[i] = i;
        }
        
        return ShuffleIndices(allIndices);
    }

    private int[] ShuffleIndices(int[] indices)
    {
        for (var i = 0; i < indices.Length; i++)
        {
            var randomIndex = UnityEngine.Random.Range(i, indices.Length);
            var temp = indices[i];
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }

        return indices;
    }
}
