using block_racing_server.Game.Matchs;
using block_racing_server.Game.Players;
using block_racing_server.Network;
using block_racing_server.Network.Packets;
using System.Net.Sockets;

namespace block_racing_server.Game.Rooms;

public class Room
{
    public int Id { get; }

    public RoomState State { get; private set; } = RoomState.Waiting;

    private readonly List<Player> _players = new();

    private readonly Dictionary<int, bool> _readyMap = new();


    public Room(int id)
    {
        Id = id;
    }

    public IReadOnlyList<Player> Players => _players;

    public async Task<bool> AddPlayer(Player player)
    {
        if (_players.Count >= 2)
            return false;

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

        Console.WriteLine($"Room {Id}: Player removed");

        // 방 종료 처리
        if (_players.Count == 0)
        {

            Console.WriteLine($"Room {Id} closed (empty)");
        }

        return true;
    }

    public void SetReady(Player player)
    {
        if (State != RoomState.Ready)
            return;

        _readyMap[player.Id] = true;

        if (_readyMap.Values.All(v => v))
        {
            StartGameSync();
        }
    }

    private async void StartGameSync()
    {
        State = RoomState.Starting;

        Console.WriteLine($"Room {Id} START GAME SYNC");

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

        Tick();
        Sync();
    }

    private void Tick()
    {
        // 블록 이동, 충돌 등
    }

    private void Sync()
    {
        foreach (var p in _players)
        {
            // 상태 데이터 패킷 전송
        }
    }
}