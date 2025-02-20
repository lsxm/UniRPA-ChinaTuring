﻿using System;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Activities;
using Microsoft.VisualBasic.Activities;
using System.Activities.Expressions;
using Plugins.Shared.Library.UiAutomation;
using Plugins.Shared.Library.Window;
using System.IO;
using System.Diagnostics;
using Plugins.Shared.Library.Librarys;
using Plugins.Shared.Library.Extensions;

namespace KeyboardActivity
{
    public partial class TypeIntoDesigner
    {
        private string screenshotsPath { get; set; }
        public TypeIntoDesigner()
        {
            InitializeComponent();
        }

        private void HyperlinkClick(object sender, RoutedEventArgs e)
        {
            UiElement.OnSelected = UiElement_OnSelected;//也可在程序安装始化时赋值
            UiElement.StartElementHighlight();
        }

        private void UiElement_OnSelected(UiElement uiElement)
        {
            var screenshotsPath = uiElement.CaptureInformativeScreenshotToFile();
            grid1.Visibility = System.Windows.Visibility.Hidden;
            setPropertyValue("SourceImgPath", screenshotsPath);
            setPropertyValue("Selector", new InArgument<string>(uiElement.Selector));
            setPropertyValue("SelectorOrigin", SerializeObj.Serialize(InExplorerOpen.BuildSelectorStatusModelFromStr(uiElement.Selector.ToString())));
            setPropertyValue("visibility", System.Windows.Visibility.Visible);
            if (getPropertyValue("DisplayName").Equals(getPropertyValue("DefaultName")))
            {
                string displayName = getPropertyValue("DisplayName") + " \"" + uiElement.ProcessName + " " + uiElement.Name + "\"";
                setPropertyValue("DisplayName", displayName);
            }
        }

        private void NavigateButtonClick(object sender, RoutedEventArgs e)
        {
            contextMenu.PlacementTarget = this.navigateButton;
            contextMenu.Placement = PlacementMode.Top;
            contextMenu.IsOpen = true;
        }

        private void NavigateButtonInitialized(object sender, EventArgs e)
        {
            navigateButton.ContextMenu = null;
        }

        private void meauItemClickOne(object sender, RoutedEventArgs e)
        {
            UiElement.OnSelected = UiElement_OnSelected;//也可在程序安装始化时赋值
            UiElement.StartElementHighlight();
        }

        /// <summary>
        /// 在 UI 探测器中打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void meauItemClickTwo(object sender, RoutedEventArgs e)
        {
            var selectorModelProperty = base.ModelItem.Properties["Selector"];
            var selectorOriginModelProperty = base.ModelItem.Properties["SelectorOrigin"];

            if (selectorModelProperty.Value != null)
            {
                InExplorerOpen.Execute(ref selectorModelProperty, ref selectorOriginModelProperty);
            }
            else
            {
                UniMessageBox.Show("没有数据！请先通过鼠标选取器选择元素。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selectedValue = (e.OriginalSource as ComboBox).SelectedValue;
            if (selectedValue == null)
            {
                return;
            }
            string text = selectedValue.ToString().TrimStart(' ');
            if (text == "Special Keys")
            {
                return;
            }
            InArgument<string> inArgument = base.ModelItem.Properties["Text"].ComputedValue as InArgument<string>;
            if (inArgument == null || inArgument.Expression == null)
            {
                base.ModelItem.Properties["Text"].SetValue(new InArgument<string>("[k(" + text + ")]"));
            }
            else if (inArgument.Expression.GetType().Equals(typeof(VisualBasicValue<string>)))
            {
                VisualBasicValue<string> visualBasicValue = inArgument.Expression as VisualBasicValue<string>;
                if (visualBasicValue == null || string.IsNullOrWhiteSpace(visualBasicValue.ExpressionText))
                {
                    base.ModelItem.Properties["Text"].SetValue(new InArgument<string>("[k(" + text + ")]"));
                    return;
                }
                string expressionText = visualBasicValue.ExpressionText;
                expressionText = ((expressionText[expressionText.Length - 1] != '"') ? (expressionText + "+ \"[k(" + text + ")]\"") : (expressionText.Substring(0, expressionText.Length - 1) + "[k(" + text + ")]\""));
                InArgument<string> inArgument2 = new InArgument<string>();
                inArgument2.Expression = new VisualBasicValue<string>(expressionText);
                base.ModelItem.Properties["Text"].SetValue(inArgument2);
            }
            else if (inArgument.Expression.GetType().Equals(typeof(Literal<string>)))
            {
                Literal<string> literal = inArgument.Expression as Literal<string>;
                if (literal == null || string.IsNullOrEmpty(literal.Value))
                {
                    base.ModelItem.Properties["Text"].SetValue(new InArgument<string>("[k(" + text + ")]"));
                }
                else
                {
                    base.ModelItem.Properties["Text"].SetValue(new InArgument<string>(literal.Value + "[k(" + text + ")]"));
                }
            }
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
                navigateTextBlock.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
