using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FloorSimulation
{
    internal class MetaInfo : SmoothPanel
    {
        MainDisplay Display;
        Size PanelSize;
        Floor floor;
        Rectangle progressBarScreenBounds;
        Pen BlackPen;
        Font StandardFont = new Font("Segoe UI", 12.0f, FontStyle.Bold);

        private ProgressBar TrolleysLeftBar;

        public MetaInfo(Point PanelLocation, MainDisplay di, Floor floor_)
        {
            Display = di;
            floor = floor_;
            BlackPen = new Pen(Color.Black, 2);
            
            this.Location = PanelLocation;
            this.Size = new Size((int)(2000 * Floor.ScaleFactor), (int)(2000 * Floor.ScaleFactor));
            this.BackColor = Color.White;

            InitProgressbar();
            this.Controls.Add(TrolleysLeftBar);
            this.Paint += PaintInfo;
            Invalidate();
        }

        private void InitProgressbar()
        {
            Label TLeftBarLabel = new Label();
            TLeftBarLabel.Text = "Trolleys Left";
            TLeftBarLabel.Location = new Point(5, 5);
            TLeftBarLabel.Font = StandardFont;
            Controls.Add(TLeftBarLabel);


            TrolleysLeftBar = new ProgressBar();
            TrolleysLeftBar.Location = new Point(5, 40);
            TrolleysLeftBar.Size = new Size(this.Size.Width / 3, this.Size.Height / 20);
            TrolleysLeftBar.Style = ProgressBarStyle.Continuous;
            TrolleysLeftBar.Maximum = floor.TotalUndistributedTrolleys();

            progressBarScreenBounds = new Rectangle(TrolleysLeftBar.Location, TrolleysLeftBar.Size);
            progressBarScreenBounds.Inflate(1, 1);
        }

        public void PaintInfo(object obj, PaintEventArgs pea)
        {
            Graphics g = pea.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawRectangle(BlackPen, progressBarScreenBounds);
            TrolleysLeftBar.Value = floor.TotalUndistributedTrolleys();
        }

        public void Inval()
        {
            Invalidate();
        }
    }
}
