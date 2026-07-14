using block_racing_server.Game.Players;
using block_racing_server.Game.Rules;
using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;
using System.Collections.Concurrent;

namespace block_racing_server.Game.Simulations;

public class GameSimulation
{
    private readonly GameState _gameState;


    private readonly ConcurrentQueue<PlayerInputCommand> _inputQueue
        = new();


    private readonly PieceGenerator _pieceGenerator = new();

    private readonly LineClearSystem _lineClearSystem = new();
    private readonly LaneScrollSystem _laneScrollSystem = new();
    private readonly AttackSystem _attackSystem = new();
    private readonly CollisionSystem _collisionSystem = new();
    private readonly GameEndSystem _gameEndSystem = new();


    public GameSimulation(GameState gameState)
    {
        _gameState = gameState;
    }

    public void Initialize()
    {
        foreach (Player player in Players.Values)
        {
            player.SetCurrentPiece(_pieceGenerator.Create());

            // 앞으로 추가될 초기화들도 여기서
            // player.Car.SetSpeed(...)
            // player.Mode = PlayMode.Defense;
            // player.ResetCooldown();
        }
    }

    private IReadOnlyDictionary<int, Player> Players
        => _gameState.Players;

    public bool IsGameEnd
        => _gameState.IsGameEnd;


    public void EnqueueInput(PlayerInputCommand command)
    {
        _inputQueue.Enqueue(command);
    }

    public GameEndResult? Update(float deltaTime)
    {
        ProcessInput();

        UpdatePlayers(deltaTime);

        _attackSystem.Update(_gameState);

        UpdateBlockSystem(deltaTime);

        UpdateLineClear();

        UpdateLaneScroll(deltaTime);

        _collisionSystem.Update(Players);

        _gameState.UpdateTick(deltaTime);

        GameEndResult result =
        _gameEndSystem.Update(_gameState);

        if (result.IsGameEnd)
        {
            _gameState.EndGame();
            return result;
        }

        return null;
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

            player.Update(deltaTime);

            if (player.CanCreatePiece)
            {
                player.SetCurrentPiece(
                    _pieceGenerator.Create());
            }
        }
    }

    private void UpdateBlockSystem(float deltaTime)
    {
        foreach (Player player in Players.Values)
        {
            Lane lane = player.Lane;


            foreach (FlyingBlock block in lane.FlyingBlocks)
            {
                block.MoveDown(deltaTime);


                if (CheckBlockCollision(lane, block))
                {
                    lane.SettleBlock(block);

                    block.Finish();
                }
            }

            lane.FlyingBlocks.RemoveAll(
                b => b.IsFinished
            );
        }
    }

    private void UpdateLaneScroll(float deltaTime)
    {
        foreach (Player player in Players.Values)
        {
            _laneScrollSystem.Update(
                player.Lane,
                deltaTime);
        }
    }

    private void UpdateLineClear()
    {
        foreach (Player player in Players.Values)
        {
            _lineClearSystem.ClearLines(player.Lane);
        }
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
                sender.Car.X,
                _gameState!.Tick + 30,
                sender.Id
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
            int y = (int)MathF.Floor(block.Y) + cell.Y;


            if (y >= Lane.Height)
                return true;


            if (lane.Grid[y, x].Block != null)
                return true;
        }

        return false;
    }

}
