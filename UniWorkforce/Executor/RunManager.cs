﻿using System;
using System.Windows;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Activities.Validation;
using GalaSoft.MvvmLight.Messaging;
using UniRobot.Librarys;
using Plugins.Shared.Library.Extensions;
using Plugins.Shared.Library;
using log4net;
using UniRobot.ViewModel;

namespace UniRobot.Executor
{
    public class RunManager
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public PackageItem m_packageItem { get; set; }

        private WorkflowApplication m_app;
        private string m_xamlPath { get; set; }

        public RunManager(PackageItem packageItem,string xamlPath)
        {
            m_packageItem = packageItem;
            m_xamlPath = xamlPath;
        }



        /// <summary>
        /// 开始执行运行流程
        /// </summary>
        public void Run()
        {
            Activity workflow = ActivityXamlServices.Load(m_xamlPath);

            var result = ActivityValidationServices.Validate(workflow);
            if (result.Errors.Count == 0)
            {
                Logger.Debug(string.Format("开始执行工作流文件 {0} ……", m_xamlPath),logger);
                Messenger.Default.Send(this, "BeginRun");

                if(m_app != null)
                {
                    m_app.Terminate("");
                }

                m_app = new WorkflowApplication(workflow);
                m_app.Extensions.Add(new LogToOutputWindowTextWriter());

                if (workflow is DynamicActivity)
                {
                    var wr = new WorkflowRuntime();
                    wr.RootActivity = workflow;
                    m_app.Extensions.Add(wr);
                }

                m_app.OnUnhandledException = WorkflowApplicationOnUnhandledException;
                m_app.Completed = WorkflowApplicationExecutionCompleted;
                m_app.Run();
            }
            else
            {
                MessageBox.Show(App.Current.MainWindow, "工作流校验错误，请检查参数配置", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private UnhandledExceptionAction WorkflowApplicationOnUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs e)
        {
            var name = e.ExceptionSource.DisplayName;
            SharedObject.Instance.Output(SharedObject.enOutputType.Error, string.Format("{0} 执行时出现异常", name), e.UnhandledException.ToString());

            return UnhandledExceptionAction.Terminate;
        }

        private void WorkflowApplicationExecutionCompleted(WorkflowApplicationCompletedEventArgs obj)
        {
            if(obj.TerminationException != null)
            {
                if(!string.IsNullOrEmpty(obj.TerminationException.Message))
                {
                    Common.RunInUI(()=> {
                        SharedObject.Instance.Output(SharedObject.enOutputType.Error, "提示", obj.TerminationException.ToString());
                        MessageBox.Show(App.Current.MainWindow, obj.TerminationException.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            Messenger.Default.Send(this, "EndRun");

            Logger.Debug(string.Format("结束执行工作流文件 {0}", m_xamlPath), logger);
        }

        public void Stop()
        {
            if (m_app != null)
            {
                try
                {
                    m_app.Terminate("执行已取消", new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception )
                {
                    MessageBox.Show(App.Current.MainWindow, "停止调试发生异常！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
