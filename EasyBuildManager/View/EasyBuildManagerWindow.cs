using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace EasyBuildManager.View
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("f33cec90-248d-4be0-9f2a-a5ab4ae8379d")]
    public class EasyBuildManagerWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EasyBuildManagerWindow"/> class.
        /// </summary>
        public EasyBuildManagerWindow() : base(null)
        {
            this.Caption = "Easy Build Manager";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new EasyBuildManagerWindowControl();
        }
    }
}
