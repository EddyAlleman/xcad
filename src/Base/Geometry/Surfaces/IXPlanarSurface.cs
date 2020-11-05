﻿using System;
using System.Collections.Generic;
using System.Text;
using Xarial.XCad.Geometry.Structures;

namespace Xarial.XCad.Geometry.Surfaces
{
    public interface IXPlanarSurface : IXSurface
    {
        Plane Plane { get; }
    }
}