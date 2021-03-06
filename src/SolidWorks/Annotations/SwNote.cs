﻿using SolidWorks.Interop.sldworks;
using System;
using System.Collections.Generic;
using System.Text;
using Xarial.XCad.Annotations;
using Xarial.XCad.SolidWorks.Documents;

namespace Xarial.XCad.SolidWorks.Annotations
{
    public interface ISwNote : IXNote 
    {
        INote Note { get; }
    }

    internal class SwNote : SwSelObject, ISwNote
    {
        public INote Note { get; }

        internal SwNote(INote note, ISwDocument doc) : base(note, doc)
        {
            Note = note;
        }

        public string Text 
        {
            get => Note.GetText();
            set 
            {
                if (!Note.SetText(value)) 
                {
                    throw new Exception("Failed to set the note text value");
                }
            }
        }
    }
}
