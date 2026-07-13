using block_racing_server.Game.Players;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace block_racing_server.Game.Simulations;

public class GameSimulation
{
    private readonly GameState _gameState;


    private readonly ConcurrentQueue<PlayerInputCommand> _inputQueue
        = new();

    private IReadOnlyDictionary<int, Player> Players
        => _gameState.Players;

    public GameSimulation(GameState gameState)
    {
        _gameState = gameState;
    }


    public bool IsGameEnd
        => _gameState.IsGameEnd;


    public void EnqueueInput(PlayerInputCommand command)
    {
        _inputQueue.Enqueue(command);
    }

    public void Update(float deltaTime)
    {
        ProcessInput();

        UpdatePlayers(deltaTime);

        UpdateBlockSystem();

        CheckCollision();

        _gameState.UpdateTick(deltaTime);
    }


    private void ProcessInput()
    {
        while (_inputQueue.TryDequeue(out var input))
        {
            switch (input.Type)
            {
                case InputType.MoveLeft:
                    input.Player.MoveLeft();
                    break;

                case InputType.MoveRight:
                    input.Player.MoveRight();
                    break;

                case InputType.ChangeMode:
                    input.Player.ChangeMode();
                    break;

                case InputType.Shoot:
                    Shoot(input.Player);
                    break;

                case InputType.Rotate:
                    input.Player.RotatePiece();
                    break;
            }
        }
    }

    private void UpdatePlayers(float deltaTime)
    {
        foreach (Player player in Players.Values)
        {
            player.Car.Update(deltaTime);
        }
    }

    private void UpdateBlockSystem()
    {
        foreach (Player player in Players.Values)
        {
            Lane lane = player.Lane;


            foreach (FlyingBlock block in lane.FlyingBlocks)
            {
                block.MoveDown();


                if (CheckBlockCollision(lane, block))
                {
                    SettleBlock(lane, block);

                    block.Finish();
                }
            }

            lane.FlyingBlocks.RemoveAll(
                b => b.IsFinished
            );
        }
    }

    private void CheckCollision()
    {
    }


    private void Shoot(Player player)
    {

        BlockPiece? piece = player.TakePiece();

        if (piece == null)
            return;


        if (player.Mode == PlayMode.Attack)
        {
            SendAttack(player, piece);
        }
        else
        {
            SpawnFlyingBlock(player, piece);
        }


        // 다음 블록 생성
        player.SetCurrentPiece(_gameState.CreatePiece());
    }

    private void SendAttack(Player sender, BlockPiece piece)
    {
        Player target =
            Players.Values.First(
                p => p.Id != sender.Id
            );


        target.Lane.PendingAttacks.Enqueue(
            new AttackPiece(
                piece,
                _gameState!.Tick + 30
            )
        );
    }

    private void SpawnFlyingBlock(Player player, BlockPiece piece)
    {
        FlyingBlock block =
            new FlyingBlock(
                piece,
                player.Car.X,
                0,
                player.Id
            );

        player.Lane.FlyingBlocks.Add(block);
    }

    private bool CheckBlockCollision(Lane lane, FlyingBlock block)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int x = block.X + cell.X;
            int y = block.Y + cell.Y;


            if (y >= Lane.Height)
                return true;


            if (lane.Grid[y, x].Block != null)
                return true;
        }


        return false;
    }

    private void SettleBlock(Lane lane, FlyingBlock block)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int x = block.X + cell.X;

            int y = block.Y + cell.Y;


            if (y < 0)
                continue;


            lane.Grid[y, x].Block =
                new Block(
                    BlockType.Normal,
                    block.OwnerId
                );
        }
    }
}
