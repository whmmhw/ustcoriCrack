using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using USTCORi.WebLabClient;
using USTCORi.WebLabClient.BizServiceReference;
using USTCORi.WebLabClient.Common;
using USTCORi.WebLabClient.DataModel;
using USTCORi.WebLabClient.FileTransferReference;
using PackageLibrary;
using System.Xml;

namespace ustcori_crack
{
    public class FakeHall
    {
        public const char SPLIT = ';';
        public const char SPLIT2 = ',';
        public const double MINI_DELTA = 1E-11;
        public const string LOST_WHOLEQUESTION = "LostWholeQuestion";
        public const string LOST_WHOLECHECKPOINT = "LostWholeCheckPoint";
        public const string USTCORI_SHOWSELF = "USTCORI_SHOWSELF";
        public const string SAVE_DATA = "SAVE_DATA";
        public const string USTCORI_SHUTDOWNEXP = "USTCORI_SHUTDOWNEXP";
        public const string USTCORI_INITCOMPLETE = "USTCORI_INITCOMPLETE";
        public const string USTCORI_ADDTIME = "USTCORI_ADDTIME";
        public const string USTCORI_GETEXAMTIME = "USTCORI_GETEXAMTIME";
        public const string USTCORI_SENDPAPER = "USTCORI_SENDPAPER";
        public const string USTCORI_USERTYPE = "USTCORI_USERTYPE";
        private string destFileName = string.Empty;
        public LABINFO lab;
        public string path;
        public string userid;
        public string userType;
        private XElement downXml;
        private string downXmlAdd;
        public Task SetLabStartTask;
        public int RunningID;
        public Task SetLabEndTask;
        public LABTIMERECORD labTime;
        public Task UserCountControlTask;
        public int ucID = 0;
        public Task JFTask;
        private Package pack;
        private bool hasAnswered;
        public string StudentID = "";
        public string StudentName = "";
        public string LabName = "";
        public string LabTypeName = "";
        public string FinishPercent = "";
        private DispatcherTimer closetimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 0, 0, 5)
        };
        public string ZipFilePath;
        public string UpDicPath;
        private FileTransferClient FileTransferClient;
        private string UpTo = "Upload\\LabImage\\";
        public Task UnZipFileTask;
        public IList<LabRecord> recordList = (IList<LabRecord>)new List<LabRecord>();
        public string Name;
        public Task InsertLabRecordTask;
        private bool _contentLoaded;

        public FakeHall(LABINFO lab, string path, string userid, string userType)
        {
            this.lab = lab;
            this.path = path;
            this.userid = userid;
            this.userType = userType;
            this.FileTransferClient = WcfServiceClientFactory<FileTransferClient, IFileTransfer>.CreateServiceClient();
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
                {
                    this.CheckIsDown();
                }
                else
                {
                    int num = (int)MessageBox.Show("服务器正忙，请稍后重试！", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    this.ExitApp();
                }
            }
            catch (Exception ex)
            {
                int num = (int)MessageBox.Show("服务器连接出错，请稍后重试！", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                    Environment.Exit(0);
                    Application.Current.Shutdown();
                }
                else
                {
                    Console.WriteLine(svcResponse.Message, "Warning");
                    App.runningInfo.Global.ShutdownProcess();
                    Environment.Exit(0);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("服务器连接出错，请稍后重试！");
                App.runningInfo.Global.ShutdownProcess();
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
                    isDown = new FileInfo(this.path + "Download\\" + this.lab.LABTYPENAME + "\\" + this.lab.LABNAME + ".lab").Exists;
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
                    if (this.SetLabStartTask == null)
                    {
                        this.SetLabStartTask = new Task();
                        this.SetLabStartTask.BizCode = "UstcOri.BLL.BLLLabClent";
                        this.SetLabStartTask.MethodName = "SetLabTimeRecord";
                        this.SetLabStartTask.ReturnType = typeof(int);
                    }
                    this.labTime = new LABTIMERECORD();
                    this.labTime.Mark = "1";
                    this.labTime.USERID = App.runningInfo.Const.UserName;
                    this.labTime.LABID = this.lab.LabID;
                    this.LabName = this.lab.LABNAME;
                    this.LabTypeName = this.lab.LABTYPENAME;
                    this.SetLabStartTask.SetParameter("labTime", (object)this.labTime);
                    Exception exception;
                    try
                    {
                        SvcResponse svcResponse = this.SetLabStartTask.Run();
                        if (string.IsNullOrEmpty(svcResponse.Message))
                        {
                            if (svcResponse.Data != null)
                            {
                                this.RunningID = (int)svcResponse.Data;
                                if (this.RunningID <= 0)
                                {
                                    Console.WriteLine("服务器连接出错，此次操作记录无效");
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        Console.WriteLine("服务器连接出错，此次操作记录无效：" + ex.Message);
                    }
                    string str = "计费过程出现异常";
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
                                    str = (string)svcResponse.Data;
                            }
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        if (string.IsNullOrEmpty(str))
                        {
                            string downloadExperimentPath = App.runningInfo.Const.DownloadExperimentPath;
                            FileInfo file = new FileInfo(this.path + "Download\\" + this.lab.LABTYPENAME + "\\" + this.lab.LABNAME + ".lab");
                            if (file.Exists)
                            {
                                this.pack = new Package(file);
                                string environmentVariable = Environment.GetEnvironmentVariable("TEMP");
                                this.pack.SetDirectoryToUnPack(environmentVariable + "\\lab");
                                this.pack.Run();




                                Process process = new Process();
                                process.StartInfo.FileName = environmentVariable + "\\lab\\Main.exe";
                                process.StartInfo.WorkingDirectory = environmentVariable + "\\lab\\";
                                DateTime now = DateTime.Now;
                                string s1 = "USTCOri" + this.userid + this.lab.LABNAME + (Convert.ToDateTime(now.ToString()) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds.ToString();
                                string s2 = "";
                                foreach (byte num in MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(s1)))
                                    s2 += num.ToString("X");
                                if (this.lab.INTRODUTION == "1")
                                    process.StartInfo.Arguments = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.userid)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.lab.LabID.ToString())) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.lab.LABNAME)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.RunningID.ToString())) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s2)) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(now.ToString())) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(App.runningInfo.Const.ServerIP));
                                process.Exited += new EventHandler(this.ap_Exited);

                                Program.shouldAlert = false;

                                process.EnableRaisingEvents = true;
                                process.Start();
                                App.runningInfo.Global.RunningExperiment = this.lab;
                                App.runningInfo.Global.RunningProcess = process;



                            }
                            else
                            {
                                int num = (int)MessageBox.Show("文件未找到", "ERROR", MessageBoxButton.OK, MessageBoxImage.Hand);
                                this.ExitApp();
                            }
                        }
                        else
                        {
                            int num = (int)MessageBox.Show("计费失败：" + str, "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            this.ExitApp();
                        }
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine("文件打开出错" + ex1.Message + ex1.StackTrace);
                        try
                        {
                            File.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\Main.exe");
                            Directory.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab", true);
                        }
                        catch (Exception ex2)
                        {
                            return;
                        }
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
                        Console.WriteLine("下载实验失败1");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("下载实验失败2");
                    this.ExitApp();
                }
            }
        }
       
        private void ap_Exited(object sender, EventArgs e)
        {
            XmlDocument xmlDoc = Utiles.ModifyExperiment(Utiles.LoadXml());

            this.closetimer.Stop();
            if (this.SetLabEndTask == null)
            {
                this.SetLabEndTask = new Task();
                this.SetLabEndTask.BizCode = "UstcOri.BLL.BLLLabClent";
                this.SetLabEndTask.MethodName = "SetLabTimeRecord";
                this.SetLabEndTask.ReturnType = typeof(int);
            }
            LABTIMERECORD labtimerecord = new LABTIMERECORD();
            labtimerecord.Mark = "2";
            labtimerecord.LABID = this.lab.LabID;
            labtimerecord.USERID = App.runningInfo.Const.UserName;
            labtimerecord.ID = this.RunningID;
            PaperReviewer pr = new PaperReviewer();
            this.StudentScore = pr.ReviewOpXml(xmlDoc);
            string targetpath = "Upload\\LabDate\\" + this.lab.LABNAME;
            DateTime now = DateTime.Now;
            string[] strArray1 = new string[6];
            string[] strArray2 = strArray1;
            int num = now.Year;
            string str1 = num.ToString();
            strArray2[0] = str1;
            string[] strArray3 = strArray1;
            num = now.Month;
            string str2 = num.ToString();
            strArray3[1] = str2;
            string[] strArray4 = strArray1;
            num = now.Day;
            string str3 = num.ToString();
            strArray4[2] = str3;
            string[] strArray5 = strArray1;
            num = now.Hour;
            string str4 = num.ToString();
            strArray5[3] = str4;
            string[] strArray6 = strArray1;
            num = now.Minute;
            string str5 = num.ToString();
            strArray6[4] = str5;
            string[] strArray7 = strArray1;
            num = now.Second;
            string str6 = num.ToString();
            strArray7[5] = str6;
            string str7 = string.Concat(strArray1);
            string filename = App.runningInfo.Const.UserName + "_" + str7 + ".xml";
            this.UploadDateXml(xmlDoc, targetpath, filename);
            labtimerecord.LabDateUrl = targetpath + "\\" + filename;
            labtimerecord.Score = this.StudentScore;
            this.SetLabEndTask.SetParameter("labTime", (object)labtimerecord);
            Console.WriteLine("实验数据修改完成，本次得分：{0}，\n请输入在提交前需要等待时间（秒），输入 -1 阻止提交：", this.StudentScore);
            int waitTime = int.Parse(Console.ReadLine());
            if (waitTime < 0)
            {
                Program.shouldAlert = true;
                return;
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
            try
            {
                SvcResponse svcResponse = this.SetLabEndTask.Run();
                if (string.IsNullOrEmpty(svcResponse.Message))
                {
                    if (svcResponse.Data != null)
                    {
                        this.RunningID = (int)svcResponse.Data;
                        if (this.RunningID <= 0)
                        {
                            Console.WriteLine("服务器链接出错，此次操作记录无效");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("服务器连接出错，此次操作记录无效: " + ex.Message);
            }
            Console.WriteLine("提交完成，可查看分数。");
            Program.shouldAlert = true;
            try
            {
                File.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab\\Main.exe");
                Directory.Delete(Environment.GetEnvironmentVariable("TEMP") + "\\lab", true);
                File.Delete(this.destFileName);
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public double StudentScore { get; set; }

        public void UploadDateXml(XmlDocument xmlDocument, string targetpath, string filename)
        {
            byte[] inArray = null;
            using (MemoryStream outStream = new MemoryStream())
            {
                xmlDocument.Save(outStream);
                inArray = outStream.ToArray();
                outStream.Close();
            }
            if (inArray.Length == 0)
                return;
            this.FileTransferClient.UploadFile(filename, targetpath, inArray, false);

        }

        public void ExitApp() => this.EnducControl();

    }
}


