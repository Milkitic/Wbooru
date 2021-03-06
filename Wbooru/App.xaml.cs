﻿using System.ComponentModel.Composition;
using System.Windows;
using Wbooru.Kernel;
using System.Linq;
using Wbooru.Settings;
using System.Collections.Generic;
using Wbooru.Galleries;
using System;
using Wbooru.Utils.Resource;
using System.Diagnostics;
using System.Threading;
using Wbooru.Kernel.Updater;
using Wbooru.Utils;
using System.IO;
using System.Threading.Tasks;
using Wbooru.PluginExt;
using MessageBox = System.Windows.Forms.MessageBox;
using Wbooru.Kernel.Updater.PluginMarket;

namespace Wbooru
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Log.Info("Enter App()");

            BlockApplicationUntilSingle();

            PreprocessCommandLine();

            Init();

            BeginCheckUpdatable();

            Log.Info("Finish App()");
        }

        private async void BeginCheckUpdatable()
        {
            Log.Info("Enter BeginCheckUpdatable()");
            await Task.Run(() =>
            {
                if (SettingManager.LoadSetting<GlobalSetting>().EnableAutoCheckUpdatable)
                {
                    ProgramUpdater.CheckUpdatable();

                    foreach (var updatable in Container.Default.GetExportedValues<PluginInfo>().OfType<IPluginUpdatable>())
                        PluginUpdaterManager.CheckPluginUpdatable(updatable);
                }
            });
        }

        private void PreprocessCommandLine()
        {
            Log.Info("Enter PreprocessCommandLine()");
            //check if it need finish updating.
            if (CommandLine.ContainSwitchOption("update"))
            {
                ProgramUpdater.ApplyUpdate();
            }

            if (CommandLine.ContainSwitchOption("update_plugin"))
            {
                PluginUpdaterManager.ApplyPluginUpdate();
            }
        }

        private void BlockApplicationUntilSingle()
        {
            Log.Info("Enter BlockApplicationUntilSingle()");
            var cur_process = Process.GetCurrentProcess();

            Log.Info("Check&Block Application single instance.......");
            Log.Debug($"Current process info : {cur_process.Id}/{cur_process.SessionId} - {cur_process.ProcessName}");

            var time = DateTime.Now;

            while (true)
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ProgramUpdater.EXE_NAME)).Concat(Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ProgramUpdater.UPDATE_EXE_NAME))).Where(x => x.Id != cur_process.Id);

                if (!processes.Any())
                    break;

                if ((DateTime.Now - time).TotalSeconds > -1)
                {
                    Log.Debug($"Current other instances pid:{string.Join(" ", processes.Select(x => x.Id))}");
                    time = DateTime.Now;
                }

                Thread.Sleep(100);
            }

            Log.Info("OK.");
        }

        public static void Init()
        {
            Log.Info("-----------------Begin Init()-----------------");

            AppDomain.CurrentDomain.UnhandledException+= (e, d) => Log.Error($"{(d.ExceptionObject as Exception).Message} {Environment.NewLine} {(d.ExceptionObject as Exception).StackTrace}", "UnhandledException");
            if (Current!=null)
                Current.DispatcherUnhandledException += (e, d) => Log.Error($"{d?.Exception?.Message} {Environment.NewLine} {d?.Exception?.StackTrace}", "UnhandledException");

            Log.Info("Program version:" + ProgramUpdater.CurrentProgramVersion.ToString());

            Container.BuildDefault();

            CheckPlugin();

            DownloadManager.Init();

            SchedulerManager.Init();

            TagManager.InitTagManager();

            ImageResourceManager.InitImageResourceManager();

            Log.Info("-----------------End Init()-----------------");
        }

        private static void CheckPlugin()
        {
            var conflict_plugin_group = Container.Default.GetExportedValues<PluginInfo>().GroupBy(x => x.PluginName).Where(x => x.Count() > 1);

            foreach (var p in conflict_plugin_group)
            {
                var message = $"There contains plugin conflict that plugin name is same:\"{p.Key}\" : {Environment.NewLine} {string.Join(Environment.NewLine, p.Select(x => x.GetType().Assembly.Location).Select(x => $" -- {x}"))}";
                Log.Error(message);

                MessageBox.Show(message,"Wbooru.CheckPlugin()");
            }

            if (conflict_plugin_group.Any())
            {
                UnusualSafeExit();
            }
        }

        internal static void Term()
        {
            Log.Info("-----------------Begin Term()-----------------");
            PluginsTerm();
            DownloadManager.Close();
            SettingManager.SaveSettingFile();
            SchedulerManager.Term();
            Log.Term();
            Log.Info("-----------------End Term()-----------------");
        }

        private static void PluginsTerm()
        {
            Log.Info("Call PluginsTerm()");

            foreach (var plugin in Container.Default.GetExportedValues<PluginInfo>())
            {
                Log.Info($"Call {plugin.PluginName}.OnApplicationTerm()");
                plugin.CallApplicationTerm();
            }
        }

        internal static void UnusualSafeExit()
        {
            Log.Info("Call UnusualSafeExit()");

            try
            {
                Term();
            }
            catch (Exception e)
            {
                Log.Error($"Term() thrown exception:{e.Message}{Environment.NewLine}{e.StackTrace}");
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
