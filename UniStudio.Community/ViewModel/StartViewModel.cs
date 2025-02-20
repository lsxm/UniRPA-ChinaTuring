﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using UniStudio.Community.Librarys;
using UniStudio.Community.Windows;

namespace UniStudio.Community.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class StartViewModel : ViewModelBase
    {
        public UserControl m_view { get; set; }

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
                        m_view = (UserControl)p.Source;
                    }));
            }
        }


        #region 主题色配置
        public const string ItemTitleForegroundPropertyName = "ItemTitleForeground";
        private SolidColorBrush _itemTitleForeground = new BrushConverter().ConvertFrom("#383838") as SolidColorBrush;
        public SolidColorBrush ItemTitleForeground
        {
            get
            {
                return _itemTitleForeground;
            }
            set
            {
                if (_itemTitleForeground == value)
                {
                    return;
                }

                _itemTitleForeground = value;
                RaisePropertyChanged(ItemTitleForegroundPropertyName);
            }
        }

        public const string ItemDescriptionForegroundPropertyName = "ItemDescriptionForeground";
        private SolidColorBrush _itemDescriptionForeground = new BrushConverter().ConvertFrom("#606060") as SolidColorBrush;
        public SolidColorBrush ItemDescriptionForeground
        {
            get
            {
                return _itemDescriptionForeground;
            }
            set
            {
                if (_itemDescriptionForeground == value)
                {
                    return;
                }

                _itemDescriptionForeground = value;
                RaisePropertyChanged(ItemDescriptionForegroundPropertyName);
            }
        }

        public const string ItemMouseOverBackgroundPropertyName = "ItemMouseOverBackground";
        private SolidColorBrush _itemMouseOverBackground = new BrushConverter().ConvertFrom("#d6d6d6") as SolidColorBrush;
        public SolidColorBrush ItemMouseOverBackground
        {
            get
            {
                return _itemMouseOverBackground;
            }
            set
            {
                if (_itemMouseOverBackground == value)
                {
                    return;
                }

                _itemMouseOverBackground = value;
                RaisePropertyChanged(ItemMouseOverBackgroundPropertyName);
            }
        }
        #endregion


        /// <summary>
        /// Initializes a new instance of the StartViewModel class.
        /// </summary>
        public StartViewModel()
        {
            var item = new RecentProjectItem();

            InitRecentProjects();

            Messenger.Default.Register<MessengerObjects.RecentProjectsModify>(this, (obj) =>
            {
                Common.RunInUI(() =>
                {
                    InitRecentProjects();
                });
            });


            Messenger.Default.Register<MessengerObjects.CustomTemplateModify>(this, (obj) =>
            {
                Common.RunInUI(() =>
                {
                    InitCustomTemplate();
                });
            });


            Messenger.Default.Register<ProjectSettingsViewModel>(this, "ProjectSettingsModify", (obj) =>
            {
                Common.RunInUI(() =>
                {
                    ProjectSettingsModify(obj);
                });
            });
        }

        private void ProjectSettingsModify(ProjectSettingsViewModel obj)
        {
            XmlDocument doc = new XmlDocument();
            var path = App.LocalRPAStudioDir + @"\Config\RecentProjects.xml";
            doc.Load(path);
            var rootNode = doc.DocumentElement;

            var projectNodes = rootNode.SelectNodes("Project");
            foreach (XmlElement dir in projectNodes)
            {
                var filePath = dir.GetAttribute("FilePath");
                var name = dir.GetAttribute("Name");
                var description = dir.GetAttribute("Description");

                if (obj.CurrentProjectJsonFile == filePath)
                {
                    if (obj.ProjectName != name || obj.ProjectDescription != description)
                    {
                        dir.SetAttribute("Name", obj.ProjectName);
                        dir.SetAttribute("Description", obj.ProjectDescription);
                        dir.SetAttribute("FilePath", Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(filePath)), obj.ProjectName, "project.json"));

                        doc.Save(path);

                        InitRecentProjects();
                        break;
                    }
                }
            }
        }

        private void InitRecentProjects()
        {
            RecentProjects.Clear();

            XmlDocument doc = new XmlDocument();
            var path = App.LocalRPAStudioDir + @"\Config\RecentProjects.xml";
            doc.Load(path);
            var rootNode = doc.DocumentElement;

            var maxShowCount = Convert.ToInt32(rootNode.GetAttribute("MaxShowCount"));
            var projectNodes = rootNode.SelectNodes("Project");
            foreach (XmlNode dir in projectNodes)
            {
                var filePath = (dir as XmlElement).GetAttribute("FilePath");
                var name = (dir as XmlElement).GetAttribute("Name");
                var description = (dir as XmlElement).GetAttribute("Description");

                var item = new RecentProjectItem();
                item.ProjectFilePath = filePath;
                item.ProjectName = name;
                item.ProjectDescription = description;

                RecentProjects.Add(item);

                maxShowCount--;
                if (maxShowCount == 0)
                {
                    break;
                }
            }
        }


        private void InitCustomTemplate()
        {
            CustomTemplate = CustomTemplate;
        }


        /// <summary>
        /// The <see cref="RecentProjects" /> property's name.
        /// </summary>
        public const string RecentProjectsPropertyName = "RecentProjects";

        private ObservableCollection<RecentProjectItem> _recentProjectsProperty = new ObservableCollection<RecentProjectItem>();

        /// <summary>
        /// Sets and gets the RecentProjects property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<RecentProjectItem> RecentProjects
        {
            get
            {
                return _recentProjectsProperty;
            }

            set
            {
                if (_recentProjectsProperty == value)
                {
                    return;
                }

                _recentProjectsProperty = value;
                RaisePropertyChanged(RecentProjectsPropertyName);
            }
        }

        public const string DefaultTemplateProperty = "DefaultTemplate";
        private ObservableCollection<TemplateItem> _defaultTemplate = new ObservableCollection<TemplateItem>();
        public ObservableCollection<TemplateItem> DefaultTemplate
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                using (var ms = new MemoryStream(Properties.Resources.DefaultTemplate))
                {
                    ms.Flush();
                    ms.Position = 0;
                    doc.Load(ms);
                    ms.Close();
                }

                var rootNode = doc.DocumentElement;
                var templateNodes = rootNode.SelectNodes("Template");

                _defaultTemplate.Clear();
                foreach (XmlElement template in templateNodes)
                {
                    _defaultTemplate.Add(
                        new TemplateItem
                        {
                            TemplateName = template.GetAttribute("TemplateName"),
                            TemplateDescription = template.GetAttribute("TemplateDescription"),
                            DefaultProjectName = template.GetAttribute("DefaultProjectName"),
                            DefaultProjectDescription = template.GetAttribute("DefaultProjectDescription"),
                            TemplateDirectoryPath = template.GetAttribute("TemplateDirectoryPath")
                        });
                }

                return _defaultTemplate;
            }
        }

        public const string CustomTemplateProperty = "CustomTemplate";
        private ObservableCollection<TemplateItem> _customTemplate = new ObservableCollection<TemplateItem>();
        public ObservableCollection<TemplateItem> CustomTemplate
        {
            get
            {
                XmlDocument doc = new XmlDocument();
                var path = App.LocalRPAStudioDir + @"\Config\CustomTemplate.xml";
                if (!File.Exists(path))
                {
                    using (var ms = new MemoryStream(Properties.Resources.CustomTemplate))
                    {
                        ms.Flush();
                        ms.Position = 0;
                        doc.Load(ms);
                        ms.Close();
                    }
                    doc.Save(path);
                }
                else
                {
                    doc.Load(path);
                }

                var rootNode = doc.DocumentElement;
                var templateNodes = rootNode.SelectNodes("Template");

                _customTemplate.Clear();
                foreach (XmlElement template in templateNodes)
                {
                    _customTemplate.Add(
                        new TemplateItem
                        {
                            TemplateName = template.GetAttribute("TemplateName"),
                            TemplateDescription = template.GetAttribute("TemplateDescription"),
                            DefaultProjectName = template.GetAttribute("DefaultProjectName"),
                            DefaultProjectDescription = template.GetAttribute("DefaultProjectDescription"),
                            TemplateDirectoryPath = template.GetAttribute("TemplateDirectoryPath")
                        });
                }

                return _customTemplate;
            }
            set
            {
                if (_customTemplate == value)
                {
                    return;
                }

                _customTemplate = value;
                RaisePropertyChanged(CustomTemplateProperty);
            }
        }


        private RelayCommand _newProcessCommand;

        /// <summary>
        /// Gets the NewProcessCommand.
        /// </summary>
        public RelayCommand NewProcessCommand
        {
            get
            {
                return _newProcessCommand
                    ?? (_newProcessCommand = new RelayCommand(
                    () =>
                    {
                        //弹出新建项目对话框
                        var window = new NewProjectWindow();
                        window.Owner = Application.Current.MainWindow;
                        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        var vm = window.DataContext as NewProjectViewModel;
                        vm.ProjectType = NewProjectViewModel.enProjectType.Process;
                        window.ShowDialog();
                    }));
            }
        }







    }
}