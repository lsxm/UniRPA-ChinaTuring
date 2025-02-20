﻿using DataTableActivity.Dialog;

namespace DataTableActivity
{
    public partial class JoinDataTablesDesigner
    {
        public JoinDataTablesDesigner()
        {
            InitializeComponent();
        }

        private void DataTableBuild(object sender, System.Windows.RoutedEventArgs e)
        {
            JoinDataTablesWizard joinWizard = new JoinDataTablesWizard(this.ModelItem);
            joinWizard.ShowOkCancel();
        }
    }
}