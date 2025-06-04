using MySql.Data.MySqlClient;
using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
            toolTip1.SetToolTip(pictureBox1, "Прикрепить документы");
            toolTip1.SetToolTip(pictureBox2, "Скачать все документы");
            pictureBox2.Visible = true;

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
                    UpdateComboBox3(); // Обновляем список документов (если надо)

                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                        {
                            conn.Open();

                            // 1. Проверка текста черновика
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

                            // 2. Проверка документов
                            string docQuery = "SELECT id, filename, filedata FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                            Dictionary<int, Tuple<string, byte[]>> dbDocuments = new Dictionary<int, Tuple<string, byte[]>>();

                            using (MySqlCommand cmd = new MySqlCommand(docQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int id = reader.GetInt32(reader.GetOrdinal("id"));
                                        string filename = reader.GetString(reader.GetOrdinal("filename"));
                                        byte[] filedata = (byte[])reader["filedata"];
                                        dbDocuments[id] = Tuple.Create(filename, filedata);
                                    }
                                }
                            }

                            // Сравниваем с attachedFiles
                            var uiDocumentIds = new HashSet<int>(attachedFiles.Select(f => f.id));

                            // 2.1 Проверка удалённых документов
                            foreach (int dbId in dbDocuments.Keys)
                            {
                                if (!uiDocumentIds.Contains(dbId))
                                {
                                    hasChanges = true; // Документ был удалён
                                    break;
                                }
                            }

                            // 2.2 Проверка добавленных и изменённых документов
                            foreach (var file in attachedFiles)
                            {
                                if (file.id == 0 || !dbDocuments.ContainsKey(file.id))
                                {
                                    hasChanges = true; // Новый документ
                                    break;
                                }

                                // Сравнение данных файла
                                byte[] dbFileData = dbDocuments[file.id].Item2;
                                if (file.fileData.Length != dbFileData.Length ||
                                    !file.fileData.SequenceEqual(dbFileData))
                                {
                                    hasChanges = true; // Изменён документ
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при проверке изменений черновика: " + ex.Message);
                    }

                    //bool hasChanges = false;
                    //UpdateComboBox3();
                    //try
                    //{
                    //    using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                    //    {
                    //        conn.Open();
                    //        string query = "SELECT recipient, subject, body FROM drafts WHERE id = @id";
                    //        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    //        {
                    //            cmd.Parameters.AddWithValue("@id", openedDraftId.Value);
                    //            using (MySqlDataReader reader = cmd.ExecuteReader())
                    //            {
                    //                if (reader.Read())
                    //                {
                    //                    string dbRecipient = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                    //                    string dbSubject = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                    //                    string dbBody = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();

                    //                    if (dbRecipient != comboBox1.Text.Trim() ||
                    //                dbSubject != textBox1.Text.Trim() ||
                    //                dbBody != richTextBox1.Text.Trim())
                    //                    {
                    //                        hasChanges = true;
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    MessageBox.Show("Ошибка при проверке изменений черновика: " + ex.Message);
                    //}

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
                            Task.Run(() => CleanOldTempDocuments());
                        }
                        else if (result == DialogResult.No)
                        {
                            this.Close();
                            Task.Run(() => CleanOldTempDocuments());
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
                        Form6 formm6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        //form6?.LoadDraftMessages();
                        formm6.UpdateMessageCounters();
                        Task.Run(() => CleanOldTempDocuments());
                        this.Close();
                    }
                    else if (saveDraft == DialogResult.No)
                    {
                        this.Close();
                        Task.Run(() => CleanOldTempDocuments());
                        documentEditCounter = 0;
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
                        Task.Run(() => CleanOldTempDocuments());

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
                        Form6 form66 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        form66.UpdateMessageCounters();
                        this.Close(); // ← Закрываем форму только один раз здесь!
                    }
                    else if (saveDraft == DialogResult.No)
                    {
                        this.Close();
                        Task.Run(() => CleanOldTempDocuments());
                        documentEditCounter = 0;
                        originalAttachedFiles = null;
                    }
                }
                else
                {
                    this.Close();
                    Task.Run(() => CleanOldTempDocuments());
                    documentEditCounter = 0;
                    originalAttachedFiles = null;
                }
            }
            else
            {
                this.Close(); // Закрыть форму в режиме только чтения
            }
        }

        private void UpdateDraft()
        {
            if (!openedDraftId.HasValue)
                return;

            string tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                // 1. Обновляем тему и текст письма
                string updateMessageQuery = "UPDATE messages SET subject = @subject, body = @body WHERE id = @id";
                using (MySqlCommand updateCmd = new MySqlCommand(updateMessageQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@subject", textBox1.Text);
                    updateCmd.Parameters.AddWithValue("@body", richTextBox1.Text);
                    updateCmd.Parameters.AddWithValue("@id", openedDraftId.Value);
                    updateCmd.ExecuteNonQuery();
                }

                // 2. Получаем id всех документов, уже сохранённых по текущему draft_id
                HashSet<int> existingDocumentIds = new HashSet<int>();
                string getDocIdsQuery = "SELECT id FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                using (MySqlCommand cmd = new MySqlCommand(getDocIdsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingDocumentIds.Add(reader.GetInt32("id"));
                        }
                    }
                }

                // 3. Добавляем только новые файлы (тех id, которых нет в базе)
                foreach (var file in attachedFiles)
                {
                    if (existingDocumentIds.Contains(file.id))
                        continue; // файл уже есть в базе — не трогаем

                    string fileName = file.fileName;
                    string extension = file.fileType;
                    byte[] fileBytesToSave;

                    // Учитываем формат TempDocuments\{fileHash}_{fileName}
                    string tempPath = Path.Combine(tempFolderPath, $"{file.fileHash}_{fileName}");
                    if (File.Exists(tempPath))
                    {
                        fileBytesToSave = File.ReadAllBytes(tempPath); // берём отредактированную версию
                    }
                    else
                    {
                        fileBytesToSave = file.fileData; // оригинал
                    }

                    string insertDocQuery = @"
                INSERT INTO documents (filename, filedata, filetype, draft_id, is_draft)
                VALUES (@name, @data, @type, @draftId, 1)";

                    using (MySqlCommand insertCmd = new MySqlCommand(insertDocQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", fileName);
                        insertCmd.Parameters.AddWithValue("@data", fileBytesToSave);
                        insertCmd.Parameters.AddWithValue("@type", extension);
                        insertCmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }

            //try
            //{
            //    using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //    {
            //        conn.Open();

            //        string query = @"
            //    UPDATE drafts
            //    SET recipient = @recipient,
            //        subject = @subject,
            //        body = @body,
            //        priority = @priority,
            //        date_created = @date,
            //        time_created = @time
            //    WHERE id = @id";

            //        using (MySqlCommand cmd = new MySqlCommand(query, conn))
            //        {
            //            cmd.Parameters.AddWithValue("@recipient", comboBox1.Text.Trim());
            //            cmd.Parameters.AddWithValue("@subject", textBox1.Text.Trim());
            //            cmd.Parameters.AddWithValue("@body", richTextBox1.Text.Trim());
            //            cmd.Parameters.AddWithValue("@priority", comboBox2.Text); // Можно проверить на null
            //            cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("dd.MM.yyyy"));
            //            cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("HH:mm:ss"));
            //            cmd.Parameters.AddWithValue("@id", openedDraftId.Value);

            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Ошибка при обновлении черновика: " + ex.Message);
            //}
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

                    // 1. Сохраняем черновик
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

                    long insertedDraftId = cmd.LastInsertedId;

                    // 2. Обновляем is_draft = 1 для документов, уже привязанных к CurrentsmessageId
                    if (CurrentsmessageId != -1)
                    {
                        string updateQuery = "UPDATE documents SET is_draft = 1 WHERE message_id = @msgId";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@msgId", CurrentsmessageId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }

                    // 3. Добавляем новые документы, прикреплённые к этому черновику
                    foreach (var file in attachedFiles)
                    {
                        if (file.id != -1) // Это новый прикреплённый документ
                        {
                            byte[] actualFileData = null;
                            string uniqueKey = $"{file.fileHash}_{file.fileName}";

                            // 1. Пробуем взять актуальные данные из временного файла
                            if (tempDocumentPaths.TryGetValue(uniqueKey, out string tempPath) && File.Exists(tempPath))
                            {
                                try
                                {
                                    actualFileData = File.ReadAllBytes(tempPath);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Ошибка при чтении файла {file.fileName} из TempDocuments: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    actualFileData = file.fileData; // fallback
                                }
                            }
                            else
                            {
                                actualFileData = file.fileData; // fallback
                            }

                            string insertDocQuery = "INSERT INTO documents (draft_id, filename, filedata, filetype, is_signed, is_draft) " +
                                                    "VALUES (@draftId, @filename, @filedata, @filetype, @isSigned, 1)";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertDocQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@draftId", insertedDraftId);
                                insertCmd.Parameters.AddWithValue("@filename", file.fileName);
                                insertCmd.Parameters.AddWithValue("@filedata", actualFileData);
                                insertCmd.Parameters.AddWithValue("@filetype", file.fileType);
                                insertCmd.Parameters.AddWithValue("@isSigned", false);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    //MessageBox.Show("Черновик успешно сохранён!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении черновика: " + ex.Message);
            }

            //string recipient = comboBox1.SelectedItem?.ToString() ?? "";
            //string subject = textBox1.Text.Trim();
            //string body = richTextBox1.Text.Trim();
            //string priority = comboBox2.SelectedItem?.ToString() ?? "Обычное сообщение";
            //string dateCreated = DateTime.Now.ToString("dd.MM.yyyy");
            //string timeCreated = DateTime.Now.ToString("HH:mm:ss");

            //try
            //{
            //    using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //    {
            //        conn.Open();
            //        string query = "INSERT INTO drafts (sender, recipient, subject, body, priority, date_created, time_created) " +
            //                       "VALUES (@sender, @recipient, @subject, @body, @priority, @date, @time)";
            //        MySqlCommand cmd = new MySqlCommand(query, conn);
            //        cmd.Parameters.AddWithValue("@sender", senderUser);
            //        cmd.Parameters.AddWithValue("@recipient", recipient);
            //        cmd.Parameters.AddWithValue("@subject", subject);
            //        cmd.Parameters.AddWithValue("@body", body);
            //        cmd.Parameters.AddWithValue("@priority", priority);
            //        cmd.Parameters.AddWithValue("@date", dateCreated);
            //        cmd.Parameters.AddWithValue("@time", timeCreated);

            //        cmd.ExecuteNonQuery();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Ошибка при сохранении черновика: " + ex.Message);
            //}
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
                                    "Информация",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
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

                        if (!openedDraftId.HasValue)
                        {
                            // ⬇️ Новый блок обработки файлов — вставить сюда ⬇️
                            string tempDocumentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");

                            List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> updatedFiles =
                                new List<(int, string, byte[], string, string)>();


                            foreach (var file in attachedFiles)
                            {
                                byte[] actualData = null;

                                // 🔧 Используем уникальный ключ (hash + имя файла)
                                string uniqueKey = $"{file.fileHash}_{file.fileName}";

                                // 1. Пробуем взять актуальные данные из временного файла
                                if (tempDocumentPaths.TryGetValue(uniqueKey, out string tempPath) && File.Exists(tempPath))
                                {
                                    try
                                    {
                                        actualData = File.ReadAllBytes(tempPath);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Ошибка при чтении файла {file.fileName} из TempDocuments: {ex.Message}",
                                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        actualData = file.fileData; // fallback
                                    }
                                }
                                // 2. Если файл есть в базе (по ID), и временного файла нет — тянем актуальные данные из базы
                                else if (file.id != 0)
                                {
                                    try
                                    {
                                        string fileQuery = "SELECT filedata FROM documents WHERE id = @id";
                                        using (MySqlCommand fileCmd = new MySqlCommand(fileQuery, conn))
                                        {
                                            fileCmd.Parameters.AddWithValue("@id", file.id);
                                            using (var reader = fileCmd.ExecuteReader())
                                            {
                                                if (reader.Read())
                                                    actualData = (byte[])reader["filedata"];
                                                else
                                                    actualData = file.fileData; // fallback
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Ошибка при загрузке файла {file.fileName} из базы данных: {ex.Message}",
                                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        actualData = file.fileData;
                                    }
                                }
                                // 3. Если ни временного файла, ни ID — fallback на старые данные
                                else
                                {
                                    actualData = file.fileData;
                                }

                                updatedFiles.Add((file.id, file.fileName, actualData, file.fileType, file.fileHash));
                            }


                            foreach (var file in updatedFiles)
                            {
                                string insertDocQuery = "INSERT INTO documents (message_id, filename, filedata, filetype, is_signed, is_draft, draft_id) " +
                                                        "VALUES (@messageId, @filename, @filedata, @filetype, @isSigned, 0, @draftId)";

                                using (MySqlCommand docCmd = new MySqlCommand(insertDocQuery, conn))
                                {
                                    docCmd.Parameters.AddWithValue("@messageId", insertedMessageId);
                                    docCmd.Parameters.AddWithValue("@filename", file.fileName);
                                    docCmd.Parameters.AddWithValue("@filedata", file.fileData);
                                    docCmd.Parameters.AddWithValue("@filetype", file.fileType);
                                    docCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);

                                    // ❗ Явно передаём NULL, чтобы draft_id не нарушал внешний ключ
                                    docCmd.Parameters.AddWithValue("@draftId", DBNull.Value);

                                    docCmd.ExecuteNonQuery();
                                }
                            }
                            Task.Run(() => CleanOldTempDocuments());
                            
                        }

                        Form6 form6UpdateCount = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        form6UpdateCount?.UpdateMessageCounters();
                        // Теперь вставляем все файлы с актуальными данными в базу
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
                        //Task.Run(() => CleanOldTempDocuments());
                    }

                        //Task.Run(() => CleanOldTempDocuments());

                        originalAttachedFiles = null;

                        documentEditCounter = 0;

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

                        // Отмечаем черновик как отправленный
                        string updateDraftQuery = "UPDATE drafts SET is_sent = 1 WHERE id = @id";
                        using (MySqlCommand updateCmd = new MySqlCommand(updateDraftQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@id", openedDraftId.Value);
                            updateCmd.ExecuteNonQuery();
                        }

                        // ⬇️ Обработка актуальных файлов ⬇️
                        string tempDocumentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");
                        List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> updatedFiles =
                            new List<(int, string, byte[], string, string)>();

                        foreach (var file in attachedFiles)
                        {
                            byte[] actualData = null;
                            string uniqueKey = $"{file.fileHash}_{file.fileName}";

                            if (tempDocumentPaths.TryGetValue(uniqueKey, out string tempPath) && File.Exists(tempPath))
                            {
                                try
                                {
                                    actualData = File.ReadAllBytes(tempPath);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Ошибка при чтении файла {file.fileName} из TempDocuments: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    actualData = file.fileData;
                                }
                            }
                            else if (file.id != 0)
                            {
                                try
                                {
                                    string fileQuery = "SELECT filedata FROM documents WHERE id = @id";
                                    using (MySqlCommand fileCmd = new MySqlCommand(fileQuery, conn))
                                    {
                                        fileCmd.Parameters.AddWithValue("@id", file.id);
                                        using (var reader = fileCmd.ExecuteReader())
                                        {
                                            if (reader.Read())
                                                actualData = (byte[])reader["filedata"];
                                            else
                                                actualData = file.fileData;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Ошибка при загрузке файла {file.fileName} из базы данных: {ex.Message}",
                                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    actualData = file.fileData;
                                }
                            }
                            else
                            {
                                actualData = file.fileData;
                            }

                            updatedFiles.Add((file.id, file.fileName, actualData, file.fileType, file.fileHash));
                        }

                        // Обновляем существующие документы или вставляем новые
                        foreach (var file in updatedFiles)
                        {
                            bool documentExists = false;

                            if (file.id != 0)
                            {
                                string checkQuery = "SELECT COUNT(*) FROM documents WHERE id = @id AND draft_id = @draftId";
                                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                                {
                                    checkCmd.Parameters.AddWithValue("@id", file.id);
                                    checkCmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                    documentExists = count > 0;
                                }
                            }

                            if (documentExists)
                            {
                                // Обновление существующего документа из черновика
                                string updateDocQuery = @"
UPDATE documents 
SET message_id = @msgId, draft_id = NULL, is_draft = 0, 
    filename = @filename, filedata = @filedata, filetype = @filetype, is_signed = @isSigned
WHERE id = @docId";

                                using (MySqlCommand updateCmd = new MySqlCommand(updateDocQuery, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@msgId", CurrentsmessageId);
                                    updateCmd.Parameters.AddWithValue("@filename", file.fileName);
                                    updateCmd.Parameters.AddWithValue("@filedata", file.fileData);
                                    updateCmd.Parameters.AddWithValue("@filetype", file.fileType);
                                    updateCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                                    updateCmd.Parameters.AddWithValue("@docId", file.id);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Вставка нового документа
                                string insertDocQuery = @"
INSERT INTO documents (message_id, filename, filedata, filetype, is_signed, is_draft, draft_id)
VALUES (@messageId, @filename, @filedata, @filetype, @isSigned, 0, NULL)";

                                using (MySqlCommand docCmd = new MySqlCommand(insertDocQuery, conn))
                                {
                                    docCmd.Parameters.AddWithValue("@messageId", CurrentsmessageId);
                                    docCmd.Parameters.AddWithValue("@filename", file.fileName);
                                    docCmd.Parameters.AddWithValue("@filedata", file.fileData);
                                    docCmd.Parameters.AddWithValue("@filetype", file.fileType);
                                    docCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                                    docCmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Обновляем существующие документы или вставляем новые
                        //                        foreach (var file in updatedFiles)
                        //                        {
                        //                            if (file.id != 0)
                        //                            {
                        //                                // Обновление существующего документа из черновика
                        //                                string updateDocQuery = @"
                        //UPDATE documents 
                        //SET message_id = @msgId, draft_id = NULL, is_draft = 0, 
                        //    filename = @filename, filedata = @filedata, filetype = @filetype, is_signed = @isSigned
                        //WHERE id = @docId";

                        //                                using (MySqlCommand updateCmd = new MySqlCommand(updateDocQuery, conn))
                        //                                {
                        //                                    updateCmd.Parameters.AddWithValue("@msgId", CurrentsmessageId);
                        //                                    updateCmd.Parameters.AddWithValue("@filename", file.fileName);
                        //                                    updateCmd.Parameters.AddWithValue("@filedata", file.fileData);
                        //                                    updateCmd.Parameters.AddWithValue("@filetype", file.fileType);
                        //                                    updateCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                        //                                    updateCmd.Parameters.AddWithValue("@docId", file.id);
                        //                                    updateCmd.ExecuteNonQuery();
                        //                                }
                        //                            }
                        //                            else
                        //                            {
                        //                                // Вставка нового документа
                        //                                string insertDocQuery = @"
                        //INSERT INTO documents (message_id, filename, filedata, filetype, is_signed, is_draft, draft_id)
                        //VALUES (@messageId, @filename, @filedata, @filetype, @isSigned, 0, NULL)";

                        //                                using (MySqlCommand docCmd = new MySqlCommand(insertDocQuery, conn))
                        //                                {
                        //                                    docCmd.Parameters.AddWithValue("@messageId", CurrentsmessageId);
                        //                                    docCmd.Parameters.AddWithValue("@filename", file.fileName);
                        //                                    docCmd.Parameters.AddWithValue("@filedata", file.fileData);
                        //                                    docCmd.Parameters.AddWithValue("@filetype", file.fileType);
                        //                                    docCmd.Parameters.AddWithValue("@isSigned", checkBox1.Checked);
                        //                                    docCmd.ExecuteNonQuery();
                        //                                }
                        //                            }
                        //                        }

                        // ❌ УДАЛЕНИЕ ДОКУМЕНТОВ УБРАНО ❌
                        Task.Run(() => CleanOldTempDocuments());
                        // Обновляем отображение черновиков
                        Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        form6?.LoadDraftMessages();
                        form6?.UpdateMessageCounters();
                        //isFromDraft = true;
                        //string updateDraftQuery = "UPDATE drafts SET is_sent = 1 WHERE id = @id";
                        //using (MySqlCommand updateCmd = new MySqlCommand(updateDraftQuery, conn))
                        //{
                        //    updateCmd.Parameters.AddWithValue("@id", openedDraftId.Value);
                        //    updateCmd.ExecuteNonQuery();
                        //}
                        //Form6 form6 = Application.OpenForms.OfType<Form6>().FirstOrDefault();
                        //form6?.LoadDraftMessages();
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
                        form6Notif?.UpdateMessageCounters();
                        }
                        else if (!replyingToMessageId.HasValue)
                        {

                        }
                        else
                        {
                            form6Notif?.ShowNotificationCount();
                            form6Notif?.LoadIncomingMessages(); // если из Входящих
                            form6Notif?.UpdateMessageCounters();
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
            label1.Text = $"От: {senderUsername}";

            comboBox2.Enabled = false;
            button1.Enabled = false;

            button2.Text = "Закрыть";
            isReadOnlyMode = true;

            this.Text = "ПРОСМОТР СООБЩЕНИЯ";

            checkBox1.Visible = false;
            pictureBox1.Visible = false;

            comboBox3.Text = "Отправленные вам документы:";
            comboBox3.Items.Clear();
            attachedFiles.Clear();

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = @"
            SELECT d.id, d.filename, d.filedata, d.filetype
            FROM documents d
            WHERE d.message_id = @messageId";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@messageId", CurrentsmessageId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int fileId = reader.GetInt32("id");
                            string fileName = reader.GetString("filename");
                            byte[] fileData = (byte[])reader["filedata"];
                            string fileType = reader.GetString("filetype");

                            comboBox3.Items.Add(fileName);
                            string fileHash = GetFileHash(fileData);
                            attachedFiles.Add((fileId, fileName, fileData, fileType, fileHash));
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
            //textBox1.Text = subject;
            //richTextBox1.Text = body;

            //textBox1.ReadOnly = true;
            //richTextBox1.ReadOnly = true;
            //comboBox1.Visible = false; // скрываем выпадающий список получателей
            //label1.Text = $"От: {senderUsername}"; // изменяем метку на "От:"

            //comboBox2.Enabled = false;
            //button1.Enabled = false;

            //button2.Text = "Закрыть"; // изменяем текст кнопки выхода
            //isReadOnlyMode = true;

            //this.Text = "ПРОСМОТР СООБЩЕНИЯ";

            //checkBox1.Visible = false;
            //pictureBox1.Visible = false;

            //comboBox3.Text = "Отправленные вам документы:";
            //// Очистим и загрузим документы в comboBox3 и список attachedFiles
            //comboBox3.Items.Clear();
            //attachedFiles.Clear();

            //using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //{
            //    conn.Open();

            //    string query = @"
            //SELECT d.filename, d.filedata, d.filetype
            //FROM documents d
            //JOIN messages m ON d.message_id = m.id
            //WHERE m.subject = @subject AND m.recipient = @recipient AND m.sender = @sender";

            //    using (MySqlCommand cmd = new MySqlCommand(query, conn))
            //    {
            //        cmd.Parameters.AddWithValue("@subject", textBox1.Text);           // тема
            //        cmd.Parameters.AddWithValue("@recipient", senderUser);            // текущий пользователь, кому пришло
            //        cmd.Parameters.AddWithValue("@sender", senderUsername);               // отправитель (если совпадают)

            //        using (var reader = cmd.ExecuteReader())
            //        {
            //            while (reader.Read())
            //            {
            //                string fileName = reader.GetString("filename");
            //                byte[] fileData = (byte[])reader["filedata"];
            //                string fileType = reader.GetString("filetype");

            //                comboBox3.Items.Add(fileName);
            //                string fileHash = GetFileHash(fileData);
            //                attachedFiles.Add((CurrentsmessageId, fileName, fileData, fileType, fileHash)); // -1 — заглушка, так как id не запрашивается
            //            }
            //        }
            //    }
            //}
            //if (comboBox3.Items.Count == 0)
            //{
            //    действияСДокументамиToolStripMenuItem.Enabled = false;
            //    comboBox3.Text = "Пусто";
            //    comboBox3.Enabled = false;
            //}
            //else
            //{
            //    действияСДокументамиToolStripMenuItem.Enabled = true;
            //    предварительныйПросмотрДокументовToolStripMenuItem.Enabled = false;
            //    скачатьВсеДокументыToolStripMenuItem.Enabled = true;
            //    просмотретьДокументыToolStripMenuItem.Enabled = true;
            //    сброситьИзмененияВДокументахToolStripMenuItem.Enabled = false;
            //    очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Enabled = false;
            //}
            UpdateComboBox3ForReadOnly();
        }

        //private Dictionary<string, string> tempFilesMap = new Dictionary<string, string>();

        public void LoadDraftForEditing(int draftId)
        {
            предварительныйПросмотрДокументовToolStripMenuItem.Enabled = true;
            скачатьВсеДокументыToolStripMenuItem.Enabled = true;
            просмотретьДокументыToolStripMenuItem.Enabled = false;
            сброситьИзмененияВДокументахToolStripMenuItem.Enabled = true;
            очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Enabled = true;
            pictureBox2.Visible = true;

            this.openedDraftId = draftId;

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    // 1. Загрузка данных черновика
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
                            string recipient = !reader.IsDBNull(0) ? reader.GetString(0) : "";
                            string subject = !reader.IsDBNull(1) ? reader.GetString(1) : "";
                            string priority = !reader.IsDBNull(2) ? reader.GetString(2) : "";
                            string body = !reader.IsDBNull(3) ? reader.GetString(3) : "";

                            if (comboBox1.Items.Contains(recipient))
                            {
                                comboBox1.SelectedItem = recipient;
                                comboBox1.ForeColor = Color.Black;
                            }
                            else if (comboBox1.Text == "Поиск...")
                            {
                                comboBox1.ForeColor = Color.Gray;
                            }
                            else
                            {
                                comboBox1.Text = recipient;
                            }

                            textBox1.Text = subject;

                            // Обработка линии с дефисами
                            string[] lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                            bool foundDashedLine = false;
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string trimmedLine = lines[i].Trim();
                                if (trimmedLine.All(c => c == '-') && trimmedLine.Length >= 5)
                                {
                                    foundDashedLine = true;
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

                            if (!string.IsNullOrEmpty(priority))
                            {
                                comboBox2.SelectedItem = comboBox2.Items.Contains(priority)
                                    ? priority
                                    : "Обычное сообщение";
                            }
                            else
                            {
                                comboBox2.SelectedItem = null;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Черновик не найден", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    // 2. Загрузка прикреплённых документов (без создания временных файлов)
                    string docQuery = @"
SELECT id, filename, filedata, filetype
FROM documents
WHERE draft_id = @draftId AND is_draft = 1";

                    MySqlCommand docCmd = new MySqlCommand(docQuery, conn);
                    docCmd.Parameters.AddWithValue("@draftId", draftId);

                    // Сохраняем прикреплённые вручную файлы
                    var manuallyAttached = attachedFiles.Where(f => f.id == 0).ToList();

                    attachedFiles.Clear();
                    comboBox3.Items.Clear();

                    // Добавляем новые (ручные)
                    foreach (var file in manuallyAttached)
                    {
                        attachedFiles.Add(file);
                        if (!comboBox3.Items.Contains(file.fileName))
                            comboBox3.Items.Add(file.fileName);
                    }

                    using (MySqlDataReader docReader = docCmd.ExecuteReader())
                    {
                        while (docReader.Read())
                        {
                            int docId = docReader.GetInt32(0);
                            string fileName = docReader.GetString(1);
                            byte[] fileData = (byte[])docReader[2];
                            string fileType = !docReader.IsDBNull(3) ? docReader.GetString(3) : "";

                            // Проверяем, не дублируем ли вручную добавленный файл
                            if (!attachedFiles.Any(f => f.fileName == fileName && f.id == 0))
                            {
                                attachedFiles.Add((docId, fileName, fileData, fileType, ""));
                                if (!comboBox3.Items.Contains(fileName))
                                    comboBox3.Items.Add(fileName);
                            }
                        }
                    }
                    // 3. Обновление ComboBox с документами
                    UpdateComboBox3();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке черновика: " + ex.Message);
            }
            Form6 form6UpdateCounte = Application.OpenForms.OfType<Form6>().FirstOrDefault();
            form6UpdateCounte?.UpdateMessageCounters();
            //            // Курсор ставим в самое начало
            //            //richTextBox1.SelectionStart = 0;
            //            //richTextBox1.ScrollToCaret();
            //            //isDraftMode = true;

            //            this.openedDraftId = draftId;
            //            try
            //            {
            //                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //                {
            //                    conn.Open();
            //                    string query = @"
            //SELECT recipient, subject, priority, body
            //FROM drafts
            //WHERE id = @draftId";

            //                    MySqlCommand cmd = new MySqlCommand(query, conn);
            //                    cmd.Parameters.AddWithValue("@draftId", draftId);

            //                    using (MySqlDataReader reader = cmd.ExecuteReader())
            //                    {
            //                        if (reader.Read())
            //                        {
            //                            string recipient = reader.IsDBNull(reader.GetOrdinal("recipient")) ? "" : reader.GetString("recipient");
            //                            string subject = reader.IsDBNull(reader.GetOrdinal("subject")) ? "" : reader.GetString("subject");
            //                            string priority = reader.IsDBNull(reader.GetOrdinal("priority")) ? "" : reader.GetString("priority");
            //                            string body = reader.IsDBNull(reader.GetOrdinal("body")) ? "" : reader.GetString("body");

            //                            // Заполняем поля черновика
            //                            // Проверяем, существует ли получатель в comboBox1
            //                            if (comboBox1.Items.Contains(recipient))
            //                            {
            //                                comboBox1.SelectedItem = recipient;
            //                                comboBox1.ForeColor = Color.Black;// Устанавливаем получателя
            //                            }                           
            //                            else if (comboBox1.Text=="Поиск...")
            //                            {
            //                                comboBox1.ForeColor = Color.Gray;
            //                                //comboBox1.Enter += comboBox1_Enter;
            //                                //comboBox1.Leave += comboBox1_Leave;
            //                                //comboBox1.TextChanged += comboBox1_TextChanged;
            //                            }
            //                            else
            //                            {
            //                                comboBox1.Text = recipient;
            //                                //comboBox1.ForeColor = Color.Black;// Если получатель не найден в списке, ставим как текст
            //                            }
            //                            //comboBox1.ForeColor = Color.Black; // <-- сброс цвета на чёрный
            //                            textBox1.Text = subject;              // Тема
            //                            //richTextBox1.Text = body;             // Текст письма
            //                                                                  // Проверяем на наличие пунктирной линии где угодно
            //                            //string pattern = @"^\s*-{3,}\s*$"; // строка из 3 и более дефисов
            //                            string[] lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            //                            bool foundDashedLine = false;

            //                            for (int i = 0; i < lines.Length; i++)
            //                            {
            //                                string trimmedLine = lines[i].Trim(); // убираем пробелы и невидимые символы

            //                                if (trimmedLine.All(c => c == '-') && trimmedLine.Length >= 5)
            //                                {
            //                                    foundDashedLine = true;

            //                                    // Вставляем отступ перед линией, если нужно
            //                                    if (i == 0 || !string.IsNullOrWhiteSpace(lines[i - 1]))
            //                                    {
            //                                        lines = lines.Take(i).Concat(new[] { "", "" }).Concat(lines.Skip(i)).ToArray();
            //                                    }

            //                                    break;
            //                                }
            //                            }


            //                            richTextBox1.Text = string.Join(Environment.NewLine, lines);

            //                            if (foundDashedLine)
            //                            {
            //                                richTextBox1.SelectionStart = 0;
            //                                richTextBox1.ScrollToCaret();
            //                            }
            //                            // Для приоритета
            //                            if (!string.IsNullOrEmpty(priority))
            //                            {
            //                                // Проверим, существует ли этот приоритет в comboBox2
            //                                if (comboBox2.Items.Contains(priority))
            //                                {
            //                                    comboBox2.SelectedItem = priority;
            //                                }
            //                                else
            //                                {
            //                                    // Если приоритет не найден, установим по умолчанию
            //                                    comboBox2.SelectedItem = "Обычное сообщение";
            //                                }
            //                            }
            //                            else
            //                            {
            //                                comboBox2.SelectedItem = null; // Если приоритет пустой
            //                            }
            //                        }
            //                        else
            //                        {
            //                            MessageBox.Show("Черновик не найден", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //                        }
            //                    }
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                MessageBox.Show("Ошибка при загрузке черновика: " + ex.Message);
            //            }
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
            string[] allowedExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

            while (true)
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    Multiselect = true,
                    Title = "Выберите документы для прикрепления",
                    Filter = "Документы (*.doc;*.docx;*.xls;*.xlsx;*.pdf)|*.doc;*.docx;*.xls;*.xlsx;*.pdf"
                };

                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                {
                    break; // пользователь нажал "Отмена" — выходим из цикла
                }

                foreach (string file in openFileDialog1.FileNames)
                {
                    string extension = Path.GetExtension(file).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        MessageBox.Show("Файл \"" + Path.GetFileName(file) + "\" имеет недопустимый формат и не будет добавлен!",
                            "Недопустимый формат", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    byte[] fileBytes = File.ReadAllBytes(file);
                    string fileHash = GetFileHash(fileBytes);
                    string fileName = Path.GetFileName(file);

                    bool alreadyAttached = attachedFiles.Any(f => f.fileName == fileName && f.fileData.Length == fileBytes.Length);
                    if (alreadyAttached)
                    {
                        MessageBox.Show("Файл \"" + fileName + "\" уже был прикреплён (по содержимому) и не будет добавлен повторно",
                            "Дубликат файла", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }

                    int newId = GetNextId();
                    attachedFiles.Add((newId, fileName, fileBytes, extension, fileHash));

                    bool originalAlreadyExists = originalAttachedFiles.Any(f => f.id == newId);
                    if (!originalAlreadyExists)
                    {
                        originalAttachedFiles.Add((newId, fileName, (byte[])fileBytes.Clone(), extension, fileHash));
                    }

                    comboBox3.Items.Add(fileName);
                    comboBox3.Text = "Прикреплённые документы:";
                }

                UpdateComboBox3(); // Обновляем после каждой порции выбранных файлов
            }
            //OpenFileDialog openFileDialog1 = new OpenFileDialog
            //{
            //    Multiselect = true,
            //    Title = "Выберите документы для прикрепления",
            //    Filter = "Документы (*.doc;*.docx;*.xls;*.xlsx;*.pdf)|*.doc;*.docx;*.xls;*.xlsx;*.pdf"
            //};

            //if (openFileDialog1.ShowDialog() == DialogResult.OK)
            //{
            //    string[] allowedExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

            //    foreach (string file in openFileDialog1.FileNames)
            //    {
            //        string extension = Path.GetExtension(file).ToLower();

            //        if (!allowedExtensions.Contains(extension))
            //        {
            //            MessageBox.Show("Файл \"" + Path.GetFileName(file) + "\" имеет недопустимый формат и не будет добавлен!",
            //                "Недопустимый формат", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //            continue;
            //        }

            //        byte[] fileBytes = File.ReadAllBytes(file);
            //        string fileHash = GetFileHash(fileBytes);

            //        // Проверка на дубликаты по хэшу

            //        string fileName = Path.GetFileName(file);

            //        bool alreadyAttached = attachedFiles.Any(f => f.fileName == fileName && f.fileData.Length == fileBytes.Length);

            //        if (alreadyAttached)
            //        {
            //            MessageBox.Show("Файл \"" + Path.GetFileName(file) + "\" уже был прикреплён (по содержимому) и не будет добавлен повторно",
            //                "Дубликат файла", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //            continue;
            //        }

            //        //string fileName = Path.GetFileName(file);

            //        // Получаем уникальный id для файла
            //        int newId = GetNextId();

            //        // Добавляем файл с уникальным id
            //        attachedFiles.Add((newId, fileName, fileBytes, extension, fileHash));

            //        // 🆕 Добавляем в список оригиналов, если ещё не добавлен документ с таким ID
            //        bool originalAlreadyExists = originalAttachedFiles.Any(f => f.id == newId);
            //        if (!originalAlreadyExists)
            //        {
            //            originalAttachedFiles.Add((newId, fileName, (byte[])fileBytes.Clone(), extension, fileHash));
            //        }

            //        comboBox3.Items.Add(fileName);
            //        comboBox3.Text = "Прикреплённые документы:";
            //    }
            //}
            //UpdateComboBox3();
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


        private List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> originalAttachedFiles =
    new List<(int, string, byte[], string, string)>();

        private void предварительныйПросмотрДокументовToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (attachedFiles.Count == 0)
            {
                MessageBox.Show("Нет доступных документов для предварительного просмотра", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (originalAttachedFiles.Count == 0)
            {
                originalAttachedFiles = attachedFiles
                    .Select(file => (file.id, file.fileName, (byte[])file.fileData.Clone(), file.fileType, file.fileHash))
                    .ToList();
            }

            List<int> selectedIds = ShowDocumentSelectionDialogWithIds(attachedFiles);
            if (selectedIds == null || selectedIds.Count == 0)
                return;

            string connectionString = "server=localhost;user=root;password=1111;database=document_system;";
            HashSet<int> existingDocumentIds = new HashSet<int>();
            HashSet<int> draftDocumentIds = new HashSet<int>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Загружаем ID документов из текущего сообщения (если это не черновик)
                if (CurrentsmessageId != 0)
                {
                    string query = "SELECT id FROM documents WHERE message_id = @msgId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@msgId", CurrentsmessageId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                existingDocumentIds.Add(reader.GetInt32("id"));
                        }
                    }
                }

                // Загружаем ID документов, относящихся к черновику
                if (openedDraftId != 0)
                {
                    string draftQuery = "SELECT id FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                    using (var cmd = new MySqlCommand(draftQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@draftId", openedDraftId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                draftDocumentIds.Add(reader.GetInt32("id"));
                        }
                    }
                }
            }

            foreach (int id in selectedIds)
            {
                var file = attachedFiles.FirstOrDefault(f => f.id == id);
                if (file.fileData == null)
                {
                    MessageBox.Show($"Файл с ID {id} не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                int realId = file.id;
                bool fileExistsInDb = existingDocumentIds.Contains(realId);
                bool fileIsDraft = draftDocumentIds.Contains(realId);

                try
                {
                    if (fileExistsInDb || fileIsDraft)
                    {
                        using (var conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string selectQuery = "SELECT filedata FROM documents WHERE id = @id";
                            using (var cmd = new MySqlCommand(selectQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", realId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                        file.fileData = (byte[])reader["filedata"];
                                }
                            }
                        }
                    }

                    string uniqueKey = $"{file.fileHash}_{file.fileName}";
                    string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");
                    Directory.CreateDirectory(tempDir);
                    string tempPath = Path.Combine(tempDir, uniqueKey);

                    if (!File.Exists(tempPath))
                    {
                        File.WriteAllBytes(tempPath, file.fileData);
                        tempDocumentPaths[uniqueKey] = tempPath;
                    }

                    DateTime originalWriteTime = File.GetLastWriteTime(tempPath);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });

                    Task.Run(async () =>
                    {
                        documentEditCounter++;
                        await Task.Delay(3000);

                        while (true)
                        {
                            try
                            {
                                using (FileStream stream = File.Open(tempPath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                                break;
                            }
                            catch
                            {
                                await Task.Delay(1000);
                            }
                        }

                        try
                        {
                            if (File.Exists(tempPath))
                            {
                                DateTime newWriteTime = File.GetLastWriteTime(tempPath);

                                if (newWriteTime > originalWriteTime)
                                {
                                    byte[] updatedData = File.ReadAllBytes(tempPath);
                                    file.fileData = updatedData;

                                    // Обновляем содержимое файла в БД
                                    if (fileExistsInDb || fileIsDraft)
                                    {
                                        using (var conn = new MySqlConnection(connectionString))
                                        {
                                            conn.Open();
                                            string updateQuery = "UPDATE documents SET filedata = @filedata WHERE id = @id";
                                            using (var cmd = new MySqlCommand(updateQuery, conn))
                                            {
                                                cmd.Parameters.AddWithValue("@filedata", updatedData);
                                                cmd.Parameters.AddWithValue("@id", realId);
                                                cmd.ExecuteNonQuery();
                                            }
                                        }

                                        Invoke(new Action(UpdateComboBox3));
                                    }
                                }

                                // Удаляем временный файл, если он уже есть в базе
                                if (fileExistsInDb || fileIsDraft)
                                {
                                    File.Delete(tempPath);
                                    tempDocumentPaths.Remove(uniqueKey);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Invoke(new Action(() =>
                            {
                                MessageBox.Show($"Ошибка при обработке документа: {ex.Message}",
                                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии \"{file.fileName}\": {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

        private void UpdateComboBox3ForReadOnly()
        {
            comboBox3.Items.Clear();

            var displayList = GetDisplayNamesWithSizes(attachedFiles);

            foreach (var item in displayList)
                comboBox3.Items.Add(item.displayName);

            if (comboBox3.Items.Count > 0)
            {
                //comboBox3.SelectedIndex = 0;
                comboBox3.Text = "Отправленные вам документы:";
            }
        }

        private void UpdateComboBox3()
        {
            comboBox3.Items.Clear();

            var displayList = GetDisplayNamesWithSizes(attachedFiles);

            foreach (var item in displayList)
                comboBox3.Items.Add(item.displayName);

            if (comboBox3.Items.Count > 0)
            {
                //comboBox3.SelectedIndex = 0;
                comboBox3.Text = "Прикреплённые документы:";
            }           
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

            var displayList = GetDisplayNamesWithSizes(files);
            Dictionary<string, int> nameToIdMap = new Dictionary<string, int>();

            foreach (var (displayName, id) in displayList)
            {
                listBox.Items.Add(displayName);
                nameToIdMap[displayName] = id;
            }

            Button ok = new Button() { Text = "Открыть", Left = 210, Width = 75, Top = 230 };
            Button cancel = new Button() { Text = "Отмена", Left = 295, Width = 75, Top = 230, DialogResult = DialogResult.Cancel };

            prompt.Controls.Add(label);
            prompt.Controls.Add(listBox);
            prompt.Controls.Add(ok);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = ok;
            prompt.CancelButton = cancel;

            List<int> selectedIds = null;

            ok.Click += (sender, e) =>
            {
                if (listBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult confirmResult = MessageBox.Show(
                    $"Вы выбрали {listBox.CheckedItems.Count} документ(а)(ов).\nПродолжить?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmResult == DialogResult.Yes)
                {
                    selectedIds = new List<int>();

                    foreach (var item in listBox.CheckedItems)
                    {
                        string displayName = item.ToString();

                        if (nameToIdMap.TryGetValue(displayName, out int id))
                        {
                            selectedIds.Add(id);
                        }
                    }

                    prompt.DialogResult = DialogResult.OK;
                    prompt.Close();
                }
                // Если "Нет" — остаёмся на форме и не закрываем её
            };

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                return selectedIds;
            }

            return null;
        }

        //Функция для форматирования размера файла в читаемый вид
        private string FormatFileSize(long fileSize)
        {
            if (fileSize < 1024)
                return $"{fileSize} байт";
            else if (fileSize < 1024 * 1024)
                return $"{fileSize / 1024} КБ";
            else
                return $"{fileSize / (1024 * 1024)} МБ";
        }

        // Метод генерации отображаемых имён с размерами (если дубликаты по имени)
        private List<(string displayName, int id)> GetDisplayNamesWithSizes(List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> files)
        {
            // Группировка по имени без расширения (например, "ЛР1" у "ЛР1.doc" и "ЛР1.docx")
            var nameGroups = files
                .GroupBy(f => Path.GetFileNameWithoutExtension(f.fileName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<(string displayName, int id)> displayList = new List<(string displayName, int id)>();

            foreach (var file in files)
            {
                string baseName = Path.GetFileNameWithoutExtension(file.fileName);
                string displayName = file.fileName;
                long size = file.fileData?.LongLength ?? 0;

                // Добавляем размер, если дубликаты по имени без расширения
                if (nameGroups.ContainsKey(baseName) && nameGroups[baseName].Count > 1)
                {
                    string readableSize = FormatFileSize(size);
                    displayName += $" ({readableSize})";
                }

                // Убираем ID из отображения (если нужно можно включить обратно)
                displayList.Add((displayName, file.id));
            }

            return displayList;
        }

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
            else
            {
                ShowDocumentSelectionDialog(attachedFiles);
            }

                //DialogResult result = MessageBox.Show(
                //    "Вы можете выбрать один или несколько документов для просмотра.\n\n" +
                //    "Открытие большого количества документов может повлиять на производительность!",
                //    "Предупреждение",
                //    MessageBoxButtons.OKCancel,
                //    MessageBoxIcon.Warning);
        

            //if (result == DialogResult.OK)
            //{
            //    ShowDocumentSelectionDialog(attachedFiles);
            //}
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
                Height = 240,
                CheckOnClick = true
            };

            var displayNamesWithIds = GetDisplayNamesWithSizes(files);
            Dictionary<string, int> nameToIdMap = new Dictionary<string, int>();

            foreach (var (displayName, id) in displayNamesWithIds)
            {
                listBox.Items.Add(displayName);
                nameToIdMap[displayName] = id;
            }

            Button openButton = new Button()
            {
                Text = "Открыть",
                Left = 200,
                Width = 80,
                Top = 265
                // DialogResult НЕ устанавливаем — теперь контролируем вручную
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

            openButton.Click += (sender, e) =>
            {
                if (listBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Question);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Вы выбрали {listBox.CheckedItems.Count} документ(а)(ов).\nПродолжить?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                // Если пользователь нажал "Нет" — остаёмся на форме
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var item in listBox.CheckedItems)
                {
                    string selectedDisplayName = item.ToString();

                    if (nameToIdMap.TryGetValue(selectedDisplayName, out int id))
                    {
                        var selectedFile = files.FirstOrDefault(f => f.id == id);

                        if (selectedFile.fileData != null)
                        {
                            string uniqueName = $"{Guid.NewGuid()}_{selectedFile.fileName}";
                            string tempPath = Path.Combine(Path.GetTempPath(), uniqueName);

                            File.WriteAllBytes(tempPath, selectedFile.fileData);
                            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                        }
                    }
                }
            }

            //Form dialog = new Form()
            //{
            //    Width = 400,
            //    Height = 350,
            //    FormBorderStyle = FormBorderStyle.FixedDialog,
            //    Text = "Выбор документов для просмотра",
            //    StartPosition = FormStartPosition.CenterParent,
            //    MaximizeBox = false,
            //    MinimizeBox = false
            //};

            //CheckedListBox listBox = new CheckedListBox()
            //{
            //    Left = 10,
            //    Top = 10,
            //    Width = 360,
            //    Height = 240
            //};

            //// Получаем список отображаемых имён с размером (если дублируются по имени)
            //var displayNamesWithIds = GetDisplayNamesWithSizes(files);

            //// Сопоставим отображаемое имя с ID для последующего поиска
            //Dictionary<string, int> nameToIdMap = new Dictionary<string, int>();

            //foreach (var (displayName, id) in displayNamesWithIds)
            //{
            //    listBox.Items.Add(displayName);
            //    nameToIdMap[displayName] = id;
            //}

            //Button openButton = new Button()
            //{
            //    Text = "Открыть",
            //    Left = 200,
            //    Width = 80,
            //    Top = 265,
            //    DialogResult = DialogResult.OK
            //};

            //Button cancelButton = new Button()
            //{
            //    Text = "Отмена",
            //    Left = 290,
            //    Width = 80,
            //    Top = 265,
            //    DialogResult = DialogResult.Cancel
            //};

            //dialog.Controls.Add(listBox);
            //dialog.Controls.Add(openButton);
            //dialog.Controls.Add(cancelButton);
            //dialog.AcceptButton = openButton;
            //dialog.CancelButton = cancelButton;

            //if (dialog.ShowDialog() == DialogResult.OK)
            //{
            //    foreach (var item in listBox.CheckedItems)
            //    {
            //        string selectedDisplayName = item.ToString();

            //        if (nameToIdMap.TryGetValue(selectedDisplayName, out int id))
            //        {
            //            // Ищем файл по ID
            //            var selectedFile = files.FirstOrDefault(f => f.id == id);

            //            if (selectedFile.fileData != null)
            //            {
            //                // Создаём уникальное имя файла, используя Guid
            //                string uniqueName = $"{Guid.NewGuid()}_{selectedFile.fileName}";
            //                string tempPath = Path.Combine(Path.GetTempPath(), uniqueName);

            //                File.WriteAllBytes(tempPath, selectedFile.fileData);
            //                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            //            }
            //        }
            //    }
            //}
        }
        private void скачатьВсеДокументыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<(string fileName, byte[] fileData)> filesToSave = new List<(string, byte[])>();

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                if (openedDraftId.HasValue)
                {
                    // Загружаем документы черновика по draft_id и is_draft = 1
                    string query = "SELECT filename, filedata FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string fileName = reader.GetString("filename");
                                byte[] fileData = (byte[])reader["filedata"];
                                filesToSave.Add((fileName, fileData));
                            }
                        }
                    }
                }
                else
                {
                    // Обычные документы берем из уже загруженного списка attachedFiles
                    foreach (var file in attachedFiles)
                    {
                        filesToSave.Add((file.fileName, file.fileData));
                    }
                }
            }

            // Проверка: если документов нет
            if (filesToSave.Count == 0)
            {
                MessageBox.Show("Нет документов для скачивания",
                                "Документы отсутствуют",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            //DialogResult confirm = MessageBox.Show("Вы уверены, что хотите скачать все прикреплённые документы?",
            //                                       "Подтверждение",
            //                                       MessageBoxButtons.YesNo,
            //                                       MessageBoxIcon.Question);

                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string folderPath = dialog.SelectedPath;

                        foreach (var file in filesToSave)
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
            

            //if (attachedFiles.Count == 0)
            //{
            //    MessageBox.Show("Нет документов для скачивания",
            //                    "Документы отсутствуют",
            //                    MessageBoxButtons.OK,
            //                    MessageBoxIcon.Information);
            //    return;
            //}

            //DialogResult confirm = MessageBox.Show("Вы уверены, что хотите скачать все прикреплённые документы?",
            //                                       "Подтверждение",
            //                                       MessageBoxButtons.YesNo,
            //                                       MessageBoxIcon.Question);

            //if (confirm == DialogResult.Yes)
            //{
            //    using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            //    {
            //        if (dialog.ShowDialog() == DialogResult.OK)
            //        {
            //            string folderPath = dialog.SelectedPath;

            //            foreach (var file in attachedFiles)
            //            {
            //                string path = Path.Combine(folderPath, file.fileName);
            //                File.WriteAllBytes(path, file.fileData);
            //            }

            //            MessageBox.Show("Все документы успешно сохранены!",
            //                            "Успех",
            //                            MessageBoxButtons.OK,
            //                            MessageBoxIcon.Information);
            //        }
            //    }
            //}
        }

        //private int CurrentsmessageId; // должна быть в твоём классе, ты явно где-то уже её задаёшь при ответе/редактировании
        private int documentEditCounter = 0;
        private void сброситьИзмененияВДокументахToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (documentEditCounter == 0)
            {
                MessageBox.Show("Изменений в документах ещё не происходило", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "Вы уверены, что хотите сбросить изменения?\n" +
                "Документы будут восстановлены в исходном виде",
                "Подтверждение сброса", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                ResetToOriginalDocuments();
                Task.Run(() => CleanOldTempDocuments());

                MessageBox.Show("Изменения сброшены, документы восстановлены", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе изменений: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ResetToOriginalDocuments()
        {
            // Сначала заменяем список прикреплённых на оригинальные
            attachedFiles = originalAttachedFiles
                .Select(file => (file.id, file.fileName, (byte[])file.fileData.Clone(), file.fileType, file.fileHash))
                .ToList();

            // Обновляем ComboBox
            UpdateComboBox3();

            // Если это сообщение существует в базе
            if (CurrentsmessageId != 0)
            {
                string connectionString = "server=localhost;user=root;password=1111;database=document_system;";
                HashSet<int> existingDocumentIds = new HashSet<int>();

                try
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();

                        // Получаем ID документов из базы, связанных с этим сообщением
                        string getIdsQuery = "SELECT id FROM documents WHERE message_id = @msgId";
                        using (var cmd = new MySqlCommand(getIdsQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@msgId", CurrentsmessageId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                    existingDocumentIds.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    // Обновляем документы, если они есть в базе
                    foreach (var file in originalAttachedFiles)
                    {
                        if (existingDocumentIds.Contains(file.id))
                        {
                            using (var conn = new MySqlConnection(connectionString))
                            {
                                conn.Open();
                                string updateQuery = "UPDATE documents SET filedata = @filedata WHERE id = @id";
                                using (var cmd = new MySqlCommand(updateQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@filedata", file.fileData);
                                    cmd.Parameters.AddWithValue("@id", file.id);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обновлении документов в базе: " + ex.Message,
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            //originalAttachedFiles = null;
        }

        //    private List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)>
        //LoadDocumentsFromDatabase(int messageId)
        //    {
        //        var result = new List<(int, string, byte[], string, string)>();

        //        using (var conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
        //        {
        //            conn.Open();
        //            string query = "SELECT id, filename, filedata, filetype FROM documents WHERE message_id = @messageId";
        //            using (var cmd = new MySqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@messageId", messageId);
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        result.Add((
        //                            reader.GetInt32("id"),
        //                            reader.GetString("filename"),
        //                            (byte[])reader["filedata"],
        //                            reader.GetString("filetype"),
        //                            "" // пустой fileHash, которого нет в БД
        //                        ));
        //                    }
        //                }
        //            }
        //        }

        //        return result;
        //    }

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
                    UpdateComboBox3();
                }
                else if (dialogResult == DialogResult.No)
                {
                    // Очистить только выбранные документы
                    ShowDocumentDeletionDialog(attachedFiles);
                    UpdateComboBox3();
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
        "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Очистим все из comboBox3 и списка attachedFiles
                comboBox3.Items.Clear();
                attachedFiles.Clear();

                try
                {
                    using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                    {
                        conn.Open();

                        string query;
                        MySqlCommand cmd;

                        if (openedDraftId.HasValue)
                        {
                            // Если открыт черновик — удаляем по draft_id и is_draft = 1
                            query = "DELETE FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                            cmd = new MySqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                        }
                        else
                        {
                            // Иначе — удаляем по message_id (для обычных писем)
                            query = "DELETE FROM documents WHERE message_id = @messageId";
                            cmd = new MySqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@messageId", replyingToMessageId);
                        }

                        cmd.ExecuteNonQuery();
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

            //    if (MessageBox.Show("Вы уверены, что хотите очистить все прикреплённые документы?",
            //"Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            //    {
            //        // Очистим все из comboBox3 и списка attachedFiles
            //        comboBox3.Items.Clear();
            //        attachedFiles.Clear();

            //        // Удалим все документы из базы данных для текущего сообщения
            //        try
            //        {
            //            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //            {
            //                conn.Open();
            //                string query = "DELETE FROM documents WHERE message_id = @messageId";

            //                using (MySqlCommand cmd = new MySqlCommand(query, conn))
            //                {
            //                    cmd.Parameters.AddWithValue("@messageId", replyingToMessageId);
            //                    cmd.ExecuteNonQuery();
            //                }
            //            }

            //            MessageBox.Show("Все выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //            comboBox3.Text = "Пусто";
            //            comboBox3.Items.Clear();
            //        }
            //        catch (Exception ex)
            //        {
            //            MessageBox.Show("Ошибка при удалении документов: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        }
            //    }
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
                Height = 240,
                CheckOnClick = true
            };

            var displayNamesWithIds = GetDisplayNamesWithSizes(files);
            Dictionary<string, int> displayNameToId = new Dictionary<string, int>();

            foreach (var (displayName, id) in displayNamesWithIds)
            {
                listBox.Items.Add(displayName);
                displayNameToId[displayName] = id;
            }

            Button deleteButton = new Button()
            {
                Text = "Удалить",
                Left = 200,
                Width = 80,
                Top = 265
                // DialogResult НЕ устанавливаем, контролируем вручную
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

            deleteButton.Click += (sender, e) =>
            {
                if (listBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Вы действительно хотите удалить {listBox.CheckedItems.Count} документ(а)(ов)? Продолжить?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedDisplayNames = listBox.CheckedItems.Cast<string>().ToList();

                foreach (var displayName in selectedDisplayNames)
                {
                    if (displayNameToId.TryGetValue(displayName, out int id))
                    {
                        var selectedFile = files.FirstOrDefault(f => f.id == id);

                        if (selectedFile.fileData != null)
                        {
                            try
                            {
                                if (selectedFile.id != -1)
                                {
                                    using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                                    {
                                        conn.Open();

                                        string query;
                                        MySqlCommand cmd;

                                        if (openedDraftId.HasValue)
                                        {
                                            query = "DELETE FROM documents WHERE id = @id AND is_draft = 1";
                                            cmd = new MySqlCommand(query, conn);
                                            cmd.Parameters.AddWithValue("@id", selectedFile.id);
                                        }
                                        else
                                        {
                                            query = "DELETE FROM documents WHERE id = @id";
                                            cmd = new MySqlCommand(query, conn);
                                            cmd.Parameters.AddWithValue("@id", selectedFile.id);
                                        }

                                        cmd.ExecuteNonQuery();
                                    }
                                }

                                attachedFiles.Remove(selectedFile);
                                comboBox3.Items.Remove(selectedFile.fileName);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Ошибка при удалении файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }

                MessageBox.Show("Выбранные документы были удалены", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (comboBox3.Items.Count == 0)
                    comboBox3.Text = "Пусто";
            }

            //Form dialog = new Form()
            //{
            //    Width = 400,
            //    Height = 350,
            //    FormBorderStyle = FormBorderStyle.FixedDialog,
            //    Text = "Выбор документов для удаления",
            //    StartPosition = FormStartPosition.CenterParent,
            //    MaximizeBox = false,
            //    MinimizeBox = false
            //};

            //CheckedListBox listBox = new CheckedListBox()
            //{
            //    Left = 10,
            //    Top = 10,
            //    Width = 360,
            //    Height = 240
            //};

            //var displayNamesWithIds = GetDisplayNamesWithSizes(files);
            //Dictionary<string, int> displayNameToId = new Dictionary<string, int>();

            //foreach (var (displayName, id) in displayNamesWithIds)
            //{
            //    listBox.Items.Add(displayName);
            //    displayNameToId[displayName] = id;
            //}

            //Button deleteButton = new Button()
            //{
            //    Text = "Удалить",
            //    Left = 200,
            //    Width = 80,
            //    Top = 265,
            //    DialogResult = DialogResult.OK
            //};

            //Button cancelButton = new Button()
            //{
            //    Text = "Отмена",
            //    Left = 290,
            //    Width = 80,
            //    Top = 265,
            //    DialogResult = DialogResult.Cancel
            //};

            //dialog.Controls.Add(listBox);
            //dialog.Controls.Add(deleteButton);
            //dialog.Controls.Add(cancelButton);
            //dialog.AcceptButton = deleteButton;
            //dialog.CancelButton = cancelButton;

            //if (dialog.ShowDialog() == DialogResult.OK)
            //{
            //    var selectedDisplayNames = listBox.CheckedItems.Cast<string>().ToList();

            //    foreach (var displayName in selectedDisplayNames)
            //    {
            //        if (displayNameToId.TryGetValue(displayName, out int id))
            //        {
            //            var selectedFile = files.FirstOrDefault(f => f.id == id);

            //            if (selectedFile.fileData != null)
            //            {
            //                try
            //                {
            //                    if (selectedFile.id != -1)
            //                    {
            //                        using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //                        {
            //                            conn.Open();

            //                            string query;
            //                            MySqlCommand cmd;

            //                            if (openedDraftId.HasValue)
            //                            {
            //                                // Если это черновик — удаляем по id и is_draft = 1
            //                                query = "DELETE FROM documents WHERE id = @id AND is_draft = 1";
            //                                cmd = new MySqlCommand(query, conn);
            //                                cmd.Parameters.AddWithValue("@id", selectedFile.id);
            //                            }
            //                            else
            //                            {
            //                                // Иначе — обычное удаление по id
            //                                query = "DELETE FROM documents WHERE id = @id";
            //                                cmd = new MySqlCommand(query, conn);
            //                                cmd.Parameters.AddWithValue("@id", selectedFile.id);
            //                            }

            //                            cmd.ExecuteNonQuery();
            //                        }
            //                    }

            //                    // Удаляем локально из списка и UI
            //                    attachedFiles.Remove(selectedFile);
            //                    comboBox3.Items.Remove(selectedFile.fileName);
            //                }
            //                catch (Exception ex)
            //                {
            //                    MessageBox.Show("Ошибка при удалении файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //                }
            //            }
            //        }
            //    }

            //    MessageBox.Show("Выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //    if (comboBox3.Items.Count == 0)
            //        comboBox3.Text = "Пусто";
            //}
        }

        private List<int> ShowSealTargetSelectionDialog()
        {
            if (attachedFiles.Count == 0)
            {
                MessageBox.Show("Нет документов для подписания", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            Form dialog = new Form()
            {
                Width = 400,
                Height = 350,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Выбор документов для подписи",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            CheckedListBox listBox = new CheckedListBox()
            {
                Left = 10,
                Top = 10,
                Width = 360,
                Height = 240,
                CheckOnClick = true
            };

            Dictionary<string, int> displayNameToId = new Dictionary<string, int>();

            foreach (var file in attachedFiles)
            {
                string displayName = file.fileName;

                if (displayNameToId.ContainsKey(displayName))
                    displayName += $" ({file.id})";

                displayNameToId[displayName] = file.id;
                listBox.Items.Add(displayName);
            }

            Button okButton = new Button()
            {
                Text = "Подписать",
                Left = 200,
                Width = 80,
                Top = 265
                // DialogResult НЕ устанавливаем — контролируем вручную
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
            dialog.Controls.Add(okButton);
            dialog.Controls.Add(cancelButton);
            dialog.AcceptButton = okButton;
            dialog.CancelButton = cancelButton;

            okButton.Click += (sender, e) =>
            {
                if (listBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Вы действительно хотите подписать {listBox.CheckedItems.Count} документ(а)(ов)? Продолжить?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                List<int> selectedIds = new List<int>();

                foreach (var item in listBox.CheckedItems)
                {
                    string displayName = item.ToString();
                    if (displayNameToId.TryGetValue(displayName, out int id))
                    {
                        selectedIds.Add(id);
                    }
                }

                return selectedIds;
            }

            return null;

            //// Если нет прикреплённых файлов — выход
            //if (attachedFiles.Count == 0)
            //{
            //    MessageBox.Show("Нет документов для подписания", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return null;
            //}

            //// Создаём диалоговое окно
            //Form dialog = new Form()
            //{
            //    Width = 400,
            //    Height = 350,
            //    FormBorderStyle = FormBorderStyle.FixedDialog,
            //    Text = "Выбор документов для подписи",
            //    StartPosition = FormStartPosition.CenterParent,
            //    MaximizeBox = false,
            //    MinimizeBox = false
            //};

            //CheckedListBox listBox = new CheckedListBox()
            //{
            //    Left = 10,
            //    Top = 10,
            //    Width = 360,
            //    Height = 240
            //};

            //// Сопоставим displayName с ID
            //Dictionary<string, int> displayNameToId = new Dictionary<string, int>();

            //foreach (var file in attachedFiles)
            //{
            //    string displayName = file.fileName;

            //    if (displayNameToId.ContainsKey(displayName))
            //        displayName += $" ({file.id})"; // если имена совпадают, добавим id

            //    displayNameToId[displayName] = file.id;
            //    listBox.Items.Add(displayName);
            //}

            //Button okButton = new Button()
            //{
            //    Text = "Подписать",
            //    Left = 200,
            //    Width = 80,
            //    Top = 265,
            //    DialogResult = DialogResult.OK
            //};

            //Button cancelButton = new Button()
            //{
            //    Text = "Отмена",
            //    Left = 290,
            //    Width = 80,
            //    Top = 265,
            //    DialogResult = DialogResult.Cancel
            //};

            //dialog.Controls.Add(listBox);
            //dialog.Controls.Add(okButton);
            //dialog.Controls.Add(cancelButton);
            //dialog.AcceptButton = okButton;
            //dialog.CancelButton = cancelButton;

            //if (dialog.ShowDialog() == DialogResult.OK)
            //{
            //    List<int> selectedIds = new List<int>();

            //    foreach (var item in listBox.CheckedItems)
            //    {
            //        string displayName = item.ToString();
            //        if (displayNameToId.TryGetValue(displayName, out int id))
            //        {
            //            selectedIds.Add(id);
            //        }
            //    }

            //    return selectedIds;
            //}

            //return null;
        }


        private byte[] AddSealToDocument(byte[] docData, string sealPath, string fileName)
        {
            string tempDocPath = Path.GetTempFileName();
            string tempOutputPath = Path.GetTempFileName();
            File.WriteAllBytes(tempDocPath, docData);

            string extension = Path.GetExtension(fileName).ToLower();

            try
            {
                if (extension == ".doc" || extension == ".docx")
                {
                    var wordApp = new Microsoft.Office.Interop.Word.Application();
                    var doc = wordApp.Documents.Open(tempDocPath, ReadOnly: false, Visible: false);
                    wordApp.Visible = false;

                    var range = doc.Content;
                    bool sealInserted = false;

                    while (true)
                    {
                        var find = range.Find;
                        find.ClearFormatting();
                        find.Text = "М.П.";
                        find.Forward = true;
                        find.Wrap = Microsoft.Office.Interop.Word.WdFindWrap.wdFindStop;

                        if (!find.Execute()) break;

                        var position = range.Duplicate;
                        position.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseStart);
                        position.InlineShapes.AddPicture(sealPath, false, true, position);
                        sealInserted = true;

                        range.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
                    }

                    if (!sealInserted)
                    {
                        var endRange = doc.Content;
                        endRange.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
                        endRange.InlineShapes.AddPicture(sealPath, false, true, endRange);
                    }

                    doc.SaveAs2(tempOutputPath);
                    doc.Close(false);
                    wordApp.Quit();

                    return File.ReadAllBytes(tempOutputPath);
                }
                else if (extension == ".pdf")
                {
                    using (var input = new MemoryStream(docData))
                    using (var output = new MemoryStream())
                    {
                        var positions = new List<Tuple<int, double, double>>();

                        using (var pdfDoc = UglyToad.PdfPig.PdfDocument.Open(input))
                        {
                            int pageIndex = 0;
                            foreach (var page in pdfDoc.GetPages())
                            {
                                var words = page.GetWords();
                                var mpWords = words.Where(w => w.Text.Contains("М.П.")).ToList();
                                foreach (var word in mpWords)
                                {
                                    double x = word.BoundingBox.Left;
                                    double y = page.Height - word.BoundingBox.Top;
                                    positions.Add(Tuple.Create(pageIndex, x + 5, y - 5));
                                }
                                pageIndex++;
                            }

                            // Если нет "М.П.", ищем последнее слово
                            if (positions.Count == 0)
                            {
                                var lastWord = pdfDoc.GetPages()
                                    .SelectMany(p => p.GetWords().Select(w => (page: p, word: w)))
                                    .LastOrDefault();

                                if (lastWord.word != null)
                                {
                                    double x = lastWord.word.BoundingBox.Left + 5;
                                    double y = lastWord.page.Height - lastWord.word.BoundingBox.Top - 5;
                                    positions.Add(Tuple.Create(lastWord.page.Number - 1, x, y));
                                }
                                else
                                {
                                    // Если даже слов нет — ставим внизу последней страницы
                                    var lastPage = pdfDoc.GetPages().LastOrDefault();
                                    if (lastPage != null)
                                    {
                                        positions.Add(Tuple.Create(lastPage.Number - 1, 50.0, lastPage.Height - 150));
                                    }
                                }
                            }
                        }

                        using (var input2 = new MemoryStream(docData))
                        {
                            var document = PdfSharp.Pdf.IO.PdfReader.Open(input2, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
                            var sealImage = PdfSharp.Drawing.XImage.FromFile(sealPath);
                            double sealWidth = 100;
                            double sealHeight = 100;

                            foreach (var tuple in positions)
                            {
                                var page = document.Pages[tuple.Item1];
                                using (var gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page, PdfSharp.Drawing.XGraphicsPdfPageOptions.Append))
                                {
                                    gfx.DrawImage(sealImage, tuple.Item2, tuple.Item3, sealWidth, sealHeight);
                                }
                            }

                            document.Save(output);
                            return output.ToArray();
                        }
                    }
                }
                else if (extension == ".xlsx" || extension == ".xls")
                {
                    var excelApp = new Microsoft.Office.Interop.Excel.Application();
                    var workbook = excelApp.Workbooks.Open(tempDocPath);
                    excelApp.Visible = false;

                    foreach (Microsoft.Office.Interop.Excel.Worksheet sheet in workbook.Sheets)
                    {
                        var usedRange = sheet.UsedRange;
                        var findRange = usedRange.Find("М.П.");

                        if (findRange != null)
                        {
                            float leftPos = (float)findRange.Left;
                            float topPos = (float)findRange.Top;

                            sheet.Shapes.AddPicture(sealPath,
                                Microsoft.Office.Core.MsoTriState.msoFalse,
                                Microsoft.Office.Core.MsoTriState.msoCTrue,
                                leftPos + 20, topPos, 100, 100);
                        }
                        else
                        {
                            int lastRow = usedRange.Row + usedRange.Rows.Count - 1;
                            int lastCol = usedRange.Column + usedRange.Columns.Count - 1;

                            var bottomRightCell = (Microsoft.Office.Interop.Excel.Range)sheet.Cells[lastRow, lastCol];
                            float left = (float)bottomRightCell.Left;
                            float top = (float)bottomRightCell.Top;

                            sheet.Shapes.AddPicture(sealPath,
                                Microsoft.Office.Core.MsoTriState.msoFalse,
                                Microsoft.Office.Core.MsoTriState.msoCTrue,
                                left + 20, top + 20, 100, 100);
                        }
                    }

                    workbook.SaveAs(tempOutputPath);
                    workbook.Close(false);
                    excelApp.Quit();

                    return File.ReadAllBytes(tempOutputPath);
                }
                else
                {
                    MessageBox.Show("Неподдерживаемый формат для вставки печати", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return docData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка вставки печати: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return docData;
            }
            finally
            {
                try { if (File.Exists(tempDocPath)) File.Delete(tempDocPath); } catch { }
                try { if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath); } catch { }
            }

            //string tempDocPath = Path.GetTempFileName();
            //string tempOutputPath = Path.GetTempFileName();
            //File.WriteAllBytes(tempDocPath, docData);

            //string extension = Path.GetExtension(fileName).ToLower();

            //try
            //{
            //    if (extension == ".doc" || extension == ".docx")
            //    {
            //        var wordApp = new Microsoft.Office.Interop.Word.Application();
            //        var doc = wordApp.Documents.Open(tempDocPath, ReadOnly: false, Visible: false);
            //        wordApp.Visible = false;

            //        var range = doc.Content;
            //        bool sealInserted = false;

            //        while (true)
            //        {
            //            var find = range.Find;
            //            find.ClearFormatting();
            //            find.Text = "М.П.";
            //            find.Forward = true;
            //            find.Wrap = Microsoft.Office.Interop.Word.WdFindWrap.wdFindStop;

            //            if (!find.Execute()) break;

            //            range.InlineShapes.AddPicture(sealPath, false, true, range);
            //            range.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
            //            sealInserted = true;
            //        }

            //        if (!sealInserted)
            //        {
            //            var endRange = doc.Content;
            //            endRange.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
            //            endRange.InlineShapes.AddPicture(sealPath, false, true, endRange);
            //        }

            //        doc.SaveAs2(tempOutputPath);
            //        doc.Close(false);
            //        wordApp.Quit();

            //        return File.ReadAllBytes(tempOutputPath);
            //    }
            //    else if (extension == ".pdf")
            //    {
            //        using (var input = new MemoryStream(docData))
            //        using (var output = new MemoryStream())
            //        {
            //            var positions = new List<Tuple<int, double, double>>();

            //            using (var pdfDoc = UglyToad.PdfPig.PdfDocument.Open(input))
            //            {
            //                int pageIndex = 0;
            //                foreach (var page in pdfDoc.GetPages())
            //                {
            //                    var words = page.GetWords();
            //                    var mpWords = words.Where(w => w.Text.Contains("М.П.")).ToList();
            //                    foreach (var word in mpWords)
            //                    {
            //                        double x = word.BoundingBox.Left;
            //                        double y = page.Height - word.BoundingBox.Top;
            //                        positions.Add(Tuple.Create(pageIndex, x + 5, y - 5));
            //                    }
            //                    pageIndex++;
            //                }
            //            }

            //            using (var input2 = new MemoryStream(docData))
            //            {
            //                var document = PdfSharp.Pdf.IO.PdfReader.Open(input2, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
            //                var sealImage = XImage.FromFile(sealPath);
            //                double sealWidth = 100;
            //                double sealHeight = 100;

            //                if (positions.Count > 0)
            //                {
            //                    foreach (var tuple in positions)
            //                    {
            //                        var page = document.Pages[tuple.Item1];
            //                        using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
            //                        {
            //                            gfx.DrawImage(sealImage, tuple.Item2, tuple.Item3, sealWidth, sealHeight);
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    var lastPage = document.Pages[document.Pages.Count - 1];
            //                    using (XGraphics gfx = XGraphics.FromPdfPage(lastPage, XGraphicsPdfPageOptions.Append))
            //                    {
            //                        double x = 50;
            //                        double y = lastPage.Height - 150;
            //                        gfx.DrawImage(sealImage, x, y, sealWidth, sealHeight);
            //                    }
            //                }

            //                document.Save(output);
            //                return output.ToArray();
            //            }
            //        }
            //    }
            //    else if (extension == ".xlsx" || extension == ".xls")
            //    {
            //        var excelApp = new Microsoft.Office.Interop.Excel.Application();
            //        var workbook = excelApp.Workbooks.Open(tempDocPath);
            //        excelApp.Visible = false;

            //        foreach (Microsoft.Office.Interop.Excel.Worksheet sheet in workbook.Sheets)
            //        {
            //            var usedRange = sheet.UsedRange;
            //            var findRange = usedRange.Find("М.П.");

            //            if (findRange != null)
            //            {
            //                float leftPos = (float)findRange.Left;
            //                float topPos = (float)findRange.Top;

            //                sheet.Shapes.AddPicture(sealPath,
            //                    Microsoft.Office.Core.MsoTriState.msoFalse,
            //                    Microsoft.Office.Core.MsoTriState.msoCTrue,
            //                    leftPos + 20, topPos, 100, 100);
            //            }
            //            else
            //            {
            //                var lastRow = usedRange.Row + usedRange.Rows.Count;
            //                var firstCol = usedRange.Column;

            //                var bottomCell = (Microsoft.Office.Interop.Excel.Range)sheet.Cells[lastRow, firstCol];
            //                float left = (float)bottomCell.Left;
            //                float top = (float)bottomCell.Top;

            //                sheet.Shapes.AddPicture(sealPath,
            //                    Microsoft.Office.Core.MsoTriState.msoFalse,
            //                    Microsoft.Office.Core.MsoTriState.msoCTrue,
            //                    left + 20, top + 20, 100, 100);
            //            }
            //        }

            //        workbook.SaveAs(tempOutputPath);
            //        workbook.Close(false);
            //        excelApp.Quit();

            //        return File.ReadAllBytes(tempOutputPath);
            //    }
            //    else
            //    {
            //        MessageBox.Show("Неподдерживаемый формат для вставки печати", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //        return docData;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Ошибка вставки печати: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return docData;
            //}
            //finally
            //{
            //    try { if (File.Exists(tempDocPath)) File.Delete(tempDocPath); } catch { }
            //    try { if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath); } catch { }
            //}
        }


        //else if (extension == ".xlsx" || extension == ".xls")
        //{
        //    var excelApp = new Microsoft.Office.Interop.Excel.Application();
        //    var workbook = excelApp.Workbooks.Open(tempDocPath);
        //    excelApp.Visible = false;

        //    foreach (Microsoft.Office.Interop.Excel.Worksheet sheet in workbook.Sheets)
        //    {
        //        var usedRange = sheet.UsedRange;
        //        var findRange = usedRange.Find("М.П.");

        //        if (findRange != null)
        //        {
        //            float leftPos = (float)findRange.Left;
        //            float topPos = (float)findRange.Top;

        //            sheet.Shapes.AddPicture(sealPath,
        //                Microsoft.Office.Core.MsoTriState.msoFalse,
        //                Microsoft.Office.Core.MsoTriState.msoCTrue,
        //                leftPos + 20, topPos, 100, 100);
        //        }
        //        else
        //        {
        //            var lastRow = usedRange.Row + usedRange.Rows.Count;
        //            var firstCol = usedRange.Column;

        //            var bottomCell = (Microsoft.Office.Interop.Excel.Range)sheet.Cells[lastRow, firstCol];
        //            float left = (float)bottomCell.Left;
        //            float top = (float)bottomCell.Top;

        //            sheet.Shapes.AddPicture(sealPath,
        //                Microsoft.Office.Core.MsoTriState.msoFalse,
        //                Microsoft.Office.Core.MsoTriState.msoCTrue,
        //                left + 20, top + 20, 100, 100);
        //        }
        //    }

        //    workbook.SaveAs(tempOutputPath);
        //    workbook.Close(false);
        //    excelApp.Quit();

        //    return File.ReadAllBytes(tempOutputPath);
        //}



         //else if (extension == ".pdf")
         //       {
         //           using (var input = new MemoryStream(docData))
         //           using (var output = new MemoryStream())
         //           {
         //               var positions = new List<Tuple<int, double, double>>();

         //               using (var pdfDoc = UglyToad.PdfPig.PdfDocument.Open(input))
         //               {
         //                   int pageIndex = 0;
         //                   foreach (var page in pdfDoc.GetPages())
         //                   {
         //                       var words = page.GetWords();
         //                       var mpWords = words.Where(w => w.Text.Contains("М.П.")).ToList();
         //                       foreach (var word in mpWords)
         //                       {
         //                           double x = word.BoundingBox.Left;
         //                           double y = page.Height - word.BoundingBox.Top;
         //                           positions.Add(Tuple.Create(pageIndex, x + 5, y - 5));
         //                       }
         //                       pageIndex++;
         //                   }
         //               }

         //               if (positions.Count == 0)
         //               {
         //                   MessageBox.Show("Не найдено ни одного вхождения 'М.П.' в PDF-документе. Печать не вставлена.",
         //                                   "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
         //                   return docData;
         //               }

         //               using (var input2 = new MemoryStream(docData))
         //               {
         //                   var document = PdfSharp.Pdf.IO.PdfReader.Open(input2, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
         //                   var sealImage = XImage.FromFile(sealPath);

         //                   foreach (var tuple in positions)
         //                   {
         //                       var page = document.Pages[tuple.Item1];
         //                       using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
         //                       {
         //                           gfx.DrawImage(sealImage, tuple.Item2, tuple.Item3, 50, 50);
         //                       }
         //                   }

         //                   document.Save(output);
         //                   return output.ToArray();
         //               }
         //           }
         //       }


        //string tempDocPath = Path.GetTempFileName();
        //string tempOutputPath = Path.GetTempFileName();

        //File.WriteAllBytes(tempDocPath, docData);

        //Microsoft.Office.Interop.Word.Application wordApp = null;
        //Microsoft.Office.Interop.Word.Document doc = null;

        //try
        //{
        //    wordApp = new Microsoft.Office.Interop.Word.Application();
        //    doc = wordApp.Documents.Open(tempDocPath, ReadOnly: false, Visible: false);
        //    wordApp.Visible = false;

        //    Microsoft.Office.Interop.Word.Range contentRange = doc.Content;
        //    Microsoft.Office.Interop.Word.Find find = contentRange.Find;

        //    find.ClearFormatting();
        //    find.Text = "М.П.";
        //    find.Forward = true;
        //    find.Wrap = Microsoft.Office.Interop.Word.WdFindWrap.wdFindStop;

        //    Microsoft.Office.Interop.Word.Range targetRange;

        //    if (find.Execute())
        //    {
        //        targetRange = contentRange.Duplicate;
        //    }
        //    else
        //    {
        //        targetRange = doc.Content;
        //        targetRange.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
        //    }

        //    targetRange.InlineShapes.AddPicture(sealPath, false, true, targetRange);

        //    doc.SaveAs2(tempOutputPath);
        //    doc.Close(false);
        //    wordApp.Quit();

        //    return File.ReadAllBytes(tempOutputPath);
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show($"Ошибка вставки печати: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    return docData;
        //}
        //finally
        //{
        //    try { if (doc != null) Marshal.ReleaseComObject(doc); } catch { }
        //    try { if (wordApp != null) Marshal.ReleaseComObject(wordApp); } catch { }
        //    try { if (File.Exists(tempDocPath)) File.Delete(tempDocPath); } catch { }
        //    try { if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath); } catch { }
        //}       


        private void InsertSealIntoDocuments(List<int> selectedDocIds)
        {
            string sealImagePath = @"D:\diplom\ПримерПечати.png"; // Заменишь позже на свой путь
            string connectionString = "server=localhost;user=root;password=1111;database=document_system;";

            HashSet<int> existingDocumentIds = new HashSet<int>();
            HashSet<int> draftDocumentIds = new HashSet<int>();

            if (CurrentsmessageId != 0 || openedDraftId != 0)
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    if (CurrentsmessageId != 0)
                    {
                        using (var cmd = new MySqlCommand("SELECT id FROM documents WHERE message_id = @msgId", conn))
                        {
                            cmd.Parameters.AddWithValue("@msgId", CurrentsmessageId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                    existingDocumentIds.Add(reader.GetInt32("id"));
                            }
                        }
                    }

                    if (openedDraftId != 0)
                    {
                        using (var cmd = new MySqlCommand("SELECT id FROM documents WHERE draft_id = @draftId AND is_draft = 1", conn))
                        {
                            cmd.Parameters.AddWithValue("@draftId", openedDraftId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                    draftDocumentIds.Add(reader.GetInt32("id"));
                            }
                        }
                    }
                }
            }

            foreach (int docId in selectedDocIds)
            {
                var file = attachedFiles.FirstOrDefault(f => f.id == docId);
                if (file.id == -1)
                    continue;

                int realId = file.id;
                bool inDb = existingDocumentIds.Contains(realId) || draftDocumentIds.Contains(realId);

                byte[] fileBytes = null;

                if (inDb)
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand("SELECT filedata FROM documents WHERE id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", realId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                    fileBytes = (byte[])reader["filedata"];
                            }
                        }
                    }
                }
                else
                {
                    string uniqueKey = $"{file.fileHash}_{file.fileName}";
                    if (tempDocumentPaths.TryGetValue(uniqueKey, out string tempPath) && File.Exists(tempPath))
                    {
                        fileBytes = File.ReadAllBytes(tempPath);
                    }
                    else
                    {
                        fileBytes = file.fileData;
                    }
                }

                if (fileBytes == null)
                    continue;

                byte[] modifiedBytes = AddSealToDocument(fileBytes, sealImagePath, file.fileName);

                if (inDb)
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand("UPDATE documents SET filedata = @filedata WHERE id = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@filedata", modifiedBytes);
                            cmd.Parameters.AddWithValue("@id", realId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    file.fileData = modifiedBytes;
                    Invoke(new Action(UpdateComboBox3));
                }
                else
                {
                    string uniqueKey = $"{file.fileHash}_{file.fileName}";
                    string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");
                    Directory.CreateDirectory(tempDir);
                    string tempPath = Path.Combine(tempDir, uniqueKey);

                    File.WriteAllBytes(tempPath, modifiedBytes);
                    tempDocumentPaths[uniqueKey] = tempPath;

                    file.fileData = modifiedBytes;
                }
            }

            MessageBox.Show("Печати успешно добавлены!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                var selectedDocs = ShowSealTargetSelectionDialog();
                if (selectedDocs != null && selectedDocs.Count > 0)
                {
                    InsertSealIntoDocuments(selectedDocs);
                }
                else
                {
                    checkBox1.Checked = false;
                }
            }

            //if (checkBox1.Checked)
            //{
            //    var selectedDocs = ShowSealTargetSelectionDialog();
            //    if (selectedDocs != null && selectedDocs.Count > 0)
            //    {
            //        // Тут будет вставка печати
            //        // ApplyVisualSealToDocuments(selectedDocs);
            //    }
            //    else
            //    {
            //        // если пользователь ничего не выбрал — снимаем галочку
            //        checkBox1.Checked = false;
            //    }
            //}
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            List<(string fileName, byte[] fileData)> filesToSave = new List<(string, byte[])>();

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                if (openedDraftId.HasValue)
                {
                    // Загружаем документы черновика по draft_id и is_draft = 1
                    string query = "SELECT filename, filedata FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@draftId", openedDraftId.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string fileName = reader.GetString("filename");
                                byte[] fileData = (byte[])reader["filedata"];
                                filesToSave.Add((fileName, fileData));
                            }
                        }
                    }
                }
                else
                {
                    // Обычные документы берем из уже загруженного списка attachedFiles
                    foreach (var file in attachedFiles)
                    {
                        filesToSave.Add((file.fileName, file.fileData));
                    }
                }
            }

            // Проверка: если документов нет
            if (filesToSave.Count == 0)
            {
                MessageBox.Show("Нет документов для скачивания",
                                "Документы отсутствуют",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            //DialogResult confirm = MessageBox.Show("Вы уверены, что хотите скачать все прикреплённые документы?",
            //                                       "Подтверждение",
            //                                       MessageBoxButtons.YesNo,
            //                                       MessageBoxIcon.Question);

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string folderPath = dialog.SelectedPath;

                    foreach (var file in filesToSave)
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
}
