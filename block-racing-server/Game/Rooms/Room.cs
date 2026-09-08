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
using System.Diagnostics;

namespace block_racing_server.Game.Rooms;

public class Room
{
    public long Id { get; }

    public string? Code { get; private set; }


    public RoomState State { get; private set; } = RoomState.Waiting;

    private readonly List<Player> _players = new();

    private readonly Dictionary<long, bool> _readyMap = new();
    private readonly Dictionary<long, bool> _rematchMap = new();

    private readonly object _lock = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Room> _logger;

    private GameSimulation? _simulation;

    private const float TickDeltaTime = 0.05f;
    private const int CountdownTicks = 60; // 3초, 1초에 20 Tick

    private long _currentTick;
    private long _startTick;


    public Room(long id, ILoggerFactory loggerFactory, string? code = null)
    {
        Id = id;
        Code = code;

        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<Room>();
    }

    public IReadOnlyList<Player> Players => _players;

    public Task<bool> AddPlayer(Player player)
    {
        if (State != RoomState.Waiting)
        {
            _logger.LogWarning(
                "Failed to add player because room is not waiting. RoomId={RoomId} PlayerId={PlayerId} State={State}",
                Id,
                player.Id,
                State);

            return Task.FromResult(false);
        }

        if (_players.Count >= 2)
        {
            _logger.LogWarning(
                "Failed to add player because room is full. RoomId={RoomId} PlayerId={PlayerId}",
                Id,
                player.Id);

            return Task.FromResult(false);
        }

        if (player.Room != null)
        {
            _logger.LogWarning(
                "Failed to add player because player is already in a room. RoomId={RoomId} PlayerId={PlayerId} ExistingRoomId={ExistingRoomId}",
                Id,
                player.Id,
                player.Room.Id);

            return Task.FromResult(false);
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

            _ = SendRoomReadyAsync();
        }

        return Task.FromResult(true);
    }

    public async Task<bool> RemovePlayerAsync(Player player)
    {
        if (!_players.Remove(player))
        {
            _logger.LogWarning(
                "Failed to remove player because player is not in room. RoomId={RoomId} PlayerId={PlayerId}",
                Id,
                player.Id);

            return false;
        }

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

        Player remain = _players[0];

        switch (State)
        {
            case RoomState.Ready:
            case RoomState.Starting:
                {
                    _logger.LogInformation(
                        "Game canceled because opponent left. RoomId={RoomId} RemainingPlayerId={PlayerId} State={State}",
                        Id,
                        remain.Id,
                        State);

                    var packet = new S_GameCanceledPacket();

                    await remain.Session.SendAsync(packet);

                    remain.Room = null;
                    remain.MatchState = MatchState.None;

                    State = RoomState.Closing;
                    break;
                }

            case RoomState.Playing:
                {
                    _logger.LogInformation(
                        "Game ended because opponent disconnected. RoomId={RoomId} WinnerPlayerId={WinnerPlayerId} LoserPlayerId={LoserPlayerId}",
                        Id,
                        remain.Id,
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

                    State = RoomState.Closing;

                    break;
                }

            case RoomState.Result:
                {
                    _logger.LogInformation(
                        "Player exited after game result. RoomId={RoomId} RemainingPlayerId={PlayerId}",
                        Id,
                        remain.Id);

                    // 상대가 나갔음을 알림
                    var packet = new S_OpponentExitPacket();

                    await remain.Session.SendAsync(packet);

                    remain.Room = null;
                    remain.MatchState = MatchState.None;

                    State = RoomState.Closing;
                    break;
                }
        }

        return true;
    }



    public void SetReady(Player player)
    {
        bool shouldStart = false;

        lock (_lock)
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

        if (shouldStart)
        {
            _ = StartGameSync();
        }
    }

    private async Task StartGameSync()
    {
        _logger.LogInformation(
             "Initializing game simulation. RoomId={RoomId}",
             Id);

        GameState gameState = new();

        foreach (Player player in _players)
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

        var packet = new S_StartGamePacket
        {
            RoomId = Id,
            StartTick = _startTick,
            ShootCooldownTime = GameBalance.PieceCooldownTime
        };

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);

        byte[] bytes = writer.ToArray();

        foreach (Player player in _players)
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

                await Sync();
                return;
            }

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

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);
        byte[] bytes = writer.ToArray();

        Player[] players = _players.ToArray();
        foreach (Player player in players)
        {
            await player.Session.SendAsync(bytes);
        }
    }

    public void EnqueueInput(Player player, InputType type)
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

        _simulation?.EnqueueInput(
            new PlayerInputCommand(player, type)
        );
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

        ResetRematchState();

        State = RoomState.Result;

        _logger.LogInformation(
            "Room entered result state. RoomId={RoomId}",
            Id);
    }



    public void RequestRematch(Player player)
    {
        bool shouldRestart = false;

        lock (_lock)
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

        if (shouldRestart)
        {
            _ = RestartRoomAsync();
        }
    }

    private async Task RestartRoomAsync()
    {
        _logger.LogInformation(
            "Restarting room. RoomId={RoomId}",
            Id);

        ResetReadyState();
        ResetRematchState();

        await SendRoomReadyAsync();
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

    private async Task SendRoomReadyAsync()
    {
        _logger.LogDebug(
            "Sending room ready packet. RoomId={RoomId} PlayerCount={PlayerCount}",
            Id,
            _players.Count);

        var packet = new S_RoomReadyPacket
        {
            RoomId = Id
        };

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);

        byte[] bytes = writer.ToArray();

        Player[] players = _players.ToArray();

        foreach (Player player in players)
        {
            await player.Session.SendAsync(bytes);
        }
    }

}