﻿//*********************************************************************
//xCAD
//Copyright(C) 2021 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace Xarial.XCad.UI.PropertyPage
{
    /// <summary>
    /// Represents the custom control hosted in the page
    /// </summary>
    public interface IXCustomControl
    {
        /// <summary>
        /// Raised when data context of this control is changed
        /// </summary>
        event Action<IXCustomControl, object> ValueChanged;

        /// <summary>
        /// Returns the data context of this control
        /// </summary>
        object Value { get; set; }
    }
}
