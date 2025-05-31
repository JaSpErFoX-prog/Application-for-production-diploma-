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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtlantPrograma
{
    public partial class Form8 : Form
    {
        List<string> selectedDepartments;
        private string senderUsername;
        private int CurrentsmessageId = -1; // -1 — значит новое сообщение, ещё без ID
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

            скачатьВсеДокументыToolStripMenuItem.Enabled = false;
            просмотретьДокументыToolStripMenuItem.Enabled = false;
            toolTip1.SetToolTip(pictureBox1, "Прикрепить документы");
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

        private Dictionary<string, string> tempDocumentPaths = new Dictionary<string, string>();

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

            // Проверка типа прикреплённых документов, если требуется подпись
            if (checkBox1.Checked)
            {
                string[] allowedExtensions = { ".doc", ".docx", ".xls", ".xlsx", ".pdf" };

                var validFiles = attachedFiles
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f.fileName).ToLower()))
                    .ToList();

                if (validFiles.Count == 0)
                {
                    MessageBox.Show("Вы отметили, что необходимо подписать документы, но ни одного допустимого документа не добавлено.\n" +
                                    "Пожалуйста, прикрепите хотя бы один файл в формате .doc, .docx, .xls, .xlsx или .pdf",
                                    "Предупреждение",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                    return;
                }
            }

            List<string> allRecipients = new List<string>();
            string senderPhone = "";
            string senderDept = "";
            string date = DateTime.Now.ToString("dd.MM.yyyy");
            string time = DateTime.Now.ToString("HH:mm:ss");

            CurrentsmessageId = -1; // сброс перед новой отправкой

            using (var con = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                con.Open();

                foreach (string departmentName in selectedDepartments)
                {
                    int departmentId = -1;
                    using (var cmd = new MySqlCommand("SELECT id FROM departments WHERE name = @name", con))
                    {
                        cmd.Parameters.AddWithValue("@name", departmentName);
                        var result = cmd.ExecuteScalar();
                        if (result != null) departmentId = Convert.ToInt32(result);
                    }

                    if (departmentId == -1) continue;

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
                                if (!allRecipients.Contains(recipient))
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
                            senderDept = selectedDepartments.FirstOrDefault(); // или другое логичное значение
                        }
                    }
                }

                // Подготовка файлов
                string tempDocumentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");

                List<(string fileName, byte[] fileData, string fileType)> filesToAttach = new List<(string, byte[], string)>();

                foreach (var file in attachedFiles)
                {
                    byte[] actualData = null;
                    string uniqueKey = $"{file.fileHash}_{file.fileName}";

                    if (tempDocumentPaths.TryGetValue(uniqueKey, out string tempPath) && File.Exists(tempPath))
                    {
                        try { actualData = File.ReadAllBytes(tempPath); }
                        catch { actualData = file.fileData; }
                    }
                    else
                    {
                        actualData = file.fileData;
                    }

                    filesToAttach.Add((file.fileName, actualData, file.fileType));
                }

                // Отправка писем
                bool isFirstMessage = true;

                foreach (string recipient in allRecipients)
                {
                    long messageId;

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

                        messageId = cmd.LastInsertedId;

                        // сохраняем только первый messageId
                        if (isFirstMessage)
                        {
                            CurrentsmessageId = (int)messageId;
                            isFirstMessage = false;
                        }
                    }

                    // Вложения
                    foreach (var doc in filesToAttach)
                    {
                        using (var docCmd = new MySqlCommand(
                            @"INSERT INTO documents 
                      (message_id, filename, filedata, filetype, is_signed, is_draft, draft_id) 
                      VALUES (@msgId, @name, @data, @type, @signed, 0, NULL)", con))
                        {
                            docCmd.Parameters.AddWithValue("@msgId", messageId);
                            docCmd.Parameters.AddWithValue("@name", doc.fileName);
                            docCmd.Parameters.AddWithValue("@data", doc.fileData);
                            docCmd.Parameters.AddWithValue("@type", doc.fileType);
                            docCmd.Parameters.AddWithValue("@signed", checkBox1.Checked);
                            docCmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            Task.Run(() => CleanOldTempDocuments());
            Form6 form6updatete = Application.OpenForms.OfType<Form6>().FirstOrDefault();

            form6updatete?.UpdateMessageCounters();

            MessageBox.Show("Письмо успешно отправлено сотрудникам выбранных отделов!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();

            //string subject = textBox1.Text.Trim();
            //string body = richTextBox1.Text.Trim();
            //string priority = comboBox2.SelectedItem.ToString();

            //if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(body))
            //{
            //    MessageBox.Show("Пожалуйста, заполните тему и текст для отправки письма!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //// Собираем всех получателей по всем отделам
            //List<string> allRecipients = new List<string>();
            //string senderPhone = "";
            //string senderDept = "";
            //string date = DateTime.Now.ToString("dd.MM.yyyy");
            //string time = DateTime.Now.ToString("HH:mm:ss");

            //using (var con = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            //{
            //    con.Open();

            //    // Получаем ID и сотрудников по каждому отделу
            //    foreach (string departmentName in selectedDepartments)
            //    {
            //        int departmentId = -1;
            //        using (var cmd = new MySqlCommand("SELECT id FROM departments WHERE name = @name", con))
            //        {
            //            cmd.Parameters.Clear();
            //            cmd.Parameters.AddWithValue("@name", departmentName);
            //            var result = cmd.ExecuteScalar();
            //            if (result != null) departmentId = Convert.ToInt32(result);
            //        }

            //        if (departmentId == -1) continue; // Пропустить, если отдел не найден

            //        using (var cmd = new MySqlCommand(
            //            @"SELECT u.username FROM users u 
            //      JOIN user_details d ON u.id = d.user_id 
            //      WHERE d.department_id = @deptId AND u.username != @sender", con))
            //        {
            //            cmd.Parameters.AddWithValue("@deptId", departmentId);
            //            cmd.Parameters.AddWithValue("@sender", senderUsername);
            //            using (var reader = cmd.ExecuteReader())
            //            {
            //                while (reader.Read())
            //                {
            //                    string recipient = reader.GetString(0);
            //                    if (!allRecipients.Contains(recipient)) // избегаем дубликатов
            //                        allRecipients.Add(recipient);
            //                }
            //            }
            //        }
            //    }

            //    if (allRecipients.Count == 0)
            //    {
            //        MessageBox.Show("Нет доступных получателей в выбранных отделах", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //        return;
            //    }

            //    // Получаем доп. инфо об отправителе (телефон и отдел)
            //    using (var cmd = new MySqlCommand(
            //        @"SELECT d.department_id, d.phone 
            //  FROM user_details d 
            //  JOIN users u ON u.id = d.user_id 
            //  WHERE u.username = @sender", con))
            //    {
            //        cmd.Parameters.AddWithValue("@sender", senderUsername);
            //        using (var reader = cmd.ExecuteReader())
            //        {
            //            if (reader.Read())
            //            {
            //                senderPhone = reader["phone"].ToString();

            //                // При желании можно определить отдел отправителя по ID:
            //                int deptId = Convert.ToInt32(reader["department_id"]);
            //                senderDept = selectedDepartments.FirstOrDefault(); // или можно использовать любое значение
            //            }
            //        }
            //    }

            //    // Вставляем сообщение для каждого получателя
            //    foreach (string recipient in allRecipients)
            //    {
            //        using (var cmd = new MySqlCommand(
            //            @"INSERT INTO messages 
            //    (sender, recipient, subject, body, priority, date_sent, time_sent, sender_department, sender_phone, is_sent) 
            //    VALUES (@s, @r, @subj, @body, @prio, @date, @time, @dept, @phone, 1)", con))
            //        {
            //            cmd.Parameters.AddWithValue("@s", senderUsername);
            //            cmd.Parameters.AddWithValue("@r", recipient);
            //            cmd.Parameters.AddWithValue("@subj", subject);
            //            cmd.Parameters.AddWithValue("@body", body);
            //            cmd.Parameters.AddWithValue("@prio", priority);
            //            cmd.Parameters.AddWithValue("@date", date);
            //            cmd.Parameters.AddWithValue("@time", time);
            //            cmd.Parameters.AddWithValue("@dept", senderDept);
            //            cmd.Parameters.AddWithValue("@phone", senderPhone);
            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //}

            //MessageBox.Show("Письмо успешно отправлено сотрудникам выбранных отделов!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var res = MessageBox.Show("Вы уверены, что хотите выйти из отправки письма всем?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
                this.Close();
            Task.Run(() => CleanOldTempDocuments());
        }

        private void предварительныйПросмотрДокументовToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void просмотретьДокументыToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void скачатьВсеДокументыToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void сброситьИзмененияВДокументахToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void очиститьСписокПрикреплённыхСообщенийToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private string GetFileHash(byte[] fileBytes)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(fileBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> attachedFiles =
    new List<(int, string, byte[], string, string)>();

        private List<(int id, string fileName, byte[] fileData, string fileType, string fileHash)> originalAttachedFiles =
    new List<(int, string, byte[], string, string)>();

        private int GetNextId()
        {
            return attachedFiles.Any() ? attachedFiles.Max(f => f.id) + 1 : 0;
        }

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

        private string FormatFileSize(long fileSize)
        {
            if (fileSize < 1024)
                return $"{fileSize} байт";
            else if (fileSize < 1024 * 1024)
                return $"{fileSize / 1024} КБ";
            else
                return $"{fileSize / (1024 * 1024)} МБ";
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
                    break; // пользователь нажал "Отмена"
                }

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
                    string fileName = Path.GetFileName(file);

                    // Проверка на дубликаты по имени и размеру
                    bool alreadyAttached = attachedFiles.Any(f => f.fileName == fileName && f.fileData.Length == fileBytes.Length);
                    if (alreadyAttached)
                    {
                        MessageBox.Show("Файл \"" + fileName + "\" уже был прикреплён (по содержимому) и не будет добавлен повторно",
                            "Дубликат файла", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }

                    int newId = GetNextId();
                    attachedFiles.Add((newId, fileName, fileBytes, extension, fileHash));

                    // Добавляем в оригиналы, если такого ID ещё не было
                    bool originalAlreadyExists = originalAttachedFiles.Any(f => f.id == newId);
                    if (!originalAlreadyExists)
                    {
                        originalAttachedFiles.Add((newId, fileName, (byte[])fileBytes.Clone(), extension, fileHash));
                    }

                    comboBox3.Items.Add(fileName);
                    comboBox3.Text = "Прикреплённые документы:";
                }

                UpdateComboBox3(); // Обновляем после каждой порции файлов
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

            Button ok = new Button()
            {
                Text = "Открыть",
                Left = 210,
                Width = 75,
                Top = 230
                // DialogResult НЕ указываем — обрабатываем вручную
            };

            Button cancel = new Button()
            {
                Text = "Отмена",
                Left = 295,
                Width = 75,
                Top = 230,
                DialogResult = DialogResult.Cancel
            };

            prompt.Controls.Add(label);
            prompt.Controls.Add(listBox);
            prompt.Controls.Add(ok);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = ok;
            prompt.CancelButton = cancel;

            ok.Click += (sender, e) =>
            {
                if (listBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Вы действительно хотите открыть {listBox.CheckedItems.Count} документ(а)(ов)? Продолжить?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    prompt.DialogResult = DialogResult.OK;
                    prompt.Close();
                }
            };

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                List<int> selectedIds = new List<int>();

                foreach (var item in listBox.CheckedItems)
                {
                    string displayName = item.ToString();

                    if (nameToIdMap.TryGetValue(displayName, out int id))
                    {
                        selectedIds.Add(id);
                    }
                }

                return selectedIds;
            }

            return null;

            //Form prompt = new Form()
            //{
            //    Width = 400,
            //    Height = 320,
            //    Text = "Выберите документы",
            //    StartPosition = FormStartPosition.CenterParent,
            //    FormBorderStyle = FormBorderStyle.FixedDialog,
            //    MaximizeBox = false,
            //    MinimizeBox = false
            //};

            //Label label = new Label() { Left = 10, Top = 10, Text = "Доступные документы:", AutoSize = true };

            //CheckedListBox listBox = new CheckedListBox()
            //{
            //    Left = 10,
            //    Top = 35,
            //    Width = 360,
            //    Height = 180,
            //    CheckOnClick = true
            //};

            //// Получаем отображаемые имена и соответствующие ID
            //var displayList = GetDisplayNamesWithSizes(files);
            //Dictionary<string, int> nameToIdMap = new Dictionary<string, int>();

            //foreach (var (displayName, id) in displayList)
            //{
            //    listBox.Items.Add(displayName);
            //    nameToIdMap[displayName] = id;
            //}

            //Button ok = new Button() { Text = "Открыть", Left = 210, Width = 75, Top = 230, DialogResult = DialogResult.OK };
            //Button cancel = new Button() { Text = "Отмена", Left = 295, Width = 75, Top = 230, DialogResult = DialogResult.Cancel };

            //prompt.Controls.Add(label);
            //prompt.Controls.Add(listBox);
            //prompt.Controls.Add(ok);
            //prompt.Controls.Add(cancel);
            //prompt.AcceptButton = ok;
            //prompt.CancelButton = cancel;

            //if (prompt.ShowDialog() == DialogResult.OK)
            //{
            //    List<int> selectedIds = new List<int>();

            //    foreach (var item in listBox.CheckedItems)
            //    {
            //        string displayName = item.ToString();

            //        if (nameToIdMap.TryGetValue(displayName, out int id))
            //        {
            //            selectedIds.Add(id);
            //        }
            //    }

            //    return selectedIds;
            //}

            //return null;
        }

        private int documentEditCounter = 0;

        private void предварительныйПросмотрДокументовToolStripMenuItem_Click_1(object sender, EventArgs e)
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

                //// Загружаем ID документов, относящихся к черновику
                //if (openedDraftId != 0)
                //{
                //    string draftQuery = "SELECT id FROM documents WHERE draft_id = @draftId AND is_draft = 1";
                //    using (var cmd = new MySqlCommand(draftQuery, conn))
                //    {
                //        cmd.Parameters.AddWithValue("@draftId", openedDraftId);
                //        using (var reader = cmd.ExecuteReader())
                //        {
                //            while (reader.Read())
                //                draftDocumentIds.Add(reader.GetInt32("id"));
                //        }
                //    }
                //}
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
                                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void сброситьИзмененияВДокументахToolStripMenuItem_Click_1(object sender, EventArgs e)
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

                MessageBox.Show("Изменения сброшены, документы восстановлены", "Готово",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сбросе изменений: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int? openedDraftId = null;
        private void ClearAllAttachedDocuments()
        {
            if (MessageBox.Show("Вы уверены, что хотите очистить все прикреплённые документы?",
        "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // Очистка списка файлов и визуальных элементов
                attachedFiles.Clear();
                comboBox3.Items.Clear();
                comboBox3.Text = "Пусто";

                // Чистим временные документы на всякий случай
                Task.Run(() => CleanOldTempDocuments());

                MessageBox.Show("Все выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                // Убираем DialogResult — будем обрабатывать вручную
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
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Вы действительно хотите удалить {listBox.CheckedItems.Count} документ(а)(ов)? Продолжить?",
                    "Подтверждение удаления",
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
                            // Удаляем только локально, без работы с базой
                            attachedFiles.Remove(selectedFile);
                            comboBox3.Items.Remove(selectedFile.fileName);
                        }
                    }
                }

                // Также удалим временные файлы на всякий случай
                Task.Run(() => CleanOldTempDocuments());

                MessageBox.Show("Выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
            //                // Удаляем только локально, без работы с базой
            //                attachedFiles.Remove(selectedFile);
            //                comboBox3.Items.Remove(selectedFile.fileName);
            //            }
            //        }
            //    }

            //    // Также удалим временные файлы на всякий случай
            //    Task.Run(() => CleanOldTempDocuments());

            //    MessageBox.Show("Выбранные документы были удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //    if (comboBox3.Items.Count == 0)
            //        comboBox3.Text = "Пусто";
            //}
        }

        private void очиститьСписокПрикреплённыхСообщенийToolStripMenuItem_Click_1(object sender, EventArgs e)
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
                MessageBox.Show("Нет прикреплённых документов для очистки", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        private List<int> ShowSealTargetSelectionDialog()
        {
            // Если нет прикреплённых файлов — выход
            if (attachedFiles.Count == 0)
            {
                MessageBox.Show("Нет документов для подписания", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Создаём диалоговое окно
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
                Height = 240
            };

            // Сопоставим displayName с ID
            Dictionary<string, int> displayNameToId = new Dictionary<string, int>();

            foreach (var file in attachedFiles)
            {
                string displayName = file.fileName;

                if (displayNameToId.ContainsKey(displayName))
                    displayName += $" ({file.id})"; // если имена совпадают, добавим id

                displayNameToId[displayName] = file.id;
                listBox.Items.Add(displayName);
            }

            Button okButton = new Button()
            {
                Text = "Подписать",
                Left = 200,
                Width = 80,
                Top = 265
                // DialogResult убираем — обрабатываем вручную
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

            List<int> selectedIds = null;

            okButton.Click += (sender, e) =>
            {
                if (listBox.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один документ!", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult confirm = MessageBox.Show(
                    $"Подписать {listBox.CheckedItems.Count} документ(а)(ов)? Продолжить?",
                    "Подтверждение подписи",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    selectedIds = new List<int>();

                    foreach (var item in listBox.CheckedItems)
                    {
                        string displayName = item.ToString();
                        if (displayNameToId.TryGetValue(displayName, out int id))
                        {
                            selectedIds.Add(id);
                        }
                    }

                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                return selectedIds;

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
                    MessageBox.Show("Неподдерживаемый формат для вставки печати", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void InsertSealIntoDocuments(List<int> selectedDocIds)
        {
            string sealImagePath = @"D:\diplom\ПримерПечати.png"; // Укажи нужный путь к печати

            foreach (int docId in selectedDocIds)
            {
                var file = attachedFiles.FirstOrDefault(f => f.id == docId);
                if (file.id == -1)
                    continue;

                byte[] fileBytes = null;

                // Ищем изменённую версию в TempDocuments
                string uniqueKey = $"{file.fileHash}_{file.fileName}";
                if (tempDocumentPaths.TryGetValue(uniqueKey, out string tempPath) && File.Exists(tempPath))
                {
                    fileBytes = File.ReadAllBytes(tempPath);
                }
                else
                {
                    fileBytes = file.fileData;
                }

                if (fileBytes == null)
                    continue;

                byte[] modifiedBytes = AddSealToDocument(fileBytes, sealImagePath, file.fileName);

                // Сохраняем изменённую версию обратно
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempDocuments");
                Directory.CreateDirectory(tempDir);
                string updatedTempPath = Path.Combine(tempDir, uniqueKey);
                File.WriteAllBytes(updatedTempPath, modifiedBytes);
                tempDocumentPaths[uniqueKey] = updatedTempPath;

                file.fileData = modifiedBytes;
            }

            MessageBox.Show("Печати успешно добавлены", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        }

        private void просмотретьДокументыToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

        }

        private void скачатьВсеДокументыToolStripMenuItem_Click_1(object sender, EventArgs e)
        {

        }
    }
}
