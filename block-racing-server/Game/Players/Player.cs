using block_racing_server.Game.Matchs;
using block_racing_server.Game.Rooms;
using block_racing_server.Game.Simulations.Blocks;
using block_racing_server.Game.Simulations.Lanes;
using block_racing_server.Network;
using block_racing_common.Game.Enums;

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

    public float PieceCooldown { get; private set; }

    private const float PieceCooldownTime = 1.5f;


    public Lane Lane { get; }

    public PlayMode Mode { get; private set; }

    public Player(PlayerSession session, int id, string nickname)
    {
        Session = session;
        Id = id;
        NickName = nickname;

        Lane = new Lane();
        Car = new Car(1);
    }

    public void ResetGameState()
    {
        // 자동차 상태 초기화
        Car.Reset();

        // 현재 들고 있는 블록 초기화
        CurrentPiece = null;

        // 블록 생성 쿨다운 초기화
        PieceCooldown = 0f;

        // 플레이 모드 초기화
        Mode = PlayMode.Defense;

        // 레인 상태 초기화
        Lane.Reset();

        // 입력 상태 초기화
        Input.Clear();
    }

    public void Update(float deltaTime)
    {
        if (CurrentPiece != null)
            return;

        PieceCooldown -= deltaTime;
    }

    public bool CanCreatePiece =>
        CurrentPiece == null && PieceCooldown <= 0;


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

        PieceCooldown = PieceCooldownTime;

        return piece;
    }
}
