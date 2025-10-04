// Batch Complete Plugin for SDL Trados Studio
// Allows users to mark multiple projects as completed in a single operation
// 
// License: MIT
// Author: Thornumak
// Version: 1.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Presentation.DefaultLocations;

namespace BatchComplete
{
    /// <summary>
    /// Adds a context menu action to mark multiple selected projects as completed.
    /// This addresses the limitation in Trados Studio where projects can only be 
    /// completed one at a time through the standard UI.
    /// </summary>
    [Action("BatchCompleteAction",
            Name = "Mark Selected as Completed",
            Description = "Marks multiple selected projects as completed",
            Icon = "completed")]
    [ActionLayout(typeof(TranslationStudioDefaultContextMenus.ProjectsContextMenuLocation), 10, DisplayType.Large)]
    public class BatchCompleteAction : AbstractAction
    {
        private ProjectsController _projectsController;

        protected override void Execute()
        {
            try
            {
                _projectsController = SdlTradosStudio.Application.GetController<ProjectsController>();

                if (_projectsController == null)

                    if (_projectsController == null)
                    {
                        MessageBox.Show("Unable to access Projects Controller.",
                                        "Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                        return;
                    }

                var selectedProjects = _projectsController.SelectedProjects;

                if (selectedProjects == null || !selectedProjects.Any())
                {
                    MessageBox.Show("No projects selected. Please select one or more projects to complete.",
                                    "No Selection",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                    return;
                }

                // Filter out already completed projects
                var incompleteProjects = selectedProjects
                    .Where(p => p.GetProjectInfo().Status != Sdl.ProjectAutomation.Core.ProjectStatus.Completed)
                    .ToList();

                if (!incompleteProjects.Any())
                {
                    MessageBox.Show("All selected projects are already completed.",
                                    "Already Completed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                    return;
                }

                int projectCount = incompleteProjects.Count();

                // Show confirmation before proceeding
                string message = projectCount == 1
                    ? "Are you sure you want to mark 1 project as completed?"
                    : $"Are you sure you want to mark {projectCount} projects as completed?";

                DialogResult result = MessageBox.Show(message,
                                                      "Confirm Batch Complete",
                                                      MessageBoxButtons.YesNo,
                                                      MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                // Process each project and track results
                int successCount = 0;
                int failCount = 0;
                List<string> failedProjects = new List<string>();

                foreach (var project in incompleteProjects)
                {
                    try
                    {
                        // Use the Complete() API method to mark project as completed
                        project.Complete();
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        failedProjects.Add($"{project.GetProjectInfo().Name}: {ex.Message}");
                    }
                }

                _projectsController.RefreshProjects();

                // Display results summary
                string resultMessage = $"Successfully completed {successCount} project(s).";

                if (failCount > 0)
                {
                    resultMessage += $"\n\nFailed to complete {failCount} project(s):\n";
                    resultMessage += string.Join("\n", failedProjects.Take(5));

                    if (failedProjects.Count > 5)
                    {
                        resultMessage += $"\n... and {failedProjects.Count - 5} more.";
                    }

                    MessageBox.Show(resultMessage,
                                    "Batch Complete Results",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(resultMessage,
                                    "Success",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Application initializer for plugin registration.
    /// Required by Trados Studio plugin framework but no initialization logic needed for this plugin.
    /// </summary>
    [ApplicationInitializer]
    public class BatchCompleteInitializer : IApplicationInitializer
    {
        public void Execute()
        {
            // No initialization required
        }
    }
}