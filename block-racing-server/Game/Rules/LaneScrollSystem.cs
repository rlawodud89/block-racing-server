using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Rules;

using block_racing_server.Game.Simulations.Lanes;

public class LaneScrollSystem
{
    public float ScrollInterval { get; set; } = 0.5f;


    public void Update(Lane lane, float deltaTime)
    {
        lane.ScrollTimer += deltaTime;

        if (lane.ScrollTimer < ScrollInterval)
            return;

        Scroll(lane);

        lane.ScrollTimer = 0;
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
