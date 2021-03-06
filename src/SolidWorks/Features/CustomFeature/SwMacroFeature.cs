﻿//*********************************************************************
//xCAD
//Copyright(C) 2021 Xarial Pty Limited
//Product URL: https://www.xcad.net
//License: https://xcad.xarial.com/license/
//*********************************************************************

using SolidWorks.Interop.sldworks;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xarial.XCad.Annotations;
using Xarial.XCad.Documents;
using Xarial.XCad.Features.CustomFeature;
using Xarial.XCad.Features.CustomFeature.Attributes;
using Xarial.XCad.Features.CustomFeature.Enums;
using Xarial.XCad.Geometry;
using Xarial.XCad.Reflection;
using Xarial.XCad.SolidWorks.Documents;
using Xarial.XCad.SolidWorks.Features.CustomFeature.Exceptions;
using Xarial.XCad.SolidWorks.Features.CustomFeature.Toolkit;
using Xarial.XCad.SolidWorks.Geometry;
using Xarial.XCad.SolidWorks.Utils;
using Xarial.XCad.Toolkit.Exceptions;
using Xarial.XCad.Utils.CustomFeature;
using Xarial.XCad.Utils.Reflection;

namespace Xarial.XCad.SolidWorks.Features.CustomFeature
{
    public interface ISwMacroFeature : ISwFeature, IXCustomFeature
    {
        //TODO: remove this
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        ISwDocument Document { get; }

        ISwMacroFeature<TParams> ToParameters<TParams>()
            where TParams : class, new();
    }

    internal class SwMacroFeature : SwFeature, ISwMacroFeature
    {
        private IMacroFeatureData m_FeatData;

        private Type m_DefinitionType;

        public Type DefinitionType 
        {
            get 
            {
                if (IsCommitted) 
                {
                    if (m_DefinitionType == null) 
                    {
                        var progId = FeatureData.GetProgId();

                        if (!string.IsNullOrEmpty(progId))
                        {
                            m_DefinitionType = Type.GetTypeFromProgID(progId);
                        }
                    }
                }

                return m_DefinitionType;
            }
            set 
            {
                if (!IsCommitted)
                {
                    m_DefinitionType = value;
                }
                else
                {
                    throw new CommittedElementPropertyChangeNotSupported();
                }
            }
        }

        public IMacroFeatureData FeatureData => m_FeatData ?? (m_FeatData = Feature.GetDefinition() as IMacroFeatureData);

        private readonly IFeatureManager m_FeatMgr;

        public ISwDocument Document => m_Doc;

        internal SwMacroFeature(SwDocument doc, IFeatureManager featMgr, IFeature feat, bool created)
            : base(doc, feat, created)
        {
            m_FeatMgr = featMgr;
        }

        //TODO: check constant context disconnection exception
        public IXConfiguration Configuration 
            => SwObject.FromDispatch<SwConfiguration>(FeatureData.CurrentConfiguration, m_Doc);

        public ISwMacroFeature<TParams> ToParameters<TParams>()
            where TParams : class, new()
        {
            return ToParameters<TParams>(new MacroFeatureParametersParser(((SwDocument)m_Doc).App.Sw));
        }

        private SwMacroFeature<TParams> ToParameters<TParams>(MacroFeatureParametersParser paramsParser)
            where TParams : class, new()
        {
            return new SwMacroFeature<TParams>((SwDocument)m_Doc, m_FeatMgr, Feature, paramsParser, IsCommitted);
        }

        protected override IFeature CreateFeature(CancellationToken cancellationToken)
        {
            return InsertComFeatureBase(null, null, null, null, null, null, null);
        }

        protected IFeature InsertComFeatureBase(string[] paramNames, int[] paramTypes, string[] paramValues,
            int[] dimTypes, double[] dimValues, object[] selection, object[] editBodies)
        {
            ValidateDefinitionType();

            var options = CustomFeatureOptions_e.Default;
            var provider = "";

            DefinitionType.TryGetAttribute<CustomFeatureOptionsAttribute>(a =>
            {
                options = a.Flags;
            });

            DefinitionType.TryGetAttribute<MissingDefinitionErrorMessage>(a =>
            {
                provider = a.Message;
            });

            var baseName = MacroFeatureInfo.GetBaseName(DefinitionType);

            var progId = MacroFeatureInfo.GetProgId(DefinitionType);

            if (string.IsNullOrEmpty(progId))
            {
                throw new NullReferenceException("Prog id for macro feature cannot be extracted");
            }

            var icons = MacroFeatureIconInfo.GetIcons(DefinitionType,
                CompatibilityUtils.SupportsHighResIcons(SwMacroFeatureDefinition.Application.Sw, CompatibilityUtils.HighResIconsScope_e.MacroFeature));

            using (var selSet = new SelectionGroup(m_FeatMgr.Document.ISelectionManager))
            {
                if (selection != null && selection.Any())
                {
                    selSet.AddRange(selection);
                }

                var feat = m_FeatMgr.InsertMacroFeature3(baseName,
                    progId, null, paramNames, paramTypes,
                    paramValues, dimTypes, dimValues, editBodies, icons, (int)options) as IFeature;

                return feat;
            }
        }

        protected virtual void ValidateDefinitionType()
        {
            if (!typeof(SwMacroFeatureDefinition).IsAssignableFrom(DefinitionType))
            {
                throw new MacroFeatureDefinitionTypeMismatch(DefinitionType, typeof(SwMacroFeatureDefinition));
            }
        }
    }

    public interface ISwMacroFeature<TParams> : ISwMacroFeature, IXCustomFeature<TParams>
        where TParams : class, new()
    { 
    }

    internal class SwMacroFeature<TParams> : SwMacroFeature, ISwMacroFeature<TParams>
        where TParams : class, new()
    {
        private readonly MacroFeatureParametersParser m_ParamsParser;

        internal SwMacroFeature(SwDocument model, IFeatureManager featMgr, IFeature feat, MacroFeatureParametersParser paramsParser, bool created)
            : base(model, featMgr, feat, created)
        {
            m_ParamsParser = paramsParser;
        }

        private TParams m_ParametersCache;

        public TParams Parameters
        {
            get
            {
                if (IsCommitted)
                {
                    if (FeatureData.AccessSelections(m_Doc.Model, null))
                    {
                        return (TParams)m_ParamsParser.GetParameters(this, m_Doc, typeof(TParams),
                            out IXDimension[] _, out string[] _, out IXBody[] _, out IXSelObject[] sels, out CustomFeatureOutdateState_e _);
                    }
                    else
                    {
                        throw new Exception("Failed to edit feature");
                    }
                }
                else
                {
                    return m_ParametersCache;
                }
            }
            set
            {
                if (IsCommitted)
                {
                    if (value == null)
                    {
                        FeatureData.ReleaseSelectionAccess();
                    }
                    else
                    {
                        m_ParamsParser.SetParameters(m_Doc, this, value, out CustomFeatureOutdateState_e _);

                        if (!Feature.ModifyDefinition(FeatureData, m_Doc.Model, null))
                        {
                            throw new Exception("Failed to update parameters");
                        }
                    }
                }
                else
                {
                    m_ParametersCache = value;
                }
            }
        }

        protected override IFeature CreateFeature(CancellationToken cancellationToken)
        {
            return InsertComFeatureWithParameters();
        }

        private IFeature InsertComFeatureWithParameters()
        {
            CustomFeatureParameter[] atts;
            IXSelObject[] selection;
            CustomFeatureDimensionType_e[] dimTypes;
            double[] dimValues;
            IXBody[] editBodies;

            m_ParamsParser.Parse(Parameters,
                out atts, out selection, out dimTypes, out dimValues,
                out editBodies);

            string[] paramNames;
            string[] paramValues;
            int[] paramTypes;

            m_ParamsParser.ConvertParameters(atts, out paramNames, out paramTypes, out paramValues);

            //TODO: add dim types conversion

            return InsertComFeatureBase(
                paramNames, paramTypes, paramValues,
                dimTypes?.Select(d => (int)d)?.ToArray(), dimValues,
                selection?.Cast<SwSelObject>()?.Select(s => s.Dispatch)?.ToArray(),
                editBodies?.Cast<SwBody>()?.Select(b => b.Body)?.ToArray());
        }

        protected override void ValidateDefinitionType()
        {
            if (!typeof(SwMacroFeatureDefinition<TParams>).IsAssignableFrom(DefinitionType))
            {
                throw new MacroFeatureDefinitionTypeMismatch(DefinitionType, typeof(SwMacroFeatureDefinition<TParams>));
            }
        }
    }
}