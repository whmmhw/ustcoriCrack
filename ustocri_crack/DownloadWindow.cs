using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using USTCORi.WebLabClient;
using USTCORi.WebLabClient.BizServiceReference;
using USTCORi.WebLabClient.DataModel;
using USTCORi.WebLabClient.Menu;

namespace ustcori_crack
{
    public partial class DownloadWindow
    {
        private WebClient downloadClient;
        private Uri downloadAddress;
        private string downto;
        public bool isSuccess = false;
        public bool isDownloadCompleted = false;
        private string path;
        public LABINFO experiment;
        public Task AddOneToLabFileDownLoadTask;
        public long TotalBytesToReceive = 0;
        private int nowProgress = -10;
        private DispatcherTimer CalBytesTime = new DispatcherTimer()
        {
            Interval = new TimeSpan(0, 0, 0, 1)
        };

        public DownloadWindow()
        {
            this.downto = App.runningInfo.Const.DownloadExperimentPath;
        }

        public void SetExperiment(LABINFO ex, string path)
        {
            this.experiment = ex;
            this.path = path;
            int a = App.runningInfo.Const.ServerIP.IndexOf("http://");
            string b = a == 0 ? "" : "http://";
            this.downloadAddress = new Uri(b + App.runningInfo.Const.ServerIP + "/Upload/lab/" + experiment.LabFileUrl);
            this.downto = path + "DownLoad\\" + experiment.LABTYPENAME + "\\";
            Directory.CreateDirectory(this.downto);
            downto = downto + this.experiment.LABNAME + ".lab";
            this.BeginDownload();
        }

        public void BeginDownload()
        {
            this.downloadClient = new WebClient();
            try
            {
                this.downloadClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(this.downloadClient_DownloadProgressChanged);
                this.downloadClient.DownloadFileCompleted += new AsyncCompletedEventHandler(this.downloadClient_DownloadFileCompleted);
                this.downloadClient.DownloadFileAsync(this.downloadAddress, this.downto);
                Console.WriteLine("开始下载……");
                this.CalBytesTime.Start();
                if (this.AddOneToLabFileDownLoadTask == null)
                {
                    this.AddOneToLabFileDownLoadTask = new Task();
                    this.AddOneToLabFileDownLoadTask.BizCode = "UstcOri.BLL.BLLLabClent";
                    this.AddOneToLabFileDownLoadTask.MethodName = "AddOneToLabFileDownLoadCount";
                    this.AddOneToLabFileDownLoadTask.ReturnType = typeof(int);
                }
                this.AddOneToLabFileDownLoadTask.SetParameter("labFile", (object)new LABFILE()
                {
                    LABID = this.experiment.LabID,
                    FILETYPE = 1
                });
                try
                {
                    SvcResponse svcResponse = this.AddOneToLabFileDownLoadTask.Run();
                    if (!string.IsNullOrEmpty(svcResponse.Message) || svcResponse.Data == null)
                        return;
                    int data = (int)svcResponse.Data;
                }
                catch (Exception ex)
                {
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("下载失败："+ ex.ToString());
            }
        }

        private void downloadClient_DownloadProgressChanged(
          object sender,
          DownloadProgressChangedEventArgs e)
        {
            this.TotalBytesToReceive = e.TotalBytesToReceive;
            int progress = (int)(e.BytesReceived * 100L / e.TotalBytesToReceive);
            if(progress > nowProgress + 4)
            {
                nowProgress = progress;
                Console.Write(progress+"%... ");
            }
        }

        private void downloadClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.CalBytesTime.Stop();
            FileInfo fileInfo = new FileInfo(this.downto);
            if (fileInfo.Length == 0L || fileInfo.Length != this.TotalBytesToReceive)
            {
                int num1 = (int)MessageBox.Show("文件下载出错");
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                    int num2 = (int)MessageBox.Show(ex.Message);
                }
                Console.WriteLine("下载失败1");
            }
            else
            {
                AddDownloadXElement(this.experiment, this.path);
                Console.WriteLine("\n下载完成。");
                this.isSuccess = true;
            }
            this.isDownloadCompleted = true;
        }
        public void AddDownloadXElement(LABINFO ex, string path)
        {
            XElement downXml = XElement.Load(path + "Download\\download.xml");
            IEnumerable<XElement> source = downXml.Elements((XName)"Experiment").Where<XElement>((Func<XElement, bool>)(n => (int)n.Attribute((XName)"ID") == ex.LabID));
            if (source.Count<XElement>() != 0)
                source.First<XElement>().Remove();
            downXml.Add((object)new XElement((XName)"Experiment", new object[4]
            {
        (object) new XAttribute((XName) "Sort", (object) ex.LABTYPENAME),
        (object) new XAttribute((XName) "Name", (object) ex.LABNAME),
        (object) new XAttribute((XName) "UpTime", (object) ex.UpTime),
        (object) new XAttribute((XName) "ID", (object) ex.LabID)
            }));
            downXml.Save(path + "Download\\download.xml");
        }
    }

}