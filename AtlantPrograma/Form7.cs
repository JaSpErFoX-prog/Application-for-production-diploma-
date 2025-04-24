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
            comboBox1.Text = "Поиск...";
            comboBox1.ForeColor = Color.Gray;
            LoadRecipients();
            comboBox2.Items.AddRange(new string[] { "Не срочно", "Обычное сообщение", "Срочно!" });
            comboBox2.SelectedIndex = 1; // по умолчанию "Обычное сообщение"
            comboBox1.Enter += comboBox1_Enter;
            comboBox1.Leave += comboBox1_Leave;
            comboBox1.TextChanged += comboBox1_TextChanged;
        }

        private bool isReadOnlyMode = false;
        private void button2_Click(object sender, EventArgs e)
        {
            if (!isReadOnlyMode)
            {
                bool hasRecipient = comboBox1.Text != "Поиск..." && !string.IsNullOrWhiteSpace(comboBox1.Text);
                bool hasSubject = !string.IsNullOrWhiteSpace(textBox1.Text);
                bool hasBody = !string.IsNullOrWhiteSpace(richTextBox1.Text);

                // Если нет получателя, но есть тема/текст — предлагать сохранить
                if (!hasRecipient && (hasSubject || hasBody))
                {
                    DialogResult saveDraft = MessageBox.Show(
                        "Вы не указали получателя. Сохранить как черновик?",
                        "Сохранение черновика",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (saveDraft == DialogResult.Yes)
                    {
                        SaveDraft();

                        MessageBox.Show("Черновик сохранён успешно!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Найдём открытую форму Form6 и вызовем у неё метод
                        Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        form6?.LoadDraftMessages();

                        this.Close();
                    }
                    else if (saveDraft == DialogResult.No)
                    {
                        this.Close();
                    }
                }
                else if (hasRecipient && hasSubject && hasBody)
                {
                    DialogResult saveDraft = MessageBox.Show(
                        "Вы не отправили письмо. Сохранить как черновик?",
                        "Сохранение черновика",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (saveDraft == DialogResult.Yes)
                    {
                        SaveDraft();

                        MessageBox.Show("Черновик сохранён успешно!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        form6?.LoadDraftMessages();

                        this.Close();
                    }
                    else if (saveDraft == DialogResult.No)
                    {
                        this.Close();
                    }
                }
                else
                {
                    this.Close();
                }
            }
        }
        private void SaveDraft()
        {
            string recipient = comboBox1.SelectedItem?.ToString() ?? "";
            string subject = textBox1.Text.Trim();
            string body = richTextBox1.Text.Trim();
            string priority = comboBox2.SelectedItem?.ToString() ?? "Обычное сообщение";
            string dateCreated = DateTime.Now.ToString("dd.MM.yyyy");
            string timeCreated = DateTime.Now.ToString("HH:mm:ss");

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();
                    string query = "INSERT INTO drafts (sender, recipient, subject, body, priority, date_created, time_created) " +
                                   "VALUES (@sender, @recipient, @subject, @body, @priority, @date, @time)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sender", senderUsername);
                    cmd.Parameters.AddWithValue("@recipient", recipient);
                    cmd.Parameters.AddWithValue("@subject", subject);
                    cmd.Parameters.AddWithValue("@body", body);
                    cmd.Parameters.AddWithValue("@priority", priority);
                    cmd.Parameters.AddWithValue("@date", dateCreated);
                    cmd.Parameters.AddWithValue("@time", timeCreated);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении черновика: " + ex.Message);
            }
        }

        private List<string> allUsernames = new List<string>();
        private void Form7_Load(object sender, EventArgs e)
        {
            // Загружаем список пользователей (кому отправить)
            //LoadRecipients();

            // Добавляем приоритеты для сотрудников
            //comboBox2.Items.AddRange(new string[] { "Не срочно", "Обычное сообщение", "Срочно!" });
            //comboBox2.SelectedIndex = 1; // по умолчанию "Обычное сообщение"
            //comboBox1.Text = "Поиск...";
            //comboBox1.ForeColor = Color.Gray;
            //comboBox1.Enter += comboBox1_Enter;
            //comboBox1.Leave += comboBox1_Leave;
            //comboBox1.TextChanged += comboBox1_TextChanged;
        }
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
        public void LoadDraftForEditing(int draftId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();
                    string query = @"
SELECT recipient, subject, priority, body
FROM drafts
WHERE id = @draftId";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@draftId", draftId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string recipient = reader.IsDBNull(reader.GetOrdinal("recipient")) ? "" : reader.GetString("recipient");
                            string subject = reader.IsDBNull(reader.GetOrdinal("subject")) ? "" : reader.GetString("subject");
                            string priority = reader.IsDBNull(reader.GetOrdinal("priority")) ? "" : reader.GetString("priority");
                            string body = reader.IsDBNull(reader.GetOrdinal("body")) ? "" : reader.GetString("body");

                            // Заполняем поля черновика
                            // Проверяем, существует ли получатель в comboBox1
                            if (comboBox1.Items.Contains(recipient))
                            {
                                comboBox1.SelectedItem = recipient;  // Устанавливаем получателя
                            }
                            else
                            {
                                comboBox1.Text = recipient;  // Если получатель не найден в списке, ставим как текст
                            }
                            comboBox1.ForeColor = Color.Black; // <-- сброс цвета на чёрный
                            textBox1.Text = subject;              // Тема
                            richTextBox1.Text = body;             // Текст письма

                            // Для приоритета
                            if (!string.IsNullOrEmpty(priority))
                            {
                                // Проверим, существует ли этот приоритет в comboBox2
                                if (comboBox2.Items.Contains(priority))
                                {
                                    comboBox2.SelectedItem = priority;
                                }
                                else
                                {
                                    // Если приоритет не найден, установим по умолчанию
                                    comboBox2.SelectedItem = "Обычное сообщение";
                                }
                            }
                            else
                            {
                                comboBox2.SelectedItem = null; // Если приоритет пустой
                            }
                        }
                        else
                        {
                            MessageBox.Show("Черновик не найден");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке черновика: " + ex.Message);
            }
        }
    }
}
