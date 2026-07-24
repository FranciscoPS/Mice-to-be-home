using UnityEngine;

namespace MiceToBeHome
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static readonly Color BackgroundColor = new Color(0.15f, 0.12f, 0.12f);

        [SerializeField] private float pitch = 55f;
        [SerializeField] private float smoothTime = 0.15f;

        private Camera view;
        private Transform followTarget;
        private Vector3 gridCenter;
        private Vector3 smoothVelocity;
        private float frameDistance;
        private float followDistance;
        private bool following;
        private bool ready;

        private void Awake()
        {
            view = GetComponent<Camera>();
        }

        public void Initialize(BalanceSettings balance, Vector3 center)
        {
            gridCenter = center;

            view.orthographic = false;
            view.fieldOfView = 45f;
            view.nearClipPlane = 0.1f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = BackgroundColor;

            float extent = Mathf.Max(balance.gridColumns, balance.gridRows) * balance.cellSize;
            frameDistance = extent * 1.15f + 3f;
            followDistance = extent * 0.55f + 3f;

            ready = true;
            SnapTo(gridCenter, frameDistance);
        }

        public void FrameGrid()
        {
            following = false;
        }

        public void Follow(Transform target)
        {
            followTarget = target;
            following = true;
        }

        private void LateUpdate()
        {
            if (view == null || !ready)
            {
                return;
            }

            Vector3 focus = following && followTarget != null ? followTarget.position : gridCenter;
            float distance = following ? followDistance : frameDistance;
            Vector3 desired = focus + OffsetFor(distance);

            view.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            view.transform.position = following
                ? Vector3.SmoothDamp(view.transform.position, desired, ref smoothVelocity, smoothTime)
                : desired;
        }

        private void SnapTo(Vector3 focus, float distance)
        {
            view.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            view.transform.position = focus + OffsetFor(distance);
        }

        private Vector3 OffsetFor(float distance)
        {
            float radians = pitch * Mathf.Deg2Rad;
            return new Vector3(0f, Mathf.Sin(radians) * distance, -Mathf.Cos(radians) * distance);
        }
    }
}
