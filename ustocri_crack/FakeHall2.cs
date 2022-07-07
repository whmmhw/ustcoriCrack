using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Codaxy.Xlio.IO;
using LitJson;
using PackageLibrary;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using USTCORi.WebLabClient.BizServiceReference;
using USTCORi.WebLabClient.Common;
using USTCORi.WebLabClient.DataModel;
using USTCORi.WebLabClient;

namespace ustcori_crack
{
    public class FakeHall2
    {
        private string destFileName = string.Empty;
        public LABINFO lab;
        public string path;
        public string userid;
        public string userType;
        private XElement downXml;
        private string downXmlAdd;
        public USTCORi.WebLabClient.Task SetLabStartTask;
        public int RunningID;
        public Task SetLabEndTask;
        public LABTIMERECORD labTime;
        public Task UserCountControlTask;
        public int ucID;
        public Task JFTask;
        public string clientip;
        public string clientarea;
        public string CourseID;
        private Package pack;

        public static ExamStartInfo examStartInfo;


        public FakeHall2(
          LABINFO lab,
          string path,
          string userid,
          string userType,
          string clientip,
          string clientarea,
          string CourseID)
        {
            //this.InitializeComponent();
            this.lab = lab;
            this.path = path;
            this.userid = userid;
            this.userType = userType;
            this.clientip = clientip;
            this.clientarea = clientarea;
            this.CourseID = CourseID;
        }


        public void UserControlModel()
        {
            if (this.UserCountControlTask == null)
            {
                this.UserCountControlTask = new Task();
                this.UserCountControlTask.BizCode = "UstcOri.BLL.BLLUserCountControl";
                this.UserCountControlTask.MethodName = "ucControlMethod";
                this.UserCountControlTask.ReturnType = typeof(int);
            }
            this.UserCountControlTask.SetParameter("ucControl", (object)new UserCountControl()
            {
                UserID = this.userid,
                ServiceID = "开始实验",
                OperType = 0,
                LabID = this.lab.LabID
            });
            try
            {
                SvcResponse svcResponse = this.UserCountControlTask.Run();
                if (!string.IsNullOrEmpty(svcResponse.Message) || svcResponse.Data == null)
                    return;
                this.ucID = (int)svcResponse.Data;
                if (this.ucID > 0)
                    this.CheckIsDown();
                else if (this.ucID < 0)
                {
                    Console.WriteLine("您前面还有" + (-this.ucID).ToString() + "人在排队，请耐心等候！温馨提醒：如果排到您，您10分钟内未进入的话则重新排队！", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    this.ExitApp();
                }
                else
                {
                    Console.WriteLine("您当前排在第一位，请耐心等候！温馨提醒：如果排到您，您10分钟内未进入的话则重新排队！");
                    this.ExitApp();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("服务器连接出错，请稍后重试！", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.ExitApp();
            }
        }

        public void EnducControl()
        {
            if (this.UserCountControlTask == null)
            {
                this.UserCountControlTask = new Task();
                this.UserCountControlTask.BizCode = "UstcOri.BLL.BLLUserCountControl";
                this.UserCountControlTask.MethodName = "ucControlMethod";
                this.UserCountControlTask.ReturnType = typeof(int);
            }
            this.UserCountControlTask.SetParameter("ucControl", (object)new UserCountControl()
            {
                ID = this.ucID,
                OperType = 1
            });
            try
            {
                SvcResponse svcResponse = this.UserCountControlTask.Run();
                if (string.IsNullOrEmpty(svcResponse.Message))
                {
                    if (svcResponse.Data == null)
                        return;
                    int data = (int)svcResponse.Data;
                    App.runningInfo.Global.ShutdownProcess();
                    APIMethod.Instant.RemoveHook();
                    Environment.Exit(0);
                    Application.Current.Shutdown();
                }
                else
                {
                    int num = (int)MessageBox.Show(svcResponse.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    App.runningInfo.Global.ShutdownProcess();
                    APIMethod.Instant.RemoveHook();
                    Environment.Exit(0);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("服务器连接出错，请稍后重试！", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                App.runningInfo.Global.ShutdownProcess();
                APIMethod.Instant.RemoveHook();
                Environment.Exit(0);
                Application.Current.Shutdown();
            }
        }

        public void CheckIsDown()
        {
            this.downXmlAdd = this.path + "Download";
            if (!Directory.Exists(this.downXmlAdd))
                Directory.CreateDirectory(this.downXmlAdd);
            if (!File.Exists(this.downXmlAdd + "\\download.xml"))
            {
                this.downXml = new XElement((XName)"root");
                this.downXml.Save(this.downXmlAdd + "\\download.xml");
            }
            else
                this.downXml = XElement.Load(this.downXmlAdd + "\\download.xml");
            IEnumerable<XElement> source = this.downXml.Elements((XName)"Experiment").Where<XElement>((Func<XElement, bool>)(n => (int)n.Attribute((XName)"ID") == this.lab.LabID));
            bool isDown;
            if (source.Count<XElement>() != 0)
            {
                if (((DateTime)source.First<XElement>().Attribute((XName)"UpTime")).Equals(this.lab.UpTime))
                {
                    string downloadExperimentPath = App.runningInfo.Const.DownloadExperimentPath;
                    if (new FileInfo(this.path + "DownLoad\\" + this.lab.LABTYPENAME + "\\" + this.lab.LABNAME + ".lab").Exists)
                        isDown = true;
                    else
                        isDown = new FileInfo(this.path + "DownLoad\\" + this.lab.LABTYPENAME + "\\" + this.lab.LABNAME + ".zip").Exists;
                }
                else
                    isDown = false;
            }
            else
                isDown = false;
            Console.WriteLine("已下载:" + isDown);
            this.StartLab(isDown);
        }

        public void StartLab(bool isDown)
        {
            if (isDown)
            {
                foreach (Process process in Process.GetProcesses())
                {
                    if (process.ProcessName.Trim() == "Main" || process.ProcessName.Trim() == "wowexec")
                    {

                        Console.WriteLine("已经有一个实验程序正在运行,如要开始新实验,请先结束当前实验");
                        
                        this.ExitApp();
                        return;
                    }
                }
                Process runningProcess = App.runningInfo.Global.RunningProcess;
                if (runningProcess != null && !runningProcess.HasExited)
                {

                    Console.WriteLine("已经有一个实验程序正在运行,如要开始新实验,请先结束当前实验");
                    
                    this.ExitApp();
                }
                else
                {
                    Console.WriteLine("准备完毕，接下来将启动实验程序，\n请在所有实验内容的实验数据表格中点击“完成操作”或“保存状态”等类似按钮(如果有)，之后点击“结束操作”。\n输入 y 继续。");
                    string ans = Console.ReadLine();
                    if (ans != "y")
                    {
                        return;
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes("USERID=" + App.runningInfo.Const.UserName + "&LABID=" + this.lab.LabID.ToString() + "&ClientIP=" + this.clientip + "&ClientArea=" + this.clientarea + "&CourseID=" + this.CourseID);
                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(App.runningInfo.Const.ServerIP + "/ServiceAPI/SetLabTimeRecordStart");
                    httpWebRequest.Method = "Post";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    httpWebRequest.ContentLength = (long)bytes.Length;
                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                        requestStream.Write(bytes, 0, bytes.Length);
                    using (WebResponse response = httpWebRequest.GetResponse())
                    {
                        StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        try
                        {
                            this.RunningID = Convert.ToInt32(streamReader.ReadToEnd());
                            streamReader.Close();
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine("服务器连接出错，此次操作记录无效");
                            
                            this.ExitApp();
                        }
                    }
                    string str1 = "";
                    try
                    {
                        if (this.JFTask == null)
                        {
                            this.JFTask = new Task();
                            this.JFTask.BizCode = "UstcOri.BLL.BLLLabClent";
                            this.JFTask.MethodName = "JFIsAccess";
                            this.JFTask.ReturnType = typeof(string);
                        }
                        this.JFTask.SetParameter("UserID", (object)App.runningInfo.Const.UserName);
                        this.JFTask.SetParameter("LabName", (object)this.lab.LABNAME);
                        this.JFTask.SetParameter("SysID", (object)1);
                        try
                        {
                            SvcResponse svcResponse = this.JFTask.Run();
                            if (string.IsNullOrEmpty(svcResponse.Message))
                            {
                                if (svcResponse.Data != null)
                                    str1 = (string)svcResponse.Data;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        if (string.IsNullOrEmpty(str1))
                        {
                            string downloadExperimentPath = App.runningInfo.Const.DownloadExperimentPath;
                            FileInfo fileInfo = new FileInfo(this.path + "DownLoad\\" + this.lab.LABTYPENAME + "\\" + this.lab.LABNAME + ".lab");
                            string environmentVariable;
                            if (fileInfo.Exists)
                            {
                                this.pack = new Package(fileInfo);
                                environmentVariable = Environment.GetEnvironmentVariable("TEMP");
                                this.pack.SetDirectoryToUnPack(environmentVariable + "\\lab");
                                this.pack.Run();
                            }
                            else
                            {
                                string str2 = this.path + "DownLoad\\" + this.lab.LABTYPENAME + "\\" + this.lab.LABNAME + ".zip";
                                if (new FileInfo(str2).Exists)
                                {
                                    environmentVariable = Environment.GetEnvironmentVariable("TEMP");
                                    ZipHelp.UnZipFile(str2, environmentVariable + "\\lab");
                                }
                                else
                                {
                                    int num = (int)MessageBox.Show("文件未找到", "ERROR", MessageBoxButton.OK, MessageBoxImage.Hand);
                                    this.ExitApp();
                                    return;
                                }
                            }
                            Process process = new Process();
                            if (new FileInfo(environmentVariable + "\\lab\\Main.exe").Exists)
                            {
                                process.EnableRaisingEvents = true;
                                process.Exited += new EventHandler(this.LabProcess_Exited);
                                process.StartInfo.FileName = environmentVariable + "\\lab\\Main.exe";
                                process.StartInfo.WorkingDirectory = environmentVariable + "\\lab\\";
                            }
                            else if (new FileInfo(environmentVariable + "\\lab\\4\\Main.exe").Exists)
                            {
                                process.EnableRaisingEvents = true;
                                process.Exited += new EventHandler(this.LabProcess_Exited);
                                process.StartInfo.FileName = environmentVariable + "\\lab\\4\\Main.exe";
                                process.StartInfo.WorkingDirectory = environmentVariable + "\\lab\\4\\";
                            }
                            else
                            {

                                Console.WriteLine("仿真实验部署错误，实验无法运行！");
                                
                                this.ExitApp();
                            }
                            DateTime now = DateTime.Now;
                            string s1 = "USTCOri" + this.userid + this.lab.LABNAME + "." + this.CourseID + (Convert.ToDateTime(now.ToString()) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds.ToString();
                            string s2 = "";
                            foreach (byte num in MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(s1)))
                                s2 += num.ToString("X");
                            string pToEncrypt = Convert.ToBase64String(Encoding.UTF8.GetBytes(APIMethod.Instant.hWnd_local.ToString())) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes("-sim")) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes("")) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.userid)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.lab.LabID.ToString())) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.lab.LABNAME + "." + this.CourseID)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.RunningID.ToString())) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s2)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(now.ToString(""))) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(App.runningInfo.Const.ServerIP)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes("ZH-CN"));
                            string str3 = XlsxFileWriter.Write(pToEncrypt);
                            CMMDataCrypto cmmDataCrypto = new CMMDataCrypto();
                            if (this.lab.INTRODUTION == "1")
                                str3 = cmmDataCrypto.Encrypt(pToEncrypt);
                            if (System.IO.File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\labConfig.xml") && XElement.Load(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\labConfig.xml").Element((XName)"IsEncrypt").Value.ToUpper().Trim() == "TRUE")
                                process.StartInfo.Arguments = str3;
                            process.Start();
                            Program.shouldAlert = false;
                            App.runningInfo.Global.RunningExperiment = this.lab;
                            App.runningInfo.Global.RunningProcess = process;
                            Thread.Sleep(3000);
                            if (!System.IO.File.Exists(process.StartInfo.FileName))
                                return;
                        }
                        else
                        {
                            Console.WriteLine("计费失败：" + str1);
                            
                            this.ExitApp();
                        }
                    }
                    catch (Exception ex1)
                    {
                        int num12 = (int)MessageBox.Show("文件打开出错" + ex1.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Hand);
                        
                        try
                        {
                            System.IO.File.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\Main.exe");
                            Directory.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab", true);
                        }
                        catch (Exception ex2)
                        {
                            this.ExitApp();
                            return;
                        }
                        this.ExitApp();
                    }
                }
            }
            else
            {
                try
                {
                    DownloadWindow downloadWindow = new DownloadWindow();
                    downloadWindow.SetExperiment(lab, path);
                    while (!downloadWindow.isDownloadCompleted)
                    {
                        Thread.Sleep(100);
                    }
                    if (downloadWindow.isSuccess)
                        this.StartLab(true);
                    else
                        this.ExitApp();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("下载实验失败", "ERROR");
                    this.ExitApp();
                }
            }
        }





        private void LabProcess_Exited(object sender, EventArgs e)
        {

            XmlDocument modifiedXml = Utiles.ModifyExperiment(Utiles.LoadXml());

            try
            {
                if (System.IO.File.Exists(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\labConfig.xml"))
                {
                    DateTime now = DateTime.Now;
                    string[] strArray = new string[6];
                    int num1 = now.Year;
                    strArray[0] = num1.ToString();
                    num1 = now.Month;
                    strArray[1] = num1.ToString();
                    strArray[2] = now.Day.ToString();
                    strArray[3] = now.Hour.ToString();
                    strArray[4] = now.Minute.ToString();
                    strArray[5] = now.Second.ToString();
                    string str1 = string.Concat(strArray);
                    string str2 = App.runningInfo.Const.UserName + str1 + ".xml";
                    string key = Sec.GenerateKey(App.runningInfo.Const.UserName + "," + this.lab.LabID.ToString() + str2);
                    byte[] inArray = null;
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        modifiedXml.Save((Stream)outStream);
                        inArray = JsonWriter.Write(outStream.ToArray(), "haohaoxuexi,tiantianxiangshang.");
                        outStream.Close();
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes("UserID=" + App.runningInfo.Const.UserName + "&LabID=" + this.lab.LabID.ToString() + "&LabName=" + this.lab.LABNAME + "." + this.CourseID + "&RecordID=" + this.RunningID.ToString() + "&FileName=" + str2 + "&Content=" + Convert.ToBase64String(inArray) + "&Key=" + key);
                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(App.runningInfo.Const.ServerIP + "/ServiceAPI/UpdateRecord");
                    httpWebRequest.Method = "Post";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    Console.WriteLine("实验数据修改完成，按 y 显示答案，其他键跳过：");
                    if (Console.ReadKey().KeyChar == 'y')
                    {
                        PaperReviewer pr = new PaperReviewer();
                        pr.ReviewOpXml(modifiedXml);
                    }
                    httpWebRequest.ContentLength = (long)bytes.Length;
                    if(!wait()) return;
                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                        requestStream.Write(bytes, 0, bytes.Length);
                    using (WebResponse response = httpWebRequest.GetResponse())
                    {
                        StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        try
                        {
                            this.RunningID = Convert.ToInt32(streamReader.ReadToEnd());
                            streamReader.Close();
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine("服务器连接出错，此次操作记录无效");

                            this.ExitApp();
                        }
                    }

                }
                else
                {
                    DateTime now = DateTime.Now;
                    string[] strArray = new string[6];
                    int num4 = now.Year;
                    strArray[0] = num4.ToString();
                    num4 = now.Month;
                    strArray[1] = num4.ToString();
                    strArray[2] = now.Day.ToString();
                    strArray[3] = now.Hour.ToString();
                    strArray[4] = now.Minute.ToString();
                    strArray[5] = now.Second.ToString();
                    string str3 = string.Concat(strArray);
                    string str4 = App.runningInfo.Const.UserName + "_" + str3 + ".xml";
                    string key = Sec.GenerateKey(App.runningInfo.Const.UserName + "," + this.lab.LabID.ToString() + str4);
                    byte[] inArray = (byte[])null;
                    XmlDocument xmlDocument = modifiedXml;
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        xmlDocument.Save((Stream)outStream);
                        inArray = JsonWriter.Write(outStream.ToArray(), "haohaoxuexi,tiantianxiangshang.");
                        outStream.Close();
                    }
                    byte[] bytes = Encoding.UTF8.GetBytes("UserID=" + App.runningInfo.Const.UserName + "&LabID=" + this.lab.LabID.ToString() + "&LabName=" + this.lab.LABNAME + "." + this.CourseID + "&RecordID=" + this.RunningID.ToString() + "&FileName=" + str4 + "&Content=" + Convert.ToBase64String(inArray) + "&Key=" + key);
                    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(App.runningInfo.Const.ServerIP + "/ServiceAPI/UpdateRecord");
                    httpWebRequest.Method = "Post";
                    httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                    httpWebRequest.ContentLength = (long)bytes.Length;
                    Console.WriteLine("实验数据修改完成，按 y 显示答案，其他键跳过：");
                    if (Console.ReadKey().KeyChar == 'y')
                    {
                        PaperReviewer pr = new PaperReviewer();
                        pr.ReviewOpXml(modifiedXml);
                    }
                    if (!wait()) return;
                    using (Stream requestStream = httpWebRequest.GetRequestStream())
                        requestStream.Write(bytes, 0, bytes.Length);
                    using (WebResponse response = httpWebRequest.GetResponse())
                    {
                        StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                        try
                        {
                            this.RunningID = Convert.ToInt32(streamReader.ReadToEnd());
                            streamReader.Close();
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine("服务器连接出错，此次操作记录无效", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            
                            this.ExitApp();
                        }
                    }
                }
                Console.WriteLine("提交完成，可查看分数。");
                Console.ReadKey();
                Program.shouldAlert = true;
                try
                {
                    if (App.runningInfo.Global.RunningProcess != null && !App.runningInfo.Global.RunningProcess.HasExited)
                        App.runningInfo.Global.RunningProcess.Kill();
                    App.runningInfo.Global.RunningProcess.Close();
                }
                catch (Exception ex)
                {
                }
                try
                {
                    System.IO.File.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\Main.exe");
                    Directory.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab", true);
                    System.IO.File.Delete(this.destFileName);
                }
                catch (Exception ex)
                {
                    this.ExitApp();
                    return;
                }
                this.ExitApp();
            }
            catch (Exception ex)
            {
                this.ExitApp();
            }
        }

        public void ExitApp() => this.EnducControl();
        
        private bool wait()
        {
            Console.WriteLine("请输入在提交前需要等待时间（秒），输入 -1 阻止提交：");
            int waitTime = int.Parse(Console.ReadLine());
            while (true)
            {
                
                if (waitTime < 0)
                {
                    Program.shouldAlert = true;
                    return false;
                }
                if(waitTime < 600)
                {
                    Console.WriteLine("输入时间小于10分钟，可能有风险，输入 continue 继续提交，或输入更长的时间：");
                }
                else
                {
                    break;
                }
                string input = Console.ReadLine();
                if (input == "continue")
                    break;
                else
                    waitTime = int.Parse(input);
            }

            while (waitTime > 0)
            {
                Console.WriteLine("剩余时间：" + waitTime + "秒");
                if (waitTime > 60)
                {
                    Thread.Sleep(60000);
                    waitTime -= 60;
                }
                else
                {
                    Thread.Sleep(waitTime * 1000);
                    waitTime = 0;
                }
            }
            return true;
        }
    }
}
