namespace Youtube
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            mainLayout = new TableLayoutPanel();
            titleBar = new Panel();
            titlePanel = new Panel();
            iconTitle = new PictureBox();
            labelTitle = new Label();
            btnMinimize = new Button();
            btnMaximize = new Button();
            btnClose = new Button();
            topPanel = new Panel();
            searchPanel = new Panel();
            txtAddress = new TextBox();
            btnGo = new Button();
            toolbarPanel = new FlowLayoutPanel();
            notifyIcon = new NotifyIcon(components);
            mainLayout.SuspendLayout();
            titleBar.SuspendLayout();
            titlePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)iconTitle).BeginInit();
            topPanel.SuspendLayout();
            searchPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.BackColor = Color.FromArgb(15, 15, 18);
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(titleBar, 0, 0);
            mainLayout.Controls.Add(topPanel, 0, 1);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Margin = new Padding(0);
            mainLayout.Name = "mainLayout";
            mainLayout.RowCount = 3;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 41F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(1040, 725);
            mainLayout.TabIndex = 0;
            // 
            // titleBar
            // 
            titleBar.BackColor = Color.FromArgb(20, 21, 26);
            titleBar.Controls.Add(titlePanel);
            titleBar.Controls.Add(btnMinimize);
            titleBar.Controls.Add(btnMaximize);
            titleBar.Controls.Add(btnClose);
            titleBar.Dock = DockStyle.Fill;
            titleBar.Location = new Point(0, 0);
            titleBar.Margin = new Padding(0);
            titleBar.Name = "titleBar";
            titleBar.Size = new Size(1040, 41);
            titleBar.TabIndex = 0;
            // 
            // titlePanel
            // 
            titlePanel.BackColor = Color.Transparent;
            titlePanel.Controls.Add(iconTitle);
            titlePanel.Controls.Add(labelTitle);
            titlePanel.Dock = DockStyle.Left;
            titlePanel.Location = new Point(0, 0);
            titlePanel.Name = "titlePanel";
            titlePanel.Size = new Size(350, 41);
            titlePanel.TabIndex = 0;
            // 
            // iconTitle
            // 
            iconTitle.BackColor = Color.Transparent;
            iconTitle.Image = (Image)resources.GetObject("iconTitle.Image");
            iconTitle.Location = new Point(12, 9);
            iconTitle.Name = "iconTitle";
            iconTitle.Size = new Size(20, 23);
            iconTitle.SizeMode = PictureBoxSizeMode.Zoom;
            iconTitle.TabIndex = 0;
            iconTitle.TabStop = false;
            // 
            // labelTitle
            // 
            labelTitle.AutoEllipsis = true;
            labelTitle.BackColor = Color.Transparent;
            labelTitle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(230, 230, 235);
            labelTitle.Location = new Point(38, 0);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(300, 41);
            labelTitle.TabIndex = 1;
            labelTitle.Text = "AUREON YouTube";
            labelTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnMinimize
            // 
            btnMinimize.BackColor = Color.FromArgb(20, 21, 26);
            btnMinimize.Cursor = Cursors.Hand;
            btnMinimize.Dock = DockStyle.Right;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 62, 72);
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 47, 56);
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.Font = new Font("Segoe UI", 11F);
            btnMinimize.ForeColor = Color.FromArgb(220, 220, 225);
            btnMinimize.Location = new Point(902, 0);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(46, 41);
            btnMinimize.TabIndex = 1;
            btnMinimize.TabStop = false;
            btnMinimize.Text = "—";
            btnMinimize.UseVisualStyleBackColor = false;
            // 
            // btnMaximize
            // 
            btnMaximize.BackColor = Color.FromArgb(20, 21, 26);
            btnMaximize.Cursor = Cursors.Hand;
            btnMaximize.Dock = DockStyle.Right;
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 62, 72);
            btnMaximize.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 47, 56);
            btnMaximize.FlatStyle = FlatStyle.Flat;
            btnMaximize.Font = new Font("Segoe UI", 10F);
            btnMaximize.ForeColor = Color.FromArgb(220, 220, 225);
            btnMaximize.Location = new Point(948, 0);
            btnMaximize.Name = "btnMaximize";
            btnMaximize.Size = new Size(46, 41);
            btnMaximize.TabIndex = 2;
            btnMaximize.TabStop = false;
            btnMaximize.Text = "□";
            btnMaximize.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            btnClose.BackColor = Color.FromArgb(20, 21, 26);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Dock = DockStyle.Right;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 25, 30);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 40, 45);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Segoe UI", 10F);
            btnClose.ForeColor = Color.White;
            btnClose.Location = new Point(994, 0);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(46, 41);
            btnClose.TabIndex = 3;
            btnClose.TabStop = false;
            btnClose.Text = "×";
            btnClose.UseVisualStyleBackColor = false;
            // 
            // topPanel
            // 
            topPanel.BackColor = Color.FromArgb(26, 27, 34);
            topPanel.Controls.Add(searchPanel);
            topPanel.Controls.Add(toolbarPanel);
            topPanel.Dock = DockStyle.Fill;
            topPanel.Location = new Point(0, 41);
            topPanel.Margin = new Padding(0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1040, 52);
            topPanel.TabIndex = 1;
            // 
            // searchPanel
            // 
            searchPanel.BackColor = Color.FromArgb(18, 19, 24);
            searchPanel.Controls.Add(txtAddress);
            searchPanel.Controls.Add(btnGo);
            searchPanel.Dock = DockStyle.Fill;
            searchPanel.Location = new Point(0, 0);
            searchPanel.Margin = new Padding(0);
            searchPanel.Name = "searchPanel";
            searchPanel.Padding = new Padding(8, 9, 8, 9);
            searchPanel.Size = new Size(1040, 52);
            searchPanel.TabIndex = 0;
            searchPanel.Visible = false;
            // 
            // txtAddress
            // 
            txtAddress.BackColor = Color.FromArgb(35, 36, 44);
            txtAddress.BorderStyle = BorderStyle.None;
            txtAddress.Dock = DockStyle.Fill;
            txtAddress.Font = new Font("Segoe UI", 10.5F);
            txtAddress.ForeColor = Color.White;
            txtAddress.Location = new Point(8, 9);
            txtAddress.Name = "txtAddress";
            txtAddress.PlaceholderText = "Введіть пошуковий запит або URL...";
            txtAddress.Size = new Size(939, 19);
            txtAddress.TabIndex = 0;
            // 
            // btnGo
            // 
            btnGo.BackColor = Color.FromArgb(200, 30, 35);
            btnGo.Cursor = Cursors.Hand;
            btnGo.Dock = DockStyle.Right;
            btnGo.FlatAppearance.BorderSize = 0;
            btnGo.FlatStyle = FlatStyle.Flat;
            btnGo.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnGo.ForeColor = Color.White;
            btnGo.Location = new Point(947, 9);
            btnGo.Name = "btnGo";
            btnGo.Size = new Size(85, 34);
            btnGo.TabIndex = 1;
            btnGo.Text = "Перейти";
            btnGo.UseVisualStyleBackColor = false;
            // 
            // toolbarPanel
            // 
            toolbarPanel.BackColor = Color.Transparent;
            toolbarPanel.Dock = DockStyle.Fill;
            toolbarPanel.Location = new Point(0, 0);
            toolbarPanel.Margin = new Padding(0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Padding = new Padding(6, 1, 6, 1);
            toolbarPanel.Size = new Size(1040, 52);
            toolbarPanel.TabIndex = 1;
            toolbarPanel.WrapContents = false;
            // 
            // notifyIcon
            // 
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.BalloonTipText = "YouTube продовжує працювати у фоні";
            notifyIcon.BalloonTipTitle = "AUREON YouTube";
            notifyIcon.Text = "AUREON YouTube";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 15, 18);
            ClientSize = new Size(1040, 725);
            Controls.Add(mainLayout);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(900, 567);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AUREON YouTube";
            mainLayout.ResumeLayout(false);
            titleBar.ResumeLayout(false);
            titlePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)iconTitle).EndInit();
            topPanel.ResumeLayout(false);
            searchPanel.ResumeLayout(false);
            searchPanel.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Panel titleBar;
        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.PictureBox iconTitle;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Button btnMinimize;
        private System.Windows.Forms.Button btnMaximize;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Panel searchPanel;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.FlowLayoutPanel toolbarPanel;
        private System.Windows.Forms.NotifyIcon notifyIcon;
    }
}