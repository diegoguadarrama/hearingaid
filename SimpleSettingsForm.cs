using System;
using System.Drawing;
using System.Windows.Forms;

namespace HearingAI
{
    public class SimpleSettingsForm : Form
    {
        public SimpleSettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "HearingAI - Simple Settings Test";
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Information;
            
            // Add a simple label
            var label = new Label
            {
                Text = "HearingAI Settings Window - Test Mode",
                Size = new Size(400, 50),
                Location = new Point(200, 250),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft Sans Serif", 12, FontStyle.Bold)
            };
            
            this.Controls.Add(label);
            
            // Ensure the form is visible on startup
            this.WindowState = FormWindowState.Normal;
            this.Show();
        }
    }
}
