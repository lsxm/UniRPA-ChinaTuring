﻿using System.Activities.Presentation.PropertyEditing;
using System.Windows;

namespace MouseActivity
{
    public class KeyModifiersEditor : PropertyValueEditor
    {
        public KeyModifiersEditor()
        {
            this.InlineEditorTemplate = PropertyEditorResources.GetResources()["KeyModifiersEditor"] as DataTemplate;
        }
    }
}