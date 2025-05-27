namespace AtlantPrograma
{
    partial class Form8
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form8));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.действияСДокументамиToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.предварительныйПросмотрДокументовToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.просмотретьДокументыToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.скачатьВсеДокументыToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.сброситьИзмененияВДокументахToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(12, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Кому:";
            this.label1.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(12, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 25);
            this.label2.TabIndex = 4;
            this.label2.Text = "Тема:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(12, 125);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 25);
            this.label3.TabIndex = 6;
            this.label3.Text = "Приоритет:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(12, 188);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 25);
            this.label4.TabIndex = 8;
            this.label4.Text = "Текст:";
            // 
            // comboBox1
            // 
            this.comboBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(137, 45);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(326, 26);
            this.comboBox1.TabIndex = 9;
            this.comboBox1.Text = "Выберите кому отправить";
            this.comboBox1.Visible = false;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(137, 90);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(326, 22);
            this.textBox1.TabIndex = 10;
            // 
            // comboBox2
            // 
            this.comboBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(137, 124);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(326, 26);
            this.comboBox2.TabIndex = 11;
            this.comboBox2.Text = "Выберите приоритет";
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button1.Location = new System.Drawing.Point(846, 152);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(126, 58);
            this.button1.TabIndex = 12;
            this.button1.Text = "Отправить";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Segoe UI", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.button2.Location = new System.Drawing.Point(978, 152);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(126, 58);
            this.button2.TabIndex = 13;
            this.button2.Text = "Выход";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(0, 216);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(1104, 317);
            this.richTextBox1.TabIndex = 14;
            this.richTextBox1.Text = "";
            // 
            // comboBox3
            // 
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new System.Drawing.Point(575, 45);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(266, 24);
            this.comboBox3.TabIndex = 15;
            this.comboBox3.Text = "Пусто";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(486, 102);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(84, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 16;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.действияСДокументамиToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1104, 28);
            this.menuStrip1.TabIndex = 17;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // действияСДокументамиToolStripMenuItem
            // 
            this.действияСДокументамиToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.предварительныйПросмотрДокументовToolStripMenuItem,
            this.просмотретьДокументыToolStripMenuItem,
            this.скачатьВсеДокументыToolStripMenuItem,
            this.сброситьИзмененияВДокументахToolStripMenuItem,
            this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem});
            this.действияСДокументамиToolStripMenuItem.Name = "действияСДокументамиToolStripMenuItem";
            this.действияСДокументамиToolStripMenuItem.Size = new System.Drawing.Size(196, 24);
            this.действияСДокументамиToolStripMenuItem.Text = "Действия с документами";
            // 
            // предварительныйПросмотрДокументовToolStripMenuItem
            // 
            this.предварительныйПросмотрДокументовToolStripMenuItem.Name = "предварительныйПросмотрДокументовToolStripMenuItem";
            this.предварительныйПросмотрДокументовToolStripMenuItem.Size = new System.Drawing.Size(410, 26);
            this.предварительныйПросмотрДокументовToolStripMenuItem.Text = "Предварительный просмотр документов";
            this.предварительныйПросмотрДокументовToolStripMenuItem.Click += new System.EventHandler(this.предварительныйПросмотрДокументовToolStripMenuItem_Click_1);
            // 
            // просмотретьДокументыToolStripMenuItem
            // 
            this.просмотретьДокументыToolStripMenuItem.Name = "просмотретьДокументыToolStripMenuItem";
            this.просмотретьДокументыToolStripMenuItem.Size = new System.Drawing.Size(410, 26);
            this.просмотретьДокументыToolStripMenuItem.Text = "Просмотреть документы";
            // 
            // скачатьВсеДокументыToolStripMenuItem
            // 
            this.скачатьВсеДокументыToolStripMenuItem.Name = "скачатьВсеДокументыToolStripMenuItem";
            this.скачатьВсеДокументыToolStripMenuItem.Size = new System.Drawing.Size(410, 26);
            this.скачатьВсеДокументыToolStripMenuItem.Text = "Скачать все документы";
            // 
            // сброситьИзмененияВДокументахToolStripMenuItem
            // 
            this.сброситьИзмененияВДокументахToolStripMenuItem.Name = "сброситьИзмененияВДокументахToolStripMenuItem";
            this.сброситьИзмененияВДокументахToolStripMenuItem.Size = new System.Drawing.Size(410, 26);
            this.сброситьИзмененияВДокументахToolStripMenuItem.Text = "Сбросить изменения в документах";
            this.сброситьИзмененияВДокументахToolStripMenuItem.Click += new System.EventHandler(this.сброситьИзмененияВДокументахToolStripMenuItem_Click_1);
            // 
            // очиститьСписокПрикреплённыхСообщенийToolStripMenuItem
            // 
            this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Name = "очиститьСписокПрикреплённыхСообщенийToolStripMenuItem";
            this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Size = new System.Drawing.Size(410, 26);
            this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Text = "Очистить список прикреплённых документов";
            this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Click += new System.EventHandler(this.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem_Click_1);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.checkBox1.Location = new System.Drawing.Point(474, 156);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(106, 24);
            this.checkBox1.TabIndex = 18;
            this.checkBox1.Text = "Подписать";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // Form8
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1104, 529);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.comboBox3);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Form8";
            this.Text = "ОТПРАВИТЬ ПИСЬМО ВСЕМ";
            this.Load += new System.EventHandler(this.Form8_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.RichTextBox richTextBox1;
        public System.Windows.Forms.ComboBox comboBox3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        public System.Windows.Forms.ToolStripMenuItem действияСДокументамиToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem предварительныйПросмотрДокументовToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem просмотретьДокументыToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem скачатьВсеДокументыToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem сброситьИзмененияВДокументахToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem очиститьСписокПрикреплённыхСообщенийToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBox1;
    }
}