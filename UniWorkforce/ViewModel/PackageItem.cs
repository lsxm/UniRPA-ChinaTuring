﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UniWorkforce.Librarys;
using System.Windows;
using System.Windows.Controls;
using System;
using NuGet;
using Newtonsoft.Json.Linq;
using UniWorkforce.Executor;
using Plugins.Shared.Library;
using log4net;
using System.Collections.Generic;
using UniExecutor.Core.Models;
using UniExecutor.Service.Interface;
using Plugins.Shared.Library.ActivityLog;
using Plugins.Shared.Library.Extensions;

namespace UniWorkforce.ViewModel
{
    public class PackageItem : ViewModelBase
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IExecutorService _executorService;

        public IPackage Package { get; internal set; }

        public List<string> VersionList { get; set; } = new List<string>();

        /// <summary>
        /// The <see cref="IsRunning" /> property's name.
        /// </summary>
        public const string IsRunningPropertyName = "IsRunning";

        private bool _isRunningProperty = false;

        /// <summary>
        /// Sets and gets the IsRunning property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunningProperty;
            }

            set
            {
                if (_isRunningProperty == value)
                {
                    return;
                }

                _isRunningProperty = value;
                RaisePropertyChanged(IsRunningPropertyName);

                //更新按钮状态
                StartCommand.RaiseCanExecuteChanged();
                UpdateCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// The <see cref="Name" /> property's name.
        /// </summary>
        public const string NamePropertyName = "Name";

        private string _nameProperty = "";

        /// <summary>
        /// Sets and gets the Name property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Name
        {
            get
            {
                return _nameProperty;
            }

            set
            {
                if (_nameProperty == value)
                {
                    return;
                }

                _nameProperty = value;
                RaisePropertyChanged(NamePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="Version" /> property's name.
        /// </summary>
        public const string VersionPropertyName = "Version";

        private string _versionProperty = "";

        /// <summary>
        /// Sets and gets the Version property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Version
        {
            get
            {
                return _versionProperty;
            }

            set
            {
                if (_versionProperty == value)
                {
                    return;
                }

                _versionProperty = value;
                RaisePropertyChanged(VersionPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsMouseOver" /> property's name.
        /// </summary>
        public const string IsMouseOverPropertyName = "IsMouseOver";

        private bool _isMouseOverProperty = false;

        /// <summary>
        /// Sets and gets the IsMouseOver property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsMouseOver
        {
            get
            {
                return _isMouseOverProperty;
            }

            set
            {
                if (_isMouseOverProperty == value)
                {
                    return;
                }

                _isMouseOverProperty = value;
                RaisePropertyChanged(IsMouseOverPropertyName);
            }
        }



        private RelayCommand _mouseEnterCommand;

        /// <summary>
        /// Gets the MouseEnterCommand.
        /// </summary>
        public RelayCommand MouseEnterCommand
        {
            get
            {
                return _mouseEnterCommand
                    ?? (_mouseEnterCommand = new RelayCommand(
                    () =>
                    {
                        IsMouseOver = true;
                    }));
            }
        }


        private RelayCommand _mouseLeaveCommand;

        /// <summary>
        /// Gets the MouseLeaveCommand.
        /// </summary>
        public RelayCommand MouseLeaveCommand
        {
            get
            {
                return _mouseLeaveCommand
                    ?? (_mouseLeaveCommand = new RelayCommand(
                    () =>
                    {
                        IsMouseOver = false;
                    }));
            }
        }


        /// <summary>
        /// The <see cref="IsNeedUpdate" /> property's name.
        /// </summary>
        public const string IsNeedUpdatePropertyName = "IsNeedUpdate";

        private bool _isNeedUpdateProperty = false;

        /// <summary>
        /// Sets and gets the IsNeedUpdate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsNeedUpdate
        {
            get
            {
                return _isNeedUpdateProperty;
            }

            set
            {
                if (_isNeedUpdateProperty == value)
                {
                    return;
                }

                _isNeedUpdateProperty = value;
                RaisePropertyChanged(IsNeedUpdatePropertyName);
            }
        }


        private RelayCommand _copyItemInfoCommand;

        /// <summary>
        /// Gets the CopyItemInfoCommand.
        /// </summary>
        public RelayCommand CopyItemInfoCommand
        {
            get
            {
                return _copyItemInfoCommand
                    ?? (_copyItemInfoCommand = new RelayCommand(
                    () =>
                    {
                        Clipboard.SetDataObject(ToolTip);
                    }));
            }
        }


        private RelayCommand _locateItemCommand;

        /// <summary>
        /// Gets the LocateItemCommand.
        /// </summary>
        public RelayCommand LocateItemCommand
        {
            get
            {
                return _locateItemCommand
                    ?? (_locateItemCommand = new RelayCommand(
                    () =>
                    {
                        var file = Settings.Instance.PackagesDir + @"\" + Name + @"." + Version+".nupkg";
                        Common.LocateFileInExplorer(file);
                    }));
            }
        }



        void DeleteNuPkgsFile(bool bRefresh = true)
        {
            //删除nupkg安装包
            foreach (var ver in VersionList)
            {
                var file = Settings.Instance.PackagesDir + @"\" + Name + @"." + ver + ".nupkg";
                Common.DeleteFile(file);
            }

            if(bRefresh)
            {
                Common.RunInUI(() =>
                {
                    //刷新
                    ViewModelLocator.instance.Main.RefreshCommand.Execute(null);
                });
            }
           
        }


        private RelayCommand _removeItemCommand;

        /// <summary>
        /// Gets the RemoveItemCommand.
        /// </summary>
        public RelayCommand RemoveItemCommand
        {
            get
            {
                return _removeItemCommand
                    ?? (_removeItemCommand = new RelayCommand(
                    () =>
                    {
                        var ret = UniMessageBox.Show(App.Current.MainWindow, "确定移除当前包吗？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                        if (ret == MessageBoxResult.Yes)
                        {
                            //卸载已安装的包，删除nupkg包
                            if (this.Package != null)
                            {
                                var repo = PackageRepositoryFactory.Default.CreateRepository(Settings.Instance.PackagesDir);
                                var packageManager = new PackageManager(repo, Settings.Instance.InstalledPackagesDir);

                                packageManager.PackageUninstalled += (sender, eventArgs) =>
                                {
                                    //如果都卸载完了，才刷新
                                    if (!packageManager.LocalRepository.Exists(this.Name))
                                    {
                                        DeleteNuPkgsFile();
                                    }
                                };

                                if (packageManager.LocalRepository.Exists(this.Name))
                                {
                                    while (packageManager.LocalRepository.Exists(this.Name))
                                    {
                                        packageManager.UninstallPackage(this.Name);
                                    }

                                }
                                else
                                {
                                    DeleteNuPkgsFile();
                                }

                                
                            }
                        }
                    },
                    () => !IsRunning));
            }
        }





        private RelayCommand _mouseRightButtonUpCommand;

        /// <summary>
        /// Gets the MouseRightButtonUpCommand.
        /// </summary>
        public RelayCommand MouseRightButtonUpCommand
        {
            get
            {
                return _mouseRightButtonUpCommand
                    ?? (_mouseRightButtonUpCommand = new RelayCommand(
                    () =>
                    {
                        var view = App.Current.MainWindow;
                        var cm = view.FindResource("PackageItemContextMenu") as ContextMenu;
                        cm.DataContext = this;
                        cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                        cm.IsOpen = true;
                    }));
            }
        }


    

        private RelayCommand _mouseDoubleClickCommand;

        /// <summary>
        /// Gets the MouseDoubleClickCommand.
        /// </summary>
        public RelayCommand MouseDoubleClickCommand
        {
            get
            {
                return _mouseDoubleClickCommand
                    ?? (_mouseDoubleClickCommand = new RelayCommand(
                    () =>
                    {
                        updateOrStart();
                    }));
            }
        }

        /// <summary>
        /// 如果未安装，则安装，如果需要更新则更新，如果能运行则运行，只执行一个步骤
        /// </summary>
        private void updateOrStart()
        {
            if(IsNeedUpdate)
            {
                UpdateCommand.Execute(null);
            }
            else
            {
                StartCommand.Execute(null);
            }
        }



        /// <summary>
        /// The <see cref="IsVisible" /> property's name.
        /// </summary>
        public const string IsVisiblePropertyName = "IsVisible";

        private bool _isVisibleProperty = true;

        /// <summary>
        /// Sets and gets the IsVisible property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return _isVisibleProperty;
            }

            set
            {
                if (_isVisibleProperty == value)
                {
                    return;
                }

                _isVisibleProperty = value;
                RaisePropertyChanged(IsVisiblePropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsSelected" /> property's name.
        /// </summary>
        public const string IsSelectedPropertyName = "IsSelected";

        private bool _isSelectedProperty = false;

        /// <summary>
        /// Sets and gets the IsSelected property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return _isSelectedProperty;
            }

            set
            {
                if (_isSelectedProperty == value)
                {
                    return;
                }

                _isSelectedProperty = value;
                RaisePropertyChanged(IsSelectedPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="ToolTip" /> property's name.
        /// </summary>
        public const string ToolTipPropertyName = "ToolTip";

        private string _toolTipProperty = null;

        /// <summary>
        /// Sets and gets the ToolTip property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ToolTip
        {
            get
            {
                return _toolTipProperty;
            }

            set
            {
                if (_toolTipProperty == value)
                {
                    return;
                }

                _toolTipProperty = value;
                RaisePropertyChanged(ToolTipPropertyName);
            }
        }




        /// <summary>
        /// The <see cref="IsSearching" /> property's name.
        /// </summary>
        public const string IsSearchingPropertyName = "IsSearching";

        private bool _isSearchingProperty = false;

        /// <summary>
        /// Sets and gets the IsSearching property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSearching
        {
            get
            {
                return _isSearchingProperty;
            }

            set
            {
                if (_isSearchingProperty == value)
                {
                    return;
                }

                _isSearchingProperty = value;
                RaisePropertyChanged(IsSearchingPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="SearchText" /> property's name.
        /// </summary>
        public const string SearchTextPropertyName = "SearchText";

        private string _searchTextProperty = "";

        /// <summary>
        /// Sets and gets the SearchText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SearchText
        {
            get
            {
                return _searchTextProperty;
            }

            set
            {
                if (_searchTextProperty == value)
                {
                    return;
                }

                _searchTextProperty = value;
                RaisePropertyChanged(SearchTextPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="IsMatch" /> property's name.
        /// </summary>
        public const string IsMatchPropertyName = "IsMatch";

        private bool _isMatchProperty = false;

        /// <summary>
        /// Sets and gets the IsMatch property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsMatch
        {
            get
            {
                return _isMatchProperty;
            }

            set
            {
                if (_isMatchProperty == value)
                {
                    return;
                }

                _isMatchProperty = value;
                RaisePropertyChanged(IsMatchPropertyName);
            }
        }


        public void ApplyCriteria(string criteria)
        {
            SearchText = criteria;

            if (IsCriteriaMatched(criteria))
            {
                IsMatch = true;

            }
        }


        private bool IsCriteriaMatched(string criteria)
        {
            return string.IsNullOrEmpty(criteria) || Name.ContainsIgnoreCase(criteria);
        }








        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        private RelayCommand _startCommand;

        /// <summary>
        /// Gets the StartCommand.
        /// </summary>
        public RelayCommand StartCommand
        {
            get
            {
                return _startCommand
                    ?? (_startCommand = new RelayCommand(
                    () =>
                    {
                        //授权判断，未授权不允许运行
                        var isRegistered = ViewModelLocator.instance.Register.IsNotExpired();
                        ViewModelLocator.instance.Startup.RefreshProgramStatus(isRegistered);

                        if (!isRegistered)
                        {
                            UniMessageBox.Show(App.Current.MainWindow, "软件未通过授权检测，请注册产品！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        //如果已经有一个项目正在运行，则不允许再运行
                        if(ViewModelLocator.instance.Main.IsWorkflowRunning)
                        {
                            UniMessageBox.Show(App.Current.MainWindow, "已经有工作流正在运行，请等待它结束后再运行！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var projectDir = Settings.Instance.InstalledPackagesDir + @"\" + Name + @"." + Version+ @"\lib\net452";
                        var projectJsonFile = projectDir + @"\project.json";
                        if (System.IO.File.Exists(projectJsonFile))
                        {
                            //项目配置文件存在
                            //1.找到主XAML文件，然后运行它
                            string json = System.IO.File.ReadAllText(projectJsonFile);
                            JObject jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(json);
                            var relativeMainXaml = jsonObj["main"].ToString();
                            var absoluteMainXaml = System.IO.Path.Combine(projectDir, relativeMainXaml);


                            if (System.IO.File.Exists(absoluteMainXaml))
                            {
                                System.GC.Collect();//提醒系统回收内存，避免内存占用过高

                                SharedObject.Instance.ProjectPath = projectDir;
                                SharedObject.Instance.SetOutputFun(LogToOutputWindow);
                                ViewModelLocator.instance.Main.RunningPackageItem = this;

                                var processInfo = new ProcessInfo
                                {
                                    ProcessName = Name,
                                    ProcessDir = projectDir,
                                    Main = relativeMainXaml
                                };
                                var jobModel = new JobModel
                                {
                                    JobName = processInfo.ProcessName,
                                    ProcessInfo = processInfo
                                };

                                ViewModelLocator.instance.Main.ExecutorService = new ExecutorService(jobModel);

                                SharedObject.Instance.Output(SharedObject.OutputType.Trace,$"开始执行工作流文件 {Name} ……");
                                ViewModelLocator.instance.Main.ExecutorService.Start();
                                //RunWorkflow(projectDir,absoluteMainXaml);
                            }
                        }                        
                    },
                    () => !IsRunning));
            }
        }

        private void LogToOutputWindow(SharedObject.OutputType type, string msg, string msgDetails)
        {
            Context.Current.CurrentTaskContext?.AddLog(type, msg, msgDetails);
            msg = msg.Replace("\0", "").Replace(ActivityLogFormat.ParameterSeparator, $"  {Environment.NewLine}");
            msgDetails=msgDetails.Replace("\0", "").Replace(ActivityLogFormat.ParameterSeparator, $"  {Environment.NewLine}");
            Logger.Info(string.Format("活动日志：type={0},msg={1},msgDetails={2}", type.ToString(), msg, msgDetails), logger);
        }


        private RelayCommand _updateCommand;

        /// <summary>
        /// Gets the UpdateCommand.
        /// </summary>
        public RelayCommand UpdateCommand
        {
            get
            {
                return _updateCommand
                    ?? (_updateCommand = new RelayCommand(
                    () =>
                    {
                        //安装或更新包
                        if(this.Package != null)
                        {
                            var repo = PackageRepositoryFactory.Default.CreateRepository(Settings.Instance.PackagesDir);
                            var packageManager = new PackageManager(repo, Settings.Instance.InstalledPackagesDir);

                            packageManager.PackageInstalled += (sender, eventArgs) =>
                            {
                                ViewModelLocator.instance.Main.RefreshCommand.Execute(null);
                            };

                            packageManager.InstallPackage(this.Package, false, true, true);
                        }
                    },
                    () => !IsRunning));
            }
        }




        

        
    }
}