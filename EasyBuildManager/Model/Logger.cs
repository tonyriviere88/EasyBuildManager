using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace EasyBuildManager.Model
{
    static class Logger
    {
        static IVsOutputWindowPane CustomPane;

        public static void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            // Use e.g. Tools -> Create GUID to make a stable, but unique GUID for your pane.
            // Also, in a real project, this should probably be a static constant, and not a local variable
            Guid customGuid = new Guid("CDD5952F-C127-4563-A908-370B471C7209");
            string customTitle = "Easy Build Manager";
            outWindow.CreatePane(ref customGuid, customTitle, 1, 1);

            outWindow.GetPane(ref customGuid, out CustomPane);
        }

        public static void LogInfo(string info)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!info.EndsWith("\n"))
                info += '\n';
            CustomPane.OutputString(info);
        }

        public delegate void FunctionToProfile();
        public static void ProfileFunction(FunctionToProfile function, string name)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            function();
            stopWatch.Stop();
            var elapsed = stopWatch.ElapsedMilliseconds;
            LogInfo($"{name}: {elapsed} ms");
        }
    }
}
