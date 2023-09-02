﻿using System.Numerics;

namespace SPICA.Formats.CtrH3D.Camera
{
    public class H3DCameraViewLookAt
    {
        public Vector3 Target;
        public Vector3 UpVector;

        public H3DCameraViewLookAt()
        {
            UpVector = new Vector3(0, 1, 0);
        }
    }
}
