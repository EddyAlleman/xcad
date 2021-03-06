﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Xarial.XCad.Annotations
{
    /// <summary>
    /// Represents the note annotation
    /// </summary>
    public interface IXNote : IXSelObject
    {
        /// <summary>
        /// Text of the note
        /// </summary>
        string Text { get; set; }
    }
}
