using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace AtlantPrograma
{
    public partial class Form7 : Form
    {
        private int CurrentsmessageId = -1; // -1 — значит новое сообщение, ещё без ID
        private string senderUser;
        public Form7(string sender, int messageId = -1)
        {
            InitializeComponent();
            senderUser = sender;
            CurrentsmessageId = messageId;
            comboBox1.Text = "Поиск...";
            comboBox1.ForeColor = Color.Gray;
            LoadRecipients();
            comboBox2.Items.AddRange(new string[] { "Не срочно", "Обычное сообщение", "Срочно!" });
            comboBox2.SelectedIndex = 1; // по умолчанию "Обычное сообщение"
            //comboBox1.Enter += comboBox1_Enter;
            //comboBox1.Leave += comboBox1_Leave;
            //comboBox1.TextChanged += comboBox1_TextChanged;
           // this.FormClosing += FormMessages_FormClosing;

        }
        //private void FormMessages_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    foreach (var path in cachedPaths.Values)
        //    {
        //        try
        //        {
        //            if (File.Exists(path))
        //                File.Delete(path);
        //        }
        //        catch
        //        {
        //            // Можно залогировать ошибку или просто проигнорировать
        //        }
        //    }
        //}

        private bool isReadOnlyMode = false;

        bool isReplyRead = false;
        private void button2_Click(object sender, EventArgs e)
        {
            if (!isReadOnlyMode)
            {
                bool hasRecipient = comboBox1.Text != "Поиск..." && !string.IsNullOrWhiteSpace(comboBox1.Text);
                bool hasSubject = !string.IsNullOrWhiteSpace(textBox1.Text);
                bool hasBody = !string.IsNullOrWhiteSpace(richTextBox1.Text);

               //string currentRecipient = comboBox1.Text.Trim();
                //string currentSubject = textBox1.Text.Trim();
                //string currentBody = richTextBox1.Text.Trim();

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

                                        if (dbRecipient != comboBox1.Text.Trim() ||
                                    dbSubject != textBox1.Text.Trim() ||
                                    dbBody != richTextBox1.Text.Trim())
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

                    if (hasChanges && comboBox1.Text!="Поиск...")
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
                //else
                //{
                //    this.Close();
                //}
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
                        Task.Run(() => CleanOldTempDocuments());
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
                        Task.Run(() => CleanOldTempDocuments());
                    }
                }
                else
                {
                    this.Close();
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
                    cmd.Parameters.AddWithValue("@sender", senderUser);
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
                    cmd.Parameters.AddWithValue("@currentUser", senderUser);

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
            //Task.Run(() => CleanOldTempDocuments());

            if (checkBox1.Checked)
            {
                string[] allowedExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

                var validFiles = attachedFiles.Where(f => allowedExtensions.Contains(Path.GetExtension(f.fileName).ToLower())).ToList();

                if (validFiles.Count == 0)
                {
                    MessageBox.Show("Вы отметили, что необходимо подписать документы, но ни одного допустимого документа не добавлено.\n" +
                                    "Пожалуйста, прикрепите хотя бы один файл в формате .doc, .docx, .xls, .xlsx или .pdf",
                                    "Предупреждение",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                    return; // Прерываем отправку
                }
            }

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
                        cmd.Parameters.AddWithValue("@sender", senderUser);
                        cmd.Parameters.AddWithValue("@recipient", recipient);
                        cmd.Parameters.AddWithValue("@subject", subject);
                        cmd.Parameters.AddWithValue("@body", body);
                        cmd.Parameters.AddWithValue("@priority", priority);
                        cmd.Parameters.AddWithValue("@date", dateSent);
                        cmd.Parameters.AddWithValue("@time", timeSent);

                        cmd.ExecuteNonQuery();
                        insertedMessageId = cmd.LastInsertedId; // Вот здесь мы получаем ID
                        CurrentsmessageId = (int)cmd.LastInsertedId;  // сохраняем ID в поле формы


                        // ⬇️ Новый блок обработки файлов — вставить сюда ⬇️
                        string tempDocumentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");

                        List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> updatedFiles =
                            new List<(int, string, byte[], string, string)>();


                        foreach (var file in attachedFiles)
                        {
                            byte[] actualData = null;

                            // Пытаемся взять актуальные данные из временного файла, если он есть
                            if (tempDocumentPaths.TryGetValue(file.fileHash, out string tempPath) && File.Exists(tempPath))
                            {
                                try
                                {
                                    actualData = File.ReadAllBytes(tempPath);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Ошибка при чтении файла {file.fileName} из TempDocuments: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    actualData = file.fileData; // fallback
                                }
                            }
                            else
                            {
                                // Нет изменённого файла во временных — используем данные из объекта
                                actualData = file.fileData;
                            }

                            updatedFiles.Add((file.id, file.fileName, actualData, file.fileType, file.fileHash));
                        }

                        // Теперь вставляем все файлы с актуальными данными в базу
                        foreach (var file in updatedFiles)
                        {
                            string insertDocQuery = "INSERT INTO documents (message_id, filename, filedata, filetype, is_signed, is_draft) " +
                                                    "VALUES (@messageId, @filename, @filedata, @filetype, @isSigned, 0)";

                            using (MySqlCommand docCmd = new MySqlCommand(insertDocQuery, conn))
                            {
                                docCmd.Parameters.AddWithValue("@messageId", insertedMessageId);
                                docCmd.Parameters.AddWithValue("@filename", file.fileName);
                                docCmd.Parameters.AddWithValue("@filedata", file.fileData);
                                docCmd.Parameters.AddWithValue("@filetype", file.fileType);
                                docCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                                docCmd.ExecuteNonQuery();
                            }
                        }

                        //foreach (var file in attachedFiles)
                        //{
                        //    byte[] actualData = null;

                        //    if (file.id > 0)
                        //    {
                        //        string selectQuery = "SELECT filedata FROM documents WHERE id = @docId LIMIT 1";
                        //        using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn))
                        //        {
                        //            selectCmd.Parameters.AddWithValue("@docId", file.id);
                        //            using (MySqlDataReader reader = selectCmd.ExecuteReader())
                        //            {
                        //                if (reader.Read())
                        //                {
                        //                    actualData = (byte[])reader["filedata"];
                        //                }
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        string tempPath = Path.Combine(tempDocumentsDir, file.fileName);
                        //        if (File.Exists(tempPath))
                        //        {
                        //            try
                        //            {
                        //                actualData = File.ReadAllBytes(tempPath);
                        //            }
                        //            catch (Exception ex)
                        //            {
                        //                MessageBox.Show($"Ошибка при чтении файла {file.fileName} из TempDocuments: {ex.Message}",
                        //                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        //            }
                        //        }
                        //    }

                        //    updatedFiles.Add((file.id, file.fileName, actualData ?? file.fileData, file.fileType, file.fileHash));
                        //}

                        //foreach (var file in updatedFiles)
                        //{
                        //    string insertDocQuery = "INSERT INTO documents (message_id, filename, filedata, filetype, is_signed, is_draft) " +
                        //                            "VALUES (@messageId, @filename, @filedata, @filetype, @isSigned, 0)";

                        //    using (MySqlCommand docCmd = new MySqlCommand(insertDocQuery, conn))
                        //    {
                        //        docCmd.Parameters.AddWithValue("@messageId", insertedMessageId);
                        //        docCmd.Parameters.AddWithValue("@filename", file.fileName);
                        //        docCmd.Parameters.AddWithValue("@filedata", file.fileData);
                        //        docCmd.Parameters.AddWithValue("@filetype", file.fileType);
                        //        docCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                        //        docCmd.ExecuteNonQuery();
                        //    }
                        //}

                        //foreach (var file in attachedFiles)
                        //{
                        //    string insertDocQuery = "INSERT INTO documents (message_id, filename, filedata, filetype, is_signed, is_draft) " +
                        //                            "VALUES (@messageId, @filename, @filedata, @filetype, @isSigned, 0)";

                        //    using (MySqlCommand docCmd = new MySqlCommand(insertDocQuery, conn))
                        //    {
                        //        docCmd.Parameters.AddWithValue("@messageId", insertedMessageId);
                        //        docCmd.Parameters.AddWithValue("@filename", file.fileName);
                        //        docCmd.Parameters.AddWithValue("@filedata", file.fileData); // уже актуальные данные
                        //        docCmd.Parameters.AddWithValue("@filetype", file.fileType);
                        //        docCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                        //        docCmd.ExecuteNonQuery();
                        //    }
                        //}
                    }

                    Task.Run(() => CleanOldTempDocuments());

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

                        // Обновляем is_sent, но уже сделали выше
                        string updateQuery1 = "UPDATE messages SET is_sent = 0 WHERE id = @id";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateQuery1, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", replyingToMessageId.Value);
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
                    else if (!replyingToMessageId.HasValue)
                    {

                    }
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

            checkBox1.Visible = false;
            pictureBox1.Visible = false;

            comboBox3.Text = "Отправленные вам документы:";
            // Очистим и загрузим документы в comboBox3 и список attachedFiles
            comboBox3.Items.Clear();
            attachedFiles.Clear();

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = @"
            SELECT d.filename, d.filedata, d.filetype
            FROM documents d
            JOIN messages m ON d.message_id = m.id
            WHERE m.subject = @subject AND m.recipient = @recipient AND m.sender = @sender";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@subject", textBox1.Text);           // тема
                    cmd.Parameters.AddWithValue("@recipient", senderUser);            // текущий пользователь, кому пришло
                    cmd.Parameters.AddWithValue("@sender", senderUsername);               // отправитель (если совпадают)

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fileName = reader.GetString("filename");
                            byte[] fileData = (byte[])reader["filedata"];
                            string fileType = reader.GetString("filetype");

                            comboBox3.Items.Add(fileName);
                            string fileHash = GetFileHash(fileData);
                            attachedFiles.Add((-1, fileName, fileData, fileType, fileHash)); // -1 — заглушка, так как id не запрашивается
                        }
                    }
                }
            }
            if (comboBox3.Items.Count == 0)
            {
                действияСДокументамиToolStripMenuItem.Enabled = false;
                comboBox3.Text = "Пусто";
                comboBox3.Enabled = false;
            }
            else
            {
                действияСДокументамиToolStripMenuItem.Enabled = true;
                предварительныйПросмотрДокументовToolStripMenuItem.Enabled = false;
                скачатьВсеДокументыToolStripMenuItem.Enabled = true;
                просмотретьДокументыToolStripMenuItem.Enabled = true;
                сброситьИзмененияВДокументахToolStripMenuItem.Enabled = false;
                очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Enabled = false;
            }
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
                            //richTextBox1.Text = body;             // Текст письма
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

            // Очистим предыдущие документы (если остались)
            attachedFiles.Clear();
            comboBox3.Items.Clear();
            comboBox3.Text = "Прикреплённые документы:";

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    string query = @"SELECT id, filename, filedata, filetype FROM documents WHERE message_id = @messageId";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32("id");
                                string fileName = reader.GetString("filename");
                                byte[] fileData = (byte[])reader["filedata"];
                                string fileType = reader.GetString("filetype");
                                string fileHash = GetFileHash(fileData);

                                // Добавляем в список и в comboBox
                                attachedFiles.Add((id, fileName, fileData, fileType, fileHash));
                                comboBox3.Items.Add(fileName);
                            }
                        }
                    }
                }

                if (comboBox3.Items.Count == 0)
                {
                    comboBox3.Text = "Пусто";
                    comboBox3.Enabled = true;
                    действияСДокументамиToolStripMenuItem.Enabled = true;
                }
                else
                {
                    comboBox3.Enabled = true;
                    действияСДокументамиToolStripMenuItem.Enabled = true;
                    предварительныйПросмотрДокументовToolStripMenuItem.Enabled = true;
                    скачатьВсеДокументыToolStripMenuItem.Enabled = true;
                    просмотретьДокументыToolStripMenuItem.Enabled = false;
                    сброситьИзмененияВДокументахToolStripMenuItem.Enabled = true;
                    очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке прикреплённых документов: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> attachedFiles =
    new List<(int, string, byte[], string, string)>();

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Выберите документы для прикрепления",
                Filter = "Документы (*.doc;*.docx;*.xls;*.xlsx;*.pdf)|*.doc;*.docx;*.xls;*.xlsx;*.pdf"
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] allowedExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

                foreach (string file in openFileDialog1.FileNames)
                {
                    string extension = Path.GetExtension(file).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        MessageBox.Show("Файл \"" + Path.GetFileName(file) + "\" имеет недопустимый формат и не будет добавлен!",
                            "Недопустимый формат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    byte[] fileBytes = File.ReadAllBytes(file);
                    string fileHash = GetFileHash(fileBytes);

                    // Проверка на дубликаты по хэшу

                    string fileName = Path.GetFileName(file);

                    bool alreadyAttached = attachedFiles.Any(f => f.fileName == fileName && f.fileData.Length == fileBytes.Length);

                    if (alreadyAttached)
                    {
                        MessageBox.Show("Файл \"" + Path.GetFileName(file) + "\" уже был прикреплён (по содержимому) и не будет добавлен повторно даже если разные названия файлов, но их размер одинаковый",
                            "Дубликат файла", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }

                    //string fileName = Path.GetFileName(file);

                    // Получаем уникальный id для файла
                    int newId = GetNextId();

                    // Добавляем файл с уникальным id
                    attachedFiles.Add((newId, fileName, fileBytes, extension, fileHash));

                    comboBox3.Items.Add(fileName);
                    comboBox3.Text = "Прикреплённые документы:";
                }
            }
        }
        // Пример присваивания уникальных ID для файлов при их добавлении
        private int GetNextId()
        {
            return attachedFiles.Any() ? attachedFiles.Max(f => f.id) + 1 : 0;
        }

        private string GetFileHash(byte[] fileBytes)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(fileBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }


        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_MAXIMIZE = 3;

        Dictionary<int, string> cachedPaths = new Dictionary<int, string>();

        private Dictionary<string, string> tempDocumentPaths = new Dictionary<string, string>();

        private void предварительныйПросмотрДокументовToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (attachedFiles.Count == 0)
            {
                MessageBox.Show("Нет доступных документов для предварительного просмотра",
                    "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<int> selectedIds = ShowDocumentSelectionDialogWithIds(attachedFiles);

            if (selectedIds == null || selectedIds.Count == 0)
                return;

            foreach (int id in selectedIds)
            {
                var file = attachedFiles.FirstOrDefault(f => f.id == id);
                if (file.fileData == null)
                {
                    MessageBox.Show($"Файл с ID {id} не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                try
                {
                   string connectionString = "server=localhost;user=root;password=1111;database=document_system;";
                    // Если файл ещё не в базе — используем уже прикреплённые данные
                        // Проверка — существует ли вообще файл с таким ID в базе
                        bool fileExistsInDb = false;

                    if (file.id == 0)
                    {
                        if (file.fileData == null)
                        {
                            MessageBox.Show($"Файл \"{file.fileName}\" не содержит данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }
                    }
                    else
                    {

                        using (var conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string checkQuery = "SELECT COUNT(*) FROM documents WHERE id = @id";
                            using (var cmd = new MySqlCommand(checkQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", file.id);
                                fileExistsInDb = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                            }

                            if (fileExistsInDb)
                            {
                                string selectQuery = "SELECT filedata FROM documents WHERE id = @id";
                                using (var cmd = new MySqlCommand(selectQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", file.id);
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            file.fileData = (byte[])reader["filedata"];
                                        }
                                        else
                                        {
                                            MessageBox.Show($"Не удалось загрузить файл с ID {file.id} из базы данных",
                                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Если файла с таким ID в базе нет — значит, он временный (ещё не отправлен)
                                if (file.fileData == null)
                                {
                                    MessageBox.Show($"Файл \"{file.fileName}\" не содержит данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    continue;
                                }
                            }
                        }
                    }

                    string tempPath;
                    if (!tempDocumentPaths.TryGetValue(file.fileHash, out tempPath))
                    {
                        string tempFileName = $"{file.fileHash}_{file.fileName}";
                        string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");
                        Directory.CreateDirectory(tempDir);
                        tempPath = Path.Combine(tempDir, tempFileName);

                        File.WriteAllBytes(tempPath, file.fileData);
                        tempDocumentPaths[file.fileHash] = tempPath;
                    }

                    DateTime originalWriteTime = File.GetLastWriteTime(tempPath);

                    Process process = new Process();
                    process.StartInfo.FileName = tempPath;
                    process.StartInfo.UseShellExecute = true;
                    process.EnableRaisingEvents = true;

                    process.Exited += (s, ev) =>
                    {
                        if (File.Exists(tempPath))
                        {
                            DateTime newWriteTime = File.GetLastWriteTime(tempPath);
                            if (newWriteTime > originalWriteTime)
                            {
                                try
                                {
                                    byte[] updatedData = File.ReadAllBytes(tempPath);

                                    if (fileExistsInDb)
                                    {
                                        using (var conn = new MySqlConnection(connectionString))
                                        {
                                            conn.Open();
                                            string updateQuery = "UPDATE documents SET filedata = @filedata WHERE id = @id";
                                            using (var cmd = new MySqlCommand(updateQuery, conn))
                                            {
                                                cmd.Parameters.AddWithValue("@filedata", updatedData);
                                                cmd.Parameters.AddWithValue("@id", file.id);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }

                                        file.fileData = updatedData;
                                        Invoke(new Action(UpdateComboBox3));
                                    }
                                    else
                                    {
                                        // ещё не в базе — просто обновляем объект
                                        file.fileData = updatedData;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Не удалось сохранить изменения файла: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }

                            try
                            {
                                if (fileExistsInDb) // ← вот теперь корректно
                                {
                                    File.Delete(tempPath);
                                    tempDocumentPaths.Remove(file.fileHash);
                                }
                                // иначе оставляем файл — пригодится при отправке
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не удалось удалить временный файл: {ex.Message}",
                                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    };

                    process.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии \"{file.fileName}\": {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            //Task.Run(() => CleanOldTempDocuments()); // Фоновая очистка TempDocuments
        }

        private void CleanOldTempDocuments()
        {
            try
            {
                string customTempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");
                if (!Directory.Exists(customTempDir))
                    return;

                var files = Directory.GetFiles(customTempDir);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { /* Пропускаем ошибки на отдельных файлах */ }
                }
            }
            catch { /* Пропускаем общие ошибки */ }
        }


        //private void CleanOldTempFiles()
        //{
        //    string tempDir = Path.GetTempPath();
        //    var files = Directory.GetFiles(tempDir, "*_*");

        //    foreach (var file in files)
        //    {
        //        try
        //        {
        //            DateTime lastAccess = File.GetLastAccessTime(file);
        //            if ((DateTime.Now - lastAccess).TotalHours > 2) // старше 2 часов
        //            {
        //                File.Delete(file);
        //            }
        //        }
        //        catch { /* Пропускаем ошибки */ }
        //    }
        //}

        private void UpdateComboBox3()
        {
            comboBox3.Items.Clear();
            foreach (var file in attachedFiles)
            {
                comboBox3.Items.Add(file.fileName);
            }

            comboBox3.Text = "Прикреплённые документы:";

            if (comboBox3.Items.Count > 0)
                comboBox3.SelectedIndex = 0;
        }


        private List<int> ShowDocumentSelectionDialogWithIds(List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> files)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 320,
                Text = "Выберите документы",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label() { Left = 10, Top = 10, Text = "Доступные документы:", AutoSize = true };

            CheckedListBox listBox = new CheckedListBox()
            {
                Left = 10,
                Top = 35,
                Width = 360,
                Height = 180,
                CheckOnClick = true
            };

            foreach (var file in files)
                listBox.Items.Add($"{file.fileName} (ID: {file.id})");

            Button ok = new Button() { Text = "Открыть", Left = 210, Width = 75, Top = 230, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Отмена", Left = 295, Width = 75, Top = 230, DialogResult = DialogResult.Cancel };

            prompt.Controls.Add(label);
            prompt.Controls.Add(listBox);
            prompt.Controls.Add(ok);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = ok;
            prompt.CancelButton = cancel;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                List<int> selectedIds = new List<int>();

                foreach (var item in listBox.CheckedItems)
                {
                    string selectedText = item.ToString();
                    int startIndex = selectedText.LastIndexOf("ID: ") + 4;
                    int endIndex = selectedText.LastIndexOf(")");
                    if (int.TryParse(selectedText.Substring(startIndex, endIndex - startIndex), out int id))
                    {
                        selectedIds.Add(id);
                    }
                }

                return selectedIds;
            }

            return null;
        }

        // Функция для форматирования размера файла в читаемый вид
        //private string FormatFileSize(long fileSize)
        //{
        //    if (fileSize < 1024)
        //        return $"{fileSize} байт";
        //    else if (fileSize < 1024 * 1024)
        //        return $"{fileSize / 1024} КБ";
        //    else
        //        return $"{fileSize / (1024 * 1024)} МБ";
        //}



        private void просмотретьДокументыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Task.Run(() => CleanOldTempFiles()); // Очистка Temp в фоне

            if (attachedFiles.Count == 0)
            {
                MessageBox.Show("Нет документов для просмотра",
                                "Документы отсутствуют",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Вы можете выбрать один или несколько документов для просмотра.\n\n" +
                "Открытие большого количества документов может повлиять на производительность!",
                "Предупреждение",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.OK)
            {
                ShowDocumentSelectionDialog(attachedFiles);
            }
        }

        private void ShowDocumentSelectionDialog(List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> files)
        {
            Form dialog = new Form()
            {
                Width = 400,
                Height = 350,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Выбор документов для просмотра",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            CheckedListBox listBox = new CheckedListBox()
            {
                Left = 10,
                Top = 10,
                Width = 360,
                Height = 240
            };

            foreach (var file in files)
                listBox.Items.Add($"{file.fileName} (ID: {file.id})");

            Button openButton = new Button()
            {
                Text = "Открыть",
                Left = 200,
                Width = 80,
                Top = 265,
                DialogResult = DialogResult.OK
            };

            Button cancelButton = new Button()
            {
                Text = "Отмена",
                Left = 290,
                Width = 80,
                Top = 265,
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.Add(listBox);
            dialog.Controls.Add(openButton);
            dialog.Controls.Add(cancelButton);
            dialog.AcceptButton = openButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var item in listBox.CheckedItems)
                {
                    string selectedFileName = item.ToString();

                    // Извлекаем ID из строки
                    int startIndex = selectedFileName.LastIndexOf("ID: ") + 4;
                    int endIndex = selectedFileName.LastIndexOf(")");
                    int id = int.Parse(selectedFileName.Substring(startIndex, endIndex - startIndex));

                    // Ищем файл по ID, а не по имени
                    var selectedFile = files.FirstOrDefault(f => f.id == id);

                    if (selectedFile.fileData != null)
                    {
                        // Создаём уникальное имя файла, добавляя ID перед именем
                        string uniqueName = $"{selectedFile.id}_{selectedFile.fileName}";
                        string tempPath = Path.Combine(Path.GetTempPath(), uniqueName);

                        // Сохраняем файл и открываем
                        File.WriteAllBytes(tempPath, selectedFile.fileData);
                        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                        //string tempPath = Path.Combine(Path.GetTempPath(), selectedFile.fileName);
                        //File.WriteAllBytes(tempPath, selectedFile.fileData);
                        //Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    }
                }
            }
        }
        private void скачатьВсеДокументыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (attachedFiles.Count == 0)
            {
                MessageBox.Show("Нет документов для скачивания",
                                "Документы отсутствуют",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show("Вы уверены, что хотите скачать все прикреплённые документы?",
                                                   "Подтверждение",
                                                   MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string folderPath = dialog.SelectedPath;

                        foreach (var file in attachedFiles)
                        {
                            string path = Path.Combine(folderPath, file.fileName);
                            File.WriteAllBytes(path, file.fileData);
                        }

                        MessageBox.Show("Все документы успешно сохранены!",
                                        "Успех",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                    }
                }
            }
        }

        //private int CurrentsmessageId; // должна быть в твоём классе, ты явно где-то уже её задаёшь при ответе/редактировании

        private void сброситьИзмененияВДокументахToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
        "Вы уверены, что хотите сбросить изменения?\n" +
        "Документы будут загружены заново с вашего компьютера",
        "Подтверждение сброса", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                //int currentMessageId = currentMessageIdForEditing; // замени на свою переменную ID редактируемого сообщения

                attachedFiles = LoadDocumentsFromDatabase(CurrentsmessageId); // загружаем актуальные документы
                UpdateComboBox3(); // обновляем список документов

                MessageBox.Show("Изменения сброшены, документы загружены заново",
                    "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе изменений: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)>
    LoadDocumentsFromDatabase(int messageId)
        {
            var result = new List<(int, string, byte[], string, string)>();

            using (var conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();
                string query = "SELECT id, filename, filedata, filetype FROM documents WHERE message_id = @messageId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@messageId", messageId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add((
                                reader.GetInt32("id"),
                                reader.GetString("filename"),
                                (byte[])reader["filedata"],
                                reader.GetString("filetype"),
                                "" // пустой fileHash, которого нет в БД
                            ));
                        }
                    }
                }
            }

            return result;
        }

        private void очиститьСписокПрикреплённыхСообщенийToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (comboBox3.Items.Count != 0)
            {
                // Диалоговое окно с выбором варианта очистки
                var dialogResult = MessageBox.Show(
                    "Вы хотите очистить все документы или только выбранные?\n" +
                    "Да: Очистить всё\n" +
                    "Нет: Очистить только выбранные документы",
                    "Подтверждение очистки",
                    MessageBoxButtons.YesNoCancel, // Yes = все, No = выбранные, Cancel = отмена
                    MessageBoxIcon.Question
                );

                if (dialogResult == DialogResult.Yes)
                {
                    // Очистить все документы
                    ClearAllAttachedDocuments();
                }
                else if (dialogResult == DialogResult.No)
                {
                    // Очистить только выбранные документы
                    ShowDocumentDeletionDialog(attachedFiles);
                }
                else
                {
                    // Отмена
                    return;
                }
            }
            else
            {
                MessageBox.Show("Нет прикреплённых документов для очистки","Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        private void ClearAllAttachedDocuments()
        {
            if (MessageBox.Show("Вы уверены, что хотите очистить все прикреплённые документы?",
        "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // Очистим все из comboBox3 и списка attachedFiles
                comboBox3.Items.Clear();
                attachedFiles.Clear();

                // Удалим все документы из базы данных для текущего сообщения
                try
                {
                    using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                    {
                        conn.Open();
                        string query = "DELETE FROM documents WHERE message_id = @messageId";

                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@messageId", replyingToMessageId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Все выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    comboBox3.Text = "Пусто";
                    comboBox3.Items.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении документов: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowDocumentDeletionDialog(List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> files)
        {
            Form dialog = new Form()
            {
                Width = 400,
                Height = 350,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Выбор документов для удаления",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            CheckedListBox listBox = new CheckedListBox()
            {
                Left = 10,
                Top = 10,
                Width = 360,
                Height = 240
            };

            foreach (var file in files)
                listBox.Items.Add(file.fileName);

            Button deleteButton = new Button()
            {
                Text = "Удалить",
                Left = 200,
                Width = 80,
                Top = 265,
                DialogResult = DialogResult.OK
            };

            Button cancelButton = new Button()
            {
                Text = "Отмена",
                Left = 290,
                Width = 80,
                Top = 265,
                DialogResult = DialogResult.Cancel
            };

            dialog.Controls.Add(listBox);
            dialog.Controls.Add(deleteButton);
            dialog.Controls.Add(cancelButton);
            dialog.AcceptButton = deleteButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFiles = listBox.CheckedItems.Cast<string>().ToList();

                foreach (var selectedFileName in selectedFiles)
                {
                    var selectedFile = files.FirstOrDefault(f => f.fileName == selectedFileName);
                    if (selectedFile.fileData != null)
                    {
                        try
                        {
                            if (selectedFile.id != -1)
                            {
                                // Удаляем из базы по id
                                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                                {
                                    conn.Open();

                                    string query = "DELETE FROM documents WHERE id = @id";

                                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@id", selectedFile.id);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            // Удаляем локально из списка и UI
                            attachedFiles.Remove(selectedFile);
                            comboBox3.Items.Remove(selectedFile.fileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Ошибка при удалении файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                MessageBox.Show("Выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
