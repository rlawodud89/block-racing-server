using block_racing_server.Game.Simulations;
using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;

namespace block_racing_server.Game.Rules;

public class AttackSystem
{
    public void Update(GameState gameState)
    {
        foreach (var player in gameState.Players.Values)
        {
            UpdateLane(gameState, player.Lane);
        }
    }

    private void UpdateLane(GameState gameState, Lane lane)
    {
        while (lane.PendingAttacks.Count > 0)
        {
            AttackPiece attack = lane.PendingAttacks.Peek();

            // 아직 도착 시간이 아님
            if (attack.SpawnTick > gameState.Tick)
                break;

            // 맨 위가 막혀있음
            if (!CanSpawn(lane, attack))
                break;

            lane.SpawnAttack(attack);

            lane.PendingAttacks.Dequeue();
        }
    }

    private bool CanSpawn(Lane lane, AttackPiece attack)
    {
        foreach (var cell in attack.Piece.Cells)
        {
            int x = attack.SpawnX + cell.X;
            int y = Lane.Height - 1 + cell.Y;

            if (x < 0 || x >= Lane.Width)
                return false;

            if (y < 0 || y >= Lane.Height)
                continue;

            if (lane.HasBlock(x, y))
                return false;
        }

        return true;
    }

}