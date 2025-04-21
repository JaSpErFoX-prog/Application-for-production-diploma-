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
    public partial class Form2 : Form
    {
        private string connectionString = "server=localhost;database=document_system;uid=root;pwd=1111;";
        private bool isPasswordVisible = false; // Флаг видимости пароля

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            LoadDepartments(); // Загрузка списка отделов в comboBox1
        }

        private void LoadDepartments()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Запрос для получения всех отделов
                    string query = "SELECT id, name, phones FROM departments"; // Исправил поле с phone на phones
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    // Очистить ComboBox перед загрузкой новых данных
                    comboBox1.Items.Clear();

                    while (reader.Read())
                    {
                        string departmentName = reader.GetString("name");
                        comboBox1.Items.Add(departmentName); // Добавление отделов в ComboBox
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Получаем выбранный отдел и отображаем номер телефона
            string selectedDepartment = comboBox1.SelectedItem?.ToString();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    // Запрос для получения телефона по выбранному отделу
                    string query = "SELECT phones FROM departments WHERE name = @department_name"; // Исправил поле с phone на phones
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@department_name", selectedDepartment);
                    string phone = cmd.ExecuteScalar()?.ToString(); // Получаем телефон

                    // Если телефона нет, выводим "Телефон не найден"
                    textBox4.Text = string.IsNullOrEmpty(phone) ? "Телефон не найден" : phone;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при получении данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();
            string confirmPassword = textBox3.Text.Trim();
            string department = comboBox1.SelectedItem?.ToString();
            string phone = textBox4.Text.Trim() == "Телефон не найден" ? null : textBox4.Text.Trim(); // Проверка на телефон

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(department))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка длины пароля
            if (password.Length > 20)
            {
                MessageBox.Show("Пароль не должен превышать 20 символов!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка совпадения паролей
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверка на использование только английских символов
            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9!@#$%^&*()_+=-]+$"))
            {
                MessageBox.Show("Пароль должен содержать только английские буквы, цифры и символы!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Проверка, есть ли уже такой пользователь
                    string checkUserQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    MySqlCommand checkUserCmd = new MySqlCommand(checkUserQuery, conn);
                    checkUserCmd.Parameters.AddWithValue("@username", username);
                    int userExists = Convert.ToInt32(checkUserCmd.ExecuteScalar());

                    if (userExists > 0)
                    {
                        // Проверка на отдел
                        string checkDepartmentQuery = "SELECT d.name FROM users u " +
                                                      "JOIN user_details ud ON u.id = ud.user_id " +
                                                      "JOIN departments d ON ud.department_id = d.id " +
                                                      "WHERE u.username = @username";
                        MySqlCommand checkDepartmentCmd = new MySqlCommand(checkDepartmentQuery, conn);
                        checkDepartmentCmd.Parameters.AddWithValue("@username", username);
                        string existingDepartment = checkDepartmentCmd.ExecuteScalar()?.ToString();

                        if (existingDepartment != department)
                        {
                            MessageBox.Show("Пользователь с таким именем уже зарегистрирован в другом отделе. Регистрация одинакового пользователя с разными отделами невозможна!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        MessageBox.Show("Такой пользователь уже существует!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Добавление нового пользователя
                    string insertUserQuery = "INSERT INTO users (username, password, role) VALUES (@username, @password, 'user')";
                    MySqlCommand insertUserCmd = new MySqlCommand(insertUserQuery, conn);
                    insertUserCmd.Parameters.AddWithValue("@username", username);
                    insertUserCmd.Parameters.AddWithValue("@password", password);
                    insertUserCmd.ExecuteNonQuery();

                    // Получаем id нового пользователя
                    int userId = (int)insertUserCmd.LastInsertedId;

                    // Добавление данных о пользователе в user_details
                    string insertUserDetailsQuery = "INSERT INTO user_details (user_id, phone, department_id) VALUES (@user_id, @phone, (SELECT id FROM departments WHERE name = @department_name))";
                    MySqlCommand insertUserDetailsCmd = new MySqlCommand(insertUserDetailsQuery, conn);
                    insertUserDetailsCmd.Parameters.AddWithValue("@user_id", userId);
                    insertUserDetailsCmd.Parameters.AddWithValue("@phone", phone);
                    insertUserDetailsCmd.Parameters.AddWithValue("@department_name", department);
                    insertUserDetailsCmd.ExecuteNonQuery();

                    MessageBox.Show($"Пользователь {username} успешно зарегистрирован!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Переход на форму авторизации
                    this.Hide();
                    Form1 loginForm = new Form1();
                    loginForm.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void label5_Click(object sender, EventArgs e)
        {
            this.Hide();
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
