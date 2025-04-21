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
    public partial class Form7 : Form
    {
        private string senderUsername;
        public Form7(string sender)
        {
            InitializeComponent();
            senderUsername = sender;
        }
        private bool isReadOnlyMode = false;
        private void button2_Click(object sender, EventArgs e)
        {
            if (isReadOnlyMode)
            {
                DialogResult result = MessageBox.Show(
                    "Вы точно хотите выйти из письма?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    this.Close();
                }
            }
            else
            {
                DialogResult result = MessageBox.Show(
                    "Вы точно хотите выйти из отправки письма?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    this.Close();
                }
            }
        }
        private List<string> allUsernames = new List<string>();
        private void Form7_Load(object sender, EventArgs e)
        {
            // Загружаем список пользователей (кому отправить)
            LoadRecipients();

            // Добавляем приоритеты для сотрудников
            comboBox2.Items.AddRange(new string[] { "Не срочно", "Обычное сообщение", "Срочно!" });
            comboBox2.SelectedIndex = 1; // по умолчанию "Обычное сообщение"
            comboBox1.Text = "Поиск...";
            comboBox1.ForeColor = Color.Gray;
            comboBox1.Enter += comboBox1_Enter;
            comboBox1.Leave += comboBox1_Leave;
            comboBox1.TextChanged += comboBox1_TextChanged;
        }
        private void comboBox1_Enter(object sender, EventArgs e)
        {
            if (comboBox1.Text == "Поиск...")
            {
                comboBox1.Text = "";
                comboBox1.ForeColor = Color.Black;
            }
        }

        private void comboBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(comboBox1.Text))
            {
                comboBox1.Text = "Поиск...";
                comboBox1.ForeColor = Color.Gray;
            }
        }
        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            string input = comboBox1.Text.Trim();

            if (input.Length == 0 || input == "Поиск...") return;

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

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null || string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля!");
                return;
            }

            string recipient = comboBox1.SelectedItem.ToString();
            string subject = textBox1.Text.Trim();
            string body = richTextBox1.Text.Trim();
            string priority = comboBox2.SelectedItem?.ToString() ?? "Обычный";
            string dateSent = DateTime.Now.ToString("dd.MM.yyyy"); // формат день.месяц.год
            string timeSent = DateTime.Now.ToString("HH:mm:ss");

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();
                    string query = "INSERT INTO messages (sender, recipient, subject, body, priority, date_sent, time_sent) " +
                                   "VALUES (@sender, @recipient, @subject, @body, @priority, @date, @time)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sender", senderUsername);
                    cmd.Parameters.AddWithValue("@recipient", recipient);
                    cmd.Parameters.AddWithValue("@subject", subject);
                    cmd.Parameters.AddWithValue("@body", body);
                    cmd.Parameters.AddWithValue("@priority", priority);
                    cmd.Parameters.AddWithValue("@date", dateSent);
                    cmd.Parameters.AddWithValue("@time", timeSent);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Письмо успешно отправлено!");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отправке письма: " + ex.Message);
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
            isReadOnlyMode = true;

            this.Text = "ПРОСМОТР СООБЩЕНИЯ";
        }
    }
}
