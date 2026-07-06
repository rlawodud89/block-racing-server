using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using block_racing_server.Game.Players;

namespace block_racing_server.Game.Rooms;

public class Room
{
    public int Id { get; }

    private readonly List<Player> _players = new();

    public bool IsStarted { get; private set; }

    public Room(int id)
    {
        Id = id;
    }

    public IReadOnlyList<Player> Players => _players;

    public bool AddPlayer(Player player)
    {
        if (_players.Count >= 2)
            return false;

        _players.Add(player);
        player.Room = this;

        if (_players.Count == 2)
        {
            StartGame();
        }

        return true;
    }

    private void StartGame()
    {
        IsStarted = true;

        Console.WriteLine($"Room {Id} Game Start!");
    }

    public void Update()
    {
        
    }
}