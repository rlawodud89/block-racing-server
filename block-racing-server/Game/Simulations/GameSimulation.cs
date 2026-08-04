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

            List<(FlyingBlock Block, int LandingGridY)> landingBlocks = new();

            // 1. 이번 Tick에 착지하는 FlyingBlock 확인
            foreach (FlyingBlock block in lane.FlyingBlocks)
            {
                int? landingGridY =
                    CheckBlockCollision(
                        lane,
                        block,
                        deltaTime);

                if (landingGridY.HasValue)
                {
                    landingBlocks.Add(
                        (block, landingGridY.Value));
                }
            }

            // 2. 착지하지 않는 FlyingBlock만 이동
            foreach (FlyingBlock block in lane.FlyingBlocks)
            {
                bool isLanding =
                    landingBlocks.Any(
                        x => x.Block == block);

                if (isLanding)
                    continue;

                block.MoveDown(deltaTime);
            }

            // 3. 충돌한 FlyingBlock을 정확한 위치에 정착
            foreach (var landing in landingBlocks)
            {
                FlyingBlock block = landing.Block;
                int landingGridY = landing.LandingGridY;

                Console.WriteLine(
                    $"[Landing] " +
                    $"Owner:{block.OwnerId}, " +
                    $"OldY:{block.Y:F2}, " +
                    $"CurrentGridY:{block.GridY}, " +
                    $"LandingGridY:{landingGridY}");

                lane.SettleBlock(
                    block,
                    landingGridY);

                block.Finish();
            }

            // 4. FlyingBlock까지 포함해서 Line Clear 처리
            _lineClearSystem.SettleBlocksCompletingLines(lane);

            // 5. 정착된 FlyingBlock 제거
            lane.FlyingBlocks.RemoveAll(
                b => b.IsFinished);
        }
    }

    private void UpdateLaneScroll(float deltaTime)
    {
        foreach (Player player in Players.Values)
        {
            _laneScrollSystem.Update(
                player.Lane,
                player.Car.CurrentSpeed,
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

    private int? CheckBlockCollision(Lane lane, FlyingBlock block, float deltaTime)
    {
        int currentGridY = block.GridY;

        int nextGridY = (int)MathF.Floor(block.Y + block.MoveSpeed * deltaTime);

        // 현재 위치부터 이미 겹쳐 있다면
        // 현재 위치보다 한 칸 위에 정착
        if (!CanPlaceBlock(lane, block, currentGridY))
        {
            return currentGridY - 1;
        }

        // 같은 Grid 칸 안에서는 이동
        if (currentGridY == nextGridY)
            return null;

        // 다음 Grid까지 검사
        for (int gridY = currentGridY + 1; gridY <= nextGridY; gridY++)
        {
            if (CanPlaceBlock(lane, block, gridY))
            {
                continue;
            }

            return gridY - 1;
        }

        // 바닥에 도달한 경우
        if (nextGridY >= Lane.Height)
        {
            return Lane.Height - 1;
        }

        return null;
    }

    private bool CanPlaceBlock(Lane lane, FlyingBlock block, int gridY)
    {
        foreach (var cell in block.Piece.Cells)
        {
            int x = block.X + cell.X;
            int y = gridY + cell.Y;

            // 바닥을 넘어가면 배치 불가능
            if (y >= Lane.Height)
                return false;

            // 아직 위쪽에 있는 셀은 허용
            if (y < 0)
                continue;

            // 기존 Grid와 겹치면 배치 불가능
            if (lane.HasBlock(x, y))
                return false;
        }

        return true;
    }
}
