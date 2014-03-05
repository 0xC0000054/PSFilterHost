namespace HostTest
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                if (historyStack != null)
                {
                    historyStack.Dispose();
                    historyStack = null;
                }

                if (!string.IsNullOrEmpty(srcImageTempFileName))
                {
                    System.IO.File.Delete(srcImageTempFileName);
                }

            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.loadFiltersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshFiltersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filtersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutPluginsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.canvas = new HostTest.Canvas();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.pointerSelectBtn = new System.Windows.Forms.ToolStripButton();
            this.rectangleSelectBtn = new System.Windows.Forms.ToolStripButton();
            this.elipseSelectBtn = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.zoomInBtn = new System.Windows.Forms.ToolStripButton();
            this.zoomOutBtn = new System.Windows.Forms.ToolStripButton();
            this.zoomToWindowBtn = new System.Windows.Forms.ToolStripButton();
            this.zoomToActualSizeBtn = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.primaryColorBtn = new HostTest.ColorToolStripMenuItem();
            this.secondaryColorBtn = new HostTest.ColorToolStripMenuItem();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.filtersToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1034, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.toolStripSeparator1,
            this.loadFiltersMenuItem,
            this.refreshFiltersToolStripMenuItem,
            this.toolStripSeparator4,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Enabled = false;
            this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.saveToolStripMenuItem.Text = "&Save...";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(206, 6);
            // 
            // loadFiltersMenuItem
            // 
            this.loadFiltersMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("loadFiltersMenuItem.Image")));
            this.loadFiltersMenuItem.Name = "loadFiltersMenuItem";
            this.loadFiltersMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.O)));
            this.loadFiltersMenuItem.Size = new System.Drawing.Size(209, 22);
            this.loadFiltersMenuItem.Text = "&Load Filters...";
            this.loadFiltersMenuItem.Click += new System.EventHandler(this.loadFiltersMenuItem_Click);
            // 
            // refreshFiltersToolStripMenuItem
            // 
            this.refreshFiltersToolStripMenuItem.Enabled = false;
            this.refreshFiltersToolStripMenuItem.Image = global::HostTest.Properties.Resources.refresh;
            this.refreshFiltersToolStripMenuItem.Name = "refreshFiltersToolStripMenuItem";
            this.refreshFiltersToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.refreshFiltersToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.refreshFiltersToolStripMenuItem.Text = "&Refresh Filters";
            this.refreshFiltersToolStripMenuItem.Click += new System.EventHandler(this.refreshFiltersToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(206, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("undoToolStripMenuItem.Image")));
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.undoToolStripMenuItem.Text = "&Undo";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // redoToolStripMenuItem
            // 
            this.redoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripMenuItem.Image")));
            this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            this.redoToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.redoToolStripMenuItem.Text = "&Redo";
            this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
            // 
            // filtersToolStripMenuItem
            // 
            this.filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
            this.filtersToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
            this.filtersToolStripMenuItem.Text = "F&ilters";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutPluginsMenuItem,
            this.aboutMenuToolStripSeparator,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutPluginsMenuItem
            // 
            this.aboutPluginsMenuItem.Name = "aboutPluginsMenuItem";
            this.aboutPluginsMenuItem.Size = new System.Drawing.Size(149, 22);
            this.aboutPluginsMenuItem.Text = "About &Plugins";
            this.aboutPluginsMenuItem.Visible = false;
            // 
            // aboutMenuToolStripSeparator
            // 
            this.aboutMenuToolStripSeparator.Name = "aboutMenuToolStripSeparator";
            this.aboutMenuToolStripSeparator.Size = new System.Drawing.Size(146, 6);
            this.aboutMenuToolStripSeparator.Visible = false;
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Image = global::HostTest.Properties.Resources.Annotate_info;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
            this.aboutToolStripMenuItem.Text = "&About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.Description = "Select a folder containing Adobe® Photoshop® filters.";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = resources.GetString("openFileDialog1.Filter");
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 632);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1034, 22);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(97, 17);
            this.toolStripStatusLabel1.Text = "No filters loaded.";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBar1.Visible = false;
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.AutoScrollMargin = new System.Drawing.Size(3, 3);
            this.panel1.BackColor = System.Drawing.Color.Silver;
            this.panel1.Controls.Add(this.canvas);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(29, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1005, 608);
            this.panel1.TabIndex = 10;
            // 
            // canvas
            // 
            this.canvas.IsDirty = false;
            this.canvas.Location = new System.Drawing.Point(3, 3);
            this.canvas.Name = "canvas";
            this.canvas.SelectionType = null;
            this.canvas.Size = new System.Drawing.Size(800, 600);
            this.canvas.Surface = null;
            this.canvas.TabIndex = 9;
            this.canvas.ZoomChanged += new System.EventHandler<HostTest.CanvasZoomChangedEventArgs>(this.canvas_ZoomChanged);
            this.canvas.DirtyChanged += new System.EventHandler<HostTest.CanvasDirtyChangedEventArgs>(this.canvas_DirtyChanged);
            // 
            // colorDialog1
            // 
            this.colorDialog1.FullOpen = true;
            this.colorDialog1.SolidColorOnly = true;
            // 
            // pointerSelectBtn
            // 
            this.pointerSelectBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.pointerSelectBtn.Enabled = false;
            this.pointerSelectBtn.Image = ((System.Drawing.Image)(resources.GetObject("pointerSelectBtn.Image")));
            this.pointerSelectBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.pointerSelectBtn.Name = "pointerSelectBtn";
            this.pointerSelectBtn.Size = new System.Drawing.Size(26, 20);
            this.pointerSelectBtn.Text = "toolStripButton1";
            this.pointerSelectBtn.ToolTipText = "No selection";
            this.pointerSelectBtn.Click += new System.EventHandler(this.pointerSelectBtn_Click);
            // 
            // rectangleSelectBtn
            // 
            this.rectangleSelectBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.rectangleSelectBtn.Enabled = false;
            this.rectangleSelectBtn.Image = ((System.Drawing.Image)(resources.GetObject("rectangleSelectBtn.Image")));
            this.rectangleSelectBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.rectangleSelectBtn.Name = "rectangleSelectBtn";
            this.rectangleSelectBtn.Size = new System.Drawing.Size(26, 20);
            this.rectangleSelectBtn.Text = "toolStripButton1";
            this.rectangleSelectBtn.ToolTipText = "Rectangle select tool";
            this.rectangleSelectBtn.Click += new System.EventHandler(this.rectangleSelectBtn_Click);
            // 
            // elipseSelectBtn
            // 
            this.elipseSelectBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.elipseSelectBtn.Enabled = false;
            this.elipseSelectBtn.Image = ((System.Drawing.Image)(resources.GetObject("elipseSelectBtn.Image")));
            this.elipseSelectBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.elipseSelectBtn.Name = "elipseSelectBtn";
            this.elipseSelectBtn.Size = new System.Drawing.Size(26, 20);
            this.elipseSelectBtn.Text = "toolStripButton2";
            this.elipseSelectBtn.ToolTipText = "Elipse select tool";
            this.elipseSelectBtn.Click += new System.EventHandler(this.elipseSelectBtn_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(26, 6);
            // 
            // zoomInBtn
            // 
            this.zoomInBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.zoomInBtn.Enabled = false;
            this.zoomInBtn.Image = ((System.Drawing.Image)(resources.GetObject("zoomInBtn.Image")));
            this.zoomInBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomInBtn.Name = "zoomInBtn";
            this.zoomInBtn.Size = new System.Drawing.Size(26, 20);
            this.zoomInBtn.Text = "toolStripButton1";
            this.zoomInBtn.ToolTipText = "Zoom In";
            this.zoomInBtn.Click += new System.EventHandler(this.zoomInBtn_Click);
            // 
            // zoomOutBtn
            // 
            this.zoomOutBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.zoomOutBtn.Enabled = false;
            this.zoomOutBtn.Image = ((System.Drawing.Image)(resources.GetObject("zoomOutBtn.Image")));
            this.zoomOutBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomOutBtn.Name = "zoomOutBtn";
            this.zoomOutBtn.Size = new System.Drawing.Size(26, 20);
            this.zoomOutBtn.Text = "toolStripButton1";
            this.zoomOutBtn.ToolTipText = "Zoom Out";
            this.zoomOutBtn.Click += new System.EventHandler(this.zoomOutBtn_Click);
            // 
            // zoomToWindowBtn
            // 
            this.zoomToWindowBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.zoomToWindowBtn.Enabled = false;
            this.zoomToWindowBtn.Image = ((System.Drawing.Image)(resources.GetObject("zoomToWindowBtn.Image")));
            this.zoomToWindowBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomToWindowBtn.Name = "zoomToWindowBtn";
            this.zoomToWindowBtn.Size = new System.Drawing.Size(26, 20);
            this.zoomToWindowBtn.Text = "toolStripButton1";
            this.zoomToWindowBtn.ToolTipText = "Fit in Window";
            this.zoomToWindowBtn.Click += new System.EventHandler(this.zoomToWindowBtn_Click);
            // 
            // zoomToActualSizeBtn
            // 
            this.zoomToActualSizeBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.zoomToActualSizeBtn.Enabled = false;
            this.zoomToActualSizeBtn.Image = ((System.Drawing.Image)(resources.GetObject("zoomToActualSizeBtn.Image")));
            this.zoomToActualSizeBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.zoomToActualSizeBtn.Name = "zoomToActualSizeBtn";
            this.zoomToActualSizeBtn.Size = new System.Drawing.Size(26, 20);
            this.zoomToActualSizeBtn.Text = "toolStripButton1";
            this.zoomToActualSizeBtn.ToolTipText = "Actual Size";
            this.zoomToActualSizeBtn.Click += new System.EventHandler(this.zoomToActualSizeBtn_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(26, 6);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Left;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pointerSelectBtn,
            this.rectangleSelectBtn,
            this.elipseSelectBtn,
            this.toolStripSeparator2,
            this.zoomInBtn,
            this.zoomOutBtn,
            this.zoomToWindowBtn,
            this.zoomToActualSizeBtn,
            this.toolStripSeparator3,
            this.primaryColorBtn,
            this.secondaryColorBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(29, 608);
            this.toolStrip1.TabIndex = 8;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // primaryColorBtn
            // 
            this.primaryColorBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.primaryColorBtn.Image = ((System.Drawing.Image)(resources.GetObject("primaryColorBtn.Image")));
            this.primaryColorBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.primaryColorBtn.Name = "primaryColorBtn";
            this.primaryColorBtn.RectangleColor = System.Drawing.Color.Black;
            this.primaryColorBtn.Size = new System.Drawing.Size(26, 20);
            this.primaryColorBtn.Text = "toolStripButton1";
            this.primaryColorBtn.ToolTipText = "Primary color";
            this.primaryColorBtn.Click += new System.EventHandler(this.primaryColorBtn_Click);
            // 
            // secondaryColorBtn
            // 
            this.secondaryColorBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.secondaryColorBtn.Image = ((System.Drawing.Image)(resources.GetObject("secondaryColorBtn.Image")));
            this.secondaryColorBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.secondaryColorBtn.Name = "secondaryColorBtn";
            this.secondaryColorBtn.RectangleColor = System.Drawing.Color.White;
            this.secondaryColorBtn.Size = new System.Drawing.Size(26, 20);
            this.secondaryColorBtn.Text = "toolStripButton2";
            this.secondaryColorBtn.ToolTipText = "Secondary color";
            this.secondaryColorBtn.Click += new System.EventHandler(this.secondaryColorBtn_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1034, 654);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "HostTest";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem filtersToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem loadFiltersMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private HostTest.Canvas canvas;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutPluginsMenuItem;
        private System.Windows.Forms.ToolStripSeparator aboutMenuToolStripSeparator;
        private System.Windows.Forms.ToolStripMenuItem refreshFiltersToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton pointerSelectBtn;
        private System.Windows.Forms.ToolStripButton rectangleSelectBtn;
        private System.Windows.Forms.ToolStripButton elipseSelectBtn;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton zoomInBtn;
        private System.Windows.Forms.ToolStripButton zoomOutBtn;
        private System.Windows.Forms.ToolStripButton zoomToWindowBtn;
        private System.Windows.Forms.ToolStripButton zoomToActualSizeBtn;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private ColorToolStripMenuItem primaryColorBtn;
        private ColorToolStripMenuItem secondaryColorBtn;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}

