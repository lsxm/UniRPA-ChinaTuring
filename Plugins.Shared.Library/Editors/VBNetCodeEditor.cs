﻿using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.PropertyEditing;
using System.Windows;

namespace Plugins.Shared.Library.Editors
{
    public class VBNetCodeEditor : DialogPropertyValueEditor
    {
        private string Title = "";//标题
        private string ContentTitle = "";//内容标题
        private VBNetCodeEditorDialog dlg;

        public VBNetCodeEditor(string title = null, string contentTitle = null)
        {
            this.Title = title;
            this.ContentTitle = contentTitle;

            base.InlineEditorTemplate = (DataTemplate)new EditorTemplates()["VBNetCodeEditorTemplate"];
        }

        public override void ShowDialog(PropertyValue propertyValue, IInputElement commandSource)
        {
            try
            {
                ModelItem ownerActivity = new ModelPropertyEntryToOwnerActivityConverter().Convert(propertyValue.ParentProperty, typeof(ModelItem), false, null) as ModelItem;
                ShowDialog(ownerActivity);
            }
            catch
            {
            }
        }

        public void ShowDialog(ModelItem ownerActivity)
        {
            using (ModelEditingScope modelEditingScope = ownerActivity.BeginEdit())
            {
                if (dlg == null) {
                    dlg = new VBNetCodeEditorDialog();
                }

                dlg.Init(ownerActivity);
                //var dlg = new VBNetCodeEditorDialog(ownerActivity);

                if (!string.IsNullOrEmpty(Title))
                {
                    dlg.Title = Title;
                }

                if (!string.IsNullOrEmpty(ContentTitle))
                {
                    dlg.lblContentTitle.Content = ContentTitle;
                }

                if (dlg.ShowOkCancel())
                {
                    modelEditingScope.Complete();
                }
                else
                {
                    modelEditingScope.Revert();
                }
            }
        }
    }
}
