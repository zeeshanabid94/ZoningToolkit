using System;
using Colossal.Serialization.Entities;
using Unity.Entities;

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

        public bool Equals(ZoningInfo other) => this.zoningMode == other.zoningMode;

        public override int GetHashCode() => this.zoningMode.GetHashCode();

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write((uint)this.zoningMode);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint readZoningMode);
            this.zoningMode = (ZoningMode)readZoningMode;
        }
    }

    public struct ZoningInfoUpdated : IComponentData, IQueryTypeParameter { }
}
