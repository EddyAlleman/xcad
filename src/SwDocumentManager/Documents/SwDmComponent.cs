﻿//*********************************************************************
//xCAD
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using SolidWorks.Interop.swdocumentmgr;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using Xarial.XCad.Base;
using Xarial.XCad.Documents;
using Xarial.XCad.Documents.Enums;
using Xarial.XCad.Features;
using Xarial.XCad.Geometry;
using Xarial.XCad.Services;
using Xarial.XCad.SwDocumentManager.Services;

namespace Xarial.XCad.SwDocumentManager.Documents
{
    public interface ISwDmComponent : IXComponent, ISwDmSelObject
    {
        ISwDMComponent Component { get; }
        new ISwDmDocument3D Document { get; }
        new ISwDmConfiguration ReferencedConfiguration { get; }
    }

    internal class SwDmComponent : SwDmSelObject, ISwDmComponent
    {
        #region Not Supported

        public IXFeatureRepository Features => throw new NotSupportedException();
        public IXBodyRepository Bodies => throw new NotSupportedException();
        TSelObject IXObjectContainer.ConvertObject<TSelObject>(TSelObject obj) => throw new NotSupportedException();

        #endregion

        IXDocument3D IXComponent.Document => Document;
        IXConfiguration IXComponent.ReferencedConfiguration => ReferencedConfiguration;

        public ISwDMComponent Component { get; }

        private SwDmAssembly m_ParentAssm;

        private IFilePathResolver m_FilePathResolver;

        internal SwDmComponent(SwDmAssembly parentAssm, ISwDMComponent comp) : base(comp)
        {
            Component = comp;
            m_ParentAssm = parentAssm;
            m_FilePathResolver = new SwDmFilePathResolver();
        }

        public string Name
        {
            get
            {
                var fullName = new StringBuilder(((ISwDMComponent7)Component).Name2);

                var curParent = Parent;

                while (curParent != null) 
                {
                    fullName.Insert(0, curParent.Name + "/");
                    curParent = curParent.Parent;
                }

                return fullName.ToString();
            }
        }

        public string CachedPath => ((ISwDMComponent6)Component).PathName;

        public string Path 
        {
            get 
            {
                var rootDir = System.IO.Path.GetDirectoryName(RootAssembly.Path);

                return m_FilePathResolver.ResolvePath(rootDir, CachedPath);
            }
        }

        public ISwDmConfiguration ReferencedConfiguration => new SwDmComponentConfiguration(this);

        public ComponentState_e State
        {
            get
            {
                var state = ComponentState_e.Default;

                if (Component.IsHidden())
                {
                    state |= ComponentState_e.Hidden;
                }

                if (Component.IsSuppressed())
                {
                    state |= ComponentState_e.Suppressed;
                }

                if (((ISwDMComponent5)Component).ExcludeFromBOM == (int)swDmExcludeFromBOMResult.swDmExcludeFromBOM_TRUE)
                {
                    state |= ComponentState_e.ExcludedFromBom;
                }

                return state;
            }
        }

        private ISwDmDocument3D m_CachedDocument;

        public ISwDmDocument3D Document
        {
            get
            {
                if (m_CachedDocument == null || !m_CachedDocument.IsAlive)
                {
                    var isReadOnly = m_ParentAssm.State.HasFlag(DocumentState_e.ReadOnly);

                    try
                    {
                        var doc = (ISwDmDocument3D)m_ParentAssm.SwDmApp.Documents.PreCreateFromPath(Path);
                        doc.State = isReadOnly ? DocumentState_e.ReadOnly : DocumentState_e.Default;

                        doc.Commit();

                        m_CachedDocument = doc;
                    }
                    catch 
                    {
                        var unknownDoc = new SwDmUnknownDocument(m_ParentAssm.SwDmApp, null,
                                false,
                                ((SwDmDocumentCollection)m_ParentAssm.SwDmApp.Documents).OnDocumentCreated,
                                ((SwDmDocumentCollection)m_ParentAssm.SwDmApp.Documents).OnDocumentClosed, isReadOnly);

                        unknownDoc.Path = CachedPath;

                        m_CachedDocument = (ISwDmDocument3D)unknownDoc.GetSpecific();
                    }

                    return m_CachedDocument;
                }

                return m_CachedDocument;
            }
        }
        
        public IXComponentRepository Children
        {
            get
            {
                var refConf = ReferencedConfiguration;
                
                if (!refConf.IsCommitted) 
                {
                    refConf.Commit(default);
                }

                return new SwDmSubComponentCollection(this, m_ParentAssm, refConf);
            }
        }

        internal SwDmComponent Parent { get; set; }
        internal ISwDmAssembly RootAssembly { get; set; }
    }

    internal class SwDmComponentConfiguration : SwDmConfiguration
    {
        private readonly ISwDmComponent m_Comp;

        internal SwDmComponentConfiguration(ISwDmComponent comp) : base(null, null)
        {
            m_Comp = comp;
        }
        
        public override string Name 
        {
            get => m_Comp.Component.ConfigurationName;
            set => throw new NotSupportedException();
        }

        protected override SwDmDocument3D Document => (SwDmDocument3D)m_Comp.Document;

        public override ISwDMConfiguration Configuration => Document.Configurations[Name].Configuration;

        public override object Dispatch => Configuration;

        public override bool IsCommitted => true;
    }

    internal class SwDmSubComponentCollection : SwDmComponentCollection
    {
        private readonly SwDmComponent m_ParentComp;

        internal SwDmSubComponentCollection(SwDmComponent parentComp,
            SwDmAssembly parentAssm, ISwDmConfiguration conf) 
            : base(parentAssm, conf)
        {
            m_ParentComp = parentComp;
        }

        protected override SwDmComponent CreateComponentInstance(ISwDMComponent dmComp)
        {
            var comp =  base.CreateComponentInstance(dmComp);
            comp.Parent = m_ParentComp;
            return comp;
        }
    }
}
