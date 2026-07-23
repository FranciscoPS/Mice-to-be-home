using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class BreadcrumbTrail : MonoBehaviour
    {
        private readonly Queue<Vector3> crumbs = new Queue<Vector3>();
        private Vector3 lastRecorded;
        private bool hasLast;
        private float spacing = 0.4f;
        private float arrive = 0.35f;
        private const int MaxCrumbs = 512;

        public void Configure(float spacingDistance, float arriveDistance)
        {
            spacing = Mathf.Max(0.05f, spacingDistance);
            arrive = Mathf.Max(0.05f, arriveDistance);
        }

        public void Clear()
        {
            crumbs.Clear();
            hasLast = false;
        }

        public void Record(Vector3 position)
        {
            if (!hasLast)
            {
                lastRecorded = position;
                hasLast = true;
                return;
            }

            if (HorizontalDistance(position, lastRecorded) >= spacing)
            {
                crumbs.Enqueue(position);
                lastRecorded = position;
                if (crumbs.Count > MaxCrumbs)
                {
                    crumbs.Dequeue();
                }
            }
        }

        public Vector3 GetTarget(Vector3 from, Vector3 fallback)
        {
            while (crumbs.Count > 0)
            {
                Vector3 front = crumbs.Peek();
                if (HorizontalDistance(from, front) <= arrive)
                {
                    crumbs.Dequeue();
                }
                else
                {
                    return front;
                }
            }

            return fallback;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
