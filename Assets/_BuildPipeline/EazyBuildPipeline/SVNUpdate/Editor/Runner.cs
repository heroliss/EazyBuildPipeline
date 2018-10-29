using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;
using EazyBuildPipeline.SVNUpdate.Configs;

namespace EazyBuildPipeline.SVNUpdate
{
    [Serializable]
    public partial class Runner : EBPRunner
        <Module, ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public bool IsPartOfPipeline = false;
        public enum VersionStateEnum { Unknow, Obsolete, Latest }
        public enum ChangeStateEnum { Unknow, Changed, NoChange }
        //SVNInfo
        public bool Available = false;
        public VersionStateEnum VersionState = VersionStateEnum.Unknow;
        public string SVNInfo = "";
        public string InfoErrorMessage = "";
        public Action<bool> InfoExitedAction;
        //Diff
        public ChangeStateEnum LocalChangeState = ChangeStateEnum.Unknow;
        public string ChangedFiles = "";
        public string DiffErrorMessage = "";
        public Action<bool> DiffExitedAction;
        //Version
        public string LocalVersion = "";
        public string RepositoryVersion = "";
        //Update
        public string message = "";
        public string errorMessage = "";

        public Runner(Module module) : base(module)
        {
        }

        protected override void CheckProcess(bool onlyCheckConfig = false)
        {
        }

        protected override void RunProcess()
        {
            int progress = 0;
            message = "";
            errorMessage = "";
            EditorUtility.DisplayProgressBar("SVN Update Starting...", "", 0);
            Process p;
            //Revert
            p = ExcuteCommand("svn", "--non-interactive revert -R .",
                                      OnReceived, OnErrorReceived, OnExited);
            while (!p.HasExited)
            {
                EditorUtility.DisplayProgressBar("SVN Revert", message, (float)(progress++ % 1000) / 1000);
                System.Threading.Thread.Sleep(50);
            }
            if (p.ExitCode != 0)
            {
                throw new EBPException("SVN Revert 时发生错误：" + errorMessage);
            }
            //Update
            p = ExcuteCommand("svn", "--non-interactive update",
                                      OnReceived, OnErrorReceived, OnExited);
            while (!p.HasExited)
            {
                EditorUtility.DisplayProgressBar("SVN Update", message, (float)(progress++ % 600) / 600);
                System.Threading.Thread.Sleep(50);
            }
            if (p.ExitCode != 0)
            {
                throw new EBPException("SVN Update 时发生错误：" + errorMessage);
            }
            //Resolve
            p = ExcuteCommand("svn", "--non-interactive resolve --accept theirs-conflict -R",
                                                 OnReceived, OnErrorReceived, OnExited);
            while (!p.HasExited)
            {
                EditorUtility.DisplayProgressBar("SVN Resolve", message, (float)(progress++ % 600) / 600);
                System.Threading.Thread.Sleep(50);
            }
            if (p.ExitCode != 0)
            {
                throw new EBPException("SVN Resolve 时发生错误：" + errorMessage);
            }
            EditorUtility.DisplayProgressBar("SVN", "Finish!", 1);
        }

        void OnExited(object sender, EventArgs e)
        {
        }

        void OnErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                errorMessage += e.Data + "\n";
            }
        }

        void OnReceived(object sender, DataReceivedEventArgs e)
        {
            message = e.Data;
        }

        public void RunCheckProcess()
        {
            Available = false;
            VersionState = VersionStateEnum.Unknow;
            SVNInfo = "";
            InfoErrorMessage = "";
            LocalChangeState = ChangeStateEnum.Unknow;
            ChangedFiles = "";
            DiffErrorMessage = "";
            LocalVersion = "";
            RepositoryVersion = "";
            try
            {
                ExcuteCommand("svn", "info", OnInfoReceived, OnInfoErrorReceived, OnInfoExited);
            }
            catch(Exception err)
            {
                InfoErrorMessage = err.Message;
                InfoExitedAction(false);
            }
        }

        void OnInfoExited(object sender, EventArgs e)
        {
            Process process = (Process)sender;
            Available = process.ExitCode == 0;
            if (Available)
            {
                GetRepositoryInfoAndLocalVersion();
            }
            else
            {
                IsPartOfPipeline = false;
            }
            if (InfoExitedAction != null)
            {
                InfoExitedAction(Available);
            }
        }

        void OnRepositoryInfoExited(object sender, EventArgs e)
        {
            Process process = (Process)sender;
            Available = process.ExitCode == 0;
            if (Available)
            {
                //设置版本状态
                if (LocalVersion != "" && RepositoryVersion != "")
                {
                    VersionState = RepositoryVersion == LocalVersion ? VersionStateEnum.Latest : VersionStateEnum.Obsolete;
                }
                //检查本地修改
                if (Module.ModuleConfig.Json.EnableCheckDiff)
                {
                    try
                    {
                        ExcuteCommand("/bin/bash", Path.Combine(Module.ModuleConfig.ModuleRootPath, "SVNDiff.sh"),
                           OnDiffReceived, OnDiffErrorReceived, OnDiffExited);
                    }
                    catch (Exception err)
                    {
                        InfoErrorMessage = err.Message;
                        DiffExitedAction(false);
                    }
                }
            }
            else
            {
                IsPartOfPipeline = false;
            }
            if (InfoExitedAction != null)
            {
                InfoExitedAction(Available);
            }
        }

        void GetRepositoryInfoAndLocalVersion()
        {
            const string urlName = "URL: ";
            const string versionName = "Revision: ";
            foreach (var line in SVNInfo.Split('\n'))
            {
                //获取仓库信息
                if (line.Length > urlName.Length &&
                    line.Substring(0, urlName.Length) == urlName)
                {
                    string repositoryURL = line.Substring(urlName.Length);
                    try
                    {
                        ExcuteCommand("svn", "info " + repositoryURL, OnRepositoryInfoReceived, OnInfoErrorReceived, OnRepositoryInfoExited);
                    }
                    catch (Exception err)
                    {
                        InfoErrorMessage = err.Message;
                        InfoExitedAction(false);
                    }
                }
                //获取本地版本
                else if (line.Length > versionName.Length &&
                         line.Substring(0, versionName.Length) == versionName)
                {
                    LocalVersion = line.Substring(versionName.Length);
                }
            }
        }

        void OnDiffExited(object sender, EventArgs e)
        {
            Process process = (Process)sender;
            if (process.ExitCode == 0)
            {
                LocalChangeState = ChangedFiles == "" ? ChangeStateEnum.NoChange : ChangeStateEnum.Changed;
            }
            if (DiffExitedAction != null)
            {
                DiffExitedAction(process.ExitCode == 0);
            }
        }

        void OnDiffErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                DiffErrorMessage += e.Data + "\n";
            }
        }

        void OnDiffReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                ChangedFiles += e.Data + "\n";
            }
        }

        void OnInfoErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                InfoErrorMessage += e.Data + "\n";
            }
        }

        void OnInfoReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                SVNInfo += e.Data + "\n";
            }
        }
        void OnRepositoryInfoReceived(object sender, DataReceivedEventArgs e)
        {
            const string versionName = "Revision: ";
            //获取仓库版本号
            if (e.Data.Length > versionName.Length &&
                e.Data.Substring(0, versionName.Length) == versionName)
            {
                RepositoryVersion = e.Data.Substring(versionName.Length).Trim();
                SVNInfo += "Repository Version: " + RepositoryVersion + "\n";
            }
        }

        static Process ExcuteCommand(string command, string arguments,
           DataReceivedEventHandler outputDataReceivedHandler,
           DataReceivedEventHandler errorDataReceivedHandler,
           EventHandler exitedHandler)
        {
            //实例一个process类
            Process process = new Process();
            //设定命令和参数
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            //重新定向标准输入输出，错误输出
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //设置cmd窗口的显示
            process.StartInfo.CreateNoWindow = true;
            //设置标准输出响应
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += outputDataReceivedHandler;
            process.ErrorDataReceived += errorDataReceivedHandler;
            process.Exited += exitedHandler;

            //运行
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
    }
}