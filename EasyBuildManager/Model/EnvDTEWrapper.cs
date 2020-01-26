using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using VSLangProj;

namespace EasyBuildManager.Model
{
    static class EnvDTEWrapper
    {
        private static EasyBuildManagerPackage package;
        private static EnvDTE.DTE dte;
        private static EnvDTE.SolutionEvents solutionEvents;

        public static void Initialize(EasyBuildManagerPackage package, EnvDTE.DTE dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTEWrapper.package = package;
            EnvDTEWrapper.dte = dte;
            solutionEvents = dte.Events.SolutionEvents;
        }

        public static bool IsSolutionOpened()
        {
            return dte.Solution != null && dte.Solution.IsOpen;
        }

        public static void RegisterOnSolutionOpened(EnvDTE._dispSolutionEvents_OpenedEventHandler iCallback)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            solutionEvents.Opened += iCallback;
        }

        public static void RegisterOnSolutionClosed(EnvDTE._dispSolutionEvents_AfterClosingEventHandler iCallback)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            solutionEvents.AfterClosing += iCallback;
        }

        public static Solution GetCurrentSolution()
        {
            return new Solution(dte.Solution);
        }

        public static string GetCurrentSolutionName()
        {
            return dte.Solution?.FileName;
        }

        public static IList<Project> GetProjects(Solution solution)
        {
            var projects = new List<Project>();
            var projectsPerName = new Dictionary<string, Project>();

            List<EnvDTE.Project> nativeProjects = new List<EnvDTE.Project>();
            foreach (EnvDTE.Project nativeProject in solution.NativeSolution.Projects)
                NavigateProject(nativeProject, nativeProjects);

            // get current solution
            foreach (var project in nativeProjects)
            {
                var p = new Project(project);
                p.Solution = solution;
                projectsPerName.Add(p.Name, p);
                projects.Add(p);
            }

            projects.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
            RetrieveDependencies(projectsPerName);
            return projects;
        }

        public static string GetTargetFilename(Project project)
        {
            var vcproj = project.NativeProject.Object as VCProject;
            var cfgs = vcproj.Configurations as IVCCollection;
            var cfg = cfgs.Item(1) as VCConfiguration2;
            return cfg.PrimaryOutput;
        }

        public static string GetAdditionalDependencies(Project project)
        {
            var vcproj = project.NativeProject.Object as VCProject;
            var cfgs = vcproj.Configurations as IVCCollection;
            var cfg = cfgs.Item(1) as VCConfiguration2;
            var rule = cfg.Rules.Item("Link") as IVCRulePropertyStorage;
            return rule.GetEvaluatedPropertyValue("AdditionalDependencies");
        }

        public static string GetImportLibrary(Project project)
        {
            var vcproj = project.NativeProject.Object as VCProject;
            var cfgs = vcproj.Configurations as IVCCollection;
            var cfg = cfgs.Item(1) as VCConfiguration2;
            var rule = cfg.Rules.Item("Link") as IVCRulePropertyStorage;
            return rule.GetEvaluatedPropertyValue("ImportLibrary");
        }

        private static void RetrieveDependencies(IDictionary<string, Project> allProjects)
        {
            try
            {
                foreach (var curProject in allProjects.Values)
                {
                    if (curProject.NativeProject.Object is VCProject vcProject)
                    {
                        if (!(vcProject.VCReferences is VCReferences refs))
                            return;

                        for (int i = 1; i <= refs.Count; i++)
                        {
                            if (refs.Item(i) is VCReference projRef)
                            {
                                var projRefName = projRef.Name;
                                if (allProjects.TryGetValue(projRefName, out Project refProj))
                                {
                                    curProject.Dependencies.Add(refProj);
                                    refProj.Referecing.Add(curProject);
                                }
                            }
                        }
                    }
                    else if (curProject.NativeProject.Object is VSProject vsProject)
                    {
                        foreach (var curRef in vsProject.References)
                        {
                            if (curRef is VSLangProj.Reference curRefproj)
                            {
                                var projRef = curRefproj.SourceProject;

                                if (projRef != null)
                                {
                                    var projRefName = projRef.Name;
                                    if (allProjects.TryGetValue(projRefName, out Project refProj))
                                    {
                                        curProject.Dependencies.Add(refProj);
                                        refProj.Referecing.Add(curProject);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }

        private static void NavigateProject(EnvDTE.Project nativeProject, List<EnvDTE.Project> nativeProjects)
        {
            if (nativeProject.Kind == EnvDTE80.ProjectKinds.vsProjectKindSolutionFolder)
                NavigateProjectItems(nativeProject.ProjectItems, nativeProjects);
            else if (nativeProject.Object != null)
                nativeProjects.Add(nativeProject);
        }

        private static void NavigateProjectItems(EnvDTE.ProjectItems nativeProjectItems, List<EnvDTE.Project> nativeProjects)
        {
            if (nativeProjectItems == null)
                return;
            foreach (EnvDTE.ProjectItem nativeProjectItem in nativeProjectItems)
            {
                if (nativeProjectItem.SubProject != null)
                    NavigateProject(nativeProjectItem.SubProject, nativeProjects);
            }
        }

        private static void ShowSolutionBuildConfigurations()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            EnvDTE80.Solution2 solution2;
            EnvDTE80.SolutionBuild2 solutionBuild2;
            EnvDTE80.SolutionConfiguration2 activeSolutionConfiguration2;
            EnvDTE.SolutionContexts solutionContexts;
            List<string> usedNames = new List<string>();
            string solutionConfigurationName;
            string solutionPlatformName;

            solution2 = (EnvDTE80.Solution2)dte.Solution;
            solutionBuild2 = (EnvDTE80.SolutionBuild2)solution2.SolutionBuild;

            // Active solution configuration
            activeSolutionConfiguration2 = (EnvDTE80.SolutionConfiguration2)solutionBuild2.ActiveConfiguration;
            sb.AppendLine("Active solution configuration: " + activeSolutionConfiguration2.Name);
            sb.AppendLine("Active solution platform: " + activeSolutionConfiguration2.PlatformName);

            // Solution configuration names
            sb.AppendLine();
            sb.AppendLine("-----------------------------------------------");
            sb.AppendLine("Solution configuration names:");
            sb.AppendLine();
            usedNames.Clear();

            foreach (EnvDTE80.SolutionConfiguration2 solutionConfiguration2 in solutionBuild2.SolutionConfigurations)
            {
                solutionConfigurationName = solutionConfiguration2.Name;

                if (!usedNames.Contains(solutionConfigurationName))
                {
                    usedNames.Add(solutionConfigurationName);
                    sb.AppendLine("   - " + solutionConfigurationName);
                }
            }

            // Solution platform names
            sb.AppendLine();
            sb.AppendLine("-----------------------------------------------");
            sb.AppendLine("Solution platform names:");
            sb.AppendLine();
            usedNames.Clear();

            foreach (EnvDTE80.SolutionConfiguration2 solutionConfiguration2 in solutionBuild2.SolutionConfigurations)
            {
                solutionPlatformName = solutionConfiguration2.PlatformName;

                if (!usedNames.Contains(solutionPlatformName))
                {
                    usedNames.Add(solutionPlatformName);
                    sb.AppendLine("   - " + solutionPlatformName);
                }
            }

            // Solution configurations/platforms
            sb.AppendLine();
            sb.AppendLine("-----------------------------------------------");
            sb.AppendLine("Project contexts for each solution configuration/platform:");

            foreach (EnvDTE80.SolutionConfiguration2 solutionConfiguration2 in solutionBuild2.SolutionConfigurations)
            {
                sb.AppendLine();

                sb.AppendLine("   - Solution configuration: " + solutionConfiguration2.Name +
                   ", solution platform: " + solutionConfiguration2.PlatformName);

                solutionContexts = solutionConfiguration2.SolutionContexts;

                foreach (EnvDTE.SolutionContext solutionContext in solutionContexts)
                {
                    sb.AppendLine();
                    sb.Append("         * Project unique name: " + solutionContext.ProjectName);
                    sb.Append(", project configuration: " + solutionContext.ConfigurationName);
                    sb.Append(", project platform: " + solutionContext.PlatformName);
                    sb.Append(", project should build: " + solutionContext.ShouldBuild.ToString());
                    sb.Append(", project should deploy: " + solutionContext.ShouldDeploy.ToString());
                    sb.AppendLine();
                }
            }

            System.Windows.MessageBox.Show(sb.ToString());
        }
    }
}
