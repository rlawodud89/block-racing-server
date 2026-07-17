using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Rules;

public class LineClearSystem
{
    public int ClearLines(Lane lane)
    {
        List<int> completedLines = FindCompletedLines(lane);


        if (completedLines.Count == 0)
            return 0;


        foreach (int line in completedLines)
        {
            lane.ClearLine(line);
        }


        return completedLines.Count;
    }



    private List<int> FindCompletedLines(Lane lane)
    {
        List<int> lines = new();


        for (int y = 0; y < Lane.Height; y++)
        {
            bool complete = true;


            for (int x = 0; x < Lane.Width; x++)
            {
                if (lane.Grid[y, x].Block == null)
                {
                    complete = false;
                    break;
                }
            }


            if (complete)
            {
                lines.Add(y);
            }
        }


        return lines;
    }
}
