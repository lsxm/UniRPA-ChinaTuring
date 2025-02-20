﻿using Plugins.Shared.Library.UiAutomation;
using Plugins.Shared.Library.Window;
using System;
using System.Activities;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace WindowActivity
{
    /// <summary>
    /// FindWindowDesigner.xaml 的交互逻辑
    /// </summary>
    public partial class FindWindowDesigner
    {
        public FindWindowDesigner()
        {
            InitializeComponent();
        }
        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            UiElement.OnSelected = UiElement_OnSelected;
            UiElement.StartWindowHighlight();
        }

        private void UiElement_OnSelected(UiElement uiElement)
        {
            var screenshotsPath = uiElement.CaptureInformativeScreenshotToFile();
            setPropertyValue("SourceImgPath", screenshotsPath);
         //   setPropertyValue("Selector", new InArgument<string>(uiElement.Selector));
            grid1.Visibility = System.Windows.Visibility.Hidden;
            setPropertyValue("visibility", System.Windows.Visibility.Visible);

            if (getPropertyValue("DisplayName").Equals(getPropertyValue("DefaultName")))
            {
                string displayName = getPropertyValue("DisplayName") + " \"" + uiElement.ProcessName + " " + uiElement.Name + "\"";
                setPropertyValue("DisplayName", displayName);
            }
            InArgument<string> _name = uiElement.Name;
            InArgument<string> _className = uiElement.ClassName;
            setPropertyValue("Title", _name);
            setPropertyValue("ClassName", _className);
        }

        private void setPropertyValue<T>(string propertyName, T value)
        {
            base.ModelItem.Properties[propertyName].SetValue(value);
        }

        private string getPropertyValue(string propertyName)
        {
            ModelProperty _property = base.ModelItem.Properties[propertyName];
            if (_property.Value == null)
                return "";
            return _property.Value.ToString();
        }

        private void HiddenNavigateTextBlock()
        {
            navigateTextBlock.Visibility = System.Windows.Visibility.Hidden;
        }

        //菜单按钮点击
        private void NavigateButtonClick(object sender, RoutedEventArgs e)
        {
            contextMenu.PlacementTarget = this.navigateButton;
            contextMenu.Placement = PlacementMode.Top;
            contextMenu.IsOpen = true;
        }

        //菜单按钮初始化
        private void NavigateButtonInitialized(object sender, EventArgs e)
        {
            navigateButton.ContextMenu = null;
        }

        //菜单项点击测试
        private void meauItemClickOne(object sender, RoutedEventArgs e)
        {
            UiElement.OnSelected = UiElement_OnSelected;
            UiElement.StartWindowHighlight();
        }

        private void Button_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string src = getPropertyValue("SourceImgPath");
            ShowImageWindow imgShow = new ShowImageWindow();
            imgShow.ShowImage(src);
        }

        private void ActivityDesigner_Loaded(object sender, RoutedEventArgs e)
        {
            string src = getPropertyValue("SourceImgPath");
            if (src != "")
                grid1.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
