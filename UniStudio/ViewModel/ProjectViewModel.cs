﻿using System;
using GalaSoft.MvvmLight;
using log4net;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using UniStudio.Librarys;
using UniStudio.Windows;
using UniStudio.DataManager;
using UniStudio.DragDropHandler;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Plugins.Shared.Library.Nuget;
using System.Xml;
using System.Windows.Media;
using Plugins.Shared.Library.Extensions;

namespace UniStudio.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ProjectViewModel : ViewModelBase
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public int RemoveUnusedScreenshotsCount { get; private set; }

        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string ProjectMain { get; set; }
        public Dictionary<string, string> ProjectDependencies { get; set; }

        public ProjectTreeItem MainProjectTreeItem { get; private set; }

        public ProjectTreeItem RootProjectTreeItem { get; set; }

        public string ProjectPath { get; private set; }

        public string CurrentProjectJsonFile { get; set; }


        /// <summary>
        /// Initializes a new instance of the ProjectViewModel class.
        /// </summary>
        public ProjectViewModel()
        {
            Messenger.Default.Register<MessengerObjects.ProjectOpen>(this, OpenProject);
            Messenger.Default.Register<ProjectSettingsViewModel>(this, "ProjectSettingsModify", ProjectSettingsModify);
            Messenger.Default.Register<NewFolderViewModel>(this, "AddNewFolder", AddNewFolder);
            Messenger.Default.Register<RenameViewModel>(this, "Rename", Rename);
            Messenger.Default.Register<MessengerObjects.ProjectClose>(this, CloseProject);
        }

        #region 全局主题配色
        public const string ToolWindowsContainerBackgroundPropertyName = "ToolWindowsContainerBackground";
        private SolidColorBrush _toolWindowsContainerBackground = new BrushConverter().ConvertFrom("#ffffff") as SolidColorBrush;
        public SolidColorBrush ToolWindowsContainerBackground
        {
            get
            {
                return _toolWindowsContainerBackground;
            }
            set
            {
                if (_toolWindowsContainerBackground == value)
                {
                    return;
                }

                _toolWindowsContainerBackground = value;
                RaisePropertyChanged(ToolWindowsContainerBackgroundPropertyName);
            }
        }
        #endregion


        private void Rename(RenameViewModel obj)
        {
            if (obj.IsMain)
            {
                //修改的是主XAML，需要改project.json里的main对应的xaml文件，再刷新
                var relativeMainXaml = Common.MakeRelativePath(ProjectPath, obj.Dir + @"\" + obj.DstName);
                updateProjectJsonMain(relativeMainXaml);
            }

            RefreshCommand.Execute(null);
        }

        private void AddNewFolder(NewFolderViewModel obj)
        {
            RefreshCommand.Execute(null);
        }

        private void updateProjectJsonMain(string mainFile)
        {
            //json更新
            string json = File.ReadAllText(CurrentProjectJsonFile);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            jsonObj["main"] = mainFile;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(CurrentProjectJsonFile, output);
        }

        public void UpdateDependenciesOfProjectJsonConfig(Dictionary<string, string> projectDependencies)
        {
            var json_str = File.ReadAllText(CurrentProjectJsonFile);
            var json_cfg = JsonConvert.DeserializeObject<ProjectJsonConfig>(json_str);

            json_cfg.dependencies = projectDependencies;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(json_cfg, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(CurrentProjectJsonFile, output);
        }

        private void ProjectSettingsModify(ProjectSettingsViewModel vm)
        {
            ProjectName = vm.ProjectName;
            ProjectDescription = vm.ProjectDescription;

            RootProjectTreeItem.Name = ProjectName;
        }

        private void CloseProject(MessengerObjects.ProjectClose obj)
        {
            ProjectSettingsDataManager.instance.Unload();
        }

        private void OpenProject(MessengerObjects.ProjectOpen msg)
        {
            //根据project.json信息打开项目

            string json_file = msg.ProjectJsonFile;

            if (json_file == CurrentProjectJsonFile)
            {
                //当前项目早已经打开，只需要做一些必要操作即可
                if (!string.IsNullOrEmpty(msg.DefaultOpenXamlFile))
                {
                    //打开特定文档
                    var item = GetProjectTreeItemByFullPath(msg.DefaultOpenXamlFile);
                    if (item != null)
                    {
                        item.OpenXamlCommand.Execute(null);
                    }
                }

                //关闭起始页
                ViewModelLocator.instance.Main.IsOpenStartScreen = false;
                ViewModelLocator.instance.Main.IsBackButtonVisible = true;

                return;
            }

            // 初始化本地的 AvailableActivitiesXml 配置清单文件
            NuGetPackageController.Instance.AvailableActivitiesXmlConfigResourceInstance = null;
            var doc = new XmlDocument();
            using (var ms = new MemoryStream(UniStudio.Properties.Resources.AvailableActivities))
            {
                ms.Flush();
                ms.Position = 0;
                doc.Load(ms);
                ms.Close();
            }
            doc.Save(App.LocalRPAStudioDir + @"\Config\AvailableActivities.xml");

            ViewModelLocator.instance.Main.IsOpenStartScreen = false;  // 因为“正在加载”遮罩层无法覆盖在左滑菜单之上，所以这里先关闭左滑菜单，再显示遮罩层
            ViewModelLocator.instance.Main.IsStartContentBusy = true;
            Task.Run(async () =>
            {
                //调整最近项目顺序
                if (msg.Sender is RecentProjectItem)
                {
                    (msg.Sender as RecentProjectItem).RecentProjectsReorder();
                }

                ProjectSettingsDataManager.ResetInstance();
                ProjectSettingsDataManager.instance.Load(Path.GetDirectoryName(msg.ProjectJsonFile));

                CurrentProjectJsonFile = json_file;

                bool x = await initProjectAsync(false);
                if (!x)
                {
                    ViewModelLocator.instance.Main.IsStartContentBusy = false;

                    return;
                }

                var state_changed_msg = new MessengerObjects.ProjectStateChanged();
                state_changed_msg.ProjectPath = Path.GetDirectoryName(CurrentProjectJsonFile);
                state_changed_msg.ProjectName = ProjectName;
                Messenger.Default.Send(state_changed_msg);

                Common.RunInUI(() =>
                {
                    //清空组件树历史条目
                    if(ViewModelLocator.instance.Activities.ItemRecent != null)
                    {
                        ViewModelLocator.instance.Activities.ItemRecent.Children.Clear();
                    }

                    ProjectItems = new ObservableCollection<ProjectTreeItem>(ProjectItemsTemp);

                    if (string.IsNullOrEmpty(msg.DefaultOpenXamlFile))
                    {
                        //自动打开Main文档
                        if (MainProjectTreeItem != null)
                        {
                            MainProjectTreeItem.OpenXamlCommand.Execute(null);
                        }
                    }
                    else
                    {
                        //打开特定文档
                        var item = GetProjectTreeItemByFullPath(msg.DefaultOpenXamlFile);
                        if (item != null)
                        {
                            item.OpenXamlCommand.Execute(null);
                        }
                    }

                    //关闭起始页
                    ViewModelLocator.instance.Main.IsOpenStartScreen = false;
                    ViewModelLocator.instance.Main.IsBackButtonVisible = true;

                    ViewModelLocator.instance.Main.IsStartContentBusy = false;

                    // 每次初始化项目后，重新刷新 Activities 工具箱（可用/收藏/最近），并排序
                    ViewModelLocator.instance.Activities.initAvailableActivities();
                    ViewModelLocator.instance.Activities.initFavoriteActivities();
                    ViewModelLocator.instance.Activities.initRecentActivities();
                    //排序
                    ViewModelLocator.instance.Activities.activityItemsSortByGroupType();

                    Context.Current.SearchDataManager.StartBuildSearchData(true);
                });
            });

        }

        private async Task<bool> initProjectAsync(bool updateProjectItems = true)
        {
            ProjectItemsTemp.Clear();

            ProjectPath = Path.GetDirectoryName(CurrentProjectJsonFile);

            var json_str = File.ReadAllText(CurrentProjectJsonFile);

            try
            {
                var json_cfg = JsonConvert.DeserializeObject<ProjectJsonConfig>(json_str);
                if (json_cfg.Upgrade())
                {
                    //文件格式需要升级转换
                    string output = Newtonsoft.Json.JsonConvert.SerializeObject(json_cfg, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(CurrentProjectJsonFile, output);

                    json_str = File.ReadAllText(CurrentProjectJsonFile);
                    json_cfg = JsonConvert.DeserializeObject<ProjectJsonConfig>(json_str);
                }

                ProjectName = json_cfg.name;
                ProjectDescription = json_cfg.description;
                ProjectMain = ProjectPath + @"\" + json_cfg.main;
                ProjectDependencies = json_cfg.dependencies;

                // 创建根节点
                var projectItem = new ProjectTreeItem(this);
                RootProjectTreeItem = projectItem;
                projectItem.IsRoot = true;
                projectItem.IsExpanded = true;
                projectItem.Name = json_cfg.name;
                projectItem.Path = ProjectPath;
                projectItem.ToolTip = ProjectPath;
                projectItem.Icon = "pack://application:,,,/Resource/Image/Project/project.png";
                projectItem.ContextMenuName = "ProjectRootContextMenu";

                ProjectItemsTemp.Add(projectItem);

                await InitProjectDependenciesFolderAsync(projectItem);
                await InitDirectoryAsync(new DirectoryInfo(ProjectPath), projectItem);

                if (updateProjectItems)
                {
                    ProjectItems = new ObservableCollection<ProjectTreeItem>(ProjectItemsTemp);
                }
            }
            catch (Exception err)
            {
                Logger.Error(err, logger);
                CurrentProjectJsonFile = "";
                Common.RunInUI(() =>
                {
                    UniMessageBox.Show(App.Current.MainWindow, "打开项目出错！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return false;
            }

            return true;
        }

        /// <summary>
        /// 先初始化项目依赖包
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private async Task InitProjectDependenciesFolderAsync(ProjectTreeItem parent)
        {
            // 先添加依赖包文件夹
            var dependenciesFolderItem = new ProjectTreeItem(this);
            dependenciesFolderItem.Name = "依赖包";
            dependenciesFolderItem.IsExpanded = true;
            dependenciesFolderItem.Icon = "pack://application:,,,/Resource/Image/Project/dependencies.png";
            //dependenciesFolderItem.ContextMenuName = "";
            if (parent != null)
            {
                parent.Children.Add(dependenciesFolderItem);
            }
            else
            {
                ProjectItemsTemp.Add(dependenciesFolderItem);
            }

            // 当前项目 dependencies 信息遍历
            foreach (var dependency in ProjectDependencies)
            {
                // 添加到项目文件夹树视图
                string packageId = dependency.Key;
                string packageVersion = dependency.Value.Replace('[', ' ').Replace(']', ' ').Trim();
                var item = new ProjectTreeItem(this);
                item.Name = packageId + " = " + packageVersion;
                item.IsExpanded = true;
                item.Icon = "pack://application:,,,/Resource/Image/Project/dependency.png";
                //item.ContextMenuName = "";
                dependenciesFolderItem.Children.Add(item);

                // 下载安装依赖包
                await NuGetPackageController.Instance.DownloadAndInstall(new PackageIdentity(packageId, NuGetVersion.Parse(packageVersion)));
            }
        }

        /// <summary>
        /// 初始化项目文件夹树
        /// </summary>
        /// <param name="di"></param>
        /// <param name="parent"></param>
        private async Task InitDirectoryAsync(DirectoryInfo di, ProjectTreeItem parent = null)
        {
            try
            {
                //当前目录文件夹遍历
                DirectoryInfo[] dis = di.GetDirectories();
                for (int j = 0; j < dis.Length; j++)
                {
                    var item = new ProjectTreeItem(this);
                    item.Path = dis[j].FullName;
                    item.Name = dis[j].Name;
                    item.IsDirectory = true;
                    item.ContextMenuName = "ProjectDirectoryContextMenu";

                    if (item.Name != ".local")
                    {
                        if (parent != null)
                        {
                            parent.Children.Add(item);
                        }
                        else
                        {
                            ProjectItemsTemp.Add(item);
                        }

                        await InitDirectoryAsync(dis[j], item);
                    }

                }

                //当前目录文件遍历
                FileInfo[] fis = di.GetFiles();
                for (int i = 0; i < fis.Length; i++)
                {
                    var item = new ProjectTreeItem(this);
                    item.Path = fis[i].FullName;
                    item.Name = fis[i].Name;

                    if (item.Path.EqualsIgnoreCase(CurrentProjectJsonFile))
                    {
                        item.IsProjectJson = true;
                    }

                    if (fis[i].Extension.ToLower() == ".xaml")
                    {
                        item.IsXaml = true;
                        item.Icon = "pack://application:,,,/Resource/Image/Project/xaml.png";

                        if (item.Path == ProjectMain)
                        {
                            item.IsMain = true;
                            item.ContextMenuName = "ProjectMainXamlContextMenu";

                            MainProjectTreeItem = item;
                        }
                        else
                        {
                            item.ContextMenuName = "ProjectXamlContextMenu";
                        }

                        if (parent != null)
                        {
                            parent.Children.Add(item);
                        }
                        else
                        {
                            ProjectItemsTemp.Add(item);
                        }
                    }
                    else
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(item.Path);
                        item.IconSource = Common.ToImageSource(icon);

                        if (item.IsProjectJson)
                        {

                        }
                        else
                        {
                            item.ContextMenuName = "ProjectFileContextMenu";
                        }


                        if (IsShowAllFiles)
                        {
                            if (parent != null)
                            {
                                parent.Children.Add(item);
                            }
                            else
                            {
                                ProjectItemsTemp.Add(item);
                            }
                        }
                    }

                }
            }
            catch(UnauthorizedAccessException e)
            {
                UniMessageBox.Show("当前文件夹存在用户无权访问的问题，请移动 xaml 文档到其他目录打开", "项目启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                throw e;
            }

        }


        /// <summary>
        /// The <see cref="IsShowAllFiles" /> property's name.
        /// </summary>
        public const string IsShowAllFilesPropertyName = "IsShowAllFiles";

        private bool _isShowAllFilesProperty = false;

        /// <summary>
        /// Sets and gets the IsShowAllFiles property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsShowAllFiles
        {
            get
            {
                return _isShowAllFilesProperty;
            }

            set
            {
                if (_isShowAllFilesProperty == value)
                {
                    return;
                }

                _isShowAllFilesProperty = value;
                RaisePropertyChanged(IsShowAllFilesPropertyName);
            }
        }


        public List<ProjectTreeItem> ProjectItemsTemp = new List<ProjectTreeItem>();


        /// <summary>
        /// The <see cref="ProjectItems" /> property's name.
        /// </summary>
        public const string ProjectItemsPropertyName = "ProjectItems";

        private ObservableCollection<ProjectTreeItem> _projectItemsProperty = new ObservableCollection<ProjectTreeItem>();

        /// <summary>
        /// Sets and gets the ProjectItems property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ProjectTreeItem> ProjectItems
        {
            get
            {
                return _projectItemsProperty;
            }

            set
            {
                if (_projectItemsProperty == value)
                {
                    return;
                }

                _projectItemsProperty = value;
                RaisePropertyChanged(ProjectItemsPropertyName);
            }
        }


        public ProjectTreeItem GetProjectTreeItemByFullPath(string fullPath, ProjectTreeItem parent = null)
        {
            if (parent == null)
            {
                foreach (var item in ProjectItems)
                {
                    if (item.IsXaml && item.Path.EqualsIgnoreCase(fullPath))
                    {
                        return item;
                    }

                    var ret = GetProjectTreeItemByFullPath(fullPath, item);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            else
            {
                foreach (var child in parent.Children)
                {
                    if (child.IsXaml && child.Path.EqualsIgnoreCase(fullPath))
                    {
                        return child;
                    }

                    var ret = GetProjectTreeItemByFullPath(fullPath, child);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }

            return null;
        }


        private void ProjectTreeItemSetAllIsExpanded(ProjectTreeItem item, bool IsExpanded)
        {
            item.IsExpanded = IsExpanded;
            foreach (var child in item.Children)
            {
                ProjectTreeItemSetAllIsExpanded(child, IsExpanded);
            }
        }

        private void ProjectTreeItemSetAllIsSearching(ProjectTreeItem item, bool IsSearching)
        {
            item.IsSearching = IsSearching;
            foreach (var child in item.Children)
            {
                ProjectTreeItemSetAllIsSearching(child, IsSearching);
            }
        }

        private void ProjectTreeItemSetAllIsMatch(ProjectTreeItem item, bool IsMatch)
        {
            item.IsMatch = IsMatch;
            foreach (var child in item.Children)
            {
                ProjectTreeItemSetAllIsMatch(child, IsMatch);
            }
        }

        private void ProjectTreeItemSetAllSearchText(ProjectTreeItem item, string SearchText)
        {
            item.SearchText = SearchText;
            foreach (var child in item.Children)
            {
                ProjectTreeItemSetAllSearchText(child, SearchText);
            }
        }


        private RelayCommand _expandAllCommand;

        /// <summary>
        /// Gets the ExpandAllCommand.
        /// </summary>
        public RelayCommand ExpandAllCommand
        {
            get
            {
                return _expandAllCommand
                    ?? (_expandAllCommand = new RelayCommand(
                    () =>
                    {
                        foreach (var item in ProjectItems)
                        {
                            ProjectTreeItemSetAllIsExpanded(item, true);
                        }
                    }));
            }
        }


        private RelayCommand _collapseAllCommand;

        /// <summary>
        /// Gets the CollapseAllCommand.
        /// </summary>
        public RelayCommand CollapseAllCommand
        {
            get
            {
                return _collapseAllCommand
                    ?? (_collapseAllCommand = new RelayCommand(
                    () =>
                    {
                        foreach (var item in ProjectItems)
                        {
                            ProjectTreeItemSetAllIsExpanded(item, false);
                        }
                    }));
            }
        }


        private RelayCommand _refreshCommand;

        /// <summary>
        /// Gets the RefreshCommand.
        /// </summary>
        public RelayCommand RefreshCommand
        {
            get
            {
                return _refreshCommand
                    ?? (_refreshCommand = new RelayCommand(
                    () =>
                    {
                        initProjectAsync();
                        doSearch();
                    }));
            }
        }




        /// <summary>
        /// The <see cref="IsSearchResultEmpty" /> property's name.
        /// </summary>
        public const string IsSearchResultEmptyPropertyName = "IsSearchResultEmpty";

        private bool _isSearchResultEmptyProperty = false;

        /// <summary>
        /// Sets and gets the IsSearchResultEmpty property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSearchResultEmpty
        {
            get
            {
                return _isSearchResultEmptyProperty;
            }

            set
            {
                if (_isSearchResultEmptyProperty == value)
                {
                    return;
                }

                _isSearchResultEmptyProperty = value;
                RaisePropertyChanged(IsSearchResultEmptyPropertyName);
            }
        }


        private void doSearch()
        {
            var searchContent = SearchText.Trim();
            if (string.IsNullOrEmpty(searchContent))
            {
                //还原起始显示
                foreach (var item in ProjectItems)
                {
                    ProjectTreeItemSetAllIsSearching(item, false);
                }

                foreach (var item in ProjectItems)
                {
                    ProjectTreeItemSetAllSearchText(item, "");
                }

                IsSearchResultEmpty = false;
            }
            else
            {
                //根据搜索内容显示

                foreach (var item in ProjectItems)
                {
                    ProjectTreeItemSetAllIsSearching(item, true);
                }

                //预先全部置为不匹配
                foreach (var item in ProjectItems)
                {
                    ProjectTreeItemSetAllIsMatch(item, false);
                }


                foreach (var item in ProjectItems)
                {
                    item.ApplyCriteria(searchContent, new Stack<ProjectTreeItem>());
                }

                IsSearchResultEmpty = true;
                foreach (var item in ProjectItems)
                {
                    if (item.IsMatch)
                    {
                        IsSearchResultEmpty = false;
                        break;
                    }
                }

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

                doSearch();

            }
        }


        /// <summary>
        /// 重置变量，以便打开新项目时不被旧变量干扰
        /// </summary>
        public void ResetAll()
        {
            ProjectItems.Clear();
            SearchText = "";
            IsShowAllFiles = false;
            CurrentProjectJsonFile = "";

            ProjectTreeItem.IsExpandedDict.Clear();
        }

        private RelayCommand _openProjectSettingsCommand;

        /// <summary>
        /// Gets the OpenProjectSettingsCommand.
        /// </summary>
        public RelayCommand OpenProjectSettingsCommand
        {
            get
            {
                return _openProjectSettingsCommand
                    ?? (_openProjectSettingsCommand = new RelayCommand(
                    () =>
                    {
                        var window = new ProjectSettingsWindow();
                        window.Owner = Application.Current.MainWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        var vm = window.DataContext as ProjectSettingsViewModel;
                        vm.ProjectName = ProjectName;
                        vm.ProjectDescription = ProjectDescription;
                        vm.CurrentProjectJsonFile = CurrentProjectJsonFile;
                        window.ShowDialog();
                    }));
            }
        }




        private RelayCommand _showAllFilesCommand;

        /// <summary>
        /// Gets the ShowAllFilesCommand.
        /// </summary>
        public RelayCommand ShowAllFilesCommand
        {
            get
            {
                return _showAllFilesCommand
                    ?? (_showAllFilesCommand = new RelayCommand(
                    () =>
                    {
                        IsShowAllFiles = !IsShowAllFiles;

                        RefreshCommand.Execute(null);
                    }));
            }
        }



        private RelayCommand _openProjectDirCommand;

        /// <summary>
        /// Gets the OpenProjectDirCommand.
        /// </summary>
        public RelayCommand OpenProjectDirCommand
        {
            get
            {
                return _openProjectDirCommand
                    ?? (_openProjectDirCommand = new RelayCommand(
                    () =>
                    {
                        Common.LocateDirInExplorer(ProjectPath);
                    }));
            }
        }


        public ProjectTreeItem GetProjectTreeItemIsSelected(ProjectTreeItem parent = null)
        {
            if (parent == null)
            {
                foreach (var item in ProjectItems)
                {
                    if (item.IsSelected)
                    {
                        return item;
                    }

                    var ret = GetProjectTreeItemIsSelected(item);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            else
            {
                foreach (var child in parent.Children)
                {
                    if (child.IsSelected)
                    {
                        return child;
                    }

                    var ret = GetProjectTreeItemIsSelected(child);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }

            return null;
        }



        private RelayCommand _renameSelectedItemCommand;

        /// <summary>
        /// Gets the RenameSelectedItemCommand.
        /// </summary>
        public RelayCommand RenameSelectedItemCommand
        {
            get
            {
                return _renameSelectedItemCommand
                    ?? (_renameSelectedItemCommand = new RelayCommand(
                    () =>
                    {
                        var item = GetProjectTreeItemIsSelected();
                        if (item != null && !item.IsProjectJson && !item.IsRoot)
                        {
                            if (item.IsDirectory)
                            {
                                item.RenameDirCommand.Execute(null);
                            }
                            else
                            {
                                item.RenameFileCommand.Execute(null);
                            }
                        }
                    }));
            }
        }



        private RelayCommand _deleteSelectedItemCommand;

        /// <summary>
        /// Gets the DeleteSelectedItemCommand.
        /// </summary>
        public RelayCommand DeleteSelectedItemCommand
        {
            get
            {
                return _deleteSelectedItemCommand
                    ?? (_deleteSelectedItemCommand = new RelayCommand(
                    () =>
                    {
                        var item = GetProjectTreeItemIsSelected();
                        if (item != null && !item.IsProjectJson && !item.IsRoot)
                        {
                            if (item.IsDirectory)
                            {
                                item.DeleteDirCommand.Execute(null);
                            }
                            else
                            {
                                item.DeleteFileCommand.Execute(null);
                            }
                        }
                    }));
            }
        }





        private RelayCommand _removeUnusedScreenshotsCommand;

        /// <summary>
        /// Gets the RemoveUnusedScreenshotsCommand.
        /// </summary>
        public RelayCommand RemoveUnusedScreenshotsCommand
        {
            get
            {
                return _removeUnusedScreenshotsCommand
                    ?? (_removeUnusedScreenshotsCommand = new RelayCommand(
                    () =>
                    {
                        ViewModelLocator.instance.Main.SaveAllCommand.Execute(null);

                        //RemoveUnusedScreenshotsCount = 0;

                        //if (System.IO.Directory.Exists(ProjectPath + @"\.screenshots"))
                        //{
                        //    Common.DirectoryChildrenForEach(new DirectoryInfo(ProjectPath + @"\.screenshots"), enumScreenshotsImage);
                        //}

                        RemoveUnusedScreenshots();

                        if (RemoveUnusedScreenshotsCount == 0)
                        {
                            UniMessageBox.Show(App.Current.MainWindow, "找不到需要清理的未使用的截图", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            //RefreshCommand.Execute(null);
                            UniMessageBox.Show(App.Current.MainWindow, string.Format("{0}个未使用的截图已经被成功移除", RemoveUnusedScreenshotsCount), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                    }));
            }
        }


        public void RemoveUnusedScreenshots()
        {
            RemoveUnusedScreenshotsCount = 0;

            if (System.IO.Directory.Exists(ProjectPath + @"\.screenshots"))
            {
                Common.DirectoryChildrenForEach(new DirectoryInfo(ProjectPath + @"\.screenshots"), enumScreenshotsImage);
            }

            if (RemoveUnusedScreenshotsCount != 0)
            {
                RefreshCommand.Execute(null);
            }
        }


        private bool enumScreenshotsImage(object item, object param)
        {
            if (item is FileInfo)
            {
                bool contains = Common.DirectoryChildrenForEach(new DirectoryInfo(ProjectPath), checkScreenshotsImage, item);
                if (!contains)
                {
                    //如果找不到，说明没有人引用当前这个图片，直接删除它即可
                    var fi = item as FileInfo;
                    try
                    {
                        fi.Delete();

                        RemoveUnusedScreenshotsCount++;
                    }
                    catch (Exception err)
                    {
                        //可能是刚创建的活动引用了图片，然后活动被删除了
                        Logger.Debug(err, logger);
                    }

                }
            }

            return false;
        }

        private bool checkScreenshotsImage(object item, object param)
        {
            if (item is FileInfo)
            {
                var fi_img = param as FileInfo;
                var fi_xaml = item as FileInfo;
                if (fi_xaml.Extension.ToLower() == ".xaml")
                {
                    //判断xaml对应的文件里能否找到fi_img的文件名，找到了说明被引用了，直接返回true
                    if (Common.IsStringInFile(fi_xaml.FullName, "\"" + fi_img.Name + "\""))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        //树节点拖拽功能
        public ProjectDropHandler ProjectDropHandler { get; set; } = new ProjectDropHandler();

        public ProjectDragHandler ProjectDragHandler { get; set; } = new ProjectDragHandler();


    }
}