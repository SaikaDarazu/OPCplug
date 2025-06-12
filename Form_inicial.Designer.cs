namespace OPCplug
{
    partial class Form_inicial
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            Run_clientes = new Button();
            Run_server = new Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.GrayText;
            panel1.Controls.Add(splitContainer1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(4);
            panel1.Size = new Size(1105, 731);
            panel1.TabIndex = 2;
            // 
            // splitContainer1
            // 
            splitContainer1.BackColor = SystemColors.GrayText;
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(4, 4);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.BackColor = SystemColors.Menu;
            splitContainer1.Panel1.Controls.Add(Run_clientes);
            splitContainer1.Panel1.Controls.Add(Run_server);
            splitContainer1.Panel1.Padding = new Padding(5);
            splitContainer1.Panel1.Paint += splitContainer1_Panel1_Paint;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.BackColor = SystemColors.Window;
            splitContainer1.Panel2.Paint += splitContainer1_Panel2_Paint;
            splitContainer1.Size = new Size(1097, 723);
            splitContainer1.SplitterDistance = 200;
            splitContainer1.TabIndex = 2;
            // 
            // Run_clientes
            // 
            Run_clientes.BackColor = Color.LightGreen;
            Run_clientes.Cursor = Cursors.Hand;
            Run_clientes.Dock = DockStyle.Top;
            Run_clientes.Location = new Point(5, 75);
            Run_clientes.Name = "Run_clientes";
            Run_clientes.Size = new Size(190, 70);
            Run_clientes.TabIndex = 2;
            Run_clientes.Text = "Arrancar Lectura Clientes";
            Run_clientes.UseVisualStyleBackColor = false;
            Run_clientes.Click += Run_clientes_Click;
            // 
            // Run_server
            // 
            Run_server.BackColor = Color.LightGreen;
            Run_server.Cursor = Cursors.Hand;
            Run_server.Dock = DockStyle.Top;
            Run_server.Location = new Point(5, 5);
            Run_server.Name = "Run_server";
            Run_server.Size = new Size(190, 70);
            Run_server.TabIndex = 1;
            Run_server.Text = "Arrancar Server";
            Run_server.UseVisualStyleBackColor = false;
            Run_server.Click += Run_server_click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1105, 731);
            Controls.Add(panel1);
            Name = "Form_inicial";
            Text = "Form_inicial";
            Load += Form_inicial_Load;
            panel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private SplitContainer splitContainer1;
        private Button Run_server;
        private Button Run_clientes;
    }
}
