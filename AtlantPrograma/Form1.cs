using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace AtlantPrograma
{
    public partial class Form1 : Form
    {
        private string connectionString = "server=localhost;database=document_system;uid=root;pwd=1111;";
        private string currentUser = ""; // Переменная для хранения текущего пользователя
        private bool isPasswordVisible = false; // Флаг видимости пароля
        public Form1()
        {
            InitializeComponent();
        }
        

        // Кнопка "Выход"
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Переход на регистрацию
        private void label5_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form2 regForm = new Form2();
            regForm.Show();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите имя пользователя и пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Запрос на проверку логина и пароля
                    string query = "SELECT role FROM users WHERE username = @username AND password = @password";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    object role = cmd.ExecuteScalar();

                    if (role == null)
                    {
                        MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Запоминаем пользователя
                    currentUser = username;

                    // Открываем нужную форму в зависимости от роли
                    if (role.ToString() == "admin")
                    {
                        Form4 adminForm = new Form4(currentUser); // Передаём имя пользователя
                        adminForm.Show();
                    }
                    else
                    {
                        Form3 userForm = new Form3(currentUser); // Передаём имя пользователя
                        userForm.Show();
                    }

                    // Скрываем текущую форму
                    this.Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form5 resetForm = new Form5();
            resetForm.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Переключаем режим отображения пароля
            isPasswordVisible = !isPasswordVisible;
            textBox2.UseSystemPasswordChar = !isPasswordVisible;
        }
    }
}