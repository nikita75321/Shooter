using System;
using System.Collections.Generic;

[System.Serializable]
public class Hero
{
    public int level = 1;
    public int rank = 1;
    public int heroCard = 0;
    public int[] openSkinBody = new int[9] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
    public int currentBody;
    public int[] openSkinHead = new int[9] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
    public int currentHead;

    public Dictionary<string, object> ToLevelsJson()
    {
        return new Dictionary<string, object>
        {
            {"level", level},
            {"rank", rank}
        };
    }

    public void LoadFromLevelsJson(Dictionary<string, object> data)
    {
        level = data.TryGetValue("level", out var lvl) ? Convert.ToInt32(lvl) : 1;
        rank = data.TryGetValue("rank", out var rnk) ? Convert.ToInt32(rnk) : 1;
    }
}