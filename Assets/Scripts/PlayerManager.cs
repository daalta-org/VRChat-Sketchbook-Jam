using UdonSharp;

public class PlayerManager : UdonSharpBehaviour
{
    [UdonSynced] private int prompt = -1;

    void Start()
    {
        
    }

    public void SetPrompt(int index)
    {
        prompt = index;
    }
}
