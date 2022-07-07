using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using USTCORi.WebLabClient;
using USTCORi.WebLabClient.BizServiceReference;
using USTCORi.WebLabClient.DataModel;
using USTCORi.WebLabClient.RuningInfo;
using USTCORi.WebLabClient.WLServiceReference;

namespace ustcori_crack
{
    public class FakeApp
    {
        public static RunningInfo runningInfo;
        public string Verifykey = "USTCORikeyLab";
        public Task findLabTask;
        public FakeHall fakeHall;
        public FakeHall2 fakeHall2;
        public bool isNewVersion = false;

        public FakeApp(string labAddress)
        {
            this.InitInfo(AppDomain.CurrentDomain.BaseDirectory);
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (labAddress.Length > 0)
            {
                string str1 = labAddress;
                string str2 = "";
                try
                {
                    string str3 = str1.Split(':')[str1.Split(':').Length - 1];
                    str2 = Encoding.UTF8.GetString(Convert.FromBase64String(str3.Substring(2, str3.Length - 3)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("参数解析失败！" + ex.Message);
                    this.ExitApp();
                }
                string[] strArray = str2.Split('/');
                string str4 = strArray[0];
                string str5 = strArray[1];
                string str6 = strArray[2];
                string str7 = strArray[3];
                string str8 = strArray[4];
                string userType = "3";
                if (strArray.Length > 7)
                    userType = strArray[7];
                if(strArray.Length > 10)
                    isNewVersion = true;

                string clientip = "";
                string clientarea = "";
                if (strArray.Length > 7)
                    userType = strArray[7];
                if (strArray.Length > 9)
                {
                    clientip = strArray[8];
                    clientarea = strArray[9];
                }
                string s = "ZH-CN";
                if (strArray.Length > 10)
                    s = strArray[10];
                string CourseID = "-1";
                if (strArray.Length > 11)
                    CourseID = strArray[11];


                App.runningInfo.Const.SetUserName(str8);
                string friendlyName = AppDomain.CurrentDomain.FriendlyName;
                XElement xelement = XElement.Load(baseDirectory + friendlyName + ".config");
                foreach (XElement element in xelement.Element((XName)"system.serviceModel").Element((XName)"client").Elements((XName)"endpoint"))
                {
                    if (element.Attribute((XName)"address").Value.Contains("BizService"))
                    {
                        if (str7.ToLower() == "false" || str7.ToLower() == "undefined")
                            element.Attribute((XName)"address").Value = "http://" + str6 + "/BizService.svc";
                        else
                            element.Attribute((XName)"address").Value = "http://" + str6 + ":" + str7 + "/BizService.svc";
                    }
                    else if (element.Attribute((XName)"address").Value.Contains("WLService"))
                    {
                        if (str7.ToLower() == "false" || str7.ToLower() == "undefined")
                            element.Attribute((XName)"address").Value = "http://" + str6 + "/WLService.svc";
                        else
                            element.Attribute((XName)"address").Value = "http://" + str6 + ":" + str7 + "/WLService.svc";
                    }
                    else if (str7.ToLower() == "false" || str7.ToLower() == "undefined")
                        element.Attribute((XName)"address").Value = "http://" + str6 + "/FileTransfer.svc";
                    else
                        element.Attribute((XName)"address").Value = "http://" + str6 + ":" + str7 + "/FileTransfer.svc";
                }
                xelement.Save(baseDirectory + "WebLabClient.exe.config");
                this.initServiceClient();
                if (this.findLabTask == null)
                {
                    this.findLabTask = new Task();
                    this.findLabTask.BizCode = "UstcOri.BLL.BLLLabClent";
                    this.findLabTask.MethodName = "FindByLABID";
                    this.findLabTask.ReturnType = typeof(IList<LABINFO>);
                }
                this.findLabTask.SetParameter("LabID", (object)str5);
                try
                {
                    Console.WriteLine("获取实验项目……");
                    SvcResponse svcResponse = this.findLabTask.Run();
                    if (string.IsNullOrEmpty(svcResponse.Message))
                    {
                        if (svcResponse.Data != null)
                        {
                            IList<LABINFO> data = svcResponse.Data as IList<LABINFO>;
                            if (data.Count > 0)
                            {
                                Console.WriteLine("实验名称："+data[0].LABNAME);
                                if (isNewVersion)
                                {
                                    fakeHall2 = new FakeHall2(data[0], baseDirectory, str8, userType, clientip, clientarea, CourseID);
                                }
                                else
                                {
                                    fakeHall = new FakeHall(data[0], baseDirectory, str8, userType);
                                }
                                
                                /*TheHall theHall = new TheHall(data[0], baseDirectory, str8, userType);
                                theHall.Topmost = false;
                                theHall.WindowState = WindowState.Minimized;
                                theHall.Show();
                                theHall.UserControlModel();*/
                            }
                            else
                            {
                                Console.WriteLine("未找到相应的实验项目！");
                                this.ExitApp();
                            }
                        }
                        else
                        {
                            Console.WriteLine("获取信息时发生错误！");
                            this.ExitApp();
                        }
                    }
                    else
                    {
                        Console.WriteLine(svcResponse.Message, "Error");
                        this.ExitApp();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, "Error");
                    this.ExitApp();
                }
            }
            else
                this.ExitApp();
        }

        public void ExitApp()
        {
            Console.WriteLine("Exiting...");
        }

        private void InitInfo(string path)
        {
            App.runningInfo = new RunningInfo();
            App.runningInfo.Const.SetShoolUser("No School");
            App.runningInfo.Const.SetVersion("V2.01.0914");
            App.runningInfo.Const.SetDownloadExperimentPath(path + "Download\\");
            App.runningInfo.Const.SetDownloadExperimentXmlPath(path + "Download\\download.xml");
            App.runningInfo.Const.SetDownloadUpdataPath(path + "Download\\Updata\\");
            App.runningInfo.Global.NeedConfirmShutdown = true;
        }

        private void initServiceClient()
        {
            BizServiceClient c = new BizServiceClient();
            WLServiceClient wlServiceClient = new WLServiceClient();
            App.runningInfo.Const.SetClient(c);
        }

    }
}