using UnityEngine;

namespace MiceToBeHome
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Billboard : MonoBehaviour
    {
        [SerializeField] private int sortingScale = 100;

        private Transform cameraTransform;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Camera.main == null)
                {
                    return;
                }
                cameraTransform = Camera.main.transform;
            }

            transform.rotation = cameraTransform.rotation;

            float depth = Vector3.Dot(transform.position - cameraTransform.position, cameraTransform.forward);
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-depth * sortingScale);
        }
    }
}
