using block_racing_common.Game.Enums;
using block_racing_common.Game.Snapshots;
using block_racing_common.Network;
using block_racing_common.Network.Packets;
using block_racing_server.Data;
using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rules;
using block_racing_server.Game.Simulations;
using Microsoft.Extensions.Logging;

namespace block_racing_server.Game.Rooms;

public class Room
{
    public long Id { get; }

    public string? Code { get; private set; }

    public RoomState State { get; private set; } = RoomState.Waiting;

    private readonly List<Player> _players = new();

    private readonly Dictionary<long, bool> _readyMap = new();
    private readonly Dictionary<long, bool> _rematchMap = new();

    // PlayerSession에서 접근하는 Room 상태 변경을 직렬화
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Room> _logger;

    private GameSimulation? _simulation;

    private const float TickDeltaTime = 0.05f;
    private const int CountdownTicks = 60; // 3초, 1초에 20 Tick

    private long _currentTick;
    private long _startTick;

    private readonly PacketWriter _syncWriter;


    public Room(long id, ILoggerFactory loggerFactory, string? code = null)
    {
        Id = id;
        Code = code;

        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<Room>();

        _syncWriter = new PacketWriter(
            (ushort)PacketId.S_GameState
        );
    }

    public IReadOnlyList<Player> Players => _players;


    public async Task<bool> AddPlayer(Player player)
    {
        bool shouldSendRoomReady = false;

        await _stateLock.WaitAsync();

        try
        {
            if (State != RoomState.Waiting)
            {
                _logger.LogWarning(
                    "Failed to add player because room is not waiting. RoomId={RoomId} PlayerId={PlayerId} State={State}",
                    Id,
                    player.Id,
                    State);

                return false;
            }

            if (_players.Count >= 2)
            {
                _logger.LogWarning(
                    "Failed to add player because room is full. RoomId={RoomId} PlayerId={PlayerId}",
                    Id,
                    player.Id);

                return false;
            }

            if (player.Room != null)
            {
                _logger.LogWarning(
                    "Failed to add player because player is already in a room. RoomId={RoomId} PlayerId={PlayerId} ExistingRoomId={ExistingRoomId}",
                    Id,
                    player.Id,
                    player.Room.Id);

                return false;
            }

            _players.Add(player);

            player.Room = this;
            player.MatchState = MatchState.InRoom;

            _readyMap[player.Id] = false;
            _rematchMap[player.Id] = false;

            _logger.LogInformation(
                "Player added to room. RoomId={RoomId} PlayerId={PlayerId} PlayerCount={PlayerCount}",
                Id,
                player.Id,
                _players.Count);

            if (_players.Count == 2)
            {
                State = RoomState.Ready;
                shouldSendRoomReady = true;

                _logger.LogInformation(
                    "Room is ready. RoomId={RoomId} PlayerCount={PlayerCount}",
                    Id,
                    _players.Count);
            }
        }
        finally
        {
            _stateLock.Release();
        }

        if (shouldSendRoomReady)
        {
            await SendRoomReadyAsync(_players.ToArray());
        }

        return true;
    }


    public async Task<bool> RemovePlayerAsync(Player player)
    {
        Player? remain = null;
        RoomState previousState = RoomState.Waiting;

        await _stateLock.WaitAsync();

        try
        {
            if (!_players.Remove(player))
            {
                _logger.LogWarning(
                    "Failed to remove player because player is not in room. RoomId={RoomId} PlayerId={PlayerId}",
                    Id,
                    player.Id);

                return false;
            }

            previousState = State;

            _logger.LogInformation(
                "Player removed from room. RoomId={RoomId} PlayerId={PlayerId} State={State}",
                Id,
                player.Id,
                State);

            _readyMap.Remove(player.Id);
            _rematchMap.Remove(player.Id);

            player.Room = null;
            player.MatchState = MatchState.None;

            if (_players.Count == 0)
            {
                State = RoomState.Closing;

                _logger.LogInformation(
                    "Room is empty and closing. RoomId={RoomId}",
                    Id);

                return true;
            }

            remain = _players[0];

            // Room이 종료되는 상황임을 먼저 확정한다.
            // 실제 SendAsync는 Lock을 해제한 후 수행한다.
            State = RoomState.Closing;
        }
        finally
        {
            _stateLock.Release();
        }


        switch (previousState)
        {
            case RoomState.Ready:
            case RoomState.Starting:
                {
                    _logger.LogInformation(
                        "Game canceled because opponent left. RoomId={RoomId} RemainingPlayerId={PlayerId} State={State}",
                        Id,
                        remain!.Id,
                        previousState);

                    var packet = new S_GameCanceledPacket();

                    await remain.Session.SendAsync(packet);

                    remain.Room = null;
                    remain.MatchState = MatchState.None;

                    break;
                }

            case RoomState.Playing:
                {
                    _logger.LogInformation(
                        "Game ended because opponent disconnected. RoomId={RoomId} WinnerPlayerId={WinnerPlayerId} LoserPlayerId={LoserPlayerId}",
                        Id,
                        remain!.Id,
                        player.Id);

                    await EndGame(
                        new GameEndResult(
                            winner: remain,
                            loser: player,
                            reason: GameEndReason.OpponentDisconnected
                        )
                    );

                    remain.Room = null;
                    remain.MatchState = MatchState.None;

                    break;
                }

            case RoomState.Result:
                {
                    _logger.LogInformation(
                        "Player exited after game result. RoomId={RoomId} RemainingPlayerId={PlayerId}",
                        Id,
                        remain!.Id);

                    var packet = new S_OpponentExitPacket();

                    await remain.Session.SendAsync(packet);

                    remain.Room = null;
                    remain.MatchState = MatchState.None;

                    break;
                }
        }

        return true;
    }


    public async Task SetReady(Player player)
    {
        bool shouldStart = false;

        await _stateLock.WaitAsync();

        try
        {
            if (State != RoomState.Ready)
            {
                _logger.LogWarning(
                    "Ready request ignored because room is not ready. RoomId={RoomId} PlayerId={PlayerId} State={State}",
                    Id,
                    player.Id,
                    State);

                return;
            }

            if (!_players.Contains(player))
            {
                _logger.LogWarning(
                    "Ready request ignored because player is not in room. RoomId={RoomId} PlayerId={PlayerId}",
                    Id,
                    player.Id);

                return;
            }

            _readyMap[player.Id] = true;

            _logger.LogInformation(
                "Player is ready. RoomId={RoomId} PlayerId={PlayerId}",
                Id,
                player.Id);

            if (_readyMap.Values.All(v => v))
            {
                State = RoomState.Starting;
                shouldStart = true;

                _logger.LogInformation(
                    "All players are ready. Starting game countdown. RoomId={RoomId}",
                    Id);
            }
        }
        finally
        {
            _stateLock.Release();
        }

        if (shouldStart)
        {
            _ = StartGameSync();
        }
    }


    private async Task StartGameSync()
    {
        Player[] players;

        await _stateLock.WaitAsync();

        try
        {
            if (State != RoomState.Starting)
            {
                _logger.LogDebug(
                    "Game start canceled because room state changed. RoomId={RoomId} State={State}",
                    Id,
                    State);

                return;
            }

            if (_players.Count != 2)
            {
                _logger.LogDebug(
                    "Game start canceled because player count is invalid. RoomId={RoomId} PlayerCount={PlayerCount}",
                    Id,
                    _players.Count);

                return;
            }

            players = _players.ToArray();


            GameState gameState = new();

            foreach (Player player in players)
            {
                gameState.AddPlayer(player);
            }

            _simulation = new GameSimulation(gameState, _loggerFactory);
            _simulation.Initialize();

            _startTick = _currentTick + CountdownTicks;

            _logger.LogInformation(
                "Game start scheduled. RoomId={RoomId} CurrentTick={CurrentTick} StartTick={StartTick}",
                Id,
                _currentTick,
                _startTick);
        }
        finally
        {
            _stateLock.Release();
        }

        var packet = new S_StartGamePacket
        {
            RoomId = Id,
            StartTick = _startTick,
            ShootCooldownTime = GameBalance.PieceCooldownTime
        };

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);

        byte[] bytes = writer.ToArray();

        foreach (Player player in players)
        {
            await player.Session.SendAsync(bytes);
        }
    }


    public async Task Update(long currentTick)
    {
        _currentTick = currentTick;

        if (_simulation == null)
            return;

        if (State == RoomState.Starting)
        {
            if (currentTick < _startTick)
            {
                _simulation.SetTick(currentTick);

                if (State != RoomState.Closing)
                    await Sync();

                return;
            }

            if (State != RoomState.Starting)
                return;

            State = RoomState.Playing;

            _logger.LogInformation(
                "Game started. RoomId={RoomId} Tick={Tick}",
                Id,
                currentTick);
        }

        if (State != RoomState.Playing)
            return;

        if (_simulation.IsGameEnd)
            return;

        GameEndResult? result =
            _simulation.Update(currentTick, TickDeltaTime);

        if (State == RoomState.Closing)
            return;

        if (result != null)
        {
            await HandleGameEndAsync(result);
            return;
        }

        await Sync();
    }


    private async Task Sync()
    {
        if (_simulation == null)
            return;

        GameStateSnapshot snapshot =
            _simulation.CreateSnapshot();

        S_GameStatePacket packet = new(snapshot);

        _syncWriter.Reset((ushort)packet.PacketId);
        packet.Write(_syncWriter);

        byte[] bytes = _syncWriter.ToArray();

        Player[] players = _players.ToArray();

        foreach (Player player in players)
        {
            await player.Session.SendAsync(bytes);
        }
    }


    public async Task EnqueueInput(Player player, InputType type)
    {
        await _stateLock.WaitAsync();

        try
        {
            if (State != RoomState.Playing)
            {
                _logger.LogDebug(
                    "Input ignored because room is not playing. RoomId={RoomId} PlayerId={PlayerId} State={State} InputType={InputType}",
                    Id,
                    player.Id,
                    State,
                    type);

                return;
            }

            if (!_players.Contains(player))
            {
                _logger.LogWarning(
                    "Input ignored because player is not in room. RoomId={RoomId} PlayerId={PlayerId}",
                    Id,
                    player.Id);

                return;
            }

            _simulation?.EnqueueInput(
                new PlayerInputCommand(player, type)
            );
        }
        finally
        {
            _stateLock.Release();
        }
    }


    private async Task EndGame(GameEndResult result)
    {
        _logger.LogInformation(
            "Game ended. RoomId={RoomId} Winner={Winner} Loser={Loser} Reason={Reason}",
            Id,
            result.Winner?.Id,
            result.Loser?.Id,
            result.Reason);

        Player[] players = _players.ToArray();

        foreach (Player player in players)
        {
            GameResultType gameResult;

            if (result.Winner == null && result.Loser == null)
            {
                gameResult = GameResultType.Draw;
            }
            else if (result.Winner?.Id == player.Id)
            {
                gameResult = GameResultType.Win;
            }
            else
            {
                gameResult = GameResultType.Lose;
            }

            var packet = new S_GameEndPacket
            {
                Result = gameResult,
                Reason = result.Reason
            };

            await player.Session.SendAsync(packet);
        }
    }


    private async Task HandleGameEndAsync(GameEndResult result)
    {
        await EndGame(result);

        await _stateLock.WaitAsync();

        try
        {
            // 게임 종료 처리 중 플레이어가 나갔다면
            // Room은 이미 Closing 상태이므로 Result로 변경하지 않는다.
            if (State == RoomState.Closing)
            {
                return;
            }

            ResetRematchState();

            State = RoomState.Result;

            _logger.LogInformation(
                "Room entered result state. RoomId={RoomId}",
                Id);
        }
        finally
        {
            _stateLock.Release();
        }
    }


    public async Task RequestRematch(Player player)
    {
        bool shouldRestart = false;

        await _stateLock.WaitAsync();

        try
        {
            if (State != RoomState.Result)
            {
                _logger.LogWarning(
                    "Rematch request ignored because room is not in result state. RoomId={RoomId} PlayerId={PlayerId} State={State}",
                    Id,
                    player.Id,
                    State);

                return;
            }

            if (!_players.Contains(player))
            {
                _logger.LogWarning(
                    "Rematch request ignored because player is not in room. RoomId={RoomId} PlayerId={PlayerId}",
                    Id,
                    player.Id);

                return;
            }

            _rematchMap[player.Id] = true;

            _logger.LogInformation(
                "Player requested rematch. RoomId={RoomId} PlayerId={PlayerId}",
                Id,
                player.Id);

            if (_rematchMap.Values.All(v => v))
            {
                State = RoomState.Ready;
                shouldRestart = true;

                _logger.LogInformation(
                    "All players requested rematch. Room restarting. RoomId={RoomId}",
                    Id);
            }
        }
        finally
        {
            _stateLock.Release();
        }

        if (shouldRestart)
        {
            _ = RestartRoomAsync();
        }
    }


    private async Task RestartRoomAsync()
    {
        Player[] players;

        await _stateLock.WaitAsync();

        try
        {
            if (State != RoomState.Ready)
            {
                _logger.LogDebug(
                    "Room restart canceled because room state changed. RoomId={RoomId} State={State}",
                    Id,
                    State);

                return;
            }

            if (_players.Count != 2)
            {
                _logger.LogDebug(
                    "Room restart canceled because player count is invalid. RoomId={RoomId} PlayerCount={PlayerCount}",
                    Id,
                    _players.Count);

                return;
            }

            ResetReadyState();
            ResetRematchState();

            players = _players.ToArray();
        }
        finally
        {
            _stateLock.Release();
        }

        await SendRoomReadyAsync(players);
    }


    private void ResetReadyState()
    {
        foreach (Player player in _players)
        {
            _readyMap[player.Id] = false;
        }
    }


    private void ResetRematchState()
    {
        foreach (Player player in _players)
        {
            _rematchMap[player.Id] = false;
        }
    }


    private async Task SendRoomReadyAsync(Player[] players)
    {
        _logger.LogDebug(
            "Sending room ready packet. RoomId={RoomId} PlayerCount={PlayerCount}",
            Id,
            players.Length);

        var packet = new S_RoomReadyPacket
        {
            RoomId = Id
        };

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);

        byte[] bytes = writer.ToArray();

        foreach (Player player in players)
        {
            await player.Session.SendAsync(bytes);
        }
    }
}