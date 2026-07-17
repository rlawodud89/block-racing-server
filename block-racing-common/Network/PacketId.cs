
namespace block_racing_common.Network
{
    public enum PacketId : ushort
    {
        // Client to Server
        C_Login = 0,
        C_MatchRequest = 1,
        C_Ready = 2,
        C_Input = 3,

        // Server to Client
        S_Login = 100,
        S_MatchFound = 101,
        S_StartGame = 102,
        S_GameState = 103,
    }
}
