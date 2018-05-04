namespace DHL_Seife
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
            this.printShippingLabel = new System.Windows.Forms.Button();
            this.textBoxOrdernumber = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxRecepient = new System.Windows.Forms.TextBox();
            this.textBoxStreet = new System.Windows.Forms.TextBox();
            this.textBoxStreetNumber = new System.Windows.Forms.TextBox();
            this.textBoxPLZ = new System.Windows.Forms.TextBox();
            this.textBoxCity = new System.Windows.Forms.TextBox();
            this.textBoxCountry = new System.Windows.Forms.TextBox();
            this.textBoxMail = new System.Windows.Forms.TextBox();
            this.textBoxWeight = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.printManualShippingLabel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // printShippingLabel
            // 
            this.printShippingLabel.Enabled = false;
            this.printShippingLabel.Location = new System.Drawing.Point(12, 306);
            this.printShippingLabel.Name = "printShippingLabel";
            this.printShippingLabel.Size = new System.Drawing.Size(473, 23);
            this.printShippingLabel.TabIndex = 0;
            this.printShippingLabel.Text = "Daten aus Enventa auslesen (dafür bitte Belegnummer eintragen)";
            this.printShippingLabel.UseVisualStyleBackColor = true;
            this.printShippingLabel.Click += new System.EventHandler(this.printShippingLabel_Click);
            // 
            // textBoxOrdernumber
            // 
            this.textBoxOrdernumber.Location = new System.Drawing.Point(206, 12);
            this.textBoxOrdernumber.Name = "textBoxOrdernumber";
            this.textBoxOrdernumber.Size = new System.Drawing.Size(279, 20);
            this.textBoxOrdernumber.TabIndex = 1;
            this.textBoxOrdernumber.TextChanged += new System.EventHandler(this.textBoxOrdernumber_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Belegnummer";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Name";
            // 
            // textBoxRecepient
            // 
            this.textBoxRecepient.Location = new System.Drawing.Point(206, 39);
            this.textBoxRecepient.Name = "textBoxRecepient";
            this.textBoxRecepient.Size = new System.Drawing.Size(279, 20);
            this.textBoxRecepient.TabIndex = 4;
            this.textBoxRecepient.TextChanged += new System.EventHandler(this.textBoxRecepient_TextChanged);
            // 
            // textBoxStreet
            // 
            this.textBoxStreet.Location = new System.Drawing.Point(206, 65);
            this.textBoxStreet.Name = "textBoxStreet";
            this.textBoxStreet.Size = new System.Drawing.Size(279, 20);
            this.textBoxStreet.TabIndex = 5;
            this.textBoxStreet.TextChanged += new System.EventHandler(this.textBoxStreet_TextChanged);
            // 
            // textBoxStreetNumber
            // 
            this.textBoxStreetNumber.Location = new System.Drawing.Point(206, 91);
            this.textBoxStreetNumber.Name = "textBoxStreetNumber";
            this.textBoxStreetNumber.Size = new System.Drawing.Size(279, 20);
            this.textBoxStreetNumber.TabIndex = 6;
            this.textBoxStreetNumber.TextChanged += new System.EventHandler(this.textBoxStreetNumber_TextChanged);
            // 
            // textBoxPLZ
            // 
            this.textBoxPLZ.Location = new System.Drawing.Point(206, 117);
            this.textBoxPLZ.Name = "textBoxPLZ";
            this.textBoxPLZ.Size = new System.Drawing.Size(279, 20);
            this.textBoxPLZ.TabIndex = 7;
            this.textBoxPLZ.TextChanged += new System.EventHandler(this.textBoxPLZ_TextChanged);
            // 
            // textBoxCity
            // 
            this.textBoxCity.Location = new System.Drawing.Point(206, 143);
            this.textBoxCity.Name = "textBoxCity";
            this.textBoxCity.Size = new System.Drawing.Size(279, 20);
            this.textBoxCity.TabIndex = 8;
            this.textBoxCity.TextChanged += new System.EventHandler(this.textBoxCity_TextChanged);
            // 
            // textBoxCountry
            // 
            this.textBoxCountry.Location = new System.Drawing.Point(206, 169);
            this.textBoxCountry.Name = "textBoxCountry";
            this.textBoxCountry.Size = new System.Drawing.Size(279, 20);
            this.textBoxCountry.TabIndex = 9;
            this.textBoxCountry.TextChanged += new System.EventHandler(this.textBoxCountry_TextChanged);
            // 
            // textBoxMail
            // 
            this.textBoxMail.Location = new System.Drawing.Point(206, 221);
            this.textBoxMail.Name = "textBoxMail";
            this.textBoxMail.Size = new System.Drawing.Size(279, 20);
            this.textBoxMail.TabIndex = 10;
            this.textBoxMail.TextChanged += new System.EventHandler(this.textBoxMail_TextChanged);
            // 
            // textBoxWeight
            // 
            this.textBoxWeight.Location = new System.Drawing.Point(206, 195);
            this.textBoxWeight.Name = "textBoxWeight";
            this.textBoxWeight.Size = new System.Drawing.Size(279, 20);
            this.textBoxWeight.TabIndex = 11;
            this.textBoxWeight.TextChanged += new System.EventHandler(this.textBoxWeight_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Strasse";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 91);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Strassennummer";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 117);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(27, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "PLZ";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 143);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(21, 13);
            this.label6.TabIndex = 15;
            this.label6.Text = "Ort";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 169);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(31, 13);
            this.label7.TabIndex = 16;
            this.label7.Text = "Land";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 195);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(83, 13);
            this.label8.TabIndex = 17;
            this.label8.Text = "Gewicht (in Kilo)";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 221);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(107, 13);
            this.label9.TabIndex = 18;
            this.label9.Text = "E-Mail (Darf leer sein)";
            this.label9.Click += new System.EventHandler(this.label9_Click);
            // 
            // printManualShippingLabel
            // 
            this.printManualShippingLabel.Location = new System.Drawing.Point(12, 277);
            this.printManualShippingLabel.Name = "printManualShippingLabel";
            this.printManualShippingLabel.Size = new System.Drawing.Size(473, 23);
            this.printManualShippingLabel.TabIndex = 19;
            this.printManualShippingLabel.Text = "Versandlabel mit manueller Eingabe drucken";
            this.printManualShippingLabel.UseVisualStyleBackColor = true;
            this.printManualShippingLabel.Visible = false;
            this.printManualShippingLabel.Click += new System.EventHandler(this.printManualShippingLabel_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(497, 341);
            this.Controls.Add(this.printManualShippingLabel);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxWeight);
            this.Controls.Add(this.textBoxMail);
            this.Controls.Add(this.textBoxCountry);
            this.Controls.Add(this.textBoxCity);
            this.Controls.Add(this.textBoxPLZ);
            this.Controls.Add(this.textBoxStreetNumber);
            this.Controls.Add(this.textBoxStreet);
            this.Controls.Add(this.textBoxRecepient);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxOrdernumber);
            this.Controls.Add(this.printShippingLabel);
            this.Name = "Form1";
            this.Text = "DHL-Seife";
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxRecepient;
        private System.Windows.Forms.TextBox textBoxStreet;
        private System.Windows.Forms.TextBox textBoxStreetNumber;
        private System.Windows.Forms.TextBox textBoxPLZ;
        private System.Windows.Forms.TextBox textBoxCity;
        private System.Windows.Forms.TextBox textBoxCountry;
        private System.Windows.Forms.TextBox textBoxMail;
        private System.Windows.Forms.TextBox textBoxWeight;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        public System.Windows.Forms.Button printShippingLabel;
        private System.Windows.Forms.TextBox textBoxOrdernumber;
        public System.Windows.Forms.Button printManualShippingLabel;
    }
}