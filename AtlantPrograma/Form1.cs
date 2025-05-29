using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto.Generators;

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

            //GenerateAdminSql();
            //GenerateUsersSql();
        }

    //    public static void GenerateUsersSql()
    //    {
    //        var users = new[]
    //        {
    //    new { Id = 6, Name = "Игорь", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
    //    new { Id = 13, Name = "Алла", Password = "1111", Role = "user", Phone = "3-96, 4-29", DepartmentId = 2 },
    //    new { Id = 18, Name = "Васильев Денис Олегович", Password = "1111", Role = "user", Phone = "6-11, 5-50", DepartmentId = 24 },
    //    new { Id = 19, Name = "Новикова Ольга Сергеевна", Password = "1111", Role = "user", Phone = "2-82, 3-82", DepartmentId = 36 },
    //    new { Id = 20, Name = "Фёдоров Артём Александрович", Password = "1111", Role = "user", Phone = "2-82, 3-82", DepartmentId = 36 },
    //    new { Id = 24, Name = "Соколов Павел Евгеньевич", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
    //    new { Id = 25, Name = "Волкова Мария Алексеевна", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
    //    new { Id = 29, Name = "Григорьева Виктория Романовна", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
    //    new { Id = 33, Name = "Белова Алина Геннадьевна", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
    //    new { Id = 35, Name = "Даник", Password = "2222", Role = "user", Phone = "4-04, 4-41", DepartmentId = 2 }
    //};

    //        Console.WriteLine("SQL для вставки пользователей и их данных:\n");

    //        foreach (var user in users)
    //        {
    //            string hash = BCrypt.Net.BCrypt.HashPassword(user.Password);

    //            string sqlUser = $"INSERT INTO users (id, username, password, role) VALUES " +
    //                             $"({user.Id}, '{user.Name}', '{hash}', '{user.Role}') " +
    //                             $"ON DUPLICATE KEY UPDATE username=username;";
    //            Console.WriteLine(sqlUser);

    //            string sqlDetails = $"INSERT INTO user_details (user_id, phone, department_id) VALUES " +
    //                                $"({user.Id}, '{user.Phone}', {user.DepartmentId}) " +
    //                                $"ON DUPLICATE KEY UPDATE phone=VALUES(phone), department_id=VALUES(department_id);";
    //            Console.WriteLine(sqlDetails);
    //            Console.WriteLine();
    //        }

    //        Console.WriteLine("Нажмите любую клавишу для выхода...");
    //        Console.Read();
    //    }


    //    public static void GenerateAdminSql()
    //    {
    //        string username = "admin";
    //        string password = "admin";
    //        string role = "admin";

    //        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

    //        string sql = $"INSERT INTO users (id, username, password, role) VALUES " +
    //                     $"(1, '{username}', '{hashedPassword}', '{role}') " +
    //                     $"ON DUPLICATE KEY UPDATE username=username;";

    //        Console.WriteLine("SQL для вставки администратора:\n");
    //        Console.WriteLine(sql);
    //        Console.WriteLine("\nНажмите любую клавишу для выхода...");
    //        Console.Read();
    //    }


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

                    // Получаем хэш пароля и роль пользователя по логину
                    string query = "SELECT password, role FROM users WHERE username = @username";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hashedPasswordFromDb = reader.GetString("password");
                            string role = reader.GetString("role");

                            // Сравниваем введённый пароль с хэшом
                            if (BCrypt.Net.BCrypt.Verify(password, hashedPasswordFromDb))
                            {
                                // Запоминаем пользователя
                                currentUser = username;

                                // Открываем нужную форму в зависимости от роли
                                if (role == "admin")
                                {
                                    Form4 adminForm = new Form4(currentUser);
                                    adminForm.Show();
                                }
                                else
                                {
                                    Form3 userForm = new Form3(currentUser);
                                    userForm.Show();
                                }

                                this.Hide(); // Скрыть форму логина
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            //string username = textBox1.Text.Trim();
            //string password = textBox2.Text.Trim();

            //// Проверка на пустые поля
            //if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            //{
            //    MessageBox.Show("Введите имя пользователя и пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}

            //using (MySqlConnection conn = new MySqlConnection(connectionString))
            //{
            //    try
            //    {
            //        conn.Open();

            //        // Запрос на проверку логина и пароля
            //        string query = "SELECT role FROM users WHERE username = @username AND password = @password";
            //        MySqlCommand cmd = new MySqlCommand(query, conn);
            //        cmd.Parameters.AddWithValue("@username", username);
            //        cmd.Parameters.AddWithValue("@password", password);

            //        object role = cmd.ExecuteScalar();

            //        if (role == null)
            //        {
            //            MessageBox.Show("Неверный логин или пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //            return;
            //        }

            //        // Запоминаем пользователя
            //        currentUser = username;

            //        // Открываем нужную форму в зависимости от роли
            //        if (role.ToString() == "admin")
            //        {
            //            Form4 adminForm = new Form4(currentUser); // Передаём имя пользователя
            //            adminForm.Show();
            //        }
            //        else
            //        {
            //            Form3 userForm = new Form3(currentUser); // Передаём имя пользователя
            //            userForm.Show();
            //        }

            //        // Скрываем текущую форму
            //        this.Hide();
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
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

        private void Form1_Load(object sender, EventArgs e)
        {
            //GenerateUsersSql();
            //GenerateAdminSql();
        }
    }
}