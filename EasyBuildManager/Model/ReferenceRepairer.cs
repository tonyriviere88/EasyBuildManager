using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBuildManager.Model
{
    static class ReferenceRepairer
    {
        class ProjectInfo
        {
            public Project Project { get; set; }
            public VCProject VCProject { get; set; }
            public IEnumerable<string> LibImported { get; set; }
            public string LibGenerated { get; set; }
            public IList<ProjectInfo> MissingDependencies { get; set; } = new List<ProjectInfo>();
        };

        public static void RepairReferences(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projectInfos = solution.Projects
                .Where(p => p.NativeProject.Object is VCProject)
                .Select(p =>
                {
                    var vcproj = p.NativeProject.Object as VCProject;
                    var libImported = EnvDTEWrapper.GetAdditionalDependencies(p);
                    var libGenerated = EnvDTEWrapper.GetImportLibrary(p);

                    return new ProjectInfo
                    {
                        Project = p,
                        VCProject = vcproj,
                        LibImported = new HashSet<string>(libImported.Split(';').Select(l => Path.GetFileName(l))),
                        LibGenerated = Path.GetFileName(libGenerated)
                    };
                })
                .ToArray();

            var projectInfoPerOutputLib = projectInfos
                .Where(p => p.LibGenerated.Length > 0)
                .ToDictionary(p => p.LibGenerated, p => p);

            Parallel.ForEach(projectInfos, p =>
            {
                foreach (var curLib in p.LibImported)
                {
                    if (!projectInfoPerOutputLib.TryGetValue(curLib, out ProjectInfo dependencyProjectInfo))
                        continue; // not lib coming from a project in the solution

                    if (p.Project.Dependencies.Contains(dependencyProjectInfo.Project))
                        continue; // project already added as reference

                    p.MissingDependencies.Add(dependencyProjectInfo);
                }
            });

            foreach (var p in projectInfos)
            {
                var refs = p.VCProject.VCReferences as VCReferences;

                foreach (var dependencyProjectInfo in p.MissingDependencies)
                {
                    try
                    {
                        var refAddedObj = refs.AddProjectReference((dependencyProjectInfo.Project).NativeProject);
                        if (refAddedObj is VCReference vcRefAdded)
                        {
                            vcRefAdded.CopyLocal = false;
                            vcRefAdded.UseInBuild = false;
                        }
                        p.Project.Dependencies.Add(dependencyProjectInfo.Project);
                        dependencyProjectInfo.Project.Referecing.Add(p.Project);
                    }
                    catch (Exception)
                    { }
                }

                //foreach (var curRefProj in p.Project.Dependencies)
                //{
                //    if (!projectInfoPerProject.TryGetValue(curRefProj, out ProjectInfo curRefProjInfo))
                //        continue;
                //
                //    if (!p.LibImported.Contains(curRefProjInfo.LibGenerated))
                //    {
                //        try
                //        {
                //            refs.RemoveReference((curRefProjInfo.Project as VisualStudioProject).NativeProject.re);
                //        }
                //        catch (Exception)
                //        { }
                //        continue;
                //    }
                //}
            };
        }
    }
}
