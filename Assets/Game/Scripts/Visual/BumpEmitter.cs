using UnityEngine;

namespace MiceToBeHome
{
    /// <summary>
    /// Lives on an actor (mouse / cat) with a Rigidbody. When it physically bumps a blocking
    /// furniture piece, it triggers that piece's squash-and-stretch. Impact speed scales the punch.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BumpEmitter : MonoBehaviour
    {
        [SerializeField] private float maxImpactSpeed = 8f;

        private void OnCollisionEnter(Collision collision)
        {
            FurniturePiece piece = collision.collider.GetComponentInParent<FurniturePiece>();
            if (piece == null)
            {
                return;
            }

            float strength = Mathf.Clamp(collision.relativeVelocity.magnitude / maxImpactSpeed, 0.35f, 1.2f);
            piece.Bump(strength);
        }
    }
}
