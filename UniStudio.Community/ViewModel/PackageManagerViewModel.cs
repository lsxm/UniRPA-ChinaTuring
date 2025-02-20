﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Plugins.Shared.Library.Extensions;
using Plugins.Shared.Library.Nuget;
using UniStudio.Community.Librarys;

namespace UniStudio.Community.ViewModel
{
    public class PackageManagerViewModel : ViewModelBase
    {
        private Window m_view;
        private ListBox m_listBox;
        private ListBox m_listBoxSearchResult;
        private ScrollViewer m_listBoxSearchResultScrollViewer;
        private string currentSearchSource;
        private int currentSearchResultSkipIndex;
        private CancellationTokenSource currentCancellationTokenSource = new CancellationTokenSource();
        private Task currentSearchResultTask;
        private RichTextBox m_richTextBox;

        public SettingItem CurrentSelectedSettingItem { get; set; }

        public PackageSourceSearchResultItem CurrentSelectedPackageSourceSearchResultItem { get; set; }

        public List<IPackageSearchMetadata> m_currentSelectedItemPackageSearchMetadataList { get; set; } = new List<IPackageSearchMetadata>(); //当前选中项的Version列表信息

        public List<PackageIdentity> ToBeInstallPackageList { get; set; } = new List<PackageIdentity>();

        public List<PackageIdentity> ToBeUninstallPackageList { get; set; } = new List<PackageIdentity>();

        /// <summary>
        /// Initializes a new instance of the PackageManagerViewModel class.
        /// </summary>
        public PackageManagerViewModel()
        {
        }

        private RelayCommand<RoutedEventArgs> _loadedCommand;

        /// <summary>
        /// Gets the LoadedCommand.
        /// </summary>
        public RelayCommand<RoutedEventArgs> LoadedCommand
        {
            get
            {
                return _loadedCommand
                    ?? (_loadedCommand = new RelayCommand<RoutedEventArgs>(
                    p =>
                    {
                        m_view = (Window)p.Source;

                        initDefaultPackageSourceItems();
                        initUserDefinePackageSourceItems();

                        //默认的和自定义的条目获取后再初始化左侧面板的设置项信息
                        InitSettingItems(SettingItem.enSettingItemType.ProjectDependencies);
                    }));
            }
        }

        private RelayCommand _closingCommand;

        /// <summary>
        /// Gets the ClosedCommand.
        /// </summary>
        public RelayCommand ClosingCommand
        {
            get
            {
                return _closingCommand
                    ?? (_closingCommand = new RelayCommand(() =>
                    {
                        // 重新打开当前项目
                        var msg = new MessengerObjects.ProjectOpen();
                        msg.ProjectJsonFile = ViewModelLocator.instance.Project.CurrentProjectJsonFile;
                        ViewModelLocator.instance.Main.DoCloseProject();
                        Messenger.Default.Send(msg);
                    }));
            }
        }


        private RelayCommand<RoutedEventArgs> _listBoxLoadedCommand;

        /// <summary>
        /// Gets the ListBoxLoadedCommand.
        /// </summary>
        public RelayCommand<RoutedEventArgs> ListBoxLoadedCommand
        {
            get
            {
                return _listBoxLoadedCommand
                    ?? (_listBoxLoadedCommand = new RelayCommand<RoutedEventArgs>(
                    p =>
                    {
                        m_listBox = (ListBox)p.Source;
                    }));
            }
        }


        private RelayCommand<RoutedEventArgs> _listBoxSearchResultLoadedCommand;

        /// <summary>
        /// Gets the ListBoxSearchResultLoadedCommand.
        /// </summary>
        public RelayCommand<RoutedEventArgs> ListBoxSearchResultLoadedCommand
        {
            get
            {
                return _listBoxSearchResultLoadedCommand
                    ?? (_listBoxSearchResultLoadedCommand = new RelayCommand<RoutedEventArgs>(
                    p =>
                    {
                        m_listBoxSearchResult = (ListBox)p.Source;
                        m_listBoxSearchResultScrollViewer = FindSimpleVisualChild<ScrollViewer>(m_listBoxSearchResult);
                    }));
            }
        }


        private RelayCommand<RoutedEventArgs> _richTextBoxLoadedCommand;

        /// <summary>
        /// Gets the RichTextBoxLoadedCommand.
        /// </summary>
        public RelayCommand<RoutedEventArgs> RichTextBoxLoadedCommand
        {
            get
            {
                return _richTextBoxLoadedCommand
                    ?? (_richTextBoxLoadedCommand = new RelayCommand<RoutedEventArgs>(
                    p =>
                    {
                        m_richTextBox = (RichTextBox)p.Source;
                    }));
            }
        }


        public void InitSettingItems(SettingItem.enSettingItemType selectType = SettingItem.enSettingItemType.Null)
        {
            //初始化
            SettingItems.Clear();

            var itemSettings = new SettingItem(this, SettingItem.enSettingItemType.Settings, "设置", "配置程序包的来源", "pack://application:,,,/Resource/Image/Windows/ManagePackages/setting.png");
            SettingItems.Add(itemSettings);

            var itemProjectDependencies = new SettingItem(this, SettingItem.enSettingItemType.ProjectDependencies, "项目依赖", "查看当前项目的所有依赖包", "pack://application:,,,/Resource/Image/Windows/ManagePackages/dependency.png");
            SettingItems.Add(itemProjectDependencies);

            var itemAllPackages = new SettingItem(this, SettingItem.enSettingItemType.AllPackages, "所有包", "从所有的包源中的程序包", "pack://application:,,,/Resource/Image/Windows/ManagePackages/all-packages.png");
            SettingItems.Add(itemAllPackages);

            if (selectType == SettingItem.enSettingItemType.Settings)
            {
                itemSettings.IsSelected = true;
            }
            else if (selectType == SettingItem.enSettingItemType.ProjectDependencies)
            {
                itemProjectDependencies.IsSelected = true;
            }
            else if (selectType == SettingItem.enSettingItemType.AllPackages)
            {
                itemAllPackages.IsSelected = true;
            }


            foreach (var package_item in DefaultPackageSourceItems.Concat(UserDefinePackageSourceItems))
            {
                if (package_item.IsChecked)
                {
                    var item = new SettingItem(this, SettingItem.enSettingItemType.PackageItem, package_item.Name, package_item.Source);
                    item.PackageItemSource = package_item.Source;
                    SettingItems.Add(item);
                }
            }

        }


        private void initDefaultPackageSourceItems()
        {
            var provider = NuGetPackageController.Instance.DefaultSourceRepositoryProvider.PackageSourceProvider;
            var sources = provider.LoadPackageSources();

            DefaultPackageSourceItems.Clear();
            foreach (var item in sources)
            {
                var sourceItem = new CommonPackageSourceItem(this);
                sourceItem.IsDefault = true;
                sourceItem.PackageSourceItem = item;
                sourceItem.Name = item.Name;
                sourceItem.Source = item.Source;
                sourceItem.IsChecked = item.IsEnabled;

                DefaultPackageSourceItems.Add(sourceItem);
            }
        }

        private void initUserDefinePackageSourceItems()
        {
            var provider = NuGetPackageController.Instance.UserDefineSourceRepositoryProvider.PackageSourceProvider;
            var sources = provider.LoadPackageSources();

            UserDefinePackageSourceItems.Clear();
            foreach (var item in sources)
            {
                var sourceItem = new CommonPackageSourceItem(this);
                sourceItem.IsDefault = false;
                sourceItem.PackageSourceItem = item;
                sourceItem.Name = item.Name;
                sourceItem.Source = item.Source;
                sourceItem.IsChecked = item.IsEnabled;

                UserDefinePackageSourceItems.Add(sourceItem);
            }
        }




        /// <summary>
        /// The <see cref="CurrentSelectPackageSourceName" /> property's name.
        /// </summary>
        public const string CurrentSelectPackageSourceNamePropertyName = "CurrentSelectPackageSourceName";

        private string _currentSelectPackageSourceNameProperty = "";

        /// <summary>
        /// Sets and gets the CurrentSelectPackageSourceName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string CurrentSelectPackageSourceName
        {
            get
            {
                return _currentSelectPackageSourceNameProperty;
            }

            set
            {
                if (_currentSelectPackageSourceNameProperty == value)
                {
                    return;
                }

                _currentSelectPackageSourceNameProperty = value;
                RaisePropertyChanged(CurrentSelectPackageSourceNamePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="SettingItems" /> property's name.
        /// </summary>
        public const string SettingItemsPropertyName = "SettingItems";

        private ObservableCollection<SettingItem> _settingItemsProperty = new ObservableCollection<SettingItem>();

        /// <summary>
        /// Sets and gets the SettingItems property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<SettingItem> SettingItems
        {
            get
            {
                return _settingItemsProperty;
            }

            set
            {
                if (_settingItemsProperty == value)
                {
                    return;
                }

                _settingItemsProperty = value;
                RaisePropertyChanged(SettingItemsPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="IsNeedSave" /> property's name.
        /// </summary>
        public const string IsNeedSavePropertyName = "IsNeedSave";

        private bool _isNeedSaveProperty = false;

        /// <summary>
        /// Sets and gets the IsNeedSave property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsNeedSave
        {
            get
            {
                return _isNeedSaveProperty;
            }

            set
            {
                if (_isNeedSaveProperty == value)
                {
                    return;
                }

                _isNeedSaveProperty = value;
                RaisePropertyChanged(IsNeedSavePropertyName);
            }
        }

        public void DefaultPackageSourceItemsUnselectAll()
        {
            foreach (var item in DefaultPackageSourceItems)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }
        }

        public void UserDefinePackageSourceItemsUnselectAll()
        {
            foreach (var item in UserDefinePackageSourceItems)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }
        }

        private RelayCommand _saveCommand;

        /// <summary>
        /// Gets the SaveCommand.
        /// </summary>
        public RelayCommand SaveCommand
        {
            get
            {
                return _saveCommand
                    ?? (_saveCommand = new RelayCommand(
                    () =>
                    {

                    },
                    () => IsNeedSave));
            }
        }

        private RelayCommand _cancelCommand;

        /// <summary>
        /// Gets the CancelCommand.
        /// </summary>
        public RelayCommand CancelCommand
        {
            get
            {
                return _cancelCommand
                    ?? (_cancelCommand = new RelayCommand(
                    () =>
                    {
                        ImageExt.ClearCache();
                        m_view.Close();
                    },
                    () => true));
            }
        }




        /// <summary>
        /// 包源设置界面正在显示，这时候右半部分界面要特殊处理
        /// </summary>
        public const string IsSettingsShowPropertyName = "IsSettingsShow";

        private bool _isSettingsShowProperty = false;

        /// <summary>
        /// Sets and gets the IsSettingsShow property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSettingsShow
        {
            get
            {
                return _isSettingsShowProperty;
            }

            set
            {
                if (_isSettingsShowProperty == value)
                {
                    return;
                }

                _isSettingsShowProperty = value;
                RaisePropertyChanged(IsSettingsShowPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="DefaultPackageSourceItems" /> property's name.
        /// </summary>
        public const string DefaultPackageSourceItemsPropertyName = "DefaultPackageSourceItems";

        private ObservableCollection<CommonPackageSourceItem> _defaultPackageSourceItemsProperty = new ObservableCollection<CommonPackageSourceItem>();

        /// <summary>
        /// Sets and gets the DefaultPackageSourceItems property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<CommonPackageSourceItem> DefaultPackageSourceItems
        {
            get
            {
                return _defaultPackageSourceItemsProperty;
            }

            set
            {
                if (_defaultPackageSourceItemsProperty == value)
                {
                    return;
                }

                _defaultPackageSourceItemsProperty = value;
                RaisePropertyChanged(DefaultPackageSourceItemsPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="UserDefinePackageSourceItems" /> property's name.
        /// </summary>
        public const string UserDefinePackageSourceItemsPropertyName = "UserDefinePackageSourceItems";

        private ObservableCollection<CommonPackageSourceItem> _userDefinePackageSourceItemsProperty = new ObservableCollection<CommonPackageSourceItem>();

        /// <summary>
        /// Sets and gets the UserDefinePackageSourceItems property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<CommonPackageSourceItem> UserDefinePackageSourceItems
        {
            get
            {
                return _userDefinePackageSourceItemsProperty;
            }

            set
            {
                if (_userDefinePackageSourceItemsProperty == value)
                {
                    return;
                }

                _userDefinePackageSourceItemsProperty = value;
                RaisePropertyChanged(UserDefinePackageSourceItemsPropertyName);
            }
        }



        private RelayCommand _mouseLeftButtonDownCommand;

        /// <summary>
        /// Gets the MouseLeftButtonDownCommand.
        /// </summary>
        public RelayCommand MouseLeftButtonDownCommand
        {
            get
            {
                return _mouseLeftButtonDownCommand
                    ?? (_mouseLeftButtonDownCommand = new RelayCommand(
                    () =>
                    {
                        resetSelectToDefault();
                    }));
            }
        }



        /// <summary>
        /// The <see cref="PackageSourceName" /> property's name.
        /// </summary>
        public const string PackageSourceNamePropertyName = "PackageSourceName";

        private string _packageSourceNameProperty = "";

        /// <summary>
        /// Sets and gets the PackageSourceName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string PackageSourceName
        {
            get
            {
                return _packageSourceNameProperty;
            }

            set
            {
                value = value.Trim();
                if (_packageSourceNameProperty == value)
                {
                    return;
                }

                _packageSourceNameProperty = value;
                RaisePropertyChanged(PackageSourceNamePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="PackageSourceUri" /> property's name.
        /// </summary>
        public const string PackageSourceUriPropertyName = "PackageSourceUri";

        private string _packageSourceUriProperty = "";

        /// <summary>
        /// Sets and gets the PackageSourceUri property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string PackageSourceUri
        {
            get
            {
                return _packageSourceUriProperty;
            }

            set
            {
                value = value.Trim();
                if (_packageSourceUriProperty == value)
                {
                    return;
                }

                _packageSourceUriProperty = value;
                RaisePropertyChanged(PackageSourceUriPropertyName);
            }
        }



        private RelayCommand _browserPackageSourceDirCommand;

        /// <summary>
        /// Gets the BrowserPackageSourceDirCommand.
        /// </summary>
        public RelayCommand BrowserPackageSourceDirCommand
        {
            get
            {
                return _browserPackageSourceDirCommand
                    ?? (_browserPackageSourceDirCommand = new RelayCommand(
                    () =>
                    {
                        string dst_dir = "";
                        if (Common.ShowSelectDirDialog("请选择一个本地包源目录", ref dst_dir))
                        {
                            this.PackageSourceUri = dst_dir;
                        }
                    }));
            }
        }


        private bool checkPackageSourceInfoValid()
        {
            if (string.IsNullOrEmpty(PackageSourceName))
            {
                UniMessageBox.Show(m_view, "包源名称不能为空！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(PackageSourceUri))
            {
                UniMessageBox.Show(m_view, "包源路径不能为空！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }





        private bool PackageSourceUriValid(string uri)
        {
            //目前只判断是否有冒号
            if (uri.Contains(":"))
            {
                return true;
            }

            return false;
        }

        private bool PackageSourceItemsContains(string name, bool isSkipSelected = false)
        {
            foreach (var item in DefaultPackageSourceItems)
            {
                if (isSkipSelected && item.IsSelected)
                {
                    continue;
                }

                if (item.Name.ToLower() == name.ToLower())
                {
                    return true;
                }
            }

            foreach (var item in UserDefinePackageSourceItems)
            {
                if (isSkipSelected && item.IsSelected)
                {
                    continue;
                }

                if (item.Name.ToLower() == name.ToLower())
                {
                    return true;
                }
            }

            return false;
        }


        private RelayCommand _addPackageSourceToConfigFileCommand;

        /// <summary>
        /// Gets the AddPackageSourceToConfigFileCommand.
        /// </summary>
        public RelayCommand AddPackageSourceToConfigFileCommand
        {
            get
            {
                return _addPackageSourceToConfigFileCommand
                    ?? (_addPackageSourceToConfigFileCommand = new RelayCommand(
                    () =>
                    {
                        if (!checkPackageSourceInfoValid())
                        {
                            return;
                        }

                        //新加的包源名称不允许和默认包源或自定义包源的名称重名
                        if (PackageSourceItemsContains(PackageSourceName))
                        {
                            UniMessageBox.Show(m_view, "包源名称必须唯一！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (!PackageSourceUriValid(PackageSourceUri))
                        {
                            UniMessageBox.Show(m_view, "包源地址不合法！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        //添加到自定义包源列表中去
                        //UserDefinePackageSourceItems
                        var provider = NuGetPackageController.Instance.UserDefineSourceRepositoryProvider.PackageSourceProvider;
                        var sources = new List<PackageSource>(provider.LoadPackageSources());

                        var pkg_source = new PackageSource(PackageSourceUri, PackageSourceName, true);
                        sources.Add(pkg_source);
                        provider.SavePackageSources(sources);

                        initUserDefinePackageSourceItems();

                        //滚动条滚动到最后面
                        m_listBox.ScrollIntoView(UserDefinePackageSourceItems.LastOrDefault());

                        resetSelectToDefault();

                        //刷新左侧面板包源列表
                        InitSettingItems(SettingItem.enSettingItemType.Settings);
                    }));
            }
        }



        private RelayCommand _updatePackageSourceToConfigFileCommand;

        /// <summary>
        /// Gets the UpdatePackageSourceToConfigFileCommand.
        /// </summary>
        public RelayCommand UpdatePackageSourceToConfigFileCommand
        {
            get
            {
                return _updatePackageSourceToConfigFileCommand
                    ?? (_updatePackageSourceToConfigFileCommand = new RelayCommand(
                    () =>
                    {
                        if (!checkPackageSourceInfoValid())
                        {
                            return;
                        }

                        //更新后的名称必须唯一
                        if (PackageSourceItemsContains(PackageSourceName, true))
                        {
                            UniMessageBox.Show(m_view, "包源名称必须唯一！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (!PackageSourceUriValid(PackageSourceUri))
                        {
                            UniMessageBox.Show(m_view, "包源地址不合法！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }


                        var provider = NuGetPackageController.Instance.UserDefineSourceRepositoryProvider.PackageSourceProvider;
                        var sources = new List<PackageSource>(provider.LoadPackageSources());

                        PackageSource selectItem = null;
                        foreach (var item in sources)
                        {
                            if (item.Name.ToLower() == CurrentSelectPackageSourceName.ToLower())
                            {
                                selectItem = item;
                                break;
                            }
                        }
                        var idx = sources.IndexOf(selectItem);
                        sources.Remove(selectItem);
                        sources.Insert(idx, new PackageSource(PackageSourceUri, PackageSourceName, selectItem.IsEnabled));

                        provider.SavePackageSources(sources);

                        initUserDefinePackageSourceItems();

                        resetSelectToDefault();

                        //刷新左侧面板包源列表
                        InitSettingItems(SettingItem.enSettingItemType.Settings);
                    }));
            }
        }



        /// <summary>
        /// The <see cref="IsShowUpdatePackageSourceBtn" /> property's name.
        /// </summary>
        public const string IsShowUpdatePackageSourceBtnPropertyName = "IsShowUpdatePackageSourceBtn";

        private bool _isShowUpdatePackageSourceBtnProperty = false;

        /// <summary>
        /// Sets and gets the IsShowUpdatePackageSourceBtn property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsShowUpdatePackageSourceBtn
        {
            get
            {
                return _isShowUpdatePackageSourceBtnProperty;
            }

            set
            {
                if (_isShowUpdatePackageSourceBtnProperty == value)
                {
                    return;
                }

                _isShowUpdatePackageSourceBtnProperty = value;
                RaisePropertyChanged(IsShowUpdatePackageSourceBtnPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsOperatePackageSourceEnabled" /> property's name.
        /// </summary>
        public const string IsOperatePackageSourceEnabledPropertyName = "IsOperatePackageSourceEnabled";

        private bool _isOperatePackageSourceEnabledProperty = true;

        /// <summary>
        /// Sets and gets the IsOperatePackageSourceEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsOperatePackageSourceEnabled
        {
            get
            {
                return _isOperatePackageSourceEnabledProperty;
            }

            set
            {
                if (_isOperatePackageSourceEnabledProperty == value)
                {
                    return;
                }

                _isOperatePackageSourceEnabledProperty = value;
                RaisePropertyChanged(IsOperatePackageSourceEnabledPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsAddPackageSourceEnabled" /> property's name.
        /// </summary>
        public const string IsAddPackageSourceEnabledPropertyName = "IsAddPackageSourceEnabled";

        private bool _isAddPackageSourceEnabledProperty = true;

        /// <summary>
        /// Sets and gets the IsAddPackageSourceEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsAddPackageSourceEnabled
        {
            get
            {
                return _isAddPackageSourceEnabledProperty;
            }

            set
            {
                if (_isAddPackageSourceEnabledProperty == value)
                {
                    return;
                }

                _isAddPackageSourceEnabledProperty = value;
                RaisePropertyChanged(IsAddPackageSourceEnabledPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsRemovePackageSourceEnabled" /> property's name.
        /// </summary>
        public const string IsRemovePackageSourceEnabledPropertyName = "IsRemovePackageSourceEnabled";

        private bool _isRemovePackageSourceEnabledProperty = false;

        /// <summary>
        /// Sets and gets the IsRemovePackageSourceEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsRemovePackageSourceEnabled
        {
            get
            {
                return _isRemovePackageSourceEnabledProperty;
            }

            set
            {
                if (_isRemovePackageSourceEnabledProperty == value)
                {
                    return;
                }

                _isRemovePackageSourceEnabledProperty = value;
                RaisePropertyChanged(IsRemovePackageSourceEnabledPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsMoveUpPackageSourceEnabled" /> property's name.
        /// </summary>
        public const string IsMoveUpPackageSourceEnabledPropertyName = "IsMoveUpPackageSourceEnabled";

        private bool _isMoveUpPackageSourceEnabledProperty = false;

        /// <summary>
        /// Sets and gets the IsMoveUpPackageSourceEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsMoveUpPackageSourceEnabled
        {
            get
            {
                return _isMoveUpPackageSourceEnabledProperty;
            }

            set
            {
                if (_isMoveUpPackageSourceEnabledProperty == value)
                {
                    return;
                }

                _isMoveUpPackageSourceEnabledProperty = value;
                RaisePropertyChanged(IsMoveUpPackageSourceEnabledPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsMoveDownPackageSourceEnabled" /> property's name.
        /// </summary>
        public const string IsMoveDownPackageSourceEnabledPropertyName = "IsMoveDownPackageSourceEnabled";

        private bool _isMoveDownPackageSourceEnabledProperty = false;

        /// <summary>
        /// Sets and gets the IsMoveDownPackageSourceEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsMoveDownPackageSourceEnabled
        {
            get
            {
                return _isMoveDownPackageSourceEnabledProperty;
            }

            set
            {
                if (_isMoveDownPackageSourceEnabledProperty == value)
                {
                    return;
                }

                _isMoveDownPackageSourceEnabledProperty = value;
                RaisePropertyChanged(IsMoveDownPackageSourceEnabledPropertyName);
            }
        }


        private void resetSelectToDefault()
        {
            //重置ListBox状态为未选择
            DefaultPackageSourceItemsUnselectAll();
            UserDefinePackageSourceItemsUnselectAll();

            IsOperatePackageSourceEnabled = true;
            IsShowUpdatePackageSourceBtn = false;

            CurrentSelectPackageSourceName = "";
            PackageSourceName = "";
            PackageSourceUri = "";

            IsAddPackageSourceEnabled = true;
            IsRemovePackageSourceEnabled = false;
            IsMoveUpPackageSourceEnabled = false;
            IsMoveDownPackageSourceEnabled = false;
        }


        private RelayCommand _addPackageSourceCommand;

        /// <summary>
        /// Gets the AddPackageSourceCommand.
        /// </summary>
        public RelayCommand AddPackageSourceCommand
        {
            get
            {
                return _addPackageSourceCommand
                    ?? (_addPackageSourceCommand = new RelayCommand(
                    () =>
                    {
                        resetSelectToDefault();
                    }));
            }
        }


        private RelayCommand _removePackageSourceCommand;

        /// <summary>
        /// Gets the RemovePackageSourceCommand.
        /// </summary>
        public RelayCommand RemovePackageSourceCommand
        {
            get
            {
                return _removePackageSourceCommand
                    ?? (_removePackageSourceCommand = new RelayCommand(
                    () =>
                    {
                        var provider = NuGetPackageController.Instance.UserDefineSourceRepositoryProvider.PackageSourceProvider;
                        var sources = new List<PackageSource>(provider.LoadPackageSources());

                        PackageSource selectItem = null;
                        foreach (var item in sources)
                        {
                            if (item.Name.ToLower() == CurrentSelectPackageSourceName.ToLower())
                            {
                                selectItem = item;
                                break;
                            }
                        }

                        sources.Remove(selectItem);

                        provider.SavePackageSources(sources);

                        initUserDefinePackageSourceItems();

                        resetSelectToDefault();

                        //刷新左侧面板包源列表
                        InitSettingItems(SettingItem.enSettingItemType.Settings);
                    }));
            }
        }

        private RelayCommand _moveUpPackageSourceCommand;

        /// <summary>
        /// Gets the MoveUpPackageSourceCommand.
        /// </summary>
        public RelayCommand MoveUpPackageSourceCommand
        {
            get
            {
                return _moveUpPackageSourceCommand
                    ?? (_moveUpPackageSourceCommand = new RelayCommand(
                    () =>
                    {
                        var provider = NuGetPackageController.Instance.UserDefineSourceRepositoryProvider.PackageSourceProvider;
                        var sources = new List<PackageSource>(provider.LoadPackageSources());

                        PackageSource selectItem = null;
                        foreach (var item in sources)
                        {
                            if (item.Name.ToLower() == CurrentSelectPackageSourceName.ToLower())
                            {
                                selectItem = item;
                                break;
                            }
                        }
                        var idx = sources.IndexOf(selectItem);
                        sources.Remove(selectItem);
                        sources.Insert(idx - 1, selectItem);

                        provider.SavePackageSources(sources);

                        initUserDefinePackageSourceItems();

                        resetSelectToDefault();

                        //刷新左侧面板包源列表
                        InitSettingItems(SettingItem.enSettingItemType.Settings);

                        UserDefinePackageSourceItemsSelect(selectItem.Name);
                    }));
            }
        }


        private RelayCommand _moveDownPackageSourceCommand;

        /// <summary>
        /// Gets the MoveDownPackageSourceCommand.
        /// </summary>
        public RelayCommand MoveDownPackageSourceCommand
        {
            get
            {
                return _moveDownPackageSourceCommand
                    ?? (_moveDownPackageSourceCommand = new RelayCommand(
                    () =>
                    {
                        var provider = NuGetPackageController.Instance.UserDefineSourceRepositoryProvider.PackageSourceProvider;
                        var sources = new List<PackageSource>(provider.LoadPackageSources());

                        PackageSource selectItem = null;
                        foreach (var item in sources)
                        {
                            if (item.Name.ToLower() == CurrentSelectPackageSourceName.ToLower())
                            {
                                selectItem = item;
                                break;
                            }
                        }
                        var idx = sources.IndexOf(selectItem);
                        sources.Remove(selectItem);
                        sources.Insert(idx + 1, selectItem);

                        provider.SavePackageSources(sources);

                        initUserDefinePackageSourceItems();

                        resetSelectToDefault();

                        //刷新左侧面板包源列表
                        InitSettingItems(SettingItem.enSettingItemType.Settings);

                        UserDefinePackageSourceItemsSelect(selectItem.Name);
                    }));
            }
        }


        private void UserDefinePackageSourceItemsSelect(string name)
        {
            foreach (var item in UserDefinePackageSourceItems)
            {
                if (item.Name.ToLower() == name.ToLower())
                {
                    item.IsSelected = true;
                    m_listBox.ScrollIntoView(item);
                    break;
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

                _searchTextProperty = value.Trim();
                RaisePropertyChanged(SearchTextPropertyName);

                DoSearch();

            }
        }







        /// <summary>
        /// The <see cref="IsSearchResultLoading" /> property's name.
        /// </summary>
        public const string IsSearchResultLoadingPropertyName = "IsSearchResultLoading";

        private bool _isSearchResultLoadingProperty = false;

        /// <summary>
        /// Sets and gets the IsSearchResultLoading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSearchResultLoading
        {
            get
            {
                return _isSearchResultLoadingProperty;
            }

            set
            {
                if (_isSearchResultLoadingProperty == value)
                {
                    return;
                }

                if (value)
                {
                    IsNotExistPackageSourceSearchResultItems = false;
                }
                else if (PackageSourceSearchResultItems.Count > 0)
                {
                    IsNotExistPackageSourceSearchResultItems = false;
                }
                else
                {
                    IsNotExistPackageSourceSearchResultItems = true;
                }

                _isSearchResultLoadingProperty = value;
                RaisePropertyChanged(IsSearchResultLoadingPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="IsSearchPackageVersionListResultLoading" /> property's name.
        /// </summary>
        public const string IsSearchPackageVersionListResultLoadingPropertyName = "IsSearchPackageVersionListResultLoading";

        private bool _isSearchPackageVersionListResultLoadingProperty = false;

        /// <summary>
        /// Sets and gets the IsSearchResultLoading property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSearchPackageVersionListResultLoading
        {
            get
            {
                return _isSearchPackageVersionListResultLoadingProperty;
            }

            set
            {
                if (_isSearchPackageVersionListResultLoadingProperty == value)
                {
                    return;
                }

                _isSearchPackageVersionListResultLoadingProperty = value;
                RaisePropertyChanged(IsSearchPackageVersionListResultLoadingPropertyName);
            }
        }






        /// <summary>
        /// The <see cref="IsIncludePrerelease" /> property's name.
        /// </summary>
        public const string IsIncludePrereleasePropertyName = "IsIncludePrerelease";

        private bool _isIncludePrereleaseProperty = false;

        /// <summary>
        /// Sets and gets the IsIncludePrerelease property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsIncludePrerelease
        {
            get
            {
                return _isIncludePrereleaseProperty;
            }

            set
            {
                if (_isIncludePrereleaseProperty == value)
                {
                    return;
                }

                _isIncludePrereleaseProperty = value;
                RaisePropertyChanged(IsIncludePrereleasePropertyName);

                DoSearch();
            }
        }



        /// <summary>
        /// 显示搜索结果
        /// </summary>
        public void DoSearch()
        {
            var settingItem = CurrentSelectedSettingItem;

            UIUtils.GetListBoxScrollViewer(m_listBoxSearchResult)?.ScrollToHome();

            PackageSourceSearchResultItems.Clear();

            IsPackageSourceSearchResultItemSelected = false;//复原包详情显示

            IsSearchResultLoading = false;//加载中效果隐藏

            //TODO WJF 搜索结果应该是显示包Title，还是包Id？比如Json.net库的包id和包title不一致
            if (settingItem.ItemType == SettingItem.enSettingItemType.ProjectDependencies)
            {
                //项目依赖包搜索
                var projectDependencies = ViewModelLocator.instance.Project.ProjectDependencies;
                foreach (var dependency in projectDependencies)
                {
                    var ver_range = VersionRange.Parse((string)dependency.Value);
                    if (ver_range.IsMinInclusive)
                    {
                        var identity = new PackageIdentity(dependency.Key, ver_range.MinVersion);

                        var nuspec = NuGetPackageController.Instance.GetNuspecReaderInPackagesInstallFolder(identity);

                        var id = nuspec.GetId();
                        var title = nuspec.GetTitle();
                        var authors = nuspec.GetAuthors();
                        var description = nuspec.GetDescription();
                        var icon_url = nuspec.GetIconUrl();
                        var installed_version = ver_range.MinVersion.ToString();

                        var item = new PackageSourceSearchResultItem(this, identity, id, authors, description, icon_url);
                        item.Title = title;
                        item.IsInstalled = true;//项目依赖包肯定是已经安装了
                        item.PackageStatus = new BitmapImage(new Uri("pack://application:,,,/Resource/Image/Windows/ManagePackages/status-has-been-installed.png"));
                        item.InstalledVersion = installed_version;
                        item.LicenseUrl = nuspec.GetLicenseUrl();
                        item.ProjectUrl = nuspec.GetProjectUrl();
                        item.PublishTime = NuGetPackageController.Instance.GetLocalPackageInfo(identity).LastWriteTimeUtc.ToLocalTime().ToString("G");
                        item.Tags = nuspec.GetTags();
                        item.RequireLicenseAcceptance = nuspec.GetRequireLicenseAcceptance();
                        item.Dependencies = nuspec.GetDependencyGroups().ToList();

                        //TODO WJF 根据title过滤（因为是显示的Title）
                        if (item.Title.ContainsIgnoreCase(SearchText))
                        {
                            PackageSourceSearchResultItems.Add(item);
                        }
                    }
                    else
                    {
                        //TODO WJF 大于但不等于低版本时如何处理？
                    }
                }
            }
            else if (settingItem.ItemType == SettingItem.enSettingItemType.AllPackages
                || settingItem.ItemType == SettingItem.enSettingItemType.PackageItem
                )
            {
                m_listBoxSearchResultScrollViewer.ScrollChanged -= SearchResultScrollViewerScrollChanged;
                m_listBoxSearchResultScrollViewer.ScrollChanged += SearchResultScrollViewerScrollChanged;
                currentSearchResultSkipIndex = 0;

                IsSearchResultLoading = true;//加载中效果显示

                try
                {
                    if (currentSearchResultTask != null)
                    {
                        currentCancellationTokenSource.Cancel();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    currentCancellationTokenSource.Dispose();
                    currentCancellationTokenSource = new CancellationTokenSource();
                }

                //所有包源里搜索
                currentSearchResultTask = new Task(async () =>
                {
                    await RunSearchResultTaskAsync(settingItem, currentCancellationTokenSource.Token);
                }, currentCancellationTokenSource.Token);
                currentSearchResultTask.Start();
            }
            else
            {
                //DO NOTHING
            }
        }

        private async Task RunSearchResultTaskAsync(SettingItem settingItem, CancellationToken cancellationToken)
        {
            var _packageSourceSearchResultItems = new List<PackageSourceSearchResultItem>();

            var projectDependencies = ViewModelLocator.instance.Project.ProjectDependencies;

            currentSearchSource = settingItem.PackageItemSource;
            List<IPackageSearchMetadata> searchItemList = await NuGetPackageController.Instance.Search(SearchText, IsIncludePrerelease, currentSearchSource);

            foreach (var searchItem in searchItemList)
            {
                var item = new PackageSourceSearchResultItem(this, searchItem.Identity, searchItem.Identity.Id, searchItem.Authors, searchItem.Description, searchItem.IconUrl?.AbsoluteUri);
                item.Title = searchItem.Title;
                item.IsInstalled = IsPackageIdInProjectDependencies(projectDependencies, searchItem.Identity.Id);
                if (item.IsInstalled)
                {
                    Common.RunInUI(() =>
                    {
                        item.PackageStatus = new BitmapImage(new Uri("pack://application:,,,/Resource/Image/Windows/ManagePackages/status-has-been-installed.png"));
                        item.InstalledVersion = GetCurrentPackageInstalledVersion(projectDependencies, searchItem.Identity.Id);
                    });
                }
                else
                {
                    item.InstalledVersion = searchItem.Identity.Version.OriginalVersion;
                }
                item.LicenseUrl = searchItem.LicenseUrl?.AbsoluteUri;
                item.ProjectUrl = searchItem.ProjectUrl?.AbsoluteUri;
                item.PublishTime = searchItem.Published?.ToLocalTime().ToString("G");
                item.Tags = searchItem.Tags;
                item.Dependencies = searchItem.DependencySets.ToList();

                _packageSourceSearchResultItems.Add(item);
            }

            Common.RunInUI(() =>
            {
                if ((CurrentSelectedSettingItem.ItemType == SettingItem.enSettingItemType.AllPackages || CurrentSelectedSettingItem.ItemType == SettingItem.enSettingItemType.PackageItem)
                    && CurrentSelectedSettingItem.PackageItemSource == currentSearchSource
                    )
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();  // 如果收到任务取消信号，则停止继续执行，抛出异常
                        PackageSourceSearchResultItems = new ObservableCollection<PackageSourceSearchResultItem>(_packageSourceSearchResultItems);
                        IsSearchResultLoading = false;//加载中效果隐藏
                    }
                    catch
                    {

                    }
                }
                else
                {
                    //点击太快时会触发到这
                }

            });
        }


        private void SearchResultScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = e.OriginalSource as ScrollViewer;

            if (IsBootomScrollView(scrollViewer))
            {
                m_listBoxSearchResultScrollViewer.ScrollChanged -= SearchResultScrollViewerScrollChanged;
                currentSearchResultSkipIndex += 50;
                IsSearchResultLoading = true;//加载中效果显示

                try
                {
                    if (currentSearchResultTask != null)
                    {
                        currentCancellationTokenSource.Cancel();
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    currentCancellationTokenSource.Dispose();
                    currentCancellationTokenSource = new CancellationTokenSource();
                }

                //所有包源里搜索
                currentSearchResultTask = new Task(async () =>
                {
                    await RunSearchResultScrollViewerScrollChangedTaskAsync(currentCancellationTokenSource.Token);
                }, currentCancellationTokenSource.Token);
                currentSearchResultTask.Start();
            }
        }

        private async Task RunSearchResultScrollViewerScrollChangedTaskAsync(CancellationToken cancellationToken)
        {
            var _packageSourceSearchResultItems = new List<PackageSourceSearchResultItem>();

            var projectDependencies = ViewModelLocator.instance.Project.ProjectDependencies;

            List<IPackageSearchMetadata> searchItemList = await NuGetPackageController.Instance.Search(SearchText, IsIncludePrerelease, currentSearchSource, currentSearchResultSkipIndex);

            foreach (var searchItem in searchItemList)
            {
                var item = new PackageSourceSearchResultItem(this, searchItem.Identity, searchItem.Identity.Id, searchItem.Authors, searchItem.Description, searchItem.IconUrl?.AbsoluteUri);
                item.Title = searchItem.Title;
                item.IsInstalled = IsPackageIdInProjectDependencies(projectDependencies, searchItem.Identity.Id);
                if (item.IsInstalled)
                {
                    Common.RunInUI(() =>
                    {
                        item.PackageStatus = new BitmapImage(new Uri("pack://application:,,,/Resource/Image/Windows/ManagePackages/status-has-been-installed.png"));
                        item.InstalledVersion = GetCurrentPackageInstalledVersion(projectDependencies, searchItem.Identity.Id);
                    });
                }
                else
                {
                    item.InstalledVersion = searchItem.Identity.Version.OriginalVersion;
                }
                item.LicenseUrl = searchItem.LicenseUrl?.AbsoluteUri;
                item.ProjectUrl = searchItem.ProjectUrl?.AbsoluteUri;
                item.PublishTime = searchItem.Published?.ToLocalTime().ToString("G");
                item.Tags = searchItem.Tags;
                item.Dependencies = searchItem.DependencySets.ToList();

                _packageSourceSearchResultItems.Add(item);
            }

            Common.RunInUI(() =>
            {
                if ((CurrentSelectedSettingItem.ItemType == SettingItem.enSettingItemType.AllPackages || CurrentSelectedSettingItem.ItemType == SettingItem.enSettingItemType.PackageItem)
                    && CurrentSelectedSettingItem.PackageItemSource == currentSearchSource
                    )
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();  // 如果收到任务取消信号，则停止继续执行，抛出异常
                        foreach (PackageSourceSearchResultItem item in _packageSourceSearchResultItems)
                        {
                            PackageSourceSearchResultItems.Add(item);
                        }

                        m_listBoxSearchResultScrollViewer.ScrollChanged += SearchResultScrollViewerScrollChanged;
                        IsSearchResultLoading = false;//加载中效果隐藏
                    }
                    catch
                    {

                    }
                }
                else
                {
                    //点击太快时会触发到这
                }

            });
        }


        T FindSimpleVisualChild<T>(DependencyObject element) where T : class
        {
            while (element != null)
            {

                if (element is T)
                    return element as T;

                element = VisualTreeHelper.GetChild(element, 0);
            }

            return null;
        }

        private bool IsBootomScrollView(ScrollViewer view)
        {
            bool isBottom = false;

            double dVer = view.VerticalOffset;
            double vViewport = view.ViewportHeight;
            double eextent = view.ExtentHeight;
            if (dVer != 0)
            {
                if (dVer + vViewport == eextent)
                {
                    isBottom = true;
                }
                else
                {
                    isBottom = false;
                }
            }
            else
            {
                isBottom = false;
            }

            return isBottom;

        }

        private bool IsPackageIdInProjectDependencies(Dictionary<string, string> projectDependencies, string packageId)
        {
            foreach (var dependency in projectDependencies)
            {
                if (dependency.Key.ToLower() == packageId.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        private string GetCurrentPackageInstalledVersion(Dictionary<string, string> projectDependencies, string packageId)
        {
            foreach (var dependency in projectDependencies)
            {
                if (dependency.Key.ToLower() == packageId.ToLower())
                {
                    return dependency.Value.TrimStart('[').TrimEnd(']');
                }
            }
            return null;
        }


        public const string IsNotExistPackageSourceSearchResultItemsPropertyName = "IsNotExistPackageSourceSearchResultItems";
        private bool _isNotExistPackageSourceSearchResultItems = false;
        public bool IsNotExistPackageSourceSearchResultItems
        {
            get
            {
                return _isNotExistPackageSourceSearchResultItems;
            }
            set
            {
                if (_isNotExistPackageSourceSearchResultItems == value)
                {
                    return;
                }

                _isNotExistPackageSourceSearchResultItems = value;
                RaisePropertyChanged(IsNotExistPackageSourceSearchResultItemsPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="PackageSourceSearchResultItems" /> property's name.
        /// </summary>
        public const string PackageSourceSearchResultItemsPropertyName = "PackageSourceSearchResultItems";

        private ObservableCollection<PackageSourceSearchResultItem> _packageSourceSearchResultItemsProperty = new ObservableCollection<PackageSourceSearchResultItem>();

        /// <summary>
        /// Sets and gets the PackageSourceSearchResultItems property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<PackageSourceSearchResultItem> PackageSourceSearchResultItems
        {
            get
            {
                return _packageSourceSearchResultItemsProperty;
            }

            set
            {
                if (_packageSourceSearchResultItemsProperty == value)
                {
                    return;
                }

                if (value.Count > 0)
                {
                    IsNotExistPackageSourceSearchResultItems = false;
                }
                else
                {
                    IsNotExistPackageSourceSearchResultItems = true;
                }

                _packageSourceSearchResultItemsProperty = value;
                RaisePropertyChanged(PackageSourceSearchResultItemsPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="IsPackageSourceSearchResultItemSelected" /> property's name.
        /// </summary>
        public const string IsPackageSourceSearchResultItemSelectedPropertyName = "IsPackageSourceSearchResultItemSelected";

        private bool _isPackageSourceSearchResultItemSelectedProperty = false;

        /// <summary>
        /// Sets and gets the IsPackageSourceSearchResultItemSelected property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsPackageSourceSearchResultItemSelected
        {
            get
            {
                return _isPackageSourceSearchResultItemSelectedProperty;
            }

            set
            {
                if (_isPackageSourceSearchResultItemSelectedProperty == value)
                {
                    return;
                }

                _isPackageSourceSearchResultItemSelectedProperty = value;
                RaisePropertyChanged(IsPackageSourceSearchResultItemSelectedPropertyName);
            }
        }





        public bool SelectedItemRequireLicenseAcceptance { get; set; }



        /// <summary>
        /// The <see cref="SelectedItemIconDefault" /> property's name.
        /// </summary>
        public const string SelectedItemIconDefaultPropertyName = "SelectedItemIconDefault";

        private string _selectedItemIconDefaultProperty = "";

        /// <summary>
        /// Sets and gets the SelectedItemIconDefault property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SelectedItemIconDefault
        {
            get
            {
                return _selectedItemIconDefaultProperty;
            }

            set
            {
                if (_selectedItemIconDefaultProperty == value)
                {
                    return;
                }

                _selectedItemIconDefaultProperty = value;
                RaisePropertyChanged(SelectedItemIconDefaultPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="SelectedItemIconUrl" /> property's name.
        /// </summary>
        public const string SelectedItemIconUrlPropertyName = "SelectedItemIconUrl";

        private string _selectedItemIconUrlProperty = "";

        /// <summary>
        /// Sets and gets the SelectedItemIconUrl property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SelectedItemIconUrl
        {
            get
            {
                return _selectedItemIconUrlProperty;
            }

            set
            {
                if (value == null)
                {
                    value = "";
                }

                if (_selectedItemIconUrlProperty == value)
                {
                    return;
                }

                _selectedItemIconUrlProperty = value;
                RaisePropertyChanged(SelectedItemIconUrlPropertyName);
            }
        }




        /// <summary>
        /// The <see cref="SelectedItemIdentity" /> property's name.
        /// </summary>
        public const string SelectedItemIdentityPropertyName = "SelectedItemIdentity";

        private PackageIdentity _selectedItemIdentityProperty = null;

        /// <summary>
        /// Sets and gets the SelectedItemIdentity property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public PackageIdentity SelectedItemIdentity
        {
            get
            {
                return _selectedItemIdentityProperty;
            }

            set
            {
                if (_selectedItemIdentityProperty == value)
                {
                    return;
                }

                _selectedItemIdentityProperty = value;
                RaisePropertyChanged(SelectedItemIdentityPropertyName);
            }
        }




        /// <summary>
        /// The <see cref="SelectedItemTitle" /> property's name.
        /// </summary>
        public const string SelectedItemTitlePropertyName = "SelectedItemTitle";

        private string _selectedItemTitleProperty = "";

        /// <summary>
        /// Sets and gets the SelectedItemTitle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SelectedItemTitle
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedItemTitleProperty))
                {
                    return SelectedItemIdentity == null ? "" : SelectedItemIdentity.Id;//title为空时用packageid返回
                }
                return _selectedItemTitleProperty;
            }

            set
            {
                if (_selectedItemTitleProperty == value)
                {
                    return;
                }

                _selectedItemTitleProperty = value;
                RaisePropertyChanged(SelectedItemTitlePropertyName);
            }
        }

        /// <summary>
        /// The <see cref="SelectedItemIsInstalled" /> property's name.
        /// </summary>
        public const string SelectedItemIsInstalledPropertyName = "SelectedItemIsInstalled";

        private bool _selectedItemIsInstalledProperty = false;

        /// <summary>
        /// Sets and gets the SelectedItemIsInstalled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool SelectedItemIsInstalled
        {
            get
            {
                return _selectedItemIsInstalledProperty;
            }

            set
            {
                if (_selectedItemIsInstalledProperty == value)
                {
                    return;
                }

                _selectedItemIsInstalledProperty = value;
                RaisePropertyChanged(SelectedItemIsInstalledPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="SelectedItemInstalledVersion" /> property's name.
        /// </summary>
        public const string SelectedItemInstalledVersionPropertyName = "SelectedItemInstalledVersion";

        private string _selectedItemInstalledVersionProperty = "";

        /// <summary>
        /// Sets and gets the SelectedItemInstalledVersion property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SelectedItemInstalledVersion
        {
            get
            {
                return _selectedItemInstalledVersionProperty;
            }

            set
            {
                if (_selectedItemInstalledVersionProperty == value)
                {
                    return;
                }

                _selectedItemInstalledVersionProperty = value;
                RaisePropertyChanged(SelectedItemInstalledVersionPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="SelectedItemIsNeedUpdate" /> property's name.
        /// </summary>
        public const string SelectedItemIsNeedUpdatePropertyName = "SelectedItemIsNeedUpdate";

        private bool _selectedItemIsNeedUpdateProperty = false;

        /// <summary>
        /// Sets and gets the SelectedItemIsNeedUpdate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool SelectedItemIsNeedUpdate
        {
            get
            {
                return _selectedItemIsNeedUpdateProperty;
            }

            set
            {
                if (_selectedItemIsNeedUpdateProperty == value)
                {
                    return;
                }

                _selectedItemIsNeedUpdateProperty = value;
                RaisePropertyChanged(SelectedItemIsNeedUpdatePropertyName);
            }
        }


        /// <summary>
        /// The <see cref="SelectedItemVersionList" /> property's name.
        /// </summary>
        public const string SelectedItemVersionListPropertyName = "SelectedItemVersionList";

        private ObservableCollection<string> _selectedItemVersionListProperty = new ObservableCollection<string>();

        /// <summary>
        /// Sets and gets the SelectedItemVersionList property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<string> SelectedItemVersionList
        {
            get
            {
                return _selectedItemVersionListProperty;
            }

            set
            {
                if (_selectedItemVersionListProperty == value)
                {
                    return;
                }

                _selectedItemVersionListProperty = value;
                RaisePropertyChanged(SelectedItemVersionListPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="SelectedItemVersionListSelectedIndex" /> property's name.
        /// </summary>
        public const string SelectedItemVersionListSelectedIndexPropertyName = "SelectedItemVersionListSelectedIndex";

        private int _selectedItemVersionListSelectedIndexProperty = -1;

        /// <summary>
        /// Sets and gets the SelectedItemVersionListSelectedIndex property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int SelectedItemVersionListSelectedIndex
        {
            get
            {
                return _selectedItemVersionListSelectedIndexProperty;
            }

            set
            {
                if (_selectedItemVersionListSelectedIndexProperty == value)
                {
                    return;
                }

                _selectedItemVersionListSelectedIndexProperty = value;
                RaisePropertyChanged(SelectedItemVersionListSelectedIndexPropertyName);
            }
        }


        /// <summary>
        /// The <see cref="SelectedItemVersionListSelectedVersion" /> property's name.
        /// </summary>
        public const string SelectedItemVersionListSelectedVersionPropertyName = "SelectedItemVersionListSelectedVersion";

        private string _selectedItemVersionListSelectedVersionProperty = "";

        /// <summary>
        /// Sets and gets the SelectedItemVersionListSelectedVersion property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string SelectedItemVersionListSelectedVersion
        {
            get
            {
                return _selectedItemVersionListSelectedVersionProperty;
            }

            set
            {
                if (_selectedItemVersionListSelectedVersionProperty == value)
                {
                    return;
                }

                _selectedItemVersionListSelectedVersionProperty = value;
                RaisePropertyChanged(SelectedItemVersionListSelectedVersionPropertyName);


                //刷新当前选择的版本对应的包详细信息
                showSelectedItemVersionDetail(value);
            }
        }



        /// <summary>
        /// The <see cref="SelectedItemRuntimeRuleListSelectedIndex" /> property's name.
        /// </summary>
        public const string SelectedItemRuntimeRuleListSelectedIndexPropertyName = "SelectedItemRuntimeRuleListSelectedIndex";

        private int _selectedItemRuntimeRuleListSelectedIndexProperty = -1;

        /// <summary>
        /// Sets and gets the SelectedItemRuntimeRuleListSelectedIndex property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int SelectedItemRuntimeRuleListSelectedIndex
        {
            get
            {
                return _selectedItemRuntimeRuleListSelectedIndexProperty;
            }

            set
            {
                if (_selectedItemRuntimeRuleListSelectedIndexProperty == value)
                {
                    return;
                }

                _selectedItemRuntimeRuleListSelectedIndexProperty = value;
                RaisePropertyChanged(SelectedItemRuntimeRuleListSelectedIndexPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="IsOperateBusy" /> property's name.
        /// </summary>
        public const string IsOperateBusyPropertyName = "IsOperateBusy";

        private bool _isOperateBusyProperty = false;

        /// <summary>
        /// Sets and gets the IsOperateBusy property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsOperateBusy
        {
            get
            {
                return _isOperateBusyProperty;
            }

            set
            {
                if (_isOperateBusyProperty == value)
                {
                    return;
                }

                _isOperateBusyProperty = value;
                RaisePropertyChanged(IsOperateBusyPropertyName);
            }
        }



        private RelayCommand _selectedItemInstallCommand;

        /// <summary>
        /// Gets the SelectedItemInstallCommand.
        /// </summary>
        public RelayCommand SelectedItemInstallCommand
        {
            get
            {
                return _selectedItemInstallCommand
                    ?? (_selectedItemInstallCommand = new RelayCommand(
                    () =>
                    {
                        //记录所选择的待安装的包
                        var identity = SelectedItemIdentity;

                        //确定是否需要用户接受许可证
                        if (SelectedItemRequireLicenseAcceptance)
                        {
                            //确定接收许可证吗？
                            var ret = UniMessageBox.Show(m_view, "该程序包要求你在安装前接受其许可证条款，确定接受吗？", "询问", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                            if (ret != MessageBoxResult.Yes)
                            {
                                return;
                            }
                        }

                        IsOperateBusy = true;

                        Task.Run(async () =>
                        {
                            //添加到project.json的依赖项里，根据依赖项重新加载DLL
                            var projectDependencies = ViewModelLocator.instance.Project.ProjectDependencies;
                            projectDependencies.Remove(identity.Id);

                            var ver_range = "";
                            if (SelectedItemRuntimeRuleListSelectedIndex == 0)
                            {
                                //严格
                                ver_range = $"[{identity.Version.ToString()}]";
                            }
                            else if (SelectedItemRuntimeRuleListSelectedIndex == 1)
                            {
                                //最低适用
                                ver_range = identity.Version.ToString();
                            }
                            projectDependencies.Add(identity.Id, ver_range);
                            ViewModelLocator.instance.Project.UpdateDependenciesOfProjectJsonConfig(projectDependencies);

                            // 每次初始化项目后，重新刷新 Activities 工具箱
                            //ViewModelLocator.instance.Activities.initAvailableActivities();

                            Common.RunInUI(() =>
                            {
                                //ViewModelLocator.instance.Project.RefreshCommand.Execute(null); //重刷项目视图

                                SelectedItemIsInstalled = true;
                                SelectedItemInstalledVersion = identity.Version.OriginalVersion;

                                DoSearch();

                                IsOperateBusy = false;
                            });
                        });
                    }));
            }
        }


        private RelayCommand _selectedItemUninstallCommand;

        /// <summary>
        /// Gets the SelectedItemInstallCommand.
        /// </summary>
        public RelayCommand SelectedItemUninstallCommand
        {
            get
            {
                return _selectedItemUninstallCommand
                    ?? (_selectedItemUninstallCommand = new RelayCommand(
                    () =>
                    {
                        //记录所选择的待卸载的包
                        var identity = SelectedItemIdentity;

                        IsOperateBusy = true;

                        Task.Run(async () =>
                        {
                            //添加到project.json的依赖项里，根据依赖项重新加载DLL
                            var projectDependencies = ViewModelLocator.instance.Project.ProjectDependencies;
                            projectDependencies.Remove(identity.Id);

                            ViewModelLocator.instance.Project.UpdateDependenciesOfProjectJsonConfig(projectDependencies);

                            // 每次初始化项目后，重新刷新 Activities 工具箱
                            //ViewModelLocator.instance.Activities.initAvailableActivities();

                            Common.RunInUI(() =>
                            {
                                //ViewModelLocator.instance.Project.RefreshCommand.Execute(null); //重刷项目视图

                                SelectedItemIsInstalled = false;

                                DoSearch();

                                IsOperateBusy = false;
                            });
                        });
                    }));
            }
        }


        private RelayCommand _selectedItemUpdateCommand;

        /// <summary>
        /// Gets the SelectedItemInstallCommand.
        /// </summary>
        public RelayCommand SelectedItemUpdateCommand
        {
            get
            {
                return _selectedItemUpdateCommand
                    ?? (_selectedItemUpdateCommand = new RelayCommand(
                    () =>
                    {
                        //记录所选择的待更新的包
                        var identity = SelectedItemIdentity;

                        IsOperateBusy = true;

                        Task.Run(async () =>
                        {
                            //添加到project.json的依赖项里，根据依赖项重新加载DLL
                            var projectDependencies = ViewModelLocator.instance.Project.ProjectDependencies;
                            projectDependencies.Remove(identity.Id);

                            var ver_range = "";
                            if (SelectedItemRuntimeRuleListSelectedIndex == 0)
                            {
                                //严格
                                ver_range = $"[{identity.Version.ToString()}]";
                            }
                            else if (SelectedItemRuntimeRuleListSelectedIndex == 1)
                            {
                                //最低适用
                                ver_range = identity.Version.ToString();
                            }
                            projectDependencies.Add(identity.Id, ver_range);
                            ViewModelLocator.instance.Project.UpdateDependenciesOfProjectJsonConfig(projectDependencies);

                            // 每次初始化项目后，重新刷新 Activities 工具箱
                            //ViewModelLocator.instance.Activities.initAvailableActivities();

                            Common.RunInUI(() =>
                            {
                                //ViewModelLocator.instance.Project.RefreshCommand.Execute(null); //重刷项目视图

                                SelectedItemIsInstalled = false;

                                DoSearch();

                                IsOperateBusy = false;
                            });
                        });
                    }));
            }
        }







        private void richTextBoxInsertRun(RichTextBox rtb, string key, string val, bool insertLineBreak = false)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run(key) { FontWeight = FontWeights.Bold });

            if (insertLineBreak)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            paragraph.Inlines.Add(new Run(val));
            rtb.Document.Blocks.Add(paragraph);
        }

        private void richTextBoxInsertHyperlinkRun(RichTextBox rtb, string key, string val, string url)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run(key) { FontWeight = FontWeights.Bold });
            var hyperlink = new Hyperlink(new Run(val)) { NavigateUri = new Uri(url), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#498FCC")) };
            hyperlink.RequestNavigate += new RequestNavigateEventHandler(Hyperlink_RequestNavigate);
            paragraph.Inlines.Add(hyperlink);
            rtb.Document.Blocks.Add(paragraph);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void richTextBoxInsertDependencies(RichTextBox rtb, string key, List<PackageDependencyGroup> dependencies)
        {
            Paragraph paragraph = null;

            if (dependencies.Count > 0)
            {
                paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run(key) { FontWeight = FontWeights.Bold });
                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new LineBreak());
            }

            foreach (var group in dependencies)
            {
                if (group.TargetFramework.Framework == "Any")
                {
                    //Any的话不显示粗体文字头
                }
                else
                {
                    paragraph.Inlines.Add(new Run(group.TargetFramework.DotNetFrameworkName) { FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new LineBreak());
                }

                if (group.Packages.Count() == 0)
                {
                    paragraph.Inlines.Add(new Run("无依赖项") { FontStyle = FontStyles.Italic });
                }
                else
                {
                    foreach (var pd in group.Packages)
                    {
                        paragraph.Inlines.Add(new Run(pd.ToString()));
                        paragraph.Inlines.Add(new LineBreak());
                    }
                }

                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new LineBreak());
            }


            if (paragraph != null)
            {
                rtb.Document.Blocks.Add(paragraph);
            }
        }

        private void showSelectedItemVersionDetail(string verStr)
        {
            if (m_currentSelectedItemPackageSearchMetadataList.Count == 0)
            {
                //联网请求未完成，显示默认的离线详情信息
                return;
            }

            foreach (var verItem in m_currentSelectedItemPackageSearchMetadataList)
            {
                if (verItem.Identity.Version.OriginalVersion == verStr)
                {
                    showSelectedItemVersionDetail(verItem);
                    break;
                }
            }

        }

        public void showSelectedItemVersionDetail(IPackageSearchMetadata item)
        {
            if (CurrentSelectedPackageSourceSearchResultItem.Id != item.Identity.Id)
            {
                return;//判断当前选择的id是否是要显示的
            }

            SelectedItemIdentity = item.Identity;
            SelectedItemTitle = item.Title;
            SelectedItemIconUrl = item.IconUrl?.AbsoluteUri;
            SelectedItemRequireLicenseAcceptance = item.RequireLicenseAcceptance;

            //当前选择的版本和已安装版本不一样时，更新按钮启用
            if (SelectedItemIsInstalled)
            {
                SelectedItemIsNeedUpdate = new NuGetVersion(SelectedItemInstalledVersion) != item.Identity.Version;
            }

            m_richTextBox.Document.Blocks.Clear();

            richTextBoxInsertRun(m_richTextBox, "描述：", item.Description, true);
            richTextBoxInsertRun(m_richTextBox, "版本：", item.Identity.Version.OriginalVersion);
            richTextBoxInsertRun(m_richTextBox, "作者：", item.Authors);

            if (!string.IsNullOrEmpty(item.LicenseUrl?.AbsoluteUri))
            {
                richTextBoxInsertHyperlinkRun(m_richTextBox, "许可证：", "查看许可证信息", item.LicenseUrl.AbsoluteUri);
            }

            richTextBoxInsertRun(m_richTextBox, "发布日期：", item.Published?.ToLocalTime().ToString("G"));

            if (!string.IsNullOrEmpty(item.ProjectUrl?.AbsoluteUri))
            {
                richTextBoxInsertHyperlinkRun(m_richTextBox, "项目地址：", "查看项目信息", item.ProjectUrl.AbsoluteUri);
            }

            if (!string.IsNullOrEmpty(item.Tags))
            {
                richTextBoxInsertRun(m_richTextBox, "标签：", item.Tags);
            }

            //显示依赖项
            richTextBoxInsertDependencies(m_richTextBox, "依赖项：", item.DependencySets.ToList());

        }

        public void ShowPackageSourceSearchResultItemDetail(PackageSourceSearchResultItem item)
        {
            //最右侧面板显示包详情页面
            //默认显示离线的详情信息
            m_currentSelectedItemPackageSearchMetadataList.Clear();

            IsPackageSourceSearchResultItemSelected = true;

            SelectedItemIsNeedUpdate = false;

            SelectedItemRequireLicenseAcceptance = item.RequireLicenseAcceptance;

            if (item.IsInstalled)
            {
                SelectedItemIsInstalled = true;
                SelectedItemInstalledVersion = item.InstalledVersion;
            }
            else
            {
                SelectedItemIsInstalled = false;
            }

            SelectedItemIdentity = item.Identity;
            SelectedItemTitle = item.Title;

            SelectedItemIconDefault = "";//清空以触发数据改变来刷新
            SelectedItemIconDefault = "pack://application:,,,/Resource/Image/Windows/ManagePackages/nuget.png";
            SelectedItemIconUrl = item.IconUrl;

            SelectedItemVersionList.Clear();

            SelectedItemVersionList.Add(item.InstalledVersion);

            SelectedItemVersionListSelectedIndex = 0;

            SelectedItemRuntimeRuleListSelectedIndex = 0;

            m_richTextBox.Document.Blocks.Clear();
            m_richTextBox.ScrollToHome();

            richTextBoxInsertRun(m_richTextBox, "描述：", item.Description, true);
            richTextBoxInsertRun(m_richTextBox, "版本：", item.InstalledVersion);
            richTextBoxInsertRun(m_richTextBox, "作者：", item.Authors);

            if (!string.IsNullOrEmpty(item.LicenseUrl))
            {
                richTextBoxInsertHyperlinkRun(m_richTextBox, "许可证：", "查看许可证信息", item.LicenseUrl);
            }

            richTextBoxInsertRun(m_richTextBox, "发布日期：", item.PublishTime);

            if (!string.IsNullOrEmpty(item.ProjectUrl))
            {
                richTextBoxInsertHyperlinkRun(m_richTextBox, "项目地址：", "查看项目信息", item.ProjectUrl);
            }

            if (!string.IsNullOrEmpty(item.Tags))
            {
                richTextBoxInsertRun(m_richTextBox, "标签：", item.Tags);
            }

            //显示依赖项
            richTextBoxInsertDependencies(m_richTextBox, "依赖项：", item.Dependencies);

            //异步请求网络然后再刷新下这个包条目对应的信息（默认选择第一项，用户手动选择下拉列表项时也触发刷新）
            IsSearchPackageVersionListResultLoading = true;
            Task.Run(async () =>
            {
                List<IPackageSearchMetadata> search_package_versions = await NuGetPackageController.Instance.SearchPackageVersions(item.Id, IsIncludePrerelease);

                Common.RunInUI(() =>
                {
                    if (item.Id == CurrentSelectedPackageSourceSearchResultItem.Id)
                    {
                        search_package_versions.Sort((x, y) => -x.Identity.Version.CompareTo(y.Identity.Version));//按从高到低排序
                        m_currentSelectedItemPackageSearchMetadataList = search_package_versions;

                        SelectedItemVersionList.Clear();
                        foreach (var ver in m_currentSelectedItemPackageSearchMetadataList)
                        {
                            SelectedItemVersionList.Add(ver.Identity.Version.OriginalVersion);
                        }

                        SelectedItemVersionListSelectedIndex = 0;

                        IsSearchPackageVersionListResultLoading = false;
                    }
                    else
                    {
                        //搜索结果条目乱点太快会触发到这里
                    }

                });

            });

        }


    }
}