using UnityEngine;

namespace MiceToBeHome
{
    public static class ActorPhysics
    {
        private static PhysicsMaterial frictionless;

        public static PhysicsMaterial Frictionless
        {
            get
            {
                if (frictionless == null)
                {
                    frictionless = new PhysicsMaterial("MiceFrictionless")
                    {
                        dynamicFriction = 0f,
                        staticFriction = 0f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounciness = 0f,
                        bounceCombine = PhysicsMaterialCombine.Minimum
                    };
                }
                return frictionless;
            }
        }

        public static void ApplyTo(Collider collider)
        {
            if (collider != null && collider.sharedMaterial == null)
            {
                collider.sharedMaterial = Frictionless;
            }
        }
    }
}
