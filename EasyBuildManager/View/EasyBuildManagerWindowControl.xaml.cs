namespace EasyBuildManager.View
{
    using EasyBuildManager.Model;
    using System;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for EasyBuildManagerWindowControl.
    /// </summary>
    public partial class EasyBuildManagerWindowControl : UserControl
    {
        private readonly EasyBuildManagerModel model;

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyBuildManagerWindowControl"/> class.
        /// </summary>
        public EasyBuildManagerWindowControl(IServiceProvider serviceProvider)
        {
            this.InitializeComponent();

            model = new EasyBuildManagerModel(serviceProvider);
            this.DataContext = model;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            model.UpdateCheckState();
        }
    }
}