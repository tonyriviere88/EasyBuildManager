using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace EasyBuildManager.Model
{
    [DataContract]
    class CustomConfiguration
    {
        public string Name { get; set; }

        [DataMember]
        public IList<string> Projects { get; set; } = new List<string>();
    }

    [DataContract]
    class CustomConfigurationList
    {
        [DataMember]
        public IList<CustomConfiguration> Configurations { get; set; } = new List<CustomConfiguration>();
    }

    class CustomConfigurationManager
    {
        public CustomConfigurationList UserConfigurations { get; set; } = new CustomConfigurationList();

        private readonly Solution solution;

        public CustomConfigurationManager(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.solution = solution;

            var conf = new CustomConfiguration();

            foreach (Project project in this.solution.Projects.Where(p => p.ShouldBuild))
            {
                conf.Projects.Add(project.FullName);
            }

            UserConfigurations.Configurations.Add(conf);
            SaveToFile(UserConfigurations, GetUserConfigFilename());
        }

        public string GetUserConfigFilename()
        {
            var solutionDir = Path.GetDirectoryName(this.solution.FilePath);
            var solutionName = Path.GetFileNameWithoutExtension(this.solution.FilePath);

            return Path.Combine(solutionDir, solutionName + ".user.slnc");
        }

        public void LoadFromFile(ref CustomConfigurationList configurationsList, string filename)
        {
            if (!File.Exists(filename))
                return;

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(CustomConfigurationList));

            using (var stream = File.OpenRead(filename))
            {
                configurationsList = js.ReadObject(stream) as CustomConfigurationList;
            }
        }

        public void SaveToFile(CustomConfigurationList configurationsList, string filename)
        {
            using (var stream = File.Create(filename))
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(typeof(CustomConfigurationList));
                    serializer.WriteObject(writer, configurationsList);
                    writer.Flush();
                }
            }
        }
    }
}
