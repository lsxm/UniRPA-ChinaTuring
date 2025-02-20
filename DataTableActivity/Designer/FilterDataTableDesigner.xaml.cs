﻿using DataTableActivity.Dialog;

namespace DataTableActivity
{
    public partial class FilterDataTableDesigner
    {
        public FilterDataTableDesigner()
        {
            InitializeComponent();
        }

        private void DataTableBuild(object sender, System.Windows.RoutedEventArgs e)
        {
            FilterDataTableWizard filterDialog = new FilterDataTableWizard(this.ModelItem);
            filterDialog.ShowOkCancel();
        }
    }
}