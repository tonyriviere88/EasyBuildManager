using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using OLEMSGBUTTON = Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON;
using OLEMSGDEFBUTTON = Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON;
using OLEMSGICON = Microsoft.VisualStudio.Shell.Interop.OLEMSGICON;

namespace EasyBuildManager.Model
{
    public class IsGreaterThanZero : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((int)value) > 0 ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();//"PresenterConverter.ConvertBack() is not implemented!");
        }
        #endregion
    }

    class EasyBuildManagerModel : PropertyChangedBase
    {
        public ICommand RefreshSolutionCommand { get; }
        public ICommand CleanCommand { get; }
        public ICommand DgmlCommand { get; }
        public ICommand RepairCommand { get; }

        private Solution solution;
        public Solution Solution
        {
            get
            {
                return solution;
            }
            private set
            {
                solution = value;
                OnPropertyChanged();
            }
        }

        private string title = "Title";
        public string Title
        {
            get { return title; }
            private set
            {
                title = value;
                OnPropertyChanged();
            }
        }

        private readonly IServiceProvider package;

        public EasyBuildManagerModel(IServiceProvider package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package;
            EnvDTEWrapper.RegisterOnSolutionOpened(OnSolutionOpened);
            EnvDTEWrapper.RegisterOnSolutionClosed(OnSolutionClosed);
            RefreshSolutionCommand = new RelayCommand(action: () => Reload());
            CleanCommand = new RelayCommand(CleanUnbuiltProjects);
            DgmlCommand = new RelayCommand(GenerateDgml);
            RepairCommand = new RelayCommand(RepairReferences);

            if (EnvDTEWrapper.IsSolutionOpened())
            {
                Reload();
            }
        }

        public void Reload()
        {

            ThreadHelper.ThrowIfNotOnUIThread();
            Logger.ProfileFunction(function: () => _Reload(), name: $"Reload solution {EnvDTEWrapper.GetCurrentSolutionName()}");
        }

        private void _Reload()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution = EnvDTEWrapper.GetCurrentSolution();
        }

        public void ClearData()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution.Dispose();
            Solution = null;
        }

        public bool IsSolutionAvailable()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return Solution != null && Solution.IsReady;
        }

        public void UpdateCheckState()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Solution.UpdateCheckState();
        }

        public void CleanUnbuiltProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (IsSolutionAvailable())
            {
                // Clean command performs on 'ShouldBuild == true' projects, so invert it locally
                foreach (var project in Solution.Projects)
                {
                    project.ActiveConfiguration.ShouldBuild = !project.ActiveConfiguration.ShouldBuild;
                }

                try
                {
                    Solution.NativeSolution.SolutionBuild.Clean(true);
                }
                finally
                {
                    foreach (var project in Solution.Projects)
                    {
                        project.ActiveConfiguration.ShouldBuild = !project.ActiveConfiguration.ShouldBuild;
                    }

                    Solution.NativeSolution.SaveAs(Solution.FilePath);
                }
            }
        }

        public void GenerateDgml()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (IsSolutionAvailable())
                Logger.ProfileFunction(() => DgmlGenerator.GenerateDgml(solution), "GenerateDgml");
        }
        public void RepairReferences()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (IsSolutionAvailable())
            {
                int ret = VsShellUtilities.ShowMessageBox(
                   package,
                   "This will try to repair the project's references based on the library (.lib) it imports.\n" +
                   "This can take time and freeze the IDE for several minutes if the solution has 100+ projects.",
                   "Repair references",
                   OLEMSGICON.OLEMSGICON_WARNING,
                   OLEMSGBUTTON.OLEMSGBUTTON_OK | OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);

                if (ret != 1) // != OK
                    return;

                Logger.ProfileFunction(() => ReferenceRepairer.RepairReferences(solution), "RepairReferences");
            }
        }

        private void OnSolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Reload();
        }

        private void OnSolutionClosed()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ClearData();
        }
    }
}
