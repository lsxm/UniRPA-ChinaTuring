﻿using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.Linq;

namespace AttributeActivity
{
    public enum AttributeEnums
    {
        aaname,
        aastate,
        app,
        AppPath,
        automationId,
        cls,
        cookie,
        ctrlId,
        ctrlName,
        foreground,
        hasFocus,
        hwnd,
        innerHtml,
        innerText,
        IsRPAJavaBridgeEnabled,
        IsJavaWindow,
        items,
        javaState,
        labeledBy,
        name,
        outerHtml,
        outerText,
        parentClass,
        parentId,
        parentName,
        PID,
        position,
        readyState,
        relativeVisibility,
        role,
        selectedItem,
        subsystem,
        tag,
        text,
        TID,
        title,
        url,
        virtualName,
        visibility
    }

    public enum DirectionEnums
    {
        Left,
        Top,
        Right,
        Bottom,
        //Rectangle,
        Translate
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
