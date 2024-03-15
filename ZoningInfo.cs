using System;
using Colossal.Serialization.Entities;
using Game.Zones;
using Unity.Entities;
using Unity.Mathematics;

namespace ZoningToolkit.Components
{
    public enum ZoningMode : uint
    {
        Left,
        Right,
        Default,
        None
    }
    public struct ZoningInfo : IComponentData, IQueryTypeParameter, IEquatable<ZoningInfo>, ISerializable
    {
        public ZoningMode zoningMode;
        public ValidArea validArea;
        public int2 m_Size;

        public bool Equals(ZoningInfo other) => this.zoningMode == other.zoningMode;

        public override int GetHashCode() => this.zoningMode.GetHashCode();

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((uint)this.zoningMode);
            writer.Write(this.validArea);
            writer.Write(this.m_Size);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint readZoningMode);
            this.zoningMode = (ZoningMode)readZoningMode;
            reader.Read(out this.validArea);
            reader.Read(out this.m_Size);
        }
    }

    public struct ZoningUpdateRequired : IComponentData, IQueryTypeParameter
    {
        // Empty struct to mark entities that will need zoning updates
    }
}
