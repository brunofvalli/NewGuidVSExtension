//------------------------------------------------------------------------------
// <copyright file="NewGuidVSExt.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using EnvDTE80;
using EnvDTE;
using System.Linq;
using System.Text.RegularExpressions;

namespace MBFVSolutions.NewGuidVSExt
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class NewGuidVSExt
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("6117e7c1-7a4f-4239-ad04-d05a0de80a93");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewGuidVSExt"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private NewGuidVSExt(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static NewGuidVSExt Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new NewGuidVSExt(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            List<string> fileList;

            if( FileSelected(dte, out fileList))
            {
                foreach(var file in fileList)
                {
                    string fileContents;
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(file))
                    {
                        fileContents = sr.ReadToEnd();
                    }

                    Regex regex = new Regex(@"[0-9A-Fa-f]{8}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{4}-?[0-9A-Fa-f]{12}");

                    var matches = regex.Matches(fileContents);

                    foreach( var match in matches)
                    {
                        fileContents = fileContents.Replace(match.ToString(), Guid.NewGuid().ToString());
                    }

                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(file, false))
                    {
                        sw.WriteLine(fileContents);
                    }
                }
            }
        }

        public static bool FileSelected(DTE2 dte, out List<string> fileList)
        {
            var items = GetSelectedFiles(dte);

            fileList = items.ToList();

            return ((fileList != null) && (fileList.Count > 0));
        }

        public static IEnumerable<string> GetSelectedFiles(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;

            return from item in items.Cast<UIHierarchyItem>()
                   let pi = item.Object as ProjectItem
                   select pi.FileNames[1];

        }
    }
}
