﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xarial.XCad.SwDocumentManager.Documents;

namespace SolidWorksDocMgr.Tests.Integration
{
    public class ComponentsTest : IntegrationTests
    {
        [Test]
        public void IterateRootComponentsTest()
        {
            string[] compNames;

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                compNames = ((ISwDmAssembly)m_App.Documents.Active).Components.Select(c => c.Name).ToArray();
            }

            Assert.That(compNames.OrderBy(c => c).SequenceEqual(
                new string[] { "Part1-1", "Part1-2", "SubAssem1-1", "SubAssem1-2", "SubAssem2-1", "Part1-3" }.OrderBy(c => c)));
        }

        [Test]
        public void IterateSubComponentsTest()
        {
            string[] compNames;

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                var assm = (ISwDmAssembly)m_App.Documents.Active;
                var comp = assm.Components["SubAssem1-1"];
                compNames = comp.Children.Select(c => c.Name).ToArray();
            }

            Assert.That(compNames.OrderBy(c => c).SequenceEqual(
                new string[] { "SubAssem1-1/Part2-1", "SubAssem1-1/SubSubAssem1-1" }.OrderBy(c => c)));
        }

        [Test]
        public void GetDocumentTest()
        {
            bool doc1Contains;
            bool doc2Contains;
            string doc1FileName;
            string doc2FileName;

            using (var doc = OpenDataDocument(@"Assembly1\TopAssem1.SLDASM"))
            {
                var assm = (ISwDmAssembly)m_App.Documents.Active;

                var doc1 = assm.Components["Part1-1"].Document;
                doc1FileName = Path.GetFileName(doc1.Path);
                doc1Contains = m_App.Documents.Contains(doc1);

                var doc2 = assm.Components["SubAssem1-1"].Document;
                doc2FileName = Path.GetFileName(doc2.Path);
                doc2Contains = m_App.Documents.Contains(doc2);

                var d = assm.Components["Part1-2"].Document;

                Assert.IsTrue(doc1.IsCommitted);
                Assert.IsTrue(doc2.IsCommitted);
                Assert.IsTrue(d.IsCommitted);
                Assert.That(string.Equals(Path.Combine(Path.GetDirectoryName(assm.Path), "Part1.sldprt"),
                    d.Path, StringComparison.CurrentCultureIgnoreCase));
            }

            Assert.That(doc1FileName.Equals("Part1.sldprt", StringComparison.CurrentCultureIgnoreCase));
            Assert.That(doc2FileName.Equals("SubAssem1.sldasm", StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(doc1Contains);
            Assert.IsTrue(doc2Contains);
        }
        
        [Test]
        public void GetComponentRefConfigTest()
        {
            using (var doc = OpenDataDocument(@"Assembly2\TopAssem.SLDASM"))
            {
                var assm = (ISwDmAssembly)m_App.Documents.Active;

                var conf1 = assm.Components["Part1-1"].ReferencedConfiguration;
                var conf2 = assm.Components["Assem2-1"].ReferencedConfiguration;
                var conf3 = assm.Components["Assem2-1"].Children["Part2-1"].ReferencedConfiguration;
                var conf4 = assm.Components["Part4-1 (XYZ)-2"].ReferencedConfiguration;
                var conf5 = assm.Components["Assem1-1"].ReferencedConfiguration;

                Assert.IsFalse(conf1.IsCommitted);
                Assert.IsFalse(conf2.IsCommitted);
                Assert.IsFalse(conf3.IsCommitted);
                Assert.IsFalse(conf4.IsCommitted);
                Assert.IsFalse(conf5.IsCommitted);

                Assert.AreEqual("Default", conf1.Name);
                Assert.AreEqual("Default", conf2.Name);
                Assert.AreEqual("Default", conf3.Name);
                Assert.AreEqual("1-1", conf4.Name);
                Assert.AreEqual("Default", conf5.Name);
            }
        }
        
        [Test]
        public void VirtualComponentsTest()
        {
            string[] compNames;

            using (var doc = OpenDataDocument(@"VirtAssem1.SLDASM"))
            {
                compNames = ((ISwDmAssembly)m_App.Documents.Active).Components.Select(c => c.Name).ToArray();
            }

            Assert.That(compNames.OrderBy(c => c).SequenceEqual(
                new string[] { "Part1^VirtAssem1-1", "Assem2^VirtAssem1-1" }.OrderBy(c => c)));
        }
    }
}