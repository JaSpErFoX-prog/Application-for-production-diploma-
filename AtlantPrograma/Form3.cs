using System;
using System.Windows.Forms;

namespace AtlantPrograma
{
    public partial class Form3 : Form
    {
        private string currentUser; // Храним имя вошедшего пользователя

        public Form3(string username)
        {
            InitializeComponent();
            currentUser = username; // Получаем имя пользователя из Form1
        }
        private void выйтиИзСистемыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.Hide(); // Скрываем текущую форму
                Form1 loginForm = new Form1();
                loginForm.Show(); // Показываем форму авторизации
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form6 mailForm = new Form6(currentUser); // передаём имя пользователя
            mailForm.Show();
        }
    }
}
