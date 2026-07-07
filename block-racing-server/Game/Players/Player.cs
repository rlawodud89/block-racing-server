using block_racing_server.Game.Matchs;
using block_racing_server.Game.Rooms;
using block_racing_server.Network;

namespace block_racing_server.Game.Players;

public class Player
{
    public int Id { get; set; }

    public string NickName { get; set; } = string.Empty;

    public MatchState MatchState { get; set; } = MatchState.None;

    public PlayerInput Input { get; } = new PlayerInput();

    public int Lane { get; set; }

    public bool IsStunned { get; set; }

    public PlayerSession Session { get; }

    public Room? Room { get; set; }

    public Player(PlayerSession session, int id, string nickname)
    {
        Session = session;
        Id = id;
        NickName = nickname;
    }
}
