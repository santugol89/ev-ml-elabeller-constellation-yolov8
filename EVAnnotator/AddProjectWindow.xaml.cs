using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GenieSupervisor
{
    /// <summary>
    /// Interaction logic for AddProjectWindow.xaml
    /// </summary>
    public partial class AddProjectWindow : Telerik.Windows.Controls.RadWindow
    {
        ProjectConfigWindow app;
        public string strProjectName = null;

        public AddProjectWindow(ProjectConfigWindow app)
        {
            InitializeComponent();
            this.app = app;
            this.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(HandleEsc);
        }

        private void HandleEsc(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void AddProject_Click(object sender, MouseButtonEventArgs e)
        {
            if (txtProjectName.Text.Trim() == "")
                return;

            var listProjects = app.cmbProject.Items.Cast<string>().Select(s => s.ToLower());
            if(listProjects.Contains(txtProjectName.Text.Trim().ToLower()))
            {
                System.Windows.Forms.MessageBox.Show("Project name entered already exist.", "Duplicate Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            strProjectName = txtProjectName.Text.Trim();
            app.cmbProject.Items.Add(strProjectName);
            this.Close();
        }

        private void txtProjectName_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddProject_Click(null, null);
        }
    }
}
