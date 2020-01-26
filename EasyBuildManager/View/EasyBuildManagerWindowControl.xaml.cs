namespace EasyBuildManager.View
{
    using EasyBuildManager.Model;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for EasyBuildManagerWindowControl.
    /// </summary>
    public partial class EasyBuildManagerWindowControl : UserControl
    {
        private readonly EasyBuildManagerModel model = new EasyBuildManagerModel(EasyBuildManagerPackage.Instance);

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyBuildManagerWindowControl"/> class.
        /// </summary>
        public EasyBuildManagerWindowControl()
        {
            this.InitializeComponent();

            this.DataContext = model;
        }
    }
}