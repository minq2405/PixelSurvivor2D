using UnityEngine;

namespace PixelSurvivor.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce = 0f);
        Transform Transform { get; }
        bool IsAlive { get; }
    }
}
