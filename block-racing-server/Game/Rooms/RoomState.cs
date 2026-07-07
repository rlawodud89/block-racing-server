
namespace block_racing_server.Game.Rooms;

public enum RoomState
{
    Waiting,    // 대기 (2명 안 찬 상태)
    Ready,      // 2명 들어옴, 시작 대기
    Starting,   // 시작 동기화 단계
    Playing,    // 게임 진행
    Ended       // 종료
}
