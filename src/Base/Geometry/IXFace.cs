﻿//*********************************************************************
//xCAD
//Copyright(C) 2021 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Xarial.XCad.Geometry.Structures;
using Xarial.XCad.Geometry.Surfaces;

namespace Xarial.XCad.Geometry
{
    /// <summary>
    /// Represents face entity
    /// </summary>
    public interface IXFace : IXEntity, IXColorizable
    {
        /// <summary>
        /// Area of the face
        /// </summary>
        double Area { get; }

        /// <summary>
        /// Underlying definition for this face
        /// </summary>
        IXSurface Definition { get; }
    }

    /// <summary>
    /// Represents planar face
    /// </summary>
    public interface IXPlanarFace : IXFace 
    {
        /// <inheritdoc/>
        new IXPlanarSurface Definition { get; }
    }

    /// <summary>
    /// Represents cylindrical face
    /// </summary>
    public interface IXCylindricalFace : IXFace 
    {
        /// <inheritdoc/>
        new IXCylindricalSurface Definition { get; }
    }
}
