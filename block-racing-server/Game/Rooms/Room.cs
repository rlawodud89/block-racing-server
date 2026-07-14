using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Network;
using block_racing_server.Network.Packets;
using block_racing_server.Game.Simulations;
using System.Collections.Concurrent;
using System.Net.Sockets;

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

    public async Task<bool> AddPlayer(Player player)
    {
        if (_players.Count >= 2)
            return false;

        Console.WriteLine($"Room {Id}: Player {player.Id} added");
        _players.Add(player);
        player.Room = this;

        _readyMap[player.Id] = false;

        if (_players.Count == 2)
        {
            State = RoomState.Ready;
        }

        var packet = new S_MatchFoundPacket
        {
            RoomId = Id
        };

        PacketWriter writer = new((ushort)packet.PacketId);
        packet.Write(writer);
        byte[] bytes = writer.ToArray();

        await player.Session.SendAsync(bytes);

        return true;
    }

    public bool RemovePlayer(Player player)
    {
        if (!_players.Remove(player))
            return false;

        player.Room = null;
        player.MatchState = MatchState.None;

        if (_players.Count == 1)
        {
            Player remain = _players[0];

            switch (State)
            {
                case RoomState.Ready:
                case RoomState.Starting:
                    // TODO : MatchCanceledPacket
                    remain.MatchState = MatchState.None;
                    break;

                case RoomState.Playing:
                    // TODO : GameEndPacket (Win)
                    State = RoomState.Ended;
                    break;
            }
        }

        if (_players.Count == 0)
        {
            // TODO : RoomManager.RemoveRoom(this);
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

        State = RoomState.Playing;
    }

    public void Update()
    {
        if (State != RoomState.Playing)
            return;


        if (_simulation == null)
            return;


        if (_simulation.IsGameEnd)
            return;


        _simulation.Update(0.05f);


        Sync();
    }

    private void Sync()
    {
        foreach (var p in _players)
        {
            // 상태 데이터 패킷 전송
        }
    }

    public void EnqueueInput(Player player, InputType type)
    {
        _simulation?.EnqueueInput(
            new PlayerInputCommand(player, type)
        );
    }

}