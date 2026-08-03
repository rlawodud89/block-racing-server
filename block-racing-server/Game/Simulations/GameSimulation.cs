using block_racing_server.Game.Players;
using block_racing_server.Game.Rules;
using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;
using block_racing_server.Game.Snapshots;
using block_racing_common.Game.Enums;
using block_racing_common.Game.Snapshots;
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
            player.ResetGameState();

            player.SetCurrentPiece(_pieceGenerator.Create());
        }
    }

    private IReadOnlyDictionary<int, Player> Players
        => _gameState.Players;

    public bool IsGameEnd
        => _gameState.IsGameEnd;

    public GameStateSnapshot CreateSnapshot()
    {
        return GameStateSnapshotBuilder.Create(_gameState);
    }

    public void EnqueueInput(PlayerInputCommand command)
    {
        _inputQueue.Enqueue(command);
    }

    public GameEndResult? Update(float deltaTime)
    {
        try
        {
            ProcessInput();
            UpdatePlayers(deltaTime);
            _attackSystem.Update(_gameState);
            UpdateBlockSystem(deltaTime);
            UpdateLineClear();
            UpdateLaneScroll(deltaTime);
            _collisionSystem.Update(Players);
            _gameState.UpdateTick(deltaTime);

            GameEndResult? result = _gameEndSystem.Update(_gameState);

            if (result == null)
                return null;

            _gameState.EndGame();
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GameSimulation Update Error: {ex}");
            throw;
        }
    }

    private void ProcessInput()
    {
        while (_inputQueue.TryDequeue(out var input))
        {
            switch (input.Type)
            {
                case InputType.MoveLeft:
                    Console.WriteLine($"Player {input.Player.Id} MoveLeft");
                    input.Player.MoveLeft();
                    break;

                case InputType.MoveRight:
                    Console.WriteLine($"Player {input.Player.Id} MoveRight");
                    input.Player.MoveRight();
                    break;

                case InputType.ChangeMode:
                    Console.WriteLine($"Player {input.Player.Id} ChangeMode");
                    input.Player.ChangeMode();
                    break;

                case InputType.Shoot:
                    Console.WriteLine($"Player {input.Player.Id} Shoot");
                    Shoot(input.Player);
                    break;

                case InputType.Rotate:
                    Console.WriteLine($"Player {input.Player.Id} Rotate");
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

            List<FlyingBlock> landingBlocks = new();

            // 1. 이번 Tick에 착지할 FlyingBlock 확인
            foreach (FlyingBlock block in lane.FlyingBlocks)
            {
                if (CheckBlockCollision(lane, block, deltaTime))
                {
                    landingBlocks.Add(block);
                }
            }

            // 2. 착지하지 않는 FlyingBlock만 이동
            foreach (FlyingBlock block in lane.FlyingBlocks)
            {
                if (landingBlocks.Contains(block))
                    continue;

                block.MoveDown(deltaTime);
            }

            // 3. 일반 충돌로 착지하는 블록을 먼저 Grid에 반영
            foreach (FlyingBlock block in landingBlocks)
            {
                lane.SettleBlock(block);
                block.Finish();
            }

            // 4. 아직 날아가는 블록들을 이용해서 Line Clear 가능 여부 확인
            ResolveFlyingBlockLineClear(lane);

            // 5. 끝난 FlyingBlock 제거
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

    private bool CheckBlockCollision(Lane lane, FlyingBlock block, float deltaTime)
    {
        float nextY = block.Y + block.MoveSpeed * deltaTime;


        foreach (var cell in block.Piece.Cells)
        {
            int x = block.X + cell.X;
            int y = (int)MathF.Floor(nextY) + cell.Y;


            // 바닥 도착
            if (y >= Lane.Height)
                return true;


            // 기존 블록 충돌
            if (y >= 0 && lane.HasBlock(x, y))
                return true;
        }

        return false;
    }

    private void ResolveFlyingBlockLineClear(Lane lane)
    {
        List<FlyingBlock> activeBlocks =
            lane.FlyingBlocks
                .Where(b => !b.IsFinished)
                .ToList();

        if (activeBlocks.Count == 0)
            return;

        List<int> completedLines =
            FindCompletedLines(lane, activeBlocks);

        if (completedLines.Count == 0)
            return;

        foreach (FlyingBlock block in activeBlocks)
        {
            if (!ContributesToLine(block, completedLines))
                continue;

            lane.SettleBlock(block);
            block.Finish();
        }
    }

    private List<int> FindCompletedLines(Lane lane, IReadOnlyList<FlyingBlock> flyingBlocks)
    {
        List<int> lines = new();

        for (int y = 0; y < Lane.Height; y++)
        {
            bool complete = true;

            for (int x = 0; x < Lane.Width; x++)
            {
                if (!IsOccupied(lane, flyingBlocks, x, y))
                {
                    complete = false;
                    break;
                }
            }

            if (complete)
                lines.Add(y);
        }

        return lines;
    }

    private bool IsOccupied(Lane lane, IReadOnlyList<FlyingBlock> flyingBlocks, int x, int y)
    {
        if (lane.HasBlock(x, y))
            return true;

        foreach (FlyingBlock block in flyingBlocks)
        {
            if (FlyingBlockOccupies(block, x, y))
                return true;
        }

        return false;
    }

    private bool FlyingBlockOccupies(FlyingBlock block, int x, int y)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int blockX = block.X + cell.X;
            int blockY = block.GridY + cell.Y;

            if (blockX == x && blockY == y)
                return true;
        }

        return false;
    }

    private bool ContributesToLine(FlyingBlock block, IReadOnlyList<int> completedLines)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int y = block.GridY + cell.Y;

            if (completedLines.Contains(y))
                return true;
        }

        return false;
    }
}
