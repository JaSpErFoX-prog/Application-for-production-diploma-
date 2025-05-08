using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtlantPrograma
{
    public partial class Form8 : Form
    {
        List<string> selectedDepartments;
        private string senderUsername;
        public Form8(string sender, List<string> departments)
        {
            InitializeComponent();
            senderUsername = sender;
            selectedDepartments = departments;
            comboBox1.Text = "Поиск...";
            comboBox1.ForeColor = Color.Gray;
            LoadRecipients();
            comboBox2.Items.AddRange(new string[] { "Не срочно", "Обычное сообщение", "Срочно!" });
            comboBox2.SelectedIndex = 1; // по умолчанию "Обычное сообщение"
        }

        private void Form8_Load(object sender, EventArgs e)
        {
            comboBox1.Enter += comboBox1_Enter;
            comboBox1.Leave += comboBox1_Leave;
            comboBox1.TextChanged += comboBox1_TextChanged;
        }

        private List<string> allUsernames = new List<string>();

        private void comboBox1_Enter(object sender, EventArgs e)
        {
            // Убираем подсказку только если она действительно стоит
            if (comboBox1.Text == "Поиск..." && comboBox1.ForeColor == Color.Gray)
            {
                comboBox1.Text = "";
                comboBox1.ForeColor = Color.Black;
            }
        }

        private void comboBox1_Leave(object sender, EventArgs e)
        {
            // Если поле пустое и не выбрано значение из списка — показываем подсказку
            if (string.IsNullOrWhiteSpace(comboBox1.Text) || comboBox1.SelectedItem == null)
            {
                comboBox1.Text = "Поиск...";
                comboBox1.ForeColor = Color.Gray;
            }
            else
            {
                // Обеспечиваем, что при наличии текста он чёрный
                comboBox1.ForeColor = Color.Black;
            }
        }
        private int previousTextLength = 0;
        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            string input = comboBox1.Text.Trim();

            if (input.Length == 0 || input == "Поиск...")
            {
                previousTextLength = input.Length;
                return;
            }

            // Только если текст стал длиннее — делаем автодополнение
            if (input.Length > previousTextLength)
            {
                string match = allUsernames.FirstOrDefault(u => u.StartsWith(input, StringComparison.OrdinalIgnoreCase));

                if (match != null && comboBox1.Focused)
                {
                    // Временно отписываемся от события, чтобы избежать зацикливания
                    comboBox1.TextChanged -= comboBox1_TextChanged;
                    int selectionStart = input.Length;
                    comboBox1.Text = match;
                    comboBox1.SelectionStart = selectionStart;
                    comboBox1.SelectionLength = match.Length - input.Length;
                    comboBox1.TextChanged += comboBox1_TextChanged;
                }
            }

            previousTextLength = input.Length; // Обновляем длину текста
        }
        private void LoadRecipients()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();
                    string query = "SELECT username FROM users WHERE role != 'admin' AND username != @currentUser";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@currentUser", senderUsername);

                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string username = reader.GetString("username");
                        allUsernames.Add(username);
                        comboBox1.Items.Add(username);
                    }
                }

                //comboBox1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                //comboBox1.AutoCompleteSource = AutoCompleteSource.CustomSource;
                var autoSource = new AutoCompleteStringCollection();
                autoSource.AddRange(allUsernames.ToArray());
                comboBox1.AutoCompleteCustomSource = autoSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке пользователей: " + ex.Message);
            }
        }

        public void LoadReadOnlyMessage(string subject, string body, string senderUsername)
        {
            textBox1.Text = subject;
            richTextBox1.Text = body;

            textBox1.ReadOnly = true;
            richTextBox1.ReadOnly = true;
            comboBox1.Visible = false; // скрываем выпадающий список получателей
            label1.Text = $"От: {senderUsername}"; // изменяем метку на "От:"

            comboBox2.Enabled = false;
            button1.Enabled = false;

            button2.Text = "Закрыть"; // изменяем текст кнопки выхода
            //isReadOnlyMode = true;

            this.Text = "ПРОСМОТР СООБЩЕНИЯ";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string subject = textBox1.Text.Trim();
            string body = richTextBox1.Text.Trim();
            string priority = comboBox2.SelectedItem.ToString();

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            {
                MessageBox.Show("Пожалуйста, заполните тему и текст для отправки письма!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Собираем всех получателей по всем отделам
            List<string> allRecipients = new List<string>();
            string senderPhone = "";
            string senderDept = "";
            string date = DateTime.Now.ToString("dd.MM.yyyy");
            string time = DateTime.Now.ToString("HH:mm:ss");

            using (var con = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                con.Open();

                // Получаем ID и сотрудников по каждому отделу
                foreach (string departmentName in selectedDepartments)
                {
                    int departmentId = -1;
                    using (var cmd = new MySqlCommand("SELECT id FROM departments WHERE name = @name", con))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@name", departmentName);
                        var result = cmd.ExecuteScalar();
                        if (result != null) departmentId = Convert.ToInt32(result);
                    }

                    if (departmentId == -1) continue; // Пропустить, если отдел не найден

                    using (var cmd = new MySqlCommand(
                        @"SELECT u.username FROM users u 
                  JOIN user_details d ON u.id = d.user_id 
                  WHERE d.department_id = @deptId AND u.username != @sender", con))
                    {
                        cmd.Parameters.AddWithValue("@deptId", departmentId);
                        cmd.Parameters.AddWithValue("@sender", senderUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string recipient = reader.GetString(0);
                                if (!allRecipients.Contains(recipient)) // избегаем дубликатов
                                    allRecipients.Add(recipient);
                            }
                        }
                    }
                }

                if (allRecipients.Count == 0)
                {
                    MessageBox.Show("Нет доступных получателей в выбранных отделах", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Получаем доп. инфо об отправителе (телефон и отдел)
                using (var cmd = new MySqlCommand(
                    @"SELECT d.department_id, d.phone 
              FROM user_details d 
              JOIN users u ON u.id = d.user_id 
              WHERE u.username = @sender", con))
                {
                    cmd.Parameters.AddWithValue("@sender", senderUsername);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            senderPhone = reader["phone"].ToString();

                            // При желании можно определить отдел отправителя по ID:
                            int deptId = Convert.ToInt32(reader["department_id"]);
                            senderDept = selectedDepartments.FirstOrDefault(); // или можно использовать любое значение
                        }
                    }
                }

                // Вставляем сообщение для каждого получателя
                foreach (string recipient in allRecipients)
                {
                    using (var cmd = new MySqlCommand(
                        @"INSERT INTO messages 
                (sender, recipient, subject, body, priority, date_sent, time_sent, sender_department, sender_phone, is_sent) 
                VALUES (@s, @r, @subj, @body, @prio, @date, @time, @dept, @phone, 1)", con))
                    {
                        cmd.Parameters.AddWithValue("@s", senderUsername);
                        cmd.Parameters.AddWithValue("@r", recipient);
                        cmd.Parameters.AddWithValue("@subj", subject);
                        cmd.Parameters.AddWithValue("@body", body);
                        cmd.Parameters.AddWithValue("@prio", priority);
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@time", time);
                        cmd.Parameters.AddWithValue("@dept", senderDept);
                        cmd.Parameters.AddWithValue("@phone", senderPhone);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Письмо успешно отправлено сотрудникам выбранных отделов!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var res = MessageBox.Show("Вы уверены, что хотите выйти из отправки письма всем?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
                this.Close();
        }
    }
}
