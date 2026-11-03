namespace GMR.Runtime.Abilities
{
    /// <summary>
    /// Turns one HitShape into hits (AD-8): one resolver per shape. Begin runs when Active starts, Tick every frame
    /// of Active (including the frame it ends), so melee shapes follow the moving Hero.
    /// </summary>
    public interface IHitResolver
    {
        void Begin(AbilityCast cast, HitOrigin origin, float now);
        void Tick(AbilityCast cast, HitOrigin origin, float now);
    }
}
