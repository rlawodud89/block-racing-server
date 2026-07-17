using block_racing_server.Game.Players;
using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Rules;

public class CollisionSystem
{
    public void Update(
        IReadOnlyDictionary<int, Player> players)
    {
        foreach (Player player in players.Values)
        {
            CheckCarCollision(player);
        }
    }


    private void CheckCarCollision(Player player)
    {
        Car car = player.Car;
        Lane lane = player.Lane;


        if (car.IsInvincible)
            return;

        int y = 0;

        for (int x = car.X;
            x < car.X + Car.Width;
            x++)
        {
            if (lane.HasBlock(x, y))
            {
                player.Car.OnCollision();
                return;
            }
        }
    }
}