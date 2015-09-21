﻿using System;
namespace CrewChiefV3
{
    partial class MainWindow
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
            this.runListenForChannelOpenThread = false;
            this.runListenForButtonPressesThread = false;
            if (crewChief != null)
            {
                crewChief.stop();
                crewChief.Dispose();
            }
            if (controllerConfiguration != null)
            {
                controllerConfiguration.Dispose();
            }
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.startApplicationButton = new System.Windows.Forms.Button();
            this.buttonActionSelect = new System.Windows.Forms.ListBox();
            this.controllersList = new System.Windows.Forms.ListBox();
            this.assignButtonToAction = new System.Windows.Forms.Button();
            this.deleteAssigmentButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.alwaysOnButton = new System.Windows.Forms.RadioButton();
            this.toggleButton = new System.Windows.Forms.RadioButton();
            this.holdButton = new System.Windows.Forms.RadioButton();
            this.voiceDisableButton = new System.Windows.Forms.RadioButton();
            this.button2 = new System.Windows.Forms.Button();
            this.messagesVolumeSlider = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.backgroundVolumeSlider = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.gameDefinitionList = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.filenameTextbox = new System.Windows.Forms.TextBox();
            this.filenameLabel = new System.Windows.Forms.Label();
            this.recordSession = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.messagesVolumeSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.backgroundVolumeSlider)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(41, 80);
            this.textBox1.MaxLength = 99999999;
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(1093, 417);
            this.textBox1.TabIndex = 1;
            this.textBox1.Click += new System.EventHandler(this.assignButtonToActionClick);
            // 
            // startApplicationButton
            // 
            this.startApplicationButton.Location = new System.Drawing.Point(41, 28);
            this.startApplicationButton.Name = "startApplicationButton";
            this.startApplicationButton.Size = new System.Drawing.Size(137, 41);
            this.startApplicationButton.TabIndex = 5;
            this.startApplicationButton.Text = "Start Application";
            this.startApplicationButton.UseVisualStyleBackColor = true;
            this.startApplicationButton.Click += new System.EventHandler(this.startApplicationButton_Click);
            // 
            // buttonActionSelect
            // 
            this.buttonActionSelect.FormattingEnabled = true;
            this.buttonActionSelect.Location = new System.Drawing.Point(323, 520);
            this.buttonActionSelect.Name = "buttonActionSelect";
            this.buttonActionSelect.Size = new System.Drawing.Size(419, 95);
            this.buttonActionSelect.TabIndex = 7;
            this.buttonActionSelect.SelectedIndexChanged += new System.EventHandler(this.buttonActionSelect_SelectedIndexChanged);
            // 
            // controllersList
            // 
            this.controllersList.FormattingEnabled = true;
            this.controllersList.Location = new System.Drawing.Point(41, 520);
            this.controllersList.Name = "controllersList";
            this.controllersList.Size = new System.Drawing.Size(248, 95);
            this.controllersList.TabIndex = 8;
            this.controllersList.SelectedIndexChanged += new System.EventHandler(this.controllersList_SelectedIndexChanged);
            // 
            // assignButtonToAction
            // 
            this.assignButtonToAction.Location = new System.Drawing.Point(748, 520);
            this.assignButtonToAction.Name = "assignButtonToAction";
            this.assignButtonToAction.Size = new System.Drawing.Size(167, 39);
            this.assignButtonToAction.TabIndex = 9;
            this.assignButtonToAction.Text = "Assign control";
            this.assignButtonToAction.UseVisualStyleBackColor = true;
            this.assignButtonToAction.Click += new System.EventHandler(this.assignButtonToActionClick);
            // 
            // deleteAssigmentButton
            // 
            this.deleteAssigmentButton.Location = new System.Drawing.Point(748, 565);
            this.deleteAssigmentButton.Name = "deleteAssigmentButton";
            this.deleteAssigmentButton.Size = new System.Drawing.Size(167, 40);
            this.deleteAssigmentButton.TabIndex = 10;
            this.deleteAssigmentButton.Text = "Delete assignment";
            this.deleteAssigmentButton.UseVisualStyleBackColor = true;
            this.deleteAssigmentButton.Click += new System.EventHandler(this.deleteAssignmentButtonClicked);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(38, 500);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(135, 17);
            this.label1.TabIndex = 11;
            this.label1.Text = "Available controllers";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(323, 501);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(114, 17);
            this.label2.TabIndex = 12;
            this.label2.Text = "Available actions";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(991, 28);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(143, 41);
            this.button1.TabIndex = 14;
            this.button1.Text = "Properties";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.editPropertiesButtonClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.alwaysOnButton);
            this.groupBox1.Controls.Add(this.toggleButton);
            this.groupBox1.Controls.Add(this.holdButton);
            this.groupBox1.Controls.Add(this.voiceDisableButton);
            this.groupBox1.Location = new System.Drawing.Point(934, 520);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 121);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Voice recognition mode";
            // 
            // alwaysOnButton
            // 
            this.alwaysOnButton.AutoSize = true;
            this.alwaysOnButton.Location = new System.Drawing.Point(7, 91);
            this.alwaysOnButton.Name = "alwaysOnButton";
            this.alwaysOnButton.Size = new System.Drawing.Size(73, 17);
            this.alwaysOnButton.TabIndex = 3;
            this.alwaysOnButton.TabStop = true;
            this.alwaysOnButton.Text = "Always on";
            this.alwaysOnButton.UseVisualStyleBackColor = true;
            this.alwaysOnButton.CheckedChanged += new System.EventHandler(this.alwaysOnButton_CheckedChanged);
            // 
            // toggleButton
            // 
            this.toggleButton.AutoSize = true;
            this.toggleButton.Location = new System.Drawing.Point(7, 67);
            this.toggleButton.Name = "toggleButton";
            this.toggleButton.Size = new System.Drawing.Size(91, 17);
            this.toggleButton.TabIndex = 2;
            this.toggleButton.TabStop = true;
            this.toggleButton.Text = "Toggle button";
            this.toggleButton.UseVisualStyleBackColor = true;
            this.toggleButton.CheckedChanged += new System.EventHandler(this.toggleButton_CheckedChanged);
            // 
            // holdButton
            // 
            this.holdButton.AutoSize = true;
            this.holdButton.Location = new System.Drawing.Point(7, 44);
            this.holdButton.Name = "holdButton";
            this.holdButton.Size = new System.Drawing.Size(80, 17);
            this.holdButton.TabIndex = 1;
            this.holdButton.TabStop = true;
            this.holdButton.Text = "Hold button";
            this.holdButton.UseVisualStyleBackColor = true;
            this.holdButton.CheckedChanged += new System.EventHandler(this.holdButton_CheckedChanged);
            // 
            // voiceDisableButton
            // 
            this.voiceDisableButton.AutoSize = true;
            this.voiceDisableButton.Location = new System.Drawing.Point(7, 20);
            this.voiceDisableButton.Name = "voiceDisableButton";
            this.voiceDisableButton.Size = new System.Drawing.Size(66, 17);
            this.voiceDisableButton.TabIndex = 0;
            this.voiceDisableButton.TabStop = true;
            this.voiceDisableButton.Text = "Disabled";
            this.voiceDisableButton.UseVisualStyleBackColor = true;
            this.voiceDisableButton.CheckedChanged += new System.EventHandler(this.voiceDisableButton_CheckedChanged);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(184, 28);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(137, 41);
            this.button2.TabIndex = 16;
            this.button2.Text = "Clear console";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.clearConsole);
            // 
            // messagesVolumeSlider
            // 
            this.messagesVolumeSlider.Location = new System.Drawing.Point(327, 28);
            this.messagesVolumeSlider.Name = "messagesVolumeSlider";
            this.messagesVolumeSlider.Size = new System.Drawing.Size(176, 45);
            this.messagesVolumeSlider.TabIndex = 17;
            this.messagesVolumeSlider.Scroll += new System.EventHandler(this.messagesVolumeSlider_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(367, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Messages volume";
            // 
            // backgroundVolumeSlider
            // 
            this.backgroundVolumeSlider.Location = new System.Drawing.Point(558, 28);
            this.backgroundVolumeSlider.Name = "backgroundVolumeSlider";
            this.backgroundVolumeSlider.Size = new System.Drawing.Size(184, 45);
            this.backgroundVolumeSlider.TabIndex = 19;
            this.backgroundVolumeSlider.Scroll += new System.EventHandler(this.backgroundVolumeSlider_Scroll);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(567, 11);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(102, 13);
            this.label4.TabIndex = 20;
            this.label4.Text = "Background volume";
            // 
            // gameDefinitionList
            // 
            this.gameDefinitionList.AllowDrop = true;
            this.gameDefinitionList.FormattingEnabled = true;
            this.gameDefinitionList.Items.AddRange(new object[] {
            "Project Cars (64 bit)",
            "Project Cars (32 bit)",
            "Race Room"});
            this.gameDefinitionList.Location = new System.Drawing.Point(782, 26);
            this.gameDefinitionList.Name = "gameDefinitionList";
            this.gameDefinitionList.Size = new System.Drawing.Size(203, 43);
            this.gameDefinitionList.TabIndex = 21;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(779, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "Game";
            // 
            // filenameTextbox
            // 
            this.filenameTextbox.Location = new System.Drawing.Point(186, 2);
            this.filenameTextbox.Name = "filenameTextbox";
            this.filenameTextbox.Size = new System.Drawing.Size(137, 20);
            this.filenameTextbox.TabIndex = 23;
            // 
            // filenameLabel
            // 
            this.filenameLabel.AutoSize = true;
            this.filenameLabel.Location = new System.Drawing.Point(101, 5);
            this.filenameLabel.Name = "filenameLabel";
            this.filenameLabel.Size = new System.Drawing.Size(82, 13);
            this.filenameLabel.TabIndex = 24;
            this.filenameLabel.Text = "File name to run";
            // 
            // recordSession
            // 
            this.recordSession.AutoSize = true;
            this.recordSession.Location = new System.Drawing.Point(41, 4);
            this.recordSession.Name = "recordSession";
            this.recordSession.Size = new System.Drawing.Size(61, 17);
            this.recordSession.TabIndex = 25;
            this.recordSession.Text = "Record";
            this.recordSession.UseVisualStyleBackColor = true;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1146, 692);
            this.Controls.Add(this.recordSession);
            this.Controls.Add(this.filenameLabel);
            this.Controls.Add(this.filenameTextbox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.gameDefinitionList);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.backgroundVolumeSlider);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.messagesVolumeSlider);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.deleteAssigmentButton);
            this.Controls.Add(this.assignButtonToAction);
            this.Controls.Add(this.controllersList);
            this.Controls.Add(this.buttonActionSelect);
            this.Controls.Add(this.startApplicationButton);
            this.Controls.Add(this.textBox1);
            this.Name = "MainWindow";
            this.Text = "Crew Chief V2";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.stopApp);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.messagesVolumeSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.backgroundVolumeSlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button startApplicationButton;
        private System.Windows.Forms.ListBox buttonActionSelect;
        private System.Windows.Forms.ListBox controllersList;
        private System.Windows.Forms.Button assignButtonToAction;
        private System.Windows.Forms.Button deleteAssigmentButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton alwaysOnButton;
        private System.Windows.Forms.RadioButton toggleButton;
        private System.Windows.Forms.RadioButton holdButton;
        private System.Windows.Forms.RadioButton voiceDisableButton;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TrackBar messagesVolumeSlider;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar backgroundVolumeSlider;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.ListBox gameDefinitionList;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox filenameTextbox;
        private System.Windows.Forms.Label filenameLabel;
        private System.Windows.Forms.CheckBox recordSession;

    }
}

