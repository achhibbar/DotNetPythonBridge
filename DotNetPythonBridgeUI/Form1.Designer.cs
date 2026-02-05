namespace DotNetPythonBridge.SampleApp
{
    partial class Form1
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
            btnListEnvs = new Button();
            rtbPythonBridge = new RichTextBox();
            btnTestWSL_Helper = new Button();
            btnTestConda = new Button();
            btnPythonRunner = new Button();
            checkBoxWSL = new CheckBox();
            groupBox1 = new GroupBox();
            btnLazyInlineScriptRun = new Button();
            btnLazyServiceStartStop = new Button();
            btnLazyInit = new Button();
            buttonManual_Init = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btnListEnvs
            // 
            btnListEnvs.Location = new Point(509, 283);
            btnListEnvs.Name = "btnListEnvs";
            btnListEnvs.Size = new Size(151, 89);
            btnListEnvs.TabIndex = 0;
            btnListEnvs.Text = "List Conda Envs";
            btnListEnvs.UseVisualStyleBackColor = true;
            btnListEnvs.Click += btnListEnvs_Click;
            // 
            // rtbPythonBridge
            // 
            rtbPythonBridge.Location = new Point(700, 12);
            rtbPythonBridge.Name = "rtbPythonBridge";
            rtbPythonBridge.Size = new Size(788, 674);
            rtbPythonBridge.TabIndex = 1;
            rtbPythonBridge.Text = "";
            // 
            // btnTestWSL_Helper
            // 
            btnTestWSL_Helper.Location = new Point(352, 283);
            btnTestWSL_Helper.Name = "btnTestWSL_Helper";
            btnTestWSL_Helper.Size = new Size(151, 89);
            btnTestWSL_Helper.TabIndex = 3;
            btnTestWSL_Helper.Text = "Test WSL Helper";
            btnTestWSL_Helper.UseVisualStyleBackColor = true;
            btnTestWSL_Helper.Click += btnTestWSL_Helper_Click;
            // 
            // btnTestConda
            // 
            btnTestConda.Location = new Point(16, 79);
            btnTestConda.Name = "btnTestConda";
            btnTestConda.Size = new Size(151, 89);
            btnTestConda.TabIndex = 4;
            btnTestConda.Text = "Test Conda Manager";
            btnTestConda.UseVisualStyleBackColor = true;
            btnTestConda.Click += btnTestConda_Click;
            // 
            // btnPythonRunner
            // 
            btnPythonRunner.Location = new Point(173, 79);
            btnPythonRunner.Name = "btnPythonRunner";
            btnPythonRunner.Size = new Size(151, 89);
            btnPythonRunner.TabIndex = 5;
            btnPythonRunner.Text = "Test Python Runner";
            btnPythonRunner.UseVisualStyleBackColor = true;
            btnPythonRunner.Click += btnPythonRunner_Click;
            // 
            // checkBoxWSL
            // 
            checkBoxWSL.AutoSize = true;
            checkBoxWSL.Location = new Point(16, 25);
            checkBoxWSL.Name = "checkBoxWSL";
            checkBoxWSL.Size = new Size(128, 29);
            checkBoxWSL.TabIndex = 6;
            checkBoxWSL.Text = "Run in WSL";
            checkBoxWSL.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.BackColor = SystemColors.ControlLight;
            groupBox1.Controls.Add(btnLazyInlineScriptRun);
            groupBox1.Controls.Add(btnLazyServiceStartStop);
            groupBox1.Controls.Add(btnPythonRunner);
            groupBox1.Controls.Add(checkBoxWSL);
            groupBox1.Controls.Add(btnTestConda);
            groupBox1.Location = new Point(22, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(658, 224);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            // 
            // btnLazyInlineScriptRun
            // 
            btnLazyInlineScriptRun.Location = new Point(487, 79);
            btnLazyInlineScriptRun.Name = "btnLazyInlineScriptRun";
            btnLazyInlineScriptRun.Size = new Size(151, 89);
            btnLazyInlineScriptRun.TabIndex = 8;
            btnLazyInlineScriptRun.Text = "Inline/Script Run";
            btnLazyInlineScriptRun.UseVisualStyleBackColor = true;
            btnLazyInlineScriptRun.Click += btnLazyInlineScriptRun_Click;
            // 
            // btnLazyServiceStartStop
            // 
            btnLazyServiceStartStop.Location = new Point(330, 79);
            btnLazyServiceStartStop.Name = "btnLazyServiceStartStop";
            btnLazyServiceStartStop.Size = new Size(151, 89);
            btnLazyServiceStartStop.TabIndex = 7;
            btnLazyServiceStartStop.Text = "Service Start/Stop";
            btnLazyServiceStartStop.UseVisualStyleBackColor = true;
            btnLazyServiceStartStop.Click += btnLazyServiceStartStop_Click;
            // 
            // btnLazyInit
            // 
            btnLazyInit.Location = new Point(38, 283);
            btnLazyInit.Name = "btnLazyInit";
            btnLazyInit.Size = new Size(151, 89);
            btnLazyInit.TabIndex = 8;
            btnLazyInit.Text = "Initialize .Net Python Bridge";
            btnLazyInit.UseVisualStyleBackColor = true;
            btnLazyInit.Click += btnLazyInit_Click;
            // 
            // buttonManual_Init
            // 
            buttonManual_Init.Location = new Point(195, 283);
            buttonManual_Init.Name = "buttonManual_Init";
            buttonManual_Init.Size = new Size(151, 89);
            buttonManual_Init.TabIndex = 9;
            buttonManual_Init.Text = "Manually Initialize .Net Python Bridge";
            buttonManual_Init.UseVisualStyleBackColor = true;
            buttonManual_Init.Click += buttonManual_Init_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F);
            label1.Location = new Point(22, 446);
            label1.Name = "label1";
            label1.Size = new Size(124, 28);
            label1.TabIndex = 10;
            label1.Text = "Before using:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F);
            label2.Location = new Point(22, 507);
            label2.Name = "label2";
            label2.Size = new Size(654, 28);
            label2.TabIndex = 11;
            label2.Text = "1. Copy the files TestService.py, TestScript.py, and testCondaEnvCreate.yml";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10F);
            label3.Location = new Point(41, 547);
            label3.Name = "label3";
            label3.Size = new Size(333, 28);
            label3.TabIndex = 12;
            label3.Text = "to a suitable location on your system";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 10F);
            label4.Location = new Point(22, 606);
            label4.Name = "label4";
            label4.Size = new Size(529, 28);
            label4.TabIndex = 13;
            label4.Text = "2. Update the values in SampleConfig for your environment";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1505, 703);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonManual_Init);
            Controls.Add(btnLazyInit);
            Controls.Add(btnTestWSL_Helper);
            Controls.Add(groupBox1);
            Controls.Add(rtbPythonBridge);
            Controls.Add(btnListEnvs);
            Name = "Form1";
            Text = "DotNetPythonBridge Sample App";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnListEnvs;
        private RichTextBox rtbPythonBridge;
        private Button btnTestWSL_Helper;
        private Button btnTestConda;
        private Button btnPythonRunner;
        private CheckBox checkBoxWSL;
        private GroupBox groupBox1;
        private Button btnLazyInit;
        private Button buttonManual_Init;
        private Button btnLazyServiceStartStop;
        private Button btnLazyInlineScriptRun;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
    }
}
