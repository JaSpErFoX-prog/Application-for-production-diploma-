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
            //comboBox1.Enter += comboBox1_Enter;
            //comboBox1.Leave += comboBox1_Leave;
            //comboBox1.TextChanged += comboBox1_TextChanged;
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

                        MessageBox.Show("Черновик сохранён успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Найдём открытую форму Form6 и вызовем у неё метод
                        //Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        //form6?.LoadDraftMessages();

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

                        if (replyingToMessageId.HasValue)
                        {
                            // Отметим как прочитанное (или удалим, если нужно скрыть)
                            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                            {
                                conn.Open();

                                string updateQuery = "UPDATE messages SET is_draft = 1 WHERE id = @id";
                                using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", replyingToMessageId.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            MessageBox.Show("Черновик сохранён успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Обновим входящие
                            Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                            form6?.LoadIncomingMessages(); // 🔁 здесь вставь свой метод обновления
                            form6?.ShowNotificationCount();
                            //Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                            //form6?.LoadDraftMessages();

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
           comboBox1.Enter += comboBox1_Enter;
            comboBox1.Leave += comboBox1_Leave;
            comboBox1.TextChanged += comboBox1_TextChanged;
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
        private int? openedDraftId = null;

        private int? replyingToMessageId = null; // добавляем поле для хранения ID письма

        //private bool isDraftMode = false;  // Флаг, который будет указывать, что мы работаем с черновиками
        private int replyTextStartIndex = 0;

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null || string.IsNullOrWhiteSpace(textBox1.Text) || string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string recipient = comboBox1.SelectedItem.ToString();
            string subject = textBox1.Text.Trim();
            string priority = comboBox2.SelectedItem?.ToString() ?? "Обычное сообщение";
            string dateSent = DateTime.Now.ToString("dd.MM.yyyy");
            string timeSent = DateTime.Now.ToString("HH:mm:ss");
            string body;

            if (replyingToMessageId.HasValue)
            {
                string currentText = richTextBox1.Text;

                // Найдём, где начинается история
                int historyIndex = currentText.IndexOf("--------------------------------------");

                // Всё до начала истории — это новый ответ
                string newAnswer = historyIndex >= 0
                    ? currentText.Substring(0, historyIndex).Trim()
                    : currentText.Trim(); // если пользователь ещё не вставил историю

                if (string.IsNullOrWhiteSpace(newAnswer))
                {
                    MessageBox.Show("Пожалуйста, введите текст ответа перед отправкой!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Сохраняем всё, как есть — с новым текстом и историей
                body = currentText.Trim();
            }
            else
            {
                body = richTextBox1.Text.Trim();
            }
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    string query = "INSERT INTO messages (sender, recipient, subject, body, priority, date_sent, time_sent) " +
                                   "VALUES (@sender, @recipient, @subject, @body, @priority, @date, @time)";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@sender", senderUsername);
                        cmd.Parameters.AddWithValue("@recipient", recipient);
                        cmd.Parameters.AddWithValue("@subject", subject);
                        cmd.Parameters.AddWithValue("@body", body);
                        cmd.Parameters.AddWithValue("@priority", priority);
                        cmd.Parameters.AddWithValue("@date", dateSent);
                        cmd.Parameters.AddWithValue("@time", timeSent);

                        cmd.ExecuteNonQuery();
                    }

                    if (replyingToMessageId.HasValue)
                    {
                        string updateQuery = "UPDATE messages SET is_read = 1 WHERE id = @id";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", replyingToMessageId.Value);
                            updateCmd.ExecuteNonQuery();
                        }
                    }

                    if (openedDraftId.HasValue)
                    {
                        string updateDraftQuery = "UPDATE drafts SET is_sent = 1 WHERE id = @id";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateDraftQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", openedDraftId.Value);
                            updateCmd.ExecuteNonQuery();
                        }
                        Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        form6?.LoadDraftMessages();
                    }

                    MessageBox.Show("Письмо успешно отправлено!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Form6 form6Notif = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                    form6Notif?.ShowNotificationCount();
                    form6Notif?.LoadIncomingMessages();

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
            //isDraftMode = true;
            this.openedDraftId = draftId;
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
                                comboBox1.SelectedItem = recipient;
                                comboBox1.ForeColor = Color.Black;// Устанавливаем получателя
                            }                           
                            else if (comboBox1.Text=="Поиск...")
                            {
                                comboBox1.ForeColor = Color.Gray;
                                //comboBox1.Enter += comboBox1_Enter;
                                //comboBox1.Leave += comboBox1_Leave;
                                //comboBox1.TextChanged += comboBox1_TextChanged;
                            }
                            else
                            {
                                comboBox1.Text = recipient;
                                //comboBox1.ForeColor = Color.Black;// Если получатель не найден в списке, ставим как текст
                            }
                            //comboBox1.ForeColor = Color.Black; // <-- сброс цвета на чёрный
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
                            MessageBox.Show("Черновик не найден", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке черновика: " + ex.Message);
            }
        }

        //string originalText; // сделаем это полем класса, чтобы доступно было другим методам
        public void SetReplyMode(string recipient, string subject, string originalBody, string originalSender, string originalDate, string originalTime, int messageId)
        {
            replyingToMessageId = messageId;

            if (comboBox1.Items.Contains(recipient))
            {
                comboBox1.SelectedItem = recipient;
                comboBox1.ForeColor = Color.Black;
            }
            else
            {
                comboBox1.Text = recipient;
            }

            textBox1.Text = subject;

            string formattedHistory =
                "\n\n" +
                "--------------------------------------\n" +
                $"Отправитель: {originalSender}\n" +
                $"Дата: {originalDate} Время: {originalTime}\n" +
                $"Тема: {subject}\n" +
                "Текст:\n" +
                originalBody.Trim() + "\n" +
                "--------------------------------------\n";

            // Текст для редактирования сверху, история — снизу
            richTextBox1.Text = "" + formattedHistory;

            // Курсор ставим в самое начало
            richTextBox1.SelectionStart = 0;
            richTextBox1.ScrollToCaret();
        }
    }
}
