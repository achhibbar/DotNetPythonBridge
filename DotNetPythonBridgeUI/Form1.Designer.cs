namespace DotNetPythonBridgeUI
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
            btnTestPorts = new Button();
            btnTestWSL_Helper = new Button();
            btnTestConda = new Button();
            btnPythonRunner = new Button();
            checkBoxWSL = new CheckBox();
            groupBox1 = new GroupBox();
            btnLazyServiceStartStop = new Button();
            btnLazyInit = new Button();
            buttonManual_Init = new Button();
            btnLazyInlineScriptRun = new Button();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btnListEnvs
            // 
            btnListEnvs.Location = new Point(243, 278);
            btnListEnvs.Name = "btnListEnvs";
            btnListEnvs.Size = new Size(151, 89);
            btnListEnvs.TabIndex = 0;
            btnListEnvs.Text = "List Conda Envs";
            btnListEnvs.UseVisualStyleBackColor = true;
            btnListEnvs.Click += btnListEnvs_Click;
            // 
            // rtbPythonBridge
            // 
            rtbPythonBridge.Location = new Point(812, 12);
            rtbPythonBridge.Name = "rtbPythonBridge";
            rtbPythonBridge.Size = new Size(915, 1064);
            rtbPythonBridge.TabIndex = 1;
            rtbPythonBridge.Text = "";
            // 
            // btnTestPorts
            // 
            btnTestPorts.Location = new Point(70, 43);
            btnTestPorts.Name = "btnTestPorts";
            btnTestPorts.Size = new Size(151, 89);
            btnTestPorts.TabIndex = 2;
            btnTestPorts.Text = "Test Ports";
            btnTestPorts.UseVisualStyleBackColor = true;
            btnTestPorts.Click += btnTestPorts_Click;
            // 
            // btnTestWSL_Helper
            // 
            btnTestWSL_Helper.Location = new Point(70, 278);
            btnTestWSL_Helper.Name = "btnTestWSL_Helper";
            btnTestWSL_Helper.Size = new Size(151, 89);
            btnTestWSL_Helper.TabIndex = 3;
            btnTestWSL_Helper.Text = "Test WSL Helper";
            btnTestWSL_Helper.UseVisualStyleBackColor = true;
            btnTestWSL_Helper.Click += btnTestWSL_Helper_Click;
            // 
            // btnTestConda
            // 
            btnTestConda.Location = new Point(28, 112);
            btnTestConda.Name = "btnTestConda";
            btnTestConda.Size = new Size(151, 89);
            btnTestConda.TabIndex = 4;
            btnTestConda.Text = "Test Conda Manager";
            btnTestConda.UseVisualStyleBackColor = true;
            btnTestConda.Click += btnTestConda_Click;
            // 
            // btnPythonRunner
            // 
            btnPythonRunner.Location = new Point(28, 207);
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
            checkBoxWSL.Location = new Point(28, 58);
            checkBoxWSL.Name = "checkBoxWSL";
            checkBoxWSL.Size = new Size(73, 29);
            checkBoxWSL.TabIndex = 6;
            checkBoxWSL.Text = "WSL";
            checkBoxWSL.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnLazyInlineScriptRun);
            groupBox1.Controls.Add(btnLazyServiceStartStop);
            groupBox1.Controls.Add(btnPythonRunner);
            groupBox1.Controls.Add(checkBoxWSL);
            groupBox1.Controls.Add(btnTestConda);
            groupBox1.Location = new Point(70, 436);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(427, 508);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            // 
            // btnLazyServiceStartStop
            // 
            btnLazyServiceStartStop.Location = new Point(28, 302);
            btnLazyServiceStartStop.Name = "btnLazyServiceStartStop";
            btnLazyServiceStartStop.Size = new Size(151, 89);
            btnLazyServiceStartStop.TabIndex = 7;
            btnLazyServiceStartStop.Text = "Test Lazy Service Start/Stop";
            btnLazyServiceStartStop.UseVisualStyleBackColor = true;
            btnLazyServiceStartStop.Click += btnLazyServiceStartStop_Click;
            // 
            // btnLazyInit
            // 
            btnLazyInit.Location = new Point(70, 161);
            btnLazyInit.Name = "btnLazyInit";
            btnLazyInit.Size = new Size(151, 89);
            btnLazyInit.TabIndex = 8;
            btnLazyInit.Text = "Initialize .Net Python Bridge";
            btnLazyInit.UseVisualStyleBackColor = true;
            btnLazyInit.Click += btnLazyInit_Click;
            // 
            // buttonManual_Init
            // 
            buttonManual_Init.Location = new Point(243, 161);
            buttonManual_Init.Name = "buttonManual_Init";
            buttonManual_Init.Size = new Size(151, 89);
            buttonManual_Init.TabIndex = 9;
            buttonManual_Init.Text = "Manually Initialize .Net Python Bridge";
            buttonManual_Init.UseVisualStyleBackColor = true;
            buttonManual_Init.Click += buttonManual_Init_Click;
            // 
            // btnLazyInlineScriptRun
            // 
            btnLazyInlineScriptRun.Location = new Point(28, 397);
            btnLazyInlineScriptRun.Name = "btnLazyInlineScriptRun";
            btnLazyInlineScriptRun.Size = new Size(151, 89);
            btnLazyInlineScriptRun.TabIndex = 8;
            btnLazyInlineScriptRun.Text = "Test Lazy Inline/Script Run";
            btnLazyInlineScriptRun.UseVisualStyleBackColor = true;
            btnLazyInlineScriptRun.Click += btnLazyInlineScriptRun_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1735, 1085);
            Controls.Add(buttonManual_Init);
            Controls.Add(btnLazyInit);
            Controls.Add(btnTestWSL_Helper);
            Controls.Add(groupBox1);
            Controls.Add(btnTestPorts);
            Controls.Add(rtbPythonBridge);
            Controls.Add(btnListEnvs);
            Name = "Form1";
            Text = "Form1";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnListEnvs;
        private RichTextBox rtbPythonBridge;
        private Button btnTestPorts;
        private Button btnTestWSL_Helper;
        private Button btnTestConda;
        private Button btnPythonRunner;
        private CheckBox checkBoxWSL;
        private GroupBox groupBox1;
        private Button btnLazyInit;
        private Button buttonManual_Init;
        private Button btnLazyServiceStartStop;
        private Button btnLazyInlineScriptRun;
    }
}
