using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenCaptureApp
{
    public class RegionSelectForm : Form
    {
        private bool _isSelecting = false;
        private Point _startPoint;
        private Point _currentPoint;
        public Rectangle SelectedRectangle { get; private set; }

        public RegionSelectForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            Opacity = 0.45;
            KeyPreview = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isSelecting = true;
                _startPoint = e.Location;
                _currentPoint = e.Location;
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Klik kanan membatalkan
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isSelecting)
            {
                _currentPoint = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isSelecting && e.Button == MouseButtons.Left)
            {
                _isSelecting = false;
                var rect = GetNormalizedRectangle(_startPoint, e.Location);
                if (rect.Width > 10 && rect.Height > 10)
                {
                    // Koordinat relatif terhadap VirtualScreen dikonversi ke koordinat layar global
                    SelectedRectangle = new Rectangle(
                        Bounds.X + rect.X,
                        Bounds.Y + rect.Y,
                        rect.Width,
                        rect.Height
                    );
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    // Terlalu kecil, anggap klik tanpa sengaja
                    Invalidate();
                }
            }
        }

        private static Rectangle GetNormalizedRectangle(Point p1, Point p2)
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
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Gambar teks instruksi di bagian atas tengah layar utama
            var primaryScreen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            int promptX = primaryScreen.X - Bounds.X + (primaryScreen.Width / 2) - 260;
            int promptY = primaryScreen.Y - Bounds.Y + 30;

            using (var promptBrush = new SolidBrush(Color.FromArgb(220, 30, 30, 30)))
            using (var promptPen = new Pen(Color.FromArgb(0, 150, 255), 2))
            using (var textBrush = new SolidBrush(Color.White))
            using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            {
                var bannerRect = new Rectangle(promptX, promptY, 520, 42);
                g.FillRectangle(promptBrush, bannerRect);
                g.DrawRectangle(promptPen, bannerRect);

                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("Klik & Drag area layar yang diinginkan | ESC untuk Batal", font, textBrush, bannerRect, stringFormat);
            }

            if (_isSelecting)
            {
                var rect = GetNormalizedRectangle(_startPoint, _currentPoint);

                // Kotak seleksi dengan border terang
                using var fillBrush = new SolidBrush(Color.FromArgb(60, 0, 180, 255));
                using var borderPen = new Pen(Color.FromArgb(0, 220, 255), 2)
                {
                    DashStyle = DashStyle.Dash
                };

                g.FillRectangle(fillBrush, rect);
                g.DrawRectangle(borderPen, rect);

                // Tampilkan info ukuran di samping kursor
                string infoText = $"{rect.Width} x {rect.Height} px";
                using var infoFont = new Font("Segoe UI", 9, FontStyle.Bold);
                using var infoBg = new SolidBrush(Color.FromArgb(220, 0, 0, 0));
                using var infoTextBrush = new SolidBrush(Color.Yellow);

                var textSize = g.MeasureString(infoText, infoFont);
                int infoX = rect.Right + 10;
                int infoY = rect.Bottom + 10;

                // Pastikan info text tidak keluar layar
                if (infoX + textSize.Width > ClientRectangle.Width)
                    infoX = rect.Left - (int)textSize.Width - 10;
                if (infoY + textSize.Height > ClientRectangle.Height)
                    infoY = rect.Top - (int)textSize.Height - 10;

                var infoRect = new RectangleF(infoX, infoY, textSize.Width + 8, textSize.Height + 4);
                g.FillRectangle(infoBg, infoRect);
                g.DrawString(infoText, infoFont, infoTextBrush, infoX + 4, infoY + 2);
            }
        }
    }
}
