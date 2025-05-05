using MySql.Data.MySqlClient;
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

        bool isReplyRead = false;
        private void button2_Click(object sender, EventArgs e)
        {
            if (!isReadOnlyMode)
            {
                bool hasRecipient = comboBox1.Text != "Поиск..." && !string.IsNullOrWhiteSpace(comboBox1.Text);
                bool hasSubject = !string.IsNullOrWhiteSpace(textBox1.Text);
                bool hasBody = !string.IsNullOrWhiteSpace(richTextBox1.Text);

                string currentRecipient = comboBox1.Text.Trim();
                string currentSubject = textBox1.Text.Trim();
                string currentBody = richTextBox1.Text.Trim();
                // Если редактируется существующий черновик
                if (openedDraftId != null)
                {
                    bool hasChanges = false;

                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                        {
                            conn.Open();
                            string query = "SELECT recipient, subject, body FROM drafts WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", openedDraftId.Value);
                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        string dbRecipient = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                                        string dbSubject = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                                        string dbBody = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();

                                        if (dbRecipient != currentRecipient ||
                                            dbSubject != currentSubject ||
                                            dbBody != currentBody)
                                        {
                                            hasChanges = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при проверке изменений черновика: " + ex.Message);
                    }

                    if (hasChanges)
                    {
                        DialogResult result = MessageBox.Show(
                            "Вы внесли изменения в черновик. Сохранить их?",
                            "Сохранение изменений",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            UpdateDraft(); // Ниже покажу этот метод
                            MessageBox.Show("Изменения сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                        else if (result == DialogResult.No)
                        {
                            this.Close();
                        }
                        // Cancel — ничего не делаем
                    }
                    else
                    {
                        // Без изменений — просто выходим
                        this.Close();
                    }

                    return;
                }
                else
                {
                    this.Close();
                }
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
                        MessageBox.Show("Черновик сохранён успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        if (replyingToMessageId.HasValue)
                        {
                            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                            {
                                conn.Open();

                                string checkQuery = "SELECT is_read FROM messages WHERE id = @id";
                                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                                {
                                    checkCmd.Parameters.AddWithValue("@id", replyingToMessageId.Value);
                                    object result = checkCmd.ExecuteScalar();
                                    if (result != null && Convert.ToInt32(result) == 1)
                                    {
                                        isReplyRead = true;
                                    }
                                }

                                string updateQuery = "UPDATE messages SET is_draft = 1 WHERE id = @id";
                                using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", replyingToMessageId.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                            if (form6 != null)
                            {
                                if (isReplyRead)
                                    form6.LoadReadMessages();
                                else
                                {
                                    form6.LoadIncomingMessages();
                                    form6.ShowNotificationCount();
                                }
                            }
                        }
                        this.Close(); // ← Закрываем форму только один раз здесь!
                    }
                    else if (saveDraft == DialogResult.No)
                    {
                        this.Close();
                    }
                }
            }
            else
            {
                this.Close(); // Закрыть форму в режиме только чтения
            }
        }

        private void UpdateDraft()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    string query = @"
                UPDATE drafts
                SET recipient = @recipient,
                    subject = @subject,
                    body = @body,
                    priority = @priority,
                    date_created = @date,
                    time_created = @time
                WHERE id = @id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@recipient", comboBox1.Text.Trim());
                        cmd.Parameters.AddWithValue("@subject", textBox1.Text.Trim());
                        cmd.Parameters.AddWithValue("@body", richTextBox1.Text.Trim());
                        cmd.Parameters.AddWithValue("@priority", comboBox2.Text); // Можно проверить на null
                        cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("dd.MM.yyyy"));
                        cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@id", openedDraftId.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении черновика: " + ex.Message);
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
        //private int replyTextStartIndex = 0;

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
                long insertedMessageId;
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
                        insertedMessageId = cmd.LastInsertedId; // Вот здесь мы получаем ID
                    }               

                    bool isFromRead = false;

                    if (replyingToMessageId.HasValue)
                    {
                        // Получаем значение is_read по ID
                        string checkReadQuery = "SELECT is_read FROM messages WHERE id = @id";
                        using (MySqlCommand checkCmd = new MySqlCommand(checkReadQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@id", insertedMessageId);
                            object result = checkCmd.ExecuteScalar();
                            if (result != null && Convert.ToBoolean(result))
                            {
                                isFromRead = true;
                            }
                        }

                        // Обновляем is_sent, но уже сделали выше
                        string updateQuery = "UPDATE messages SET is_sent = 1 WHERE id = @id";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", insertedMessageId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string updateSentQuery = "UPDATE messages SET is_sent = 1 WHERE id = @id";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateSentQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", insertedMessageId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    bool isFromDraft = false;

                    if (openedDraftId.HasValue)
                    {
                        isFromDraft = true;
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

                    if (isFromDraft)
                    {
                        // Ничего не обновляем дополнительно — уже обновили LoadDraftMessages()
                    }
                    else if (isFromRead)
                    {
                        form6Notif?.LoadReadMessages(); // если из Прочитанных
                    }
                    //else if (insertedMessageId!=0)
                    //{

                    //}
                    else
                    {
                        form6Notif?.ShowNotificationCount();
                        form6Notif?.LoadIncomingMessages(); // если из Входящих
                    }

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
            // Курсор ставим в самое начало
            //richTextBox1.SelectionStart = 0;
            //richTextBox1.ScrollToCaret();
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
                                                                  // Проверяем на наличие пунктирной линии где угодно
                            //string pattern = @"^\s*-{3,}\s*$"; // строка из 3 и более дефисов
                            string[] lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                            bool foundDashedLine = false;

                            for (int i = 0; i < lines.Length; i++)
                            {
                                string trimmedLine = lines[i].Trim(); // убираем пробелы и невидимые символы

                                if (trimmedLine.All(c => c == '-') && trimmedLine.Length >= 5)
                                {
                                    foundDashedLine = true;

                                    // Вставляем отступ перед линией, если нужно
                                    if (i == 0 || !string.IsNullOrWhiteSpace(lines[i - 1]))
                                    {
                                        lines = lines.Take(i).Concat(new[] { "", "" }).Concat(lines.Skip(i)).ToArray();
                                    }

                                    break;
                                }
                            }


                            richTextBox1.Text = string.Join(Environment.NewLine, lines);

                            if (foundDashedLine)
                            {
                                richTextBox1.SelectionStart = 0;
                                richTextBox1.ScrollToCaret();
                            }
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
