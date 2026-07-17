using System.Collections.Generic;
using block_racing_common.Game.Enums;
using block_racing_common.Game.Snapshots;

namespace block_racing_common.Network.Packets
{
    public class S_GameStatePacket : IPacket
    {
        public PacketId PacketId => PacketId.S_GameState;

        public GameStateSnapshot Snapshot { get; private set; }


        public S_GameStatePacket(GameStateSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public void Read(PacketReader reader)
        {
            long tick = reader.ReadLong();

            ushort playerCount = reader.ReadUInt16();

            List<PlayerSnapshot> players = new();

            for (int i = 0; i < playerCount; i++)
            {
                players.Add(ReadPlayer(reader));
            }

            Snapshot = new GameStateSnapshot(
                tick,
                players);
        }

        private PlayerSnapshot ReadPlayer(PacketReader reader)
        {
            int id = reader.ReadInt32();


            int carX = reader.ReadInt32();

            float distance = reader.ReadFloat();

            float speed = reader.ReadFloat();


            bool stunned = reader.ReadBool();

            byte mode = reader.ReadByte();

            LaneSnapshot lane = ReadLane(reader);


            return new PlayerSnapshot(
                id,
                carX,
                distance,
                speed,
                stunned,
                (PlayMode)mode,
                lane);
        }

        private LaneSnapshot ReadLane(PacketReader reader)
        {
            ushort blockCount = reader.ReadUInt16();

            byte[] blocks = new byte[blockCount];

            for (int i = 0; i < blockCount; i++)
            {
                blocks[i] = reader.ReadByte();
            }


            List<FlyingBlockSnapshot> flyingBlocks =
                ReadFlyingBlocks(reader);


            return new LaneSnapshot(
                blocks,
                flyingBlocks);
        }

        private List<FlyingBlockSnapshot> ReadFlyingBlocks(
        PacketReader reader)
        {
            ushort count = reader.ReadUInt16();


            List<FlyingBlockSnapshot> blocks = new();


            for (int i = 0; i < count; i++)
            {
                int ownerId = reader.ReadInt32();

                int x = reader.ReadInt32();

                int y = reader.ReadInt32();


                PieceType type = (PieceType)reader.ReadByte();

                Rotation rotation = (Rotation)reader.ReadByte();


                blocks.Add(
                    new FlyingBlockSnapshot(
                        ownerId,
                        x,
                        y,
                        type,
                        rotation));
            }


            return blocks;
        }


        public void Write(PacketWriter writer)
        {
            writer.Write(Snapshot.Tick);

            writer.Write((ushort)Snapshot.Players.Count);


            foreach (var player in Snapshot.Players)
            {
                WritePlayer(writer, player);
            }
        }

        private void WritePlayer(PacketWriter writer, PlayerSnapshot player)
        {
            writer.Write(player.Id);


            writer.Write(player.CarX);

            writer.Write(player.Distance);

            writer.Write(player.Speed);


            writer.Write(player.IsStunned);

            writer.Write((byte)player.Mode);


            WriteLane(writer, player.Lane);
        }

        private void WriteLane(PacketWriter writer, LaneSnapshot lane)
        {
            writer.Write((ushort)lane.Blocks.Length);


            foreach (byte block in lane.Blocks)
            {
                writer.Write(block);
            }


            WriteFlyingBlocks(writer, lane.FlyingBlocks);
        }

        private void WriteFlyingBlocks(PacketWriter writer, IReadOnlyList<FlyingBlockSnapshot> blocks)
        {
            writer.Write((ushort)blocks.Count);


            foreach (var block in blocks)
            {
                writer.Write(block.OwnerId);

                writer.Write(block.X);

                writer.Write(block.Y);

                writer.Write((byte)block.Type);

                writer.Write((byte)block.Rotation);
            }
        }
    }
}