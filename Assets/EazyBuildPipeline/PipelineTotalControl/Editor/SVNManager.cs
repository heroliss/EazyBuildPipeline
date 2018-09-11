using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EazyBuildPipeline.PipelineTotalControl.Editor
{
    [Serializable]
    public class SVNManager
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
        public string RepositoryVersion = "";
        public string LastChangedVersion = "";
        //Update
        public string message = "";
        public string errorMessage = "";

        public SVNManager()
        {
        }

        public void RunUpdate()
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
                throw new ApplicationException("SVN Revert 时发生错误：" + errorMessage);
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
                throw new ApplicationException("SVN Update 时发生错误：" + errorMessage);
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
                throw new ApplicationException("SVN Resolve 时发生错误：" + errorMessage);
            }
            EditorUtility.DisplayProgressBar("SVN", "Finish!", 1);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            //后处理过程：重新创建Wrap和Lua文件
            ClearAndGenerateWrapAndLua();
        }

        private void ClearAndGenerateWrapAndLua()
        {
            EditorUtility.DisplayProgressBar("Clear and Generate Wrap Files...", "", 1);
            ToLuaMenu.ClearWrapFilesAndCreate();
            EditorUtility.DisplayProgressBar("Clear and Generate Lua Files...", "", 1);
            LuaScriptsPreProcessor.LuaEncryptAllThingsDone(true, () => { });
            EditorUtility.DisplayProgressBar("Clear and Generate Wrap and Lua Files Finished!", "", 1);
        }

        private void OnExited(object sender, EventArgs e)
        {
        }

        private void OnErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                errorMessage += e.Data + "\n";
            }
        }

        private void OnReceived(object sender, DataReceivedEventArgs e)
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
            RepositoryVersion = "";
            LastChangedVersion = "";
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

        private void OnInfoExited(object sender, EventArgs e)
        {
            Process process = (Process)sender;
            Available = process.ExitCode == 0;
            if (Available)
            {
                ExtractVersionsAndSetVersionState();
                try
                {
                    ExcuteCommand("/bin/bash", Path.Combine(G.configs.LocalConfig.LocalRootPath, "SVNDiff.sh"),
                       OnDiffReceived, OnDiffErrorReceived, OnDiffExited);
                }
                catch(Exception err)
                {
                    InfoErrorMessage = err.Message;
                    DiffExitedAction(false);
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

        private void ExtractVersionsAndSetVersionState()
        {
            const string lastChangedVersionName = "Last Changed Rev: ";
            const string repositoryVersionName = "Revision: ";
            foreach (var line in SVNInfo.Split('\n'))
            {
                if (line.Length > lastChangedVersionName.Length &&
                    line.Substring(0, lastChangedVersionName.Length) == lastChangedVersionName)
                {
                    LastChangedVersion = line.Substring(lastChangedVersionName.Length);
                }
                else if (line.Length > repositoryVersionName.Length &&
                         line.Substring(0, repositoryVersionName.Length) == repositoryVersionName)
                {
                    RepositoryVersion = line.Substring(repositoryVersionName.Length);
                }
            }
            if (RepositoryVersion != "" && LastChangedVersion != "")
            {
                VersionState = LastChangedVersion == RepositoryVersion ? VersionStateEnum.Latest : VersionStateEnum.Obsolete;
            }
        }

        private void OnDiffExited(object sender, EventArgs e)
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

        private void OnDiffErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                DiffErrorMessage += e.Data + "\n";
            }
        }

        private void OnDiffReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                ChangedFiles += e.Data + "\n";
            }
        }

        private void OnInfoErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                InfoErrorMessage += e.Data + "\n";
            }
        }

        private void OnInfoReceived(object sender, DataReceivedEventArgs e)
        {
            SVNInfo += e.Data + "\n";
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