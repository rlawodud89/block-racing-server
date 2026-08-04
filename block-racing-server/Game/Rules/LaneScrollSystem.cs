using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Rules;


public class LaneScrollSystem
{
    private const float BaseScrollSpeed = 3f;


    public void Update(Lane lane, float carSpeed, float deltaTime)
    {
        lane.ScrollTimer += carSpeed * BaseScrollSpeed * deltaTime;

        while (lane.ScrollTimer >= 1f)
        {
            Scroll(lane);
            lane.ScrollTimer -= 1f;
        }
    }

    private void Scroll(Lane lane)
    {
        for (int y = 0; y < Lane.Height - 1; y++)
        {
            for (int x = 0; x < Lane.Width; x++)
            {
                lane.Grid[y, x].Block =
                    lane.Grid[y + 1, x].Block;
            }
        }


        for (int x = 0; x < Lane.Width; x++)
        {
            lane.Grid[Lane.Height - 1, x].Block = null;
        }
    }
}
