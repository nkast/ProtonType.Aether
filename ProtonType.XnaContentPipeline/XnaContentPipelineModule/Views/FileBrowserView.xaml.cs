#region License
//   Copyright 2021 Kastellanos Nikolaos
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
#endregion

using System;
using System.Windows.Controls;
using nkast.ProtonType.Framework.Commands;
using nkast.ProtonType.ViewModels;
using tainicom.TreeViewEx;
using nkast.ProtonType.XnaContentPipeline.Commands;
using nkast.ProtonType.XnaContentPipeline.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.Views
{
    /// <summary>
    /// 
    /// </summary>
    public partial class FileBrowserView : UserControl
    {
        public FileBrowserView()
        {
            InitializeComponent();
            this.Loaded += new System.Windows.RoutedEventHandler(FileBrowserView_Loaded);
            propertyGrid.PropertyValueChanged += propertyGrid_PropertyValueChanged;
            treeView.OnSelecting += new EventHandler<SelectionChangedCancelEventArgs>(listView_OnSelecting);
        }

        void FileBrowserView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {    
            var viewModel = (FileBrowserEx)DataContext;
            viewModel.SelectionChanged += new EventHandler<EventArgs>(viewModel_SelectionChanged);
        }

        void propertyGrid_PropertyValueChanged(object sender, tainicom.WpfPropertyGrid.PropertyValueChangedEventArgs e)
        {
            bool refreshPropertyGrid = false;

            if (e.Property.Name == "BuildAction")
            {
                //TODO: cancel change and re-apply through a Command.

                // force a refresh of Properties.
                refreshPropertyGrid = true;
            }

            if (e.Property.Name == "Importer")
            {
                //TODO: cancel change and re-apply through a Command.

                //TODO: Validate that our processor can accept input content of the type output by the new importer.
                //      If it cannot, set the default processor through a Command.
                

                // force a refresh of Properties.
                refreshPropertyGrid = true;
            }
            
            if (e.Property.Name == "Processor")
            {
                //TODO: cancel change and re-apply through a Command.

                // force a refresh of Properties.
                refreshPropertyGrid = true;
            }

            // force a refresh of Properties.
            if (refreshPropertyGrid)
            {
                var selectedObjects = propertyGrid.SelectedObjects;
                propertyGrid.SelectedObjects = null;
                propertyGrid.SelectedObjects = selectedObjects;
            }
            
            return;
        }
        
        void viewModel_SelectionChanged(object sender, EventArgs e)
        {
            var viewModel = (FileBrowserEx)DataContext;

            if (viewModel.SelectedItems.Count == 0)
            {
                propertyGrid.SelectedObject = null;
                return;
            }

            if (viewModel.SelectedItems.Count > 1)
            {
                propertyGrid.SelectedObject = null;
                return;
            }

            if (viewModel.SelectedItems.Count == 1)
            {
                BrowserItemEx browserItemEx = viewModel.SelectedItems[0] as BrowserItemEx;
                if (browserItemEx == null) return;

                propertyGrid.SelectedObject = browserItemEx.ProjectItemVM;
            }

            return;
        }

        void listView_OnSelecting(object sender, SelectionChangedCancelEventArgs e)
        {
            var treeViewEx = (tainicom.TreeViewEx.TreeViewEx)sender;
            FileBrowserEx viewModel = (FileBrowserEx)DataContext;
            ContentPipelineViewModel model = viewModel.ContentPipelineViewModel;

            ICommandController controller = model.Module.Site.Controller;
            e.Cancel = true;

            ICommandQueue BatchCmd = controller.CreateCommandQueue();
            //List<CommandBase> cmdList = new List<CommandBase>();
            foreach (object itemToUnselect in e.ItemsToUnSelect)
            {
                if (!treeViewEx.SelectedItems.Contains(itemToUnselect)) continue;
				BatchCmd.Enqueue(new DeselectBrowserItemCmd(viewModel, (BrowserItem)itemToUnselect));
				//cmdList.Add(new DeselectBrowserItemCmd(viewModel, (BrowserItem)itemToUnselect));
            }
            foreach (object itemToSelect in e.ItemsToSelect)
            {
                if (treeViewEx.SelectedItems.Contains(itemToSelect)) continue;
                BatchCmd.Enqueue(new SelectBrowserItemCmd(viewModel, (BrowserItem)itemToSelect));
                //cmdList.Add(new SelectBrowserItemCmd(viewModel, (BrowserItem)itemToSelect));
            }
            controller.EnqueueAndExecute(BatchCmd);
            //if (cmdList.Count > 0) controller.AddAndExecute(new CommandGroupCmd(null, cmdList.ToArray()));
            return;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
