/******************************************************************************
EntropyZero Consulting deltaRunner
Copyright (C)2006 EntropyZero Consulting, LLC

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
******************************************************************************/

using System;
using System.IO;
using NUnit.Framework;

namespace EntropyZero.deltaRunner.Testing
{
	[TestFixture]
	public class DeltaFileTester : TestFixtureBase
	{
		[Test]
        public void LoadXMLInformation_FullXmll()
		{
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasFullXml.sql");
		    DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual("ThisIsATest", deltaFile.Category);
            Assert.AreEqual(true, deltaFile.UseTransaction);
            Assert.AreEqual(3, deltaFile.MaySkipCategories.Count);
            Assert.AreEqual("z", deltaFile.MaySkipCategories[2]);
		}

        [Test]
        public void LoadXMLInformation_NoXmll()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasNolXml.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(false, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }

        [Test]
        public void LoadXMLInformation_XmlButNoChildNodes()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlButNoChildNodes.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(false, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }

        [Test]
        public void LoadXMLInformation_XmlWithCategory()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithCategory.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual("ThisIsATest", deltaFile.Category);
            Assert.AreEqual(false, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }

        [Test]
        public void LoadXMLInformation_XmlWithCategoryNodeButNoValue()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithCategoryNodeButNoValue.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(false, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }


        [Test]
        public void LoadXMLInformation_XmlWithCategoryUseTransaction()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithCategoryUseTransaction.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(true, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }


        [Test]
        public void LoadXMLInformation_XmlWithMaySkipCategories()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithMaySkipCategories.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(false, deltaFile.UseTransaction);
            Assert.AreEqual(3, deltaFile.MaySkipCategories.Count);
            Assert.AreEqual("z", deltaFile.MaySkipCategories[2]);
        }


        [Test]
        public void LoadXMLInformation_XmlWithUseTransaction()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithUseTransaction.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(true, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }


        [Test]
        public void LoadXMLInformation_XmlWithUseTransactionMaySkip()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithUseTransactionMaySkip.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(true, deltaFile.UseTransaction);
            Assert.AreEqual(3, deltaFile.MaySkipCategories.Count);
            Assert.AreEqual("z", deltaFile.MaySkipCategories[2]);
        }

        [Test]
        public void LoadXMLInformation_XmlWithMaySkipCategoriesButNoValue()
        {
            FileInfo file = new FileInfo(@"..\..\TestFiles\DeltaXML\DeltaHasXmlWithMaySkipCategoriesButNoValue.sql");
            DeltaFile deltaFile = new DeltaFile();

            deltaFile.LoadXMLInformation(file);

            Assert.AreEqual(null, deltaFile.Category);
            Assert.AreEqual(false, deltaFile.UseTransaction);
            Assert.AreEqual(0, deltaFile.MaySkipCategories.Count);
        }
	}
}