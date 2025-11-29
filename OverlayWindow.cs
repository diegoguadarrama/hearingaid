using System;
using System.Drawing;
using System.Windows.Forms;

namespace HearingAI
{
    public class OverlayWindow : Form
    {
        private System.Windows.Forms.Timer leftFlashTimer = null!;
        private System.Windows.Forms.Timer rightFlashTimer = null!;
        private Panel leftPanel = null!;
        private Panel rightPanel = null!;
        
        public Color LeftFlashColor { get; set; } = Color.Red;
        public Color RightFlashColor { get; set; } = Color.Red;
        public int FlashDuration { get; set; } = 200;
        public float FlashOpacity { get; set; } = 0.8f; // 80% opacity by default
        public Screen TargetScreen { get; set; } = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        
        private const int FLASH_WIDTH = 50;

        public OverlayWindow()
        {
            InitializeOverlay();
            InitializeTimers();
            CreateFlashPanels();
        }

        private void InitializeOverlay()
        {
            // Make the window borderless and always on top
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Normal; // Changed from Maximized
            
            // Set window bounds to cover the target screen
            this.Bounds = TargetScreen.Bounds;
            
            // Make the window click-through
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            
            // Set extended window style to make it click-through
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);
        }

        private void InitializeTimers()
        {
            leftFlashTimer = new System.Windows.Forms.Timer();
            leftFlashTimer.Tick += (s, e) => HideLeftFlash();
            
            rightFlashTimer = new System.Windows.Forms.Timer();
            rightFlashTimer.Tick += (s, e) => HideRightFlash();
        }

        private void CreateFlashPanels()
        {
            // Left flash panel
            leftPanel = new Panel
            {
                BackColor = ApplyOpacity(LeftFlashColor),
                Visible = false,
                Location = new Point(0, 0),
                Size = new Size(FLASH_WIDTH, TargetScreen.Bounds.Height)
            };
            
            // Right flash panel
            rightPanel = new Panel
            {
                BackColor = ApplyOpacity(RightFlashColor),
                Visible = false,
                Location = new Point(TargetScreen.Bounds.Width - FLASH_WIDTH, 0),
                Size = new Size(FLASH_WIDTH, TargetScreen.Bounds.Height)
            };
            
            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
        }

        private Color ApplyOpacity(Color baseColor)
        {
            int alpha = (int)(255 * FlashOpacity);
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        public void FlashLeft()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(FlashLeft));
                return;
            }

            leftPanel.BackColor = ApplyOpacity(LeftFlashColor);
            leftPanel.Visible = true;
            leftFlashTimer.Interval = FlashDuration;
            leftFlashTimer.Stop();
            leftFlashTimer.Start();
        }

        public void FlashRight()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(FlashRight));
                return;
            }

            rightPanel.BackColor = ApplyOpacity(RightFlashColor);
            rightPanel.Visible = true;
            rightFlashTimer.Interval = FlashDuration;
            rightFlashTimer.Stop();
            rightFlashTimer.Start();
        }

        private void HideLeftFlash()
        {
            leftFlashTimer.Stop();
            leftPanel.Visible = false;
        }

        private void HideRightFlash()
        {
            rightFlashTimer.Stop();
            rightPanel.Visible = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Ensure the window covers the target screen
            this.Bounds = TargetScreen.Bounds;
            
            // Update panel positions in case screen resolution changed
            rightPanel.Location = new Point(TargetScreen.Bounds.Width - FLASH_WIDTH, 0);
            rightPanel.Size = new Size(FLASH_WIDTH, TargetScreen.Bounds.Height);
            leftPanel.Size = new Size(FLASH_WIDTH, TargetScreen.Bounds.Height);
        }

        public void UpdateTargetScreen(Screen screen)
        {
            TargetScreen = screen;
            
            // Update window bounds
            this.Bounds = TargetScreen.Bounds;
            
            // Update panel positions and sizes
            rightPanel.Location = new Point(TargetScreen.Bounds.Width - FLASH_WIDTH, 0);
            rightPanel.Size = new Size(FLASH_WIDTH, TargetScreen.Bounds.Height);
            leftPanel.Size = new Size(FLASH_WIDTH, TargetScreen.Bounds.Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                leftFlashTimer?.Dispose();
                rightFlashTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // Win32 API declarations for making the window click-through
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    }
}
