using BestHTTP.Logger;
using UdonSharp;
using UnityEngine;

public class Prompts : UdonSharpBehaviour
{
    private readonly string[] promts = new[] // TODO Switch this to a text parser
    {
        "Beach","Pool","Lake","Ocean","Canal","Pond","Puddle",
        "Travel","Wander","Explore","Arrive","Depart","Discover","Walk",
        "North America","South America","Europe","Africa","Asia","Oceania","Antarctica",
        "Frog","Newt","Toad","Salamander","Chameleon","Iguana","Axolotl",
        "CyanLaser","Fionna","1","Merlin","Owlboy","LuciferMStar","Jar",
        "Deer","Moose","Reindeer","Elk","Antelope","Fawn","Horse",
        "Dingo","Shiba","Chihuahua","Pug","Hyena","Pitbull","Great Dane",
        "Telescope","Surveillance Camera","Night Vision Goggles","Binoculars","Scope","GoPro","Hidden Camera",
        "Museum","Cemetery ","Memorial ","Presidential Site","Taverns","Battlefields","National Parks",
        "Car","Bus","Truck","Limousine","Campervan","Cabriolet","Sports Car",
        "The Great Pug","The Room Of The Rain","The Black Cat","Japan Shrine","Summer Solitude","Just B Club","Aquarius",
        "Brush","Ugandan Knuckles","Kanna","Nikei","Unity-Chan","Miku","Kermit",
        "Mountain","Hill","Valley","Canyon","Cliff","Peak","Volcano",
        "Catch Me If You Can","The Darjeeling Limited","Seven Years in Tibet","In Bruges","Midnight in Paris","Lost in Translation","Eat Pray Love",
        "Tennis Racket","Baseball Bat","Golf Club","Ski Pole","Skis","Hockey Stick","Fishing Pole",
        "I've Been Everywhere","I'm Gonna Be","Travelin' Man","A Thousand Miles","Highway To Hell","Hit The Road Jack","Sweet Home Alabama",
        "Indiana Jones","Bear Grylls","Tintin","Nathan Drake","Lara Croft","Steve Irwin","Marco Polo",
        "Adventure","Experience","Journey","Mission","Quest","Enterprise","Expedition",
        "Draw","Paint","Sketch","Scribble","Sculpt","Silhouette","Doodle",
        "Cat","Dog","Fox","Pig","Rabbit","Squirrel","Hedgehog",
        "Map","Blueprint","Brochure","Atlas","Design","Draft","Directions",
        "Beanie","Beret","Cowboy Hat","Sombrero","Top Hat","Fedora","Fez",
        "Soda","Water","Tea","Coffee","Juice","Milk","Lemonade",
        "Dance","Limbo","Jump","Breakdance","Backflip","Pirouette","Moonwalk"
    };

    /// <summary>
    /// Returns seven prompts for a given index
    /// </summary>
    /// <param name="index">Index of the prompt group</param>
    /// <returns>Seven similar prompts</returns>
    public string[] GetPrompt(int index)
    {
        Debug.Log("Retrieving prompt " + index);
        var result = new string[7];
        for (var i = 0; i < 7; i++)
        {
            result[i] = promts[index * 7 + i];
        }

        return result;
    }

    private int GetNumPrompts()
    {
        return promts.Length / 7;
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
            Debug.Log(result[i]);
        }

        return result;
    }

    public int[] GetPromptSequenceForRound(int[] promptSequence, int round)
    {
        Debug.Log("Prompt sequence length: " + promptSequence.Length);
        
        var numPromptsPerRound = 4;
        var numPlayersPerPrompt = 2;
        var numTotalPerRound = numPromptsPerRound * numPlayersPerPrompt;

        var result = new int[numTotalPerRound];
        for (var p = 0; p < numPromptsPerRound; p++)
        {
            Debug.Log(p + round * numPromptsPerRound);
            result[p * numPlayersPerPrompt] = promptSequence[p + round * numPromptsPerRound];
            result[p * numPlayersPerPrompt + 1] = result[p * numPlayersPerPrompt];
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
