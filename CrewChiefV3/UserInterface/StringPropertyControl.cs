﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CrewChiefV3
{
    public partial class StringPropertyControl : UserControl
    {
        public String propertyId;
        public String defaultValue;
        public String originalValue;
        public StringPropertyControl(String propertyId, String label, String currentValue, String defaultValue, String helpText)
        {
            InitializeComponent();
            this.propertyId = propertyId;
            this.label1.Text = label;
            this.originalValue = currentValue;
            this.textBox1.Text = currentValue;
            this.defaultValue = defaultValue;
            this.toolTip1.SetToolTip(this.textBox1, helpText);
            this.toolTip1.SetToolTip(this.label1, helpText);            
        }

        public String getValue()
        {
            return this.textBox1.Text;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (originalValue != defaultValue)
            {
                PropertiesForm.hasChanges = true;
            }
            this.textBox1.Text = defaultValue;
        }

        private void textChanged(object sender, EventArgs e)
        {
            if (this.textBox1.Text != originalValue)
            {
                PropertiesForm.hasChanges = true;
            }
        }
    }
}
