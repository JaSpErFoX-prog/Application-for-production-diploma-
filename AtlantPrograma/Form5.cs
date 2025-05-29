using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace AtlantPrograma
{
    public partial class Form5 : Form
    {
        private string connectionString = "server=localhost;database=document_system;uid=root;pwd=1111;";
        private bool isPasswordVisible = false; // Флаг видимости пароля
        public Form5()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string newPassword = textBox2.Text.Trim();
            string confirmPassword = textBox3.Text.Trim();

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка совпадения пароля и подтверждения пароля
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка допустимых символов
            if (!Regex.IsMatch(newPassword, @"^[a-zA-Z0-9!@#$%^&*()_+=\[{\]};:'"",<.>/?`~\\|-]+$"))
            {
                MessageBox.Show("Пароль должен содержать только английские буквы, цифры и допустимые символы!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Проверка существования пользователя
                    string checkUserQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    MySqlCommand checkCmd = new MySqlCommand(checkUserQuery, conn);
                    checkCmd.Parameters.AddWithValue("@username", username);

                    int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (userExists == 0)
                    {
                        MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Хэширование нового пароля
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                    // Обновление пароля в БД
                    string updateQuery = "UPDATE users SET password = @newPassword WHERE username = @username";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@newPassword", hashedPassword);
                    updateCmd.Parameters.AddWithValue("@username", username);

                    updateCmd.ExecuteNonQuery();

                    MessageBox.Show("Пароль успешно изменён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Возврат к форме авторизации
                    this.Close();
                    Form1 loginForm = new Form1();
                    loginForm.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            //string username = textBox1.Text.Trim();
            //string newPassword = textBox2.Text.Trim();
            //string confirmPassword = textBox3.Text.Trim();

            //// Проверка на пустые поля
            //if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            //{
            //    MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            //// Проверка совпадения пароля и подтверждения пароля
            //if (newPassword != confirmPassword)
            //{
            //    MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            //// Проверка, что пароль содержит только допустимые символы (английские буквы, цифры и спецсимволы)
            //if (!Regex.IsMatch(newPassword, @"^[a-zA-Z0-9!@#$%^&*()_+=\[{\]};:'"",<.>/?`~\\|-]+$"))
            //{
            //    MessageBox.Show("Пароль должен содержать только английские буквы, цифры и допустимые символы!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}


            //using (MySqlConnection conn = new MySqlConnection(connectionString))
            //{
            //    try
            //    {
            //        conn.Open();

            //        // Проверка, существует ли пользователь
            //        string checkUserQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
            //        MySqlCommand checkCmd = new MySqlCommand(checkUserQuery, conn);
            //        checkCmd.Parameters.AddWithValue("@username", username);

            //        int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());

            //        if (userExists == 0)
            //        {
            //            MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //            return;
            //        }

            //        // Обновление пароля
            //        string updateQuery = "UPDATE users SET password = @newPassword WHERE username = @username";
            //        MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
            //        updateCmd.Parameters.AddWithValue("@newPassword", newPassword);
            //        updateCmd.Parameters.AddWithValue("@username", username);

            //        updateCmd.ExecuteNonQuery();

            //        MessageBox.Show("Пароль успешно изменён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //        // Закрываем форму и возвращаемся к авторизации
            //        this.Close();
            //        Form1 loginForm = new Form1();
            //        loginForm.Show();
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            Form1 loginForm = new Form1();
            loginForm.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Переключаем режим отображения пароля
            isPasswordVisible = !isPasswordVisible;
            textBox2.UseSystemPasswordChar = !isPasswordVisible;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            // Переключаем режим отображения пароля
            isPasswordVisible = !isPasswordVisible;
            textBox3.UseSystemPasswordChar = !isPasswordVisible;
        }
    }
}
