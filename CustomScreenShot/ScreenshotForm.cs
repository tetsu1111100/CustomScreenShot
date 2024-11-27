using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CustomScreenShot
{
    public partial class ScreenshotForm : Form
    {
        private bool isSelecting = false;
        private Point startPoint, endPoint;
        private Rectangle selectionRectangle;
        private Bitmap screenshot;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public ScreenshotForm()
        {
            InitializeComponent();
            SetProcessDPIAware(); // 啟用高 DPI 支援，使其支援多螢幕與不同解析度的情況

            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;
            this.TopMost = true;
            this.Cursor = Cursors.Cross;
            this.BackColor = Color.Black;
            this.Opacity = 0.5;

            this.Load += ScreenshotForm_Load;
            this.KeyDown += ScreenshotForm_KeyDown;
            this.MouseDown += ScreenshotForm_MouseDown;
            this.MouseMove += ScreenshotForm_MouseMove;
            this.MouseUp += ScreenshotForm_MouseUp;

            // 設定覆蓋所有螢幕
            Rectangle allScreensBounds = GetAllScreensBounds();
            this.Bounds = allScreensBounds;
        }

        private void ScreenshotForm_Load(object sender, EventArgs e)
        {
            // 擷取所有螢幕畫面
            Rectangle bounds = GetAllScreensBounds();
            screenshot = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
        }

        private Rectangle GetAllScreensBounds()
        {
            // 合併所有螢幕的邊界
            return Screen.AllScreens.Aggregate(Rectangle.Empty, (current, screen) => Rectangle.Union(current, screen.Bounds));
        }

        private void ScreenshotForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void ScreenshotForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
            }
        }

        private void ScreenshotForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location;
                selectionRectangle = GetRectangleFromPoints(startPoint, endPoint);
                Invalidate(); // 重繪框選區域
            }
        }

        private void ScreenshotForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                endPoint = e.Location;
                selectionRectangle = GetRectangleFromPoints(startPoint, endPoint);

                if (selectionRectangle.Width > 0 && selectionRectangle.Height > 0)
                {
                    // 擷取框選區域
                    Bitmap capturedImage = new Bitmap(selectionRectangle.Width, selectionRectangle.Height);
                    using (Graphics g = Graphics.FromImage(capturedImage))
                    {
                        g.DrawImage(screenshot, new Rectangle(0, 0, capturedImage.Width, capturedImage.Height),
                            selectionRectangle, GraphicsUnit.Pixel);
                    }

                    // 儲存至剪貼簿
                    Clipboard.SetImage(capturedImage);

                    //透過小畫家開啟
                    OpenPaintAndPaste();
                }

                Close();
            }
        }

        private Rectangle GetRectangleFromPoints(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, width, height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (isSelecting)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(128, Color.White)))
                {
                    e.Graphics.FillRectangle(brush, selectionRectangle);
                }

                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, selectionRectangle);
                }
            }
        }

        // 啟動小畫家並將剪貼簿的內容粘貼到小畫家
        private void OpenPaintAndPaste()
        {
            try
            {
                // 啟動小畫家，使用 mspaint 開啟
                Process.Start("mspaint"); //小畫家

                //Process.Start("mspaint3d");
                //Process.Start("ms-paint");
                //Process.Start("explorer", "ms-paint3d:");
                //Process.Start("explorer", "shell:::{D2E4A29D-61E3-4B65-BF03-9FCA9A69AA9A}");
                //Process.Start("ms-paint3d:");
                //Process.Start("explorer.exe", "shell:::{D2E4A29D-61E3-4B65-BF03-9FCA9A69AA9A}");

                // 等待一段時間讓小畫家開啟
                System.Threading.Thread.Sleep(2000); // 2秒鐘的延遲時間，視系統速度而定

                // 模擬 Ctrl+V 以粘貼剪貼簿中的內容
                SendKeys.Send("^v");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());                
            }
            
        }
    }
}
