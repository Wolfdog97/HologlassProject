using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoloPlay
{

    public class AirTouch
    {
        public AirTouch(int _id) { id = _id; }
        public int id { get; private set; }

        public void Deactivate() { deactivated = true; }

        public void SetPosition(Vector3 _p)
        {
            if (deactivated)
            {
                deactivated = false;
                lastPos = _p;
            }
            else
            {
                lastPos = position;
            }
            position = _p;
        }
        public Vector3 GetWorldPos()
        {
            if (Capture.Instance)
                return GetWorldPos(Capture.Instance.transform);
            return GetLocalPos();
        }
        public Vector3 GetWorldPos(Transform camera)
        {
            return camera.TransformPoint(GetLocalPos());
        }
        public Vector3 GetLocalPos()
        {
            return position;
        }

        public Vector3 GetLocalDiff()
        {
            return lastPos - position;
        }

        //protected...
        bool deactivated = false;

        Vector3 position;
        Vector3 lastPos;
    }
}
