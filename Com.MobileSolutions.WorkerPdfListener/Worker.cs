using Com.MobileSolutions.WorkerPdfListener.Models;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Com.MobileSolutions.WorkerPdfListener
{
    public class Worker : BackgroundService
    {
        private static ConcurrentQueue<string> fileNames = new ConcurrentQueue<string>();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly HttpClient httpclient;
        private static List<string> threads = new List<string>();
        private static int processCount = 0;
        private static bool threadOne = false;
        private static bool threadTwo = false;
        private static bool threadThree = false;
        static Worker()
        {
            httpclient = new HttpClient();
            httpclient.Timeout = TimeSpan.FromMinutes(300);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Factory.StartNew(QueueHandlerAsync);
            //var builder = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory.Replace("\\bin\\Debug\\netcoreapp2.1", string.Empty)).AddJsonFile("appsettings.json");
            //var configuration = builder.Build();

            FileSystemWatcher watcher = new FileSystemWatcher();
            Directory.CreateDirectory("D:\\PdfReader\\Trial\\Verizon\\archive");
            Directory.CreateDirectory("D:\\PdfReader\\Trial\\Verizon\\error");
            Directory.CreateDirectory("D:\\PdfReader\\Trial\\Verizon\\input");
            Directory.CreateDirectory("D:\\PdfReader\\Trial\\Verizon\\logs");
            Directory.CreateDirectory("D:\\PdfReader\\Trial\\Verizon\\output");
            Directory.CreateDirectory("D:\\PdfReader\\Trial\\Verizon\\reconcile");
            string filePath = "D:\\PdfReader\\Trial\\Verizon\\input";
            watcher.Path = filePath;
            watcher.EnableRaisingEvents = true;
            watcher.NotifyFilter = NotifyFilters.FileName;
            watcher.Filter = "*.*";
            //watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Created += (o, e) =>
            {
                fileNames.Enqueue(e.FullPath);
            };

            new AutoResetEvent(false).WaitOne();
        }

        private static async Task QueueHandlerAsync()
        {
            bool run = true;
            //var builder = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory.Replace("\\bin\\Debug\\netcoreapp2.1", string.Empty)).AddJsonFile("appsettings.json");
            //var configuration = builder.Build();
            int init = 0;
            List<string> nameList = new List<string>();
            AppDomain.CurrentDomain.DomainUnload += (s, e) =>
            {
                run = false;
                fileNames.Enqueue("stop");
            };
            while (run)
            {

                string filename;
                if (fileNames.TryDequeue(out filename) && run)
                {
                    if (init == 0)
                    {
                        init++;
                    }
                    var info = new FileInfo(filename);
                    if (info.Extension.ToLower() == ".pdf")
                    {

                        while (true)
                        {
                            if (!(threadOne))
                            {
                                Thread.Sleep(500);
                                string threadNumber = "";
                                var url = "";
                                //if (!threadOne)
                                //{
                                //    threadNumber = "one";
                                //    url = "http://localhost/PdfReaderOne/api/pdfreader";
                                //}
                                //else if (!threadTwo)
                                //{
                                //    threadNumber = "two";
                                //    url = "http://localhost/PdfReaderTwo/api/pdfreader";
                                //}
                                //else
                                //{
                                //    threadNumber = "three";
                                //    url = "http://localhost/PdfReaderThree/api/pdfreader";
                                //}

                                threadNumber = "one";
                                url = "http://localhost/TrialPdfReader/api/pdfreader";

                                var pathValues = new PathValues() { Path = filename, OutputPath = "D:\\PdfReader\\Trial\\Verizon\\output", ProcessedFilesPath = "D:\\PdfReader\\Trial\\Verizon\\archive", FailedFiles = "D:\\PdfReader\\Trial\\Verizon\\error" };
                                var jsonParams = JsonConvert.SerializeObject(pathValues);
                                var contentData = new StringContent(jsonParams, System.Text.Encoding.UTF8, "application/json");
                                WebApiPostCallAsync(filename, processCount + "Thread", url, jsonParams, threadNumber);

                                break;
                            }
                        }
                    }
                }
            }
        }

        private static async void WebApiPostCallAsync(string currentFile, string threadName, string url, string contentData, string threadNumber)
        {
            if (threadNumber == "one")
            {
                threadOne = true;
            }
            else if (threadNumber == "two")
            {
                threadTwo = true;
            }
            else
            {
                threadThree = true;
            }

            processCount++;
            threads.Add(threadName);
            var contentDatas = new StringContent(contentData, System.Text.Encoding.UTF8, "application/json");
            var response = await httpclient.PostAsync(url, contentDatas);
            processCount--;

            if (threadNumber == "one")
            {
                threadOne = false;
            }
            else if (threadNumber == "two")
            {
                threadTwo = false;
            }
            else
            {
                threadThree = false;
            }
        }
    }
}
