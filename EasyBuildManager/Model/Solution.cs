﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyBuildManager.Model
{
    class ProjectConfiguration
    {
        public string ProjectName
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return solutionContext.ProjectName;
            }
        }

        public bool ShouldBuild
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return solutionContext.ShouldBuild;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                solutionContext.ShouldBuild = value;
            }
        }

        private readonly EnvDTE.SolutionContext solutionContext;

        public ProjectConfiguration(EnvDTE.SolutionContext solutionContext)
        {
            this.solutionContext = solutionContext;
        }
    }

    class SolutionConfiguration
    {
        private readonly IDictionary<string, ProjectConfiguration> configurationPerProject;

        public SolutionConfiguration(EnvDTE.SolutionConfiguration solutionConfiguration)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            configurationPerProject = new Dictionary<string, ProjectConfiguration>();
            var solutionContexts = solutionConfiguration.SolutionContexts;

            foreach (EnvDTE.SolutionContext context in solutionContexts)
            {
                var projectConfig = new ProjectConfiguration(context);
                configurationPerProject.Add(projectConfig.ProjectName, projectConfig);
            }
        }

        public ProjectConfiguration GetProjectConfiguration(Project project)
        {
            return configurationPerProject[project.FullName];
        }
    }

    class Project : PropertyChangedBase
    {
        public EnvDTE.Project NativeProject
        {
            get; private set;
        }
        public string Name
        {
            get; private set;
        }
        public string FullName
        {
            get; private set;
        }
        public string FilePath
        {
            get; private set;
        }
        public HashSet<Project> Dependencies { get; private set; } = new HashSet<Project>();
        public HashSet<Project> Referecing { get; private set; } = new HashSet<Project>();
        public Solution Solution
        {
            get; set;
        }
        public bool ShouldBuild
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return ActiveConfiguration.ShouldBuild;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (ActiveConfiguration.ShouldBuild == value)
                    return;

                ActiveConfiguration.ShouldBuild = value;
                UpdateDependencies();
                OnPropertyChanged();
                OnPropertyChanged(nameof(NbReferencingBuilt));
            }
        }

        public int NbReferencingBuilt
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Referecing.Where(p => p.ShouldBuild).Count();
            }
        }

        public ProjectConfiguration ActiveConfiguration
        {
            get; private set;
        }

        public Project(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.NativeProject = project;
            this.Name = project.Name;
            this.FullName = project.UniqueName;
            this.FilePath = project.FullName;
        }

        public void Clean()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (NativeProject.Object is VCProject vcProject)
            {
                vcProject.ActiveConfiguration.Clean();
                vcProject.ActiveConfiguration.WaitForBuild();
            }
        }

        public void UpdateDependencies()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (ShouldBuild)
            {
                foreach (var proj in Dependencies)
                {
                    proj.ShouldBuild = true;
                    proj.OnPropertyChanged(nameof(NbReferencingBuilt));
                }
            }
            else
            {
                foreach (var proj in Referecing)
                {
                    proj.ShouldBuild = false;
                }

                foreach (var proj in Dependencies)
                {
                    proj.OnPropertyChanged(nameof(NbReferencingBuilt));
                }
            }
        }

        public void UpdateCurrentConfig(SolutionConfiguration activeSolutionConfiguration)
        {
            ActiveConfiguration = activeSolutionConfiguration.GetProjectConfiguration(this);
            OnPropertyChanged(nameof(ShouldBuild));
        }
    }

    class Solution : PropertyChangedBase, IDisposable, IVsUpdateSolutionEvents3
    {
        public EnvDTE.Solution NativeSolution
        {
            get; private set;
        }
        public string Name
        {
            get; set;
        }
        public string FilePath
        {
            get; set;
        }
        public IList<Project> Projects
        {
            get; set;
        }
        public SolutionConfiguration ActiveSolutionConfiguration
        {
            get; private set;
        }
        public bool IsReady
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return NativeSolution.IsOpen && NativeSolution.SolutionBuild.BuildState != EnvDTE.vsBuildState.vsBuildStateInProgress;
            }
        }
        public bool? ShouldBuildAll
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (Projects == null)
                    return false;
                bool hasChecked = false;
                bool hasUnchecked = false;

                foreach (var project in Projects)
                {
                    bool shouldBuild = project.ShouldBuild;

                    if (shouldBuild)
                    {
                        if (hasUnchecked)
                            return null;

                        hasChecked = true;
                    }
                    else
                    {
                        if (hasChecked)
                            return null;

                        hasUnchecked = true;
                    }
                }

                return hasChecked;
            }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (value == null || Projects == null)
                    return;

                foreach (var project in Projects)
                    project.ShouldBuild = value.Value;

                OnPropertyChanged();
            }
        }

        private readonly EnvDTE.SolutionEvents solutionEvents;
        private readonly IVsSolutionBuildManager3 sbm;
        private readonly uint updateSolutionEventsCookie;

        public Solution(EnvDTE.Solution nativeSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.NativeSolution = nativeSolution;
            if (!IsReady)
                return;

            this.solutionEvents = nativeSolution.DTE.Events.SolutionEvents;
            this.FilePath = nativeSolution.FullName;

            ReloadProjects();

            this.solutionEvents.ProjectAdded += p => ReloadProjects();
            this.solutionEvents.ProjectRemoved += p => ReloadProjects();
            this.solutionEvents.ProjectRenamed += (p, n) => ReloadProjects();

            sbm = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager3;
            if (sbm != null)
                sbm.AdviseUpdateSolutionEvents3(this, out updateSolutionEventsCookie);
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sbm != null)
                sbm.UnadviseUpdateSolutionEvents3(updateSolutionEventsCookie);
        }

        public void ReloadProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Projects = EnvDTEWrapper.GetProjects(this);
            UpdateCurrentConfig();
        }

        public void UpdateCheckState()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OnPropertyChanged(nameof(ShouldBuildAll));
            NativeSolution.Saved = false;
        }

        public void UpdateCurrentConfig()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ActiveSolutionConfiguration = new SolutionConfiguration(NativeSolution.SolutionBuild.ActiveConfiguration);

            foreach (var project in Projects)
            {
                project.UpdateCurrentConfig(ActiveSolutionConfiguration);
            }
        }

        public int OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            UpdateCurrentConfig();
            return VSConstants.S_OK;
        }
    }

}
