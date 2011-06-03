using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;


namespace ASCOM.$safeprojectname$
{
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();
        }

        private void CmdOkClick(object sender, EventArgs e)
        {
            Dispose();
        }

        private void CmdCancelClick(object sender, EventArgs e)
        {
            Dispose();
        }

        private static void BrowseToAscom(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://ascom-standards.org/");
            }
            catch (Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }
    }
}