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
using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace EntropyZero.deltaRunner
{
	public class DeltaFile
	{
		public string Version;
		public string Hash = null;
		public string Filename;
		public FileInfo File;
		public SqlFileExecutionOption Option;
		public bool UseTransaction;
	    private bool isModified = false;
		public bool ForceRunOnceInDevelopment = false;

	    public string Category;
	    public StringCollection MaySkipCategories = new StringCollection();

	    public DeltaFile()
	    {
	        
	    }

        public DeltaFile(FileInfo file)
		{
            Version = Path.GetFileNameWithoutExtension(file.Name);
			Filename = file.Name;
			File = file;
			UseTransaction = true;
            LoadXMLInformation(file);
		}

        public DeltaFile(FileInfo file, SqlFileExecutionOption option, bool useTransaction)
		{
			switch (option)
			{
				case SqlFileExecutionOption.ExecuteBeforeDeltas:
					Version = "-2";
					break;
				case SqlFileExecutionOption.ExecuteAfterDeltas:
					Version = "-1";
					break;
				default:
					Version = Path.GetFileNameWithoutExtension(file.Name);
					break;
			}

			Filename = file.Name;
			Option = option;
			File = file;
			UseTransaction = useTransaction;
            LoadXMLInformation(file);
		}
        
        public DeltaFile(FileInfo file, SqlFileExecutionOption option, bool useTransaction, string category) : this(file, option, useTransaction)
		{
            if (category != null)
            {
                Category = category;
            }
		}


	    public bool IsModified
	    {
	        get
	        {
	            if(Hash != null)
                    return isModified;
                else
	            {
	                throw new ArgumentNullException("IsModified Bit has not been set yet.");
	            }
	        }
            set
            {
                isModified = value;
            }
	    }

	    public void CalculateHash()
	    {
            Hash = DeltaHashProvider.GetMD5Hash(File);
	    }

	    public void LoadXMLInformation(FileInfo file)
        {
            string      beginXmlTag     = "<deltarunner>";
            string      endXmlTag       = "</deltarunner>";
            string      fileData        = "";
            string      xmlString       = "";
            int         locOfBeginXml;
            int         locOfEndXml;
            XmlDocument xmlDocument;

            using (FileStream fs = file.OpenRead())
            {
                StreamReader sr = new StreamReader(fs);
                fileData = sr.ReadToEnd();
            }

            locOfBeginXml = fileData.ToLower().IndexOf(beginXmlTag);

            if (locOfBeginXml <= -1)
            {
                return;
            }

            locOfEndXml = fileData.ToLower().IndexOf(endXmlTag);
            xmlDocument = new XmlDocument();
            xmlString   = fileData.Substring(locOfBeginXml, locOfEndXml - locOfBeginXml + endXmlTag.Length);

            xmlDocument.LoadXml(xmlString);

            foreach (XmlNode xmlNode in xmlDocument.ChildNodes[0].ChildNodes)
            {
                if (xmlNode.InnerXml == null || xmlNode.InnerXml.Length <= 0)
                    continue;
                
                switch(xmlNode.Name.ToLower())
                {
                    case "category":
                        Category = xmlNode.InnerXml;
                        break;
                    case "usetransaction":
                        UseTransaction = Convert.ToBoolean(xmlNode.InnerXml);
                        break;
					case "forcerunonceindevelopment":
                        ForceRunOnceInDevelopment = Convert.ToBoolean(xmlNode.InnerXml);
                        break;
                    case "mayskipcategories":
                        foreach(XmlNode skipCatNode in xmlNode.ChildNodes)
                        {
                            if (skipCatNode.InnerXml != null && skipCatNode.InnerXml.Length > 0)
                            {
                                MaySkipCategories.Add(skipCatNode.InnerXml);
                            }
                         }
                        break;
                    default:
                        break; 
                }
            }
        }
	}
}
