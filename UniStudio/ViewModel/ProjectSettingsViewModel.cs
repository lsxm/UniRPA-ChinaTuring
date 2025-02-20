﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using System.IO;
using System.Windows;
using System;
using UniStudio.Librarys;

namespace UniStudio.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class ProjectSettingsViewModel : ViewModelBase
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Window m_view;

        public string CurrentProjectJsonFile { get; internal set; }

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

                        projectNameValidate(ProjectName);
                    }));
            }
        }



        

        /// <summary>
        /// The <see cref="IsProjectNameCorrect" /> property's name.
        /// </summary>
        public const string IsProjectNameCorrectPropertyName = "IsProjectNameCorrect";

        private bool _isProjectNameCorrectProperty = false;

        /// <summary>
        /// Sets and gets the IsProjectNameCorrect property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsProjectNameCorrect
        {
            get
            {
                return _isProjectNameCorrectProperty;
            }

            set
            {
                if (_isProjectNameCorrectProperty == value)
                {
                    return;
                }

                _isProjectNameCorrectProperty = value;
                RaisePropertyChanged(IsProjectNameCorrectPropertyName);
            }
        }



        /// <summary>
        /// The <see cref="ProjectName" /> property's name.
        /// </summary>
        public const string ProjectNamePropertyName = "ProjectName";

        private string _projectNameProperty = "";

        /// <summary>
        /// Sets and gets the ProjectName property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProjectName
        {
            get
            {
                return _projectNameProperty;
            }

            set
            {
                if (_projectNameProperty == value)
                {
                    return;
                }

                _projectNameProperty = value;
                RaisePropertyChanged(ProjectNamePropertyName);

                projectNameValidate(value);
            }
        }


        /// <summary>
        /// The <see cref="ProjectNameValidatedWrongTip" /> property's name.
        /// </summary>
        public const string ProjectNameValidatedWrongTipPropertyName = "ProjectNameValidatedWrongTip";

        private string _projectNameValidatedWrongTipProperty = "";

        /// <summary>
        /// Sets and gets the ProjectNameValidatedWrongTip property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProjectNameValidatedWrongTip
        {
            get
            {
                return _projectNameValidatedWrongTipProperty;
            }

            set
            {
                if (_projectNameValidatedWrongTipProperty == value)
                {
                    return;
                }

                _projectNameValidatedWrongTipProperty = value;
                RaisePropertyChanged(ProjectNameValidatedWrongTipPropertyName);
            }
        }

        private void projectNameValidate(string value)
        {
            IsProjectNameCorrect = true;
            if (string.IsNullOrEmpty(value))
            {
                IsProjectNameCorrect = false;
                ProjectNameValidatedWrongTip = "名称不能为空";
            }
            else if (value.Contains(@"\") || value.Contains(@"/"))
            {
                IsProjectNameCorrect = false;
                ProjectNameValidatedWrongTip = "名称不能有非法字符";
            }
            else if (value.Equals(Path.GetFileName(Path.GetDirectoryName(CurrentProjectJsonFile))))
            {
                IsProjectNameCorrect = false;
                ProjectNameValidatedWrongTip = "名称未修改";
            }
            else
            {
                System.IO.FileInfo fi = null;
                try
                {
                    fi = new System.IO.FileInfo(value);
                }
                catch (ArgumentException) { }
                catch (System.IO.PathTooLongException) { }
                catch (NotSupportedException) { }
                if (ReferenceEquals(fi, null))
                {
                    // file name is not valid
                    IsProjectNameCorrect = false;
                    ProjectNameValidatedWrongTip = "名称不能有非法字符";
                }
                else
                {
                    // file name is valid... May check for existence by calling fi.Exists.
                }
            }

            OkCommand.RaiseCanExecuteChanged();
        }




        /// <summary>
        /// The <see cref="ProjectDescription" /> property's name.
        /// </summary>
        public const string ProjectDescriptionPropertyName = "ProjectDescription";

        private string _projectDescriptionProperty = "";

        /// <summary>
        /// Sets and gets the ProjectDescription property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ProjectDescription
        {
            get
            {
                return _projectDescriptionProperty;
            }

            set
            {
                if (_projectDescriptionProperty == value)
                {
                    return;
                }

                _projectDescriptionProperty = value;
                RaisePropertyChanged(ProjectDescriptionPropertyName);
            }
        }



        /// <summary>
        /// Initializes a new instance of the ProjectSettingsViewModel class.
        /// </summary>
        public ProjectSettingsViewModel()
        {
        }






        private RelayCommand _okCommand;

        /// <summary>
        /// Gets the OkCommand.
        /// </summary>
        public RelayCommand OkCommand
        {
            get
            {
                return _okCommand
                    ?? (_okCommand = new RelayCommand(
                    () =>
                    {
                        // 关闭项目
                        ViewModelLocator.instance.Main.DoCloseProject();

                        //广播修改
                        Messenger.Default.Send(this, "ProjectSettingsModify");

                        // 重命名项目文件夹
                        var sourceProjectDirectoryName = Path.GetDirectoryName(CurrentProjectJsonFile);
                        var newProjectDirectoryName = Path.Combine(Path.GetDirectoryName(sourceProjectDirectoryName), ProjectName);
                        Directory.Move(sourceProjectDirectoryName, newProjectDirectoryName);

                        // 更新项目配置文件
                        updateProjectJson(Path.Combine(newProjectDirectoryName, "project.json"));

                        // 关闭项目设置窗口
                        m_view.Close();

                        // 重新打开当前项目
                        CurrentProjectJsonFile = Path.Combine(newProjectDirectoryName, "project.json");
                        var msg = new MessengerObjects.ProjectOpen();
                        msg.ProjectJsonFile = CurrentProjectJsonFile;
                        msg.Sender = this;
                        Messenger.Default.Send(msg);
                    },
                    () => IsProjectNameCorrect));
            }
        }

        private void updateProjectJson(string projectJsonFile)
        {
            //json更新
            string json = File.ReadAllText(projectJsonFile);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            jsonObj["name"] = ProjectName;
            jsonObj["description"] = ProjectDescription;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(projectJsonFile, output);
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
                        m_view.Close();
                    },
                    () => true));
            }
        }

        
    }
}