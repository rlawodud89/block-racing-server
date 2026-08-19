using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Game.Rules;
using block_racing_server.Game.Simulations;
using block_racing_common.Network;
using block_racing_common.Network.Packets;
using block_racing_common.Game.Snapshots;
using block_racing_common.Game.Enums;

namespace block_racing_server.Game.Rooms;

public class Room
{
    public int Id { get; }


    public RoomState State { get; private set; } = RoomState.Waiting;

    private readonly List<Player> _players = new();

    private readonly Dictionary<int, bool> _readyMap = new();

    private readonly object _lock = new();

    private GameSimulation? _simulation;


    public Room(int id)
    {
        Id = id;
    }

    public IReadOnlyList<Player> Players => _players;

    public bool AddPlayer(Player player)
    {
        if (State != RoomState.Waiting)
            return false;

        if (_players.Count >= 2)
            return false;

        if (player.Room != null)
            return false;

        //if (player.MatchState != MatchState.Matching)
        //    return false;

        Console.WriteLine($"Room {Id}: Player {player.Id} added");

        _players.Add(player);
        player.Room = this;
        player.MatchState = MatchState.InRoom;

        _readyMap[player.Id] = false;

        if (_players.Count == 2)
        {
            State = RoomState.Ready;
        }

        //var packet = new S_MatchFoundPacket
        //{
        //    RoomId = Id
        //};

        //await player.Session.SendAsync(packet);

        return true;
    }

    public async Task<bool> RemovePlayerAsync(Player player)
    {
        if (!_players.Remove(player))
            return false;

        _readyMap.Remove(player.Id);

        player.Room = null;
        player.MatchState = MatchState.None;

        if (_players.Count == 0)
        {
            State = RoomState.Ended;
            return true;
        }

        Player remain = _players[0];

        switch (State)
        {
            case RoomState.Ready:
            case RoomState.Starting:
                {
                    var packet = new S_MatchCanceledPacket();

                    await remain.Session.SendAsync(packet);

                    remain.Room = null;
                    remain.MatchState = MatchState.None;

                    State = RoomState.Ended;
                    break;
                }

            case RoomState.Playing:
                {
                    await EndGame(
                        new GameEndResult(
                            winner: remain,
                            loser: player,
                            reason: GameEndReason.OpponentDisconnected
                        )
                    );

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
                return;

            _readyMap[player.Id] = true;

            if (_readyMap.Values.All(v => v))
            {
                State = RoomState.Starting;
                shouldStart = true;
            }
        }

        if (shouldStart)
        {
            _ = StartGameSync();
        }
    }

    private async Task StartGameSync()
    {
        Console.WriteLine($"Room {Id} START GAME SYNC");

        GameState gameState = new();

        foreach (Player player in _players)
        {
            gameState.AddPlayer(player);
        }

        _simulation = new GameSimulation(gameState);
        _simulation.Initialize();

        var packet = new S_StartGamePacket
        {
            RoomId = Id
        };

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);
        byte[] bytes = writer.ToArray();

        foreach (var player in _players)
        {
            await player.Session.SendAsync(bytes);
        }

        _ = BeginAfterCountdown(packet.CountdownSeconds);
    }

    private async Task BeginAfterCountdown(float seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));

        State = RoomState.Playing;
    }

    public async Task Update()
    {
        if (State != RoomState.Playing)
            return;


        if (_simulation == null)
            return;


        if (_simulation.IsGameEnd)
            return;


        GameEndResult? result = _simulation.Update(0.05f);

        if (result != null)
        {
            await EndGame(result);
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


        foreach (Player player in _players)
        {
            await player.Session.SendAsync(bytes);
        }
    }

    private async Task EndGame(GameEndResult result)
    {
        Console.WriteLine(
            $"Room {Id} GAME END: " +
            $"Winner={result.Winner?.Id}, " +
            $"Loser={result.Loser?.Id}");

        foreach (Player player in _players)
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

        // 게임 종료 후 플레이어를 다시 매칭 가능한 상태로 변경
        foreach (Player player in _players)
        {
            player.Room = null;
            player.MatchState = MatchState.None;
        }

        State = RoomState.Ended;
    }

    public void EnqueueInput(Player player, InputType type)
    {
        _simulation?.EnqueueInput(
            new PlayerInputCommand(player, type)
        );
    }

}