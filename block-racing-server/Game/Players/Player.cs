using block_racing_server.Game.Matchs;
using block_racing_server.Game.Rooms;
using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;
using block_racing_server.Network;

namespace block_racing_server.Game.Players;

public class Player
{
    public int Id { get; set; }

    public string NickName { get; set; } = string.Empty;

    public MatchState MatchState { get; set; } = MatchState.None;

    public PlayerSession Session { get; }

    public Room? Room { get; set; }


    public PlayerInput Input { get; } = new PlayerInput();

    public Car Car { get; }

    public BlockPiece? CurrentPiece { get; set; }

    public Lane Lane { get; }

    public PlayMode Mode { get; private set; }

    public Player(PlayerSession session, int id, string nickname)
    {
        Session = session;
        Id = id;
        NickName = nickname;

        Lane = new Lane();
        Car = new Car();
    }


    public void MoveLeft()
    {
        Car.MoveLeft();
    }

    public void MoveRight()
    {
        Car.MoveRight();
    }

    public void RotatePiece()
    {
        CurrentPiece?.Rotate();
    }

    public void ChangeMode()
    {
        Mode =
            Mode == PlayMode.Attack
            ? PlayMode.Defense
            : PlayMode.Attack;
    }

    public void SetCurrentPiece(BlockPiece piece)
    {
        CurrentPiece = piece;
    }

    public BlockPiece? TakePiece()
    {
        BlockPiece? piece = CurrentPiece;

        CurrentPiece = null;

        return piece;
    }
}
