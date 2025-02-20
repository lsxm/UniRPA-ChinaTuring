﻿using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Linq;

namespace ControlActivity
{
    public enum ScopeOption
    {
        FIND_CHILDREN,
        FIND_DESCENDANTS,
        FIND_TOP_LEVELS,
        FIND_PROCESS,
        FIND_THREAD
    }

    public enum CursorPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }


    //Check - 选中复选框或单选按钮。Uncheck - 清除复选框或单选按钮。Toggle - 更改切换UI元素的值。
    public enum ActionEnums
    {
        Check,
        Uncheck,
        Toggle
    }

    public class CommonVariable
    {
        public static string getPropertyValue(string propertyName, ModelItem currItem)
        {
            List<ModelProperty> PropertyList = currItem.Properties.ToList();
            ModelProperty _property = PropertyList.Find((ModelProperty property) => property.Name.Equals(propertyName));
            return _property.Value.ToString();
        }
    }
}
