using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using LitJson;

namespace ustcori_crack
{
    public class Utiles
    {
        public static string TableDataRandomize(string tableStr,ref bool changed)
        {
            string[] strArray1 = tableStr.Split(';');
            string[][] strArray2 = new string[strArray1.Length][];
            bool hasEmpty = false;
            Random rd = new Random();
            for (int i = 0; i < strArray1.Length; ++i)
            {
                if (strArray1[i].Contains("(") && strArray1[i].Contains(")"))
                {
                    string str = strArray1[i].Substring(1, strArray1[i].Length - 2);
                    strArray2[i] = str.Split(',');
                    double a;
                    for (int j = 0; j < strArray2[i].Length; ++j)
                    {
                        if (string.IsNullOrEmpty(strArray2[i][j]) || strArray2[i][j] == "*" || (double.TryParse(strArray2[i][j], out a) && a == 0))
                        {
                            strArray2[i][j] = ((double)rd.Next(0, 1000) / 10).ToString();
                            hasEmpty = true;
                        }
                    }
                }
            }
            if (hasEmpty)
            {
                string ans = "";
                for (int i = 0; i < strArray2.Length; ++i)
                {
                    ans += "(" + string.Join(",", strArray2[i]) + (i < strArray2.Length - 1 ? ");" : ")");
                }
                changed = true;
                return ans;
            }
            else
            {
                changed = false;
                return tableStr;
            }
        }

        public static XmlNode CreateNewNode(XmlNode orignNode,string nodeName, string InnerText)
        {
            XmlDocument doc = orignNode.OwnerDocument;
            XmlNode n = doc.CreateElement(nodeName);
            n.InnerText = InnerText;
            return n;
        }
        public static XmlDocument LoadXml(string filePath = null)
        {
            XmlDocument xmlDocument = new XmlDocument();
            if (filePath == null)
            {
                try
                {
                    if (File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\SimExpConfig\\SimExpConfig.xml"))
                        filePath = Environment.GetEnvironmentVariable("TEMP") + "\\lab\\SimExpConfig\\SimExpConfig";
                    else if (File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\SimExpConfig\\ExamConfig.xml"))
                        filePath = Environment.GetEnvironmentVariable("TEMP") + "\\lab\\SimExpConfig\\ExamConfig";
                    else if (File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\Main_Data\\StreamingAssets\\ExamConfig.xml"))
                        filePath = Environment.GetEnvironmentVariable("TEMP") + "\\lab\\Main_Data\\StreamingAssets\\ExamConfig";
                    else if (File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\4\\SimExpConfig\\SimExpConfig.xml"))
                        filePath = Environment.GetEnvironmentVariable("TEMP") + "\\lab\\4\\SimExpConfig\\SimExpConfig";
                    else if (File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\4\\SimExpConfig\\ExamConfig.xml"))
                        filePath = Environment.GetEnvironmentVariable("TEMP") + "\\lab\\4\\SimExpConfig\\ExamConfig";
                    else
                        filePath = Environment.GetEnvironmentVariable("TEMP") + "\\lab\\4\\Main_Data\\StreamingAssets\\ExamConfig";

                    if (File.Exists(filePath + ".bin"))
                    {
                        FileStream fs = new FileStream(filePath + ".bin", FileMode.Open);
                        byte[] zipdata = new byte[fs.Length];
                        fs.Read(zipdata, 0, zipdata.Length);
                        fs.Close();
                        byte[] a = JsonReader.Read(zipdata, "haohaoxuexi,tiantianxiangshang.");
                        MemoryStream byteStream = new MemoryStream(a);
                        XmlTextReader xmlTextReader = new XmlTextReader(byteStream);
                        xmlDocument.Load(xmlTextReader);
                    }
                    else
                    {
                        xmlDocument.Load(filePath + ".xml");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("读取实验数据失败：" + ex.Message + ex.StackTrace);
                }
            }
            else
            {
                xmlDocument.Load(filePath);
            }
            return xmlDocument;
        }
        public static XmlDocument ModifyExperiment(XmlDocument xmlDocument)
        {
            try
            {
                foreach (XmlNode question in xmlDocument.SelectSingleNode("ExamPaper").SelectSingleNode("Content").SelectNodes("Question"))
                {
                    question.SelectSingleNode("Score").SelectSingleNode("RealScore").InnerText = question.SelectSingleNode("Score").SelectSingleNode("Total").InnerText;
                    foreach (XmlNode checkPoint in question.SelectNodes("CheckPoint"))
                    {
                        foreach (XmlNode testTarget in checkPoint.SelectNodes("TestTarget"))
                        {
                            foreach (XmlNode group in testTarget.SelectNodes("Group"))
                            {
                                foreach (XmlNode para in group.SelectNodes("Para"))
                                {
                                    para.SelectSingleNode("RealScore").InnerText = para.SelectSingleNode("TotalScore").InnerText;
                                    if (string.IsNullOrEmpty(para.SelectSingleNode("StdResult").InnerText))
                                    {
                                        para.SelectSingleNode("StdResult").InnerText = "0";
                                    }
                                    if (para.SelectSingleNode("VarType").InnerText == "TList")
                                    {
                                        foreach (XmlNode listPara in para.SelectNodes("Para"))
                                        {
                                            if (listPara.SelectSingleNode("TotalScore") != null)
                                            {
                                                listPara.SelectSingleNode("RealScore").InnerText = listPara.SelectSingleNode("TotalScore").InnerText;
                                            }
                                        }
                                        bool changed = false;
                                        para.SelectSingleNode("StdResult").InnerText = TableDataRandomize(para.SelectSingleNode("StdResult").InnerText,ref changed);
                                        if (changed)
                                        {
                                            para.AppendChild(CreateNewNode(para, "Randomized", ""));
                                        }
                                    }
                                    para.SelectSingleNode("RealResult").InnerText = para.SelectSingleNode("StdResult").InnerText;
                                }

                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("修改失败：" + ex.Message + ex.StackTrace);
            }
            return xmlDocument;
        }
    }
}
