using Unity.Netcode;
using UnityEngine;

public struct MinimapUnitData : INetworkSerializable
{
    public Vector2 Position; // x และ z
    public byte UnitType;    // 0=Player, 1=Minion, 2=Enemy

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref UnitType);
    }
}