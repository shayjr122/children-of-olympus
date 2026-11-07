using UnityEngine;

namespace GMR.Core.Enemies
{
    /// <summary>What Player-side systems (Aim Assist, later hit resolvers) may know about an enemy.</summary>
    public interface IEnemyTarget
    {
        bool IsAlive { get; }
        Vector2 Position { get; }
    }
}
