using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ustcori_crack
{
    public class PaperReviewer

    {
        private string ansStr = "";
        private bool isProcessingTList = false;
        public double ReviewOpXml(XmlDocument xmlDocument)
        {
            double StudentScore = 0;
            try
            {
                foreach (XmlNode question in xmlDocument.SelectSingleNode("ExamPaper").SelectSingleNode("Content").SelectNodes("Question"))
                {
                    XmlNodeList testTargets = question.SelectSingleNode("CheckPoint").SelectNodes("TestTarget");
                    Console.WriteLine("实验：" + question.SelectSingleNode("Name").InnerText);
                    string str = "0";
                    double num = 0.0;
                    foreach (XmlNode testTarget in testTargets)
                    {
                        Console.WriteLine("  测试目标：" + testTarget.SelectSingleNode("Script").InnerText);
                        string testTargetGainScore = "0";
                        GetPerTestTarget(testTarget, ref testTargetGainScore);
                        if (testTargetGainScore == "LostWholeQuestion")
                            str = "LostWholeQuestion";
                        else if (testTargetGainScore == "LostWholeCheckPoint")
                            num += 0.0;
                        else
                            num += double.Parse(testTargetGainScore);
                    }
                    if (str != "LostWholeQuestion")
                    {
                        str = num.ToString();
                        StudentScore += num;
                    }
                    else
                        StudentScore += 0.0;
                    string s = question.SelectSingleNode("Score").SelectSingleNode("Total").InnerText;
                    if (string.IsNullOrEmpty(s))
                    {
                        str = "0";
                        StudentScore += 0.0;
                    }
                    else if (Math.Abs(double.Parse(s)) < Math.Pow(10.0, -6.0))
                    {
                        str = "0";
                        StudentScore += 0.0;
                    }
                    question.SelectSingleNode("Score").SelectSingleNode("RealScore").InnerText = str;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("评分出错：" + ex.Message + ex.StackTrace);
            }
            return StudentScore;
        }

        private void GetPerTestTarget(XmlNode testTarget, ref string testTargetGainScore)
        {
            XmlNodeList paras = testTarget.SelectSingleNode("Group").SelectNodes("Para");
            double num = 0.0;
            foreach (XmlNode para in paras)
            {
                Console.WriteLine("    评分点：" + para.SelectSingleNode("Script").InnerText);
                string paraGainScore = "0";
                GetPerPara(para, ref paraGainScore);
                if (paraGainScore == "LostWholeCheckPoint" || paraGainScore == "LostWholeQuestion")
                    testTargetGainScore = paraGainScore;
                else
                    num += double.Parse(paraGainScore);
            }
            if (testTargetGainScore != "LostWholeCheckPoint" && testTargetGainScore != "LostWholeQuestion")
                testTargetGainScore = num.ToString();
            XmlNode XmlNode = testTarget.SelectSingleNode("RealScore");
            if (XmlNode != null)
                XmlNode.InnerText = testTargetGainScore;
            else
                testTarget.AppendChild(Utiles.CreateNewNode(testTarget, "RealScore", testTargetGainScore));
            Console.WriteLine("    得分：" + testTarget.SelectSingleNode("RealScore").InnerText);
        }

        private void GetPerPara(XmlNode para, ref string paraGainScore)
        {
            para.SelectSingleNode("Script").InnerXml.Replace("&lt;", "<").Replace("&gt;", ">");
            string varType1 = para.SelectSingleNode("VarType").InnerText;
            string str1 = para.SelectSingleNode("RealResult").InnerText;
            string str2 = para.SelectSingleNode("StdResult").InnerText;
            if (!(varType1.Trim().ToUpper() == "ANSWER"))
            {
                if (varType1.Trim().ToUpper() == "TLIST")
                {
                    isProcessingTList = true;
                    try
                    {
                        string[,] strArray1 = DoTListResult(str1);
                        string[,] strArray2 = DoTListResult(str2);
                        string[,] strArray3 = (string[,])strArray2.Clone();

                        XmlNodeList XmlNodes = para.SelectNodes("Para");
                        double num = 0.0;
                        int index1 = 0;
                        foreach (XmlNode paraElement in XmlNodes)
                        {
                            string varType2 = paraElement.SelectSingleNode("VarType").InnerText;
                            int lowerBound = strArray2.GetLowerBound(1);
                            int upperBound = strArray2.GetUpperBound(1);
                            for (int index2 = lowerBound + 1; index2 <= upperBound; ++index2)
                            {
                                string RealResultToShow = "";
                                string s = GainScore(strArray1[index1, index2], strArray2[index1, index2], varType2, paraElement, ref RealResultToShow);
                                if(index1 > 0)
                                {
                                    strArray3[index1, index2] = ansStr;
                                }
                                if (s == "LostWholeQuestion" || s == "LostWholeCheckPoint")
                                {
                                    paraGainScore = s;
                                    break;
                                }
                                num += double.Parse(s);
                            }
                            if (!(paraGainScore == "LostWholeQuestion") && !(paraGainScore == "LostWholeCheckPoint"))
                                ++index1;
                            else
                                break;
                        }
                        if (paraGainScore != "LostWholeQuestion" && paraGainScore != "LostWholeCheckPoint")
                            paraGainScore = num.ToString();

                        ConsoleTableBuilder ctb = new ConsoleTableBuilder(strArray3, " ", "      ");
                        if (para.SelectSingleNode("Randomized") != null)
                        {
                            Console.WriteLine("      由于任意值填入下列表格都正确，所以下列表格为随机生成的值，请勿参考");
                            para.RemoveChild(para.SelectSingleNode("Randomized"));
                        }
                        Console.WriteLine("      答案区间：");
                        ctb.print();
                    }
                    catch
                    {
                    }
                    isProcessingTList = false;
                }
                else
                {
                    string RealResultToShow = str1;
                    paraGainScore = GainScore(str1, str2, varType1, para, ref RealResultToShow);
                }
            }
            string content = paraGainScore;
            XmlNode XmlNode1 = para.SelectSingleNode("ParaAdd");
            if (XmlNode1 != null)
            {
                string s1 = para.SelectSingleNode("TotalScore").InnerText;
                string str3 = XmlNode1.SelectSingleNode("Name").InnerText;
                string s2 = XmlNode1.SelectSingleNode("ParaChangeInnerText").InnerText;
                if (s2 == "")
                    s2 = "0";
                string upper = XmlNode1.SelectSingleNode("VarType").InnerText.Trim().ToUpper();
                string str4 = XmlNode1.SelectSingleNode("ShowPart").InnerText;
                string str5 = XmlNode1.SelectSingleNode("ShowWrong").InnerText;
                if (!(paraGainScore == "LostWholeQuestion") && !(paraGainScore == "LostWholeCheckPoint") && upper == "DOUBLE")
                {
                    double num1 = double.Parse(s2);
                    double num2 = double.Parse(s1) * num1;
                    double num3 = double.Parse(paraGainScore);
                    if (num3 < 1E-08)
                    {
                        content = paraGainScore;
                    }
                    else
                    {
                        if (num3 > num2)
                            num3 = num2;
                        paraGainScore = num3.ToString("F1");
                        if (double.Parse(s2) < 1E-07)
                            content = paraGainScore + "   ,   " + str5;
                        else if (double.Parse(s2) < 0.999999 && double.Parse(s2) > 1E-07)
                            content = paraGainScore + "   ,   " + str4;
                    }
                }
            }
            XmlNode XmlNode2 = para.SelectSingleNode("RealScore");
            if (XmlNode2 != null)
                XmlNode2.InnerText = content;
            else
                para.AppendChild(Utiles.CreateNewNode(para, "RealScore", content));
        }


        private string GainScore(
          string studentAnswer,
          string stdAnser,
          string varType,
          XmlNode paraElement,
          ref string RealResultToShow)
        {
            try
            {
                XmlNode XmlNode1 = paraElement.SelectSingleNode("JudgeRule");
                if (XmlNode1 == null)
                {
                    if (!isProcessingTList)
                        Console.WriteLine("      答案：{0}", stdAnser);
                    else
                        ansStr = stdAnser;
                    return "0";
                }
                varType = varType.Trim().ToLower();
                if (varType == "bool" || varType == "string")
                {
                    XmlNode ruleElement1 = XmlNode1.SelectSingleNode("RightRule");
                    XmlNode ruleElement2 = XmlNode1.SelectSingleNode("WrongRule");
                    string str;
                    Console.WriteLine("      答案：{0} {1}分", ruleElement1.SelectSingleNode("RuleShowInfo").InnerText,ruleElement1.SelectSingleNode("Score").InnerText);
                    if (studentAnswer.Trim().ToLower() == stdAnser.Trim().ToLower())
                    {
                        str = ruleElement1.SelectSingleNode("Score").InnerText;
                        RealResultToShow = AddStudentAnswerShow(paraElement, ruleElement1);
                    }
                    else
                    {
                        str = ruleElement2.SelectSingleNode("Score").InnerText;
                        RealResultToShow = AddStudentAnswerShow(paraElement, ruleElement2);
                    }
                    if (varType == "string" && RealResultToShow == "")
                        RealResultToShow = studentAnswer;
                    return str;
                }
                if (varType == "int" || varType == "double")
                {
                    if (string.IsNullOrEmpty(stdAnser))
                        return "0";
                    List<XmlNode> allMatchRules = new List<XmlNode>();
                    double num1; //实际答案 x
                    if (string.IsNullOrEmpty(studentAnswer))
                    {
                        num1 = 1145141919810;
                    }
                    else
                    {
                        num1 = double.Parse(studentAnswer);
                    }
                    double num2 = double.Parse(stdAnser);//标准答案 s
                    XmlNodeList XmlNodes = XmlNode1.SelectNodes("Rule");
                    if (XmlNodes == null)
                        return "0";
                    foreach (XmlNode XmlNode2 in XmlNodes)
                    {
                        double num3 = double.Parse(XmlNode2.SelectSingleNode("StdValue").InnerText); //标准答案系数 k
                        if (num3 == 0.0)
                            num3 = 1E-11;
                        XmlNode XmlNode3 = XmlNode2.SelectSingleNode("Range");
                        double num4 = double.Parse(XmlNode3.SelectSingleNode("Min").InnerText);//最小值 min
                        double num5 = double.Parse(XmlNode3.SelectSingleNode("Max").InnerText);//最大值 max
                        double num6 = num1 - num3 * num2; //x-ks
                        string str = XmlNode3.SelectSingleNode("Type").InnerText;
                        double num7 = 1E-07;//精度调整
                        ansStr = "";
                        if (!isProcessingTList)
                            Console.WriteLine("      答案：{0}", num3 * num2);
                        else
                            ansStr = (num3 * num2).ToString();
                        if (str.Trim() == "绝对值")
                        {
                            if (num6 >= num4 - num7 && num6 <= num5 + num7)// min+sk<=x<=max+ks
                                allMatchRules.Add(XmlNode2);
                            if (!isProcessingTList)
                                Console.WriteLine("      区间：[{0},{1}] {2}分", num4 + num3 * num2, num5 + num3 * num2, XmlNode2.SelectSingleNode("Score").InnerText);
                            else
                                ansStr += " ["+(num4 + num3 * num2)+","+ (num5 + num3 * num2)+"]";
                        }
                        else
                        {
                            bool flag = false;
                            if (Math.Abs(num2) <= num7)//|s|<1e-7 <=> s=0
                            {
                                num2 = 1E-11;
                                flag = true;
                            }
                            double num8 = (num1 / num2 - num3) / num3;//x/(sk)-1
                            
                            if (!isProcessingTList)
                                Console.WriteLine("      区间：[{0},{1}] {2}分", num3 * num2 * (1 + num4), num3 * num2 * (1 + num5), XmlNode2.SelectSingleNode("Score").InnerText);
                            else
                                ansStr += " [" + num3 * num2 * (1 + num4) + "," + num3 * num2 * (1 + num5) + "]";

                            if (num8 >= num4 - num7 && num8 <= num5 + num7)//(min+1)ks<=x<=(max+1)ks
                                allMatchRules.Add(XmlNode2);
                            else if (flag && Math.Abs(num1) <= num7) //s=0 且 x足够小
                                allMatchRules.Add(XmlNode2);
                        }
                    }
                    if (allMatchRules.Count == 0)
                        return "0";
                    XmlNode outRule;
                    string maxScore;
                    try
                    {
                        maxScore = GetMaxScore(allMatchRules, out outRule);
                        if (paraElement.SelectSingleNode("TotalScore").InnerText == "0")
                            maxScore = Math.Pow(10.0, -5.0).ToString();
                    }
                    catch (Exception ex)
                    {
                        maxScore = GetMaxScore(allMatchRules, out outRule);
                    }
                    RealResultToShow = AddStudentAnswerShow(paraElement, outRule);
                    if (RealResultToShow == "")
                        RealResultToShow = num1.ToString();
                    return maxScore;
                }
            }
            catch
            {
                return "0";
            }
            return "0";
        }

        private string AddStudentAnswerShow(XmlNode paraElement, XmlNode ruleElement)
        {
            if (paraElement == null || ruleElement == null)
                return "";
            XmlNode XmlNode1 = paraElement.SelectSingleNode("StdResultShowInfo");
            if (XmlNode1 == null || XmlNode1.InnerText == "原值")
                return "";
            XmlNode XmlNode2 = ruleElement.SelectSingleNode("RuleShowInfo");
            if (XmlNode2 == null)
                return "";
            XmlNode XmlNode3 = paraElement.SelectSingleNode("StudentResultShowInfo");
            if (XmlNode3 == null)
                paraElement.AppendChild(Utiles.CreateNewNode(paraElement, "StudentResultShowInfo", XmlNode2.InnerText));
            else
                XmlNode3.InnerText = XmlNode2.InnerText;
            return XmlNode2.InnerText;
        }

        private string GetMaxScore(List<XmlNode> allMatchRules, out XmlNode outRule)
        {
            IEnumerable<XmlNode> source1 = allMatchRules.Where(r => r.SelectSingleNode("Score").InnerText == "LostWholeQuestion");
            IEnumerable<XmlNode> source2 = allMatchRules.Where(r => r.SelectSingleNode("Score").InnerText == "LostWholeCheckPoint");
            IEnumerable<XmlNode> source3 = allMatchRules.Where(r => r.SelectSingleNode("Score").InnerText != "LostWholeQuestion" && r.SelectSingleNode("Score").InnerText != "LostWholeCheckPoint");
            if (source3 != null && source3.Count() != 0)
            {
                double maxScore = source3.Select(r => double.Parse(r.SelectSingleNode("Score").InnerText)).Max();
                outRule = source3.Where(r => double.Parse(r.SelectSingleNode("Score").InnerText) == maxScore).Single();
                return Math.Round(maxScore, 2).ToString();
            }
            if (source2 != null && source2.Count() != 0)
            {
                outRule = source2.ToList()[0];
                return "LostWholeCheckPoint";
            }
            if (source1 != null && source1.Count() != 0)
            {
                outRule = source1.ToList()[0];
                return "LostWholeQuestion";
            }
            outRule = null;
            return "0";
        }

        private string[,] DoTListResult(string result)
        {
            string[] strArray1 = result.Split(';');
            string[][] strArray2 = new string[strArray1.Length][];
            for (int index = 0; index < strArray1.Length; ++index)
            {
                if (strArray1[index].Contains("(") && strArray1[index].Contains(")"))
                {
                    string str = strArray1[index].Substring(1, strArray1[index].Length - 2);
                    strArray2[index] = str.Split(',');
                }
            }
            string[,] strArray3 = new string[strArray2[0].Length, strArray2.Length];
            for (int index1 = 0; index1 < strArray2.Length; ++index1)
            {
                for (int index2 = 0; index2 < strArray2[index1].Length; ++index2)
                    strArray3[index2, index1] = strArray2[index1][index2];
            }
            return strArray3;
        }
    }
}
