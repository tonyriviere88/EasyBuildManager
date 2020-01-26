using OpenSoftware.DgmlTools;
using OpenSoftware.DgmlTools.Builders;
using OpenSoftware.DgmlTools.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace EasyBuildManager.Model
{
    class ProjectGraphInfo
    {
        public Project Project { get; set; }
        public IList<string> Parents = new List<string>();
        public bool IsUnitTest
        {
            get
            {
                return (Project.Name.EndsWith("Test") || Project.Name.EndsWith("TestHelper")) && Project.Name.CompareTo("RshpTest") != 0;
            }
        }

        public ProjectGraphInfo(Project Project)
        {
            this.Project = Project;

            bool isUnitTest = IsUnitTest;
            var solutionDir = Path.GetDirectoryName(Project.Solution.FilePath);
            var projectDir = Path.GetDirectoryName(Project.FilePath);
            Debug.Assert(projectDir.Contains(solutionDir));
            var relativeProjectDir = projectDir.Replace(solutionDir, "").Trim('\\');
            var folders = relativeProjectDir.Split('\\');
            Parents = folders.Take(folders.Count() - (isUnitTest ? 2 : 1)).ToList();
            if (isUnitTest)
            {
                if (Parents.Count > 0)
                    Parents.Add(Parents.Last() + "- Unit Tests");
                else
                    Parents.Add("Unit Tests");
            }
        }
    }

    static class DgmlGenerator
    {
        private static string VcProjectType = "VcProject";
        private static string VsProjectType = "VsProject";
        private static string FolderType = "Folder";
        private static string UnitTestFolderType = "UnitTest";

        public static void GenerateDgml(Solution solution)
        {
            var projects = solution.Projects.Select(p => new ProjectGraphInfo(p)).ToArray();

            try
            {
                var builder = new DgmlBuilder
                {
                    NodeBuilders = new[]
                        {
                            new NodeBuilder<ProjectGraphInfo>(p => Project2Node(p)),
                            new NodesBuilder<ProjectGraphInfo>(p => ProjectFolder2Node(p))
                        },
                    LinkBuilders = new[]
                        {
                            new LinksBuilder<ProjectGraphInfo>(p => Project2Link(p))
                        },
                    StyleBuilders = new[]
                        {
                            new StyleBuilder<Node>(VcProjectStyle, x => x.HasCategory(VcProjectType)),
                            new StyleBuilder<Node>(VsProjectStyle, x => x.HasCategory(VsProjectType)),
                            new StyleBuilder<Node>(FolderStyle, x => x.HasCategory(FolderType)),
                            new StyleBuilder<Node>(UnitTestFolderStyle, x => x.HasCategory(UnitTestFolderType))
                        }
                };

                var graph = builder.Build(projects);
                var filepath = solution.FilePath + ".dgml";

                graph.WriteToFile(filepath);
                solution.NativeSolution.DTE.ItemOperations.OpenFile(filepath);
            }
            catch (Exception)
            { }
        }

        private static Node Project2Node(ProjectGraphInfo projectInfo)
        {
            bool isCs = projectInfo.Project.FullName.EndsWith(".csproj");

            var node = new Node
            {
                Id = projectInfo.Project.Name,
                Label = projectInfo.Project.Name,
                Category = isCs ? VsProjectType : VcProjectType
            };

            return node;
        }

        private static IEnumerable<Node> ProjectFolder2Node(ProjectGraphInfo projectInfo)
        {
            if (projectInfo.Parents != null)
            {
                foreach (var parentName in projectInfo.Parents)
                {
                    bool isUnitTest = parentName.ToLower().Contains("unit tests");
                    yield return new Node
                    {
                        Id = "a:" + parentName,
                        Label = parentName,
                        Group = isUnitTest ? "Collapsed" : "Expanded",
                        Category = isUnitTest ? UnitTestFolderType : FolderType
                    };
                }
            }
        }

        private static IEnumerable<Link> Project2Link(ProjectGraphInfo projectInfo)
        {
            foreach (var refProject in projectInfo.Project.Dependencies)
            {
                yield return new Link
                {
                    Source = projectInfo.Project.Name,
                    Target = refProject.Name
                };
            }

            if (projectInfo.Parents != null)
            {
                string previous = projectInfo.Project.Name;
                foreach (var parentName in projectInfo.Parents.Reverse())
                {
                    var source = "a:" + parentName;
                    var target = previous;
                    previous = source;

                    yield return new Link
                    {
                        Source = source,
                        Target = target,
                        Category = "Contains"
                    };
                }
            }
        }

        private static Style VcProjectStyle(Node node)
        {
            return new Style
            {
                GroupLabel = VcProjectType,
                Setter = new List<Setter>
                {
                    new Setter {Property = "Background", Value = "LightBlue"}
                }
            };
        }

        private static Style VsProjectStyle(Node node)
        {
            return new Style
            {
                GroupLabel = VsProjectType,
                Setter = new List<Setter>
                {
                    new Setter {Property = "Background", Value = "LightGreen"}
                }
            };
        }

        private static Style FolderStyle(Node node)
        {
            return new Style
            {
                GroupLabel = FolderType,
                Setter = new List<Setter>
                {
                    new Setter {Property = "Background", Value = "LightGray"}
                }
            };
        }

        private static Style UnitTestFolderStyle(Node node)
        {
            return new Style
            {
                GroupLabel = UnitTestFolderType,
                Setter = new List<Setter>
                {
                    new Setter {Property = "Background", Value = "LightGoldenrodYellow"}
                }
            };
        }
    }
}
