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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AtlantPrograma
{
    public partial class Form6 : Form
    {
        private string currentUser;
        private string currentView = ""; // "inbox", "read", "trash", etc.

        public Form6(string username)
        {
            InitializeComponent();
            currentUser = username;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //dataGridView1.ContextMenuStrip = contextMenuStrip1;            
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            // Настройка автозаполнения для textBox1
            AutoCompleteStringCollection autoCompleteCollection = new AutoCompleteStringCollection();

            // Получаем список пользователей из базы данных
            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT username FROM users", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            autoCompleteCollection.Add(reader.GetString("username"));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при получении списка пользователей: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Применяем автозаполнение
            textBox1.AutoCompleteCustomSource = autoCompleteCollection;
            textBox1.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBox1.AutoCompleteSource = AutoCompleteSource.CustomSource;

            label1.Text = $"Почта пользователя {currentUser}";
            label2.Text = ""; // Сброс уведомлений
            label2.ForeColor = Color.Red;
            label2.Font = new Font(label2.Font, FontStyle.Bold);

            ShowNotificationCount(); // загрузка уведомлений
            //dataGridView1.MouseDown += dataGridView1_MouseDown;
            действияСКорзинойToolStripMenuItem.Enabled = false;
            пометитьКакПрочитанноеToolStripMenuItem1.Enabled = false;
            поместитьВКорзинуToolStripMenuItem.Enabled = false;
            действияСЧерновикамиToolStripMenuItem.Enabled = false;
            переслатьСообщенияToolStripMenuItem.Enabled = false;
            восстановитьПрочитанноеToolStripMenuItem.Enabled = false;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            Form7 sendMail = new Form7(currentUser);
            sendMail.действияСДокументамиToolStripMenuItem.Enabled = true;
            sendMail.предварительныйПросмотрДокументовToolStripMenuItem.Enabled = true;
            sendMail.скачатьВсеДокументыToolStripMenuItem.Enabled = false;
            sendMail.просмотретьДокументыToolStripMenuItem.Enabled = false;
            sendMail.сброситьИзмененияВДокументахToolStripMenuItem.Enabled = true;
            sendMail.очиститьСписокПрикреплённыхСообщенийToolStripMenuItem.Enabled = true;
            sendMail.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            currentView = "inbox";
            LoadIncomingMessages();
            действияСКорзинойToolStripMenuItem.Enabled = false;
            действияСпочтойToolStripMenuItem.Enabled = true;
            поместитьВКорзинуToolStripMenuItem.Enabled = true;
            пометитьКакПрочитанноеToolStripMenuItem1.Enabled = true;
            отправитьВсемToolStripMenuItem1.Enabled = true;
            действияСЧерновикамиToolStripMenuItem.Enabled = false;
            переслатьСообщенияToolStripMenuItem.Enabled = true;
        }
        public void ShowNotificationCount()
        {
            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();
                string query = @"
            SELECT COUNT(*) 
            FROM messages 
            WHERE recipient = @username 
                AND is_read = 0 
                AND is_deleted = 0 
                AND is_draft = 0
                AND is_sent = 1"; // добавляем фильтр is_deleted
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", currentUser);

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                if (count > 0)
                {
                    label2.Text = $"Новых сообщений: {count}";
                    label2.Visible = true;
                }
                else
                {
                    label2.Text = "Новых сообщений: 0";
                    label2.Visible = true;
                }
            }
        }

        public void LoadIncomingMessages()
        {
            // Обновляем обработчики мыши

            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;
            dataGridView1.MouseDown += dataGridView1_MouseDown;

            // Очистка таблицы
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Добавляем колонку с чекбоксом
            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            dataGridView1.Columns.Add(checkboxColumn);

            // Добавляем скрытую колонку для ID сообщения
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "message_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            // Добавляем основные колонки
            dataGridView1.Columns.Add("sender", "Отправитель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_sent", "Дата");
            dataGridView1.Columns.Add("time_sent", "Время");

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = @"
SELECT 
    m.id,
    m.sender, 
    m.subject, 
    m.priority, 
    m.date_sent, 
    m.time_sent,
    d.name AS department,
    d.phones AS phone
FROM messages m
LEFT JOIN users u ON m.sender = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments d ON ud.department_id = d.id
WHERE m.recipient = @username 
    AND m.is_read = 0 
    AND m.is_deleted = 0 
    AND m.is_draft = 0
    AND m.is_sent = 1
ORDER BY m.id DESC";


                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", currentUser);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        MessageBox.Show("Сообщений на данный момент нет", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    while (reader.Read())
                    {
                        int messageId = reader.GetInt32("id");
                        string sender = reader.GetString("sender");
                        string subject = reader.GetString("subject");
                        string priority = reader.GetString("priority");
                        string date = reader.GetString("date_sent");
                        string time = reader.GetTimeSpan("time_sent").ToString(@"hh\:mm\:ss");
                        string department = reader.IsDBNull(reader.GetOrdinal("department")) ? "-" : reader.GetString("department");
                        string phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "-" : reader.GetString("phone");

                        // Добавляем строку
                        int rowIndex = dataGridView1.Rows.Add(false,messageId, sender, department, phone, subject, priority, date, time);

                        // Раскрашиваем приоритет
                        DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                        switch (priority)
                        {
                            case "Не срочно":
                                priorityCell.Style.BackColor = Color.Green;
                                break;
                            case "Обычное сообщение":
                                priorityCell.Style.BackColor = Color.Yellow;
                                break;
                            case "Срочно!":
                                priorityCell.Style.BackColor = Color.Red;
                                break;
                        }
                    }
                }
            }
        }

        private void прочитатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                string senderUsername = dataGridView1.SelectedRows[0].Cells["sender"].Value.ToString();
                string subject = dataGridView1.SelectedRows[0].Cells["subject"].Value.ToString();

                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    // Считываем тело письма
                    string query = "SELECT body FROM messages WHERE sender = @sender AND recipient = @recipient AND subject = @subject AND is_read = 0 LIMIT 1";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sender", senderUsername);
                    cmd.Parameters.AddWithValue("@recipient", currentUser);
                    cmd.Parameters.AddWithValue("@subject", subject);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string body = reader.GetString("body");

                            Form7 readForm = new Form7(currentUser);
                            readForm.LoadReadOnlyMessage(subject, body, senderUsername);
                            readForm.ShowDialog();
                        }
                    }

                    // Обновляем флаг прочтения
                    string updateQuery = "UPDATE messages SET is_read = 1 WHERE sender = @sender AND recipient = @recipient AND subject = @subject";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@sender", senderUsername);
                    updateCmd.Parameters.AddWithValue("@recipient", currentUser);
                    updateCmd.Parameters.AddWithValue("@subject", subject);
                    updateCmd.ExecuteNonQuery();
                }

                LoadIncomingMessages(); // Обновляем таблицу
                ShowNotificationCount(); // обновить счётчик
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            currentView = "read";
            // Отключаем пункты меню, которые не относятся к прочитанным
            действияСпочтойToolStripMenuItem.Enabled = true;
            действияСКорзинойToolStripMenuItem.Enabled = false;
            поместитьВКорзинуToolStripMenuItem.Enabled = true;
            пометитьКакПрочитанноеToolStripMenuItem1.Enabled = false;
            отправитьВсемToolStripMenuItem1.Enabled = false;
            действияСЧерновикамиToolStripMenuItem.Enabled = false;
            переслатьСообщенияToolStripMenuItem.Enabled = true;
            восстановитьПрочитанноеToolStripMenuItem.Enabled = true;
            // Обновляем обработчики мыши
            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;
            dataGridView1.MouseDown += dataGridView1_MouseDown1;

            // Очищаем таблицу
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Добавляем колонку с чекбоксом
            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            dataGridView1.Columns.Add(checkboxColumn);

            // Скрытая колонка для ID
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "message_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            // Основные колонки
            dataGridView1.Columns.Add("sender", "Отправитель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_sent", "Дата");
            dataGridView1.Columns.Add("time_sent", "Время");

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = @"
SELECT 
    m.id,
    m.sender, 
    m.subject, 
    m.priority, 
    m.date_sent, 
    m.time_sent,
    d.name AS department,
    d.phones AS phone
FROM messages m
LEFT JOIN users u ON m.sender = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments d ON ud.department_id = d.id
WHERE m.recipient = @username 
    AND m.is_read = 1 
    AND m.is_deleted = 0 
    AND m.is_draft = 0
    AND m.is_sent = 1
ORDER BY m.id DESC";


                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", currentUser);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    bool hasMessages = false;

                    while (reader.Read())
                    {
                        hasMessages = true;

                        int messageId = reader.GetInt32("id");
                        string senderName = reader.GetString("sender");
                        string subject = reader.GetString("subject");
                        string priority = reader.GetString("priority");
                        string date = reader.GetString("date_sent");
                        string time = reader.GetTimeSpan("time_sent").ToString(@"hh\:mm\:ss");
                        string department = reader.IsDBNull(reader.GetOrdinal("department")) ? "-" : reader.GetString("department");
                        string phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "-" : reader.GetString("phone");

                        int rowIndex = dataGridView1.Rows.Add(false, messageId, senderName, department, phone, subject, priority, date, time);

                        // Раскрашиваем приоритет
                        DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                        switch (priority)
                        {
                            case "Не срочно":
                                priorityCell.Style.BackColor = Color.Green;
                                break;
                            case "Обычное сообщение":
                                priorityCell.Style.BackColor = Color.Yellow;
                                break;
                            case "Срочно!":
                                priorityCell.Style.BackColor = Color.Red;
                                break;
                        }
                    }

                    if (!hasMessages)
                    {
                        MessageBox.Show("Прочитанных сообщений нет", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Перезагружаем таблицу после успешной операции
                        LoadReadMessages();  // Вызываем метод для перезагрузки списка
                    }
                }
            }
        }
        public void LoadReadMessages()
        {
            // Обновляем обработчики мыши
            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;
            dataGridView1.MouseDown += dataGridView1_MouseDown1;

            // Очистка таблицы
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Добавляем колонку с чекбоксом
            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            dataGridView1.Columns.Add(checkboxColumn);

            // Скрытая колонка для ID
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "message_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            // Основные колонки
            dataGridView1.Columns.Add("sender", "Отправитель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_sent", "Дата");
            dataGridView1.Columns.Add("time_sent", "Время");

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = @"
SELECT 
    m.id,
    m.sender, 
    m.subject, 
    m.priority, 
    m.date_sent, 
    m.time_sent,
    d.name AS department,
    d.phones AS phone
FROM messages m
LEFT JOIN users u ON m.sender = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments d ON ud.department_id = d.id
WHERE m.recipient = @username 
    AND m.is_read = 1 
    AND m.is_deleted = 0 
    AND m.is_draft = 0
    AND m.is_sent = 1
ORDER BY m.id DESC";


                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", currentUser);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    bool hasMessages = false;

                    while (reader.Read())
                    {
                        hasMessages = true;

                        int messageId = reader.GetInt32("id");
                        string senderName = reader.GetString("sender");
                        string subject = reader.GetString("subject");
                        string priority = reader.GetString("priority");
                        string date = reader.GetString("date_sent");
                        string time = reader.GetTimeSpan("time_sent").ToString(@"hh\:mm\:ss");
                        string department = reader.IsDBNull(reader.GetOrdinal("department")) ? "-" : reader.GetString("department");
                        string phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "-" : reader.GetString("phone");

                        int rowIndex = dataGridView1.Rows.Add(false, messageId, senderName, department, phone, subject, priority, date, time);

                        // Раскрашиваем приоритет
                        DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                        switch (priority)
                        {
                            case "Не срочно":
                                priorityCell.Style.BackColor = Color.Green;
                                break;
                            case "Обычное сообщение":
                                priorityCell.Style.BackColor = Color.Yellow;
                                break;
                            case "Срочно!":
                                priorityCell.Style.BackColor = Color.Red;
                                break;
                        }
                    }

                    if (!hasMessages)
                    {
                        MessageBox.Show("Прочитанных сообщений нет", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;
                    contextMenuStrip1.Show(dataGridView1, e.Location);
                }
            }
        }

        private void dataGridView1_MouseDown1(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);

                if (hit.RowIndex >= 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[hit.RowIndex].Selected = true;

                    contextMenuStrip2.Show(dataGridView1, e.Location);
                }
                else
                {
                    contextMenuStrip2.Hide();
                }
            }
        }     
        private void button8_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы действительно хотите выйти из почты?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                this.Close(); // Закрывает только Form6
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;

            действияСпочтойToolStripMenuItem.Enabled = false;
            действияСКорзинойToolStripMenuItem.Enabled = true;
            восстановитьВсёToolStripMenuItem1.Enabled = true;
            восстановитьПомеченноеToolStripMenuItem1.Enabled = true;
            очиститьКорзинуToolStripMenuItem1.Enabled = true;
            действияСЧерновикамиToolStripMenuItem.Enabled = false;
            переслатьСообщенияToolStripMenuItem.Enabled = false;
            //dataGridView1.MouseDown -= dataGridView1_MouseDown;
            //dataGridView1.MouseDown -= dataGridView1_MouseDown1;
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Чекбокс
            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            dataGridView1.Columns.Add(checkboxColumn);

            // Скрытая колонка с ID сообщений
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "message_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            // Остальные колонки
            dataGridView1.Columns.Add("sender", "Отправитель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_sent", "Дата");
            dataGridView1.Columns.Add("time_sent", "Время");

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();
                string query = @"
SELECT 
    m.id,
    m.sender, 
    m.subject, 
    m.priority, 
    m.date_sent, 
    m.time_sent,
    d.name AS department,
    d.phones AS phone
FROM messages m
LEFT JOIN users u ON m.sender = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments d ON ud.department_id = d.id
WHERE 
    m.recipient = @username 
    AND m.is_deleted = 1 
    AND m.is_draft = 0 
    AND m.is_sent = 0
ORDER BY m.id DESC";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", currentUser);

                MySqlDataReader reader = cmd.ExecuteReader();

                bool hasMessages = false;
                while (reader.Read())
                {
                    hasMessages = true;

                    int messageId = reader.GetInt32("id");
                    string senderName = reader.GetString("sender");
                    string subject = reader.GetString("subject");
                    string priority = reader.GetString("priority");
                    string date = reader.GetString("date_sent");
                    string time = reader.GetTimeSpan("time_sent").ToString(@"hh\:mm\:ss");
                    string department = reader.IsDBNull(reader.GetOrdinal("department")) ? "-" : reader.GetString("department");
                    string phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "-" : reader.GetString("phone");

                    int rowIndex = dataGridView1.Rows.Add(false, messageId, senderName, department, phone, subject, priority, date, time);

                    // Раскраска приоритета
                    DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                    switch (priority)
                    {
                        case "Не срочно": priorityCell.Style.BackColor = Color.Green; break;
                        case "Обычное сообщение": priorityCell.Style.BackColor = Color.Yellow; break;
                        case "Срочно!": priorityCell.Style.BackColor = Color.Red; break;
                    }
                }

                if (!hasMessages)
                {
                    MessageBox.Show("Корзина пуста", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    //dataGridView1.MouseDown += dataGridView1_MouseDown1;
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;

            действияСКорзинойToolStripMenuItem.Enabled = false;
            действияСпочтойToolStripMenuItem.Enabled = false;
            действияСЧерновикамиToolStripMenuItem.Enabled = false;
            переслатьСообщенияToolStripMenuItem.Enabled = false;

            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Скрытая колонка с ID сообщений
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "message_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            dataGridView1.Columns.Add("recipient", "Получатель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_sent", "Дата");
            dataGridView1.Columns.Add("time_sent", "Время");

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = @"
SELECT 
    m.id,
    m.recipient, 
    m.subject, 
    m.priority, 
    m.date_sent, 
    m.time_sent,
    d.name AS department,
    d.phones AS phone
FROM messages m
LEFT JOIN users u ON m.recipient = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments d ON ud.department_id = d.id
WHERE m.sender = @sender AND m.is_read = 0 AND m.is_deleted = 0 AND m.is_draft = 0 AND m.is_sent = 1
ORDER BY m.id DESC";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sender", currentUser);

                MySqlDataReader reader = cmd.ExecuteReader();

                bool hasMessages = false;

                while (reader.Read())
                {
                    hasMessages = true;

                    int messageId = reader.GetInt32("id");
                    string recipient = reader.GetString("recipient");
                    string subject = reader.GetString("subject");
                    string priority = reader.GetString("priority");
                    string date = reader.GetString("date_sent");
                    string time = reader.GetTimeSpan("time_sent").ToString(@"hh\:mm\:ss");
                    string department = reader.IsDBNull(reader.GetOrdinal("department")) ? "-" : reader.GetString("department");
                    string phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "-" : reader.GetString("phone");

                    int rowIndex = dataGridView1.Rows.Add(messageId, recipient, department, phone, subject, priority, date, time);

                    // Раскраска приоритета
                    DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                    switch (priority)
                    {
                        case "Не срочно": priorityCell.Style.BackColor = Color.Green; break;
                        case "Обычное сообщение": priorityCell.Style.BackColor = Color.Yellow; break;
                        case "Срочно!": priorityCell.Style.BackColor = Color.Red; break;
                    }
                }

                if (!hasMessages)
                {
                    MessageBox.Show("Нет отправленных сообщений", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void поместитьВКорзинуToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            List<int> idsToDelete = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow && row.Cells[0].Value != null && Convert.ToBoolean(row.Cells[0].Value))
                {
                    if (row.Cells["message_id"].Value != null)
                    {
                        idsToDelete.Add(Convert.ToInt32(row.Cells["message_id"].Value));
                    }
                }
            }

            if (idsToDelete.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одно сообщение для перемещения в корзину", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show("Вы уверены, что хотите переместить выбранные сообщения в корзину?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = "UPDATE messages SET is_deleted = 1 WHERE id = @id AND recipient = @recipient";

                foreach (int id in idsToDelete)
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@recipient", currentUser);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Выбранные сообщения были перемещены в корзину!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ShowNotificationCount(); // Обновляем количество новых

            //Загружаем актуальные данные в зависимости от текущего режима
            if (currentView == "inbox")
                LoadIncomingMessages();
            else if (currentView == "read")
                button7_Click(null, null); // имитируем клик по кнопке "Прочитанные"
        }

        private void пометитьКакПрочитанноеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Коммитим изменения из чекбоксов
            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

            List<int> idsToMarkRead = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow && row.Cells[0].Value != null && Convert.ToBoolean(row.Cells[0].Value))
                {
                    if (row.Cells["message_id"].Value != null)
                    {
                        idsToMarkRead.Add(Convert.ToInt32(row.Cells["message_id"].Value));
                    }
                }
            }

            if (idsToMarkRead.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одно сообщение для пометки как прочитанное", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Запрашиваем подтверждение у пользователя
            DialogResult result = MessageBox.Show("Вы точно хотите пометить выбранные сообщения как прочитанные?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            // Если пользователь подтвердил, выполняем пометку как прочитанные
            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = "UPDATE messages SET is_read = 1 WHERE id = @id AND recipient = @recipient";

                foreach (int id in idsToMarkRead)
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@recipient", currentUser);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Выбранные сообщения были помечены как прочитанные!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ShowNotificationCount(); // Обновим количество непрочитанных

            // Обновляем таблицу — здесь важно, чтобы currentView был "inbox"
            if (currentView == "inbox")
                LoadIncomingMessages();
        }

        private void восстановитьПомеченноеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Чтобы сохранить последнее изменение чекбокса
            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            List<int> idsToRestore = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow && row.Cells[0].Value != null && Convert.ToBoolean(row.Cells[0].Value))
                {
                    if (row.Cells["message_id"].Value != null)
                    {
                        idsToRestore.Add(Convert.ToInt32(row.Cells["message_id"].Value));
                    }
                }
            }

            if (idsToRestore.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одно сообщение для восстановления", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show("Вы уверены, что хотите восстановить помеченные сообщения?", "Предупреждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = "UPDATE messages SET is_deleted = 0 WHERE id = @id AND recipient = @recipient";

                foreach (int id in idsToRestore)
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@recipient", currentUser);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Выбранные сообщения восстановлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Обновим корзину
            button5_Click(null, null);
            ShowNotificationCount();
        }

        private void восстановитьВсёToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Восстанавливать нечего", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult result = MessageBox.Show(
        "Вы уверены, что хотите восстановить все сообщения из корзины?",
        "Предупреждение",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = "UPDATE messages SET is_deleted = 0 WHERE recipient = @recipient AND is_deleted = 1";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@recipient", currentUser);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Все сообщения были восстановлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Обновляем интерфейс
            button5_Click(null, null);
            ShowNotificationCount();
        }

        private void очиститьКорзинуToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Корзина пуста. Удалять нечего", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

            List<int> idsToDelete = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow && row.Cells[0].Value != null && Convert.ToBoolean(row.Cells[0].Value))
                {
                    if (row.Cells["message_id"].Value != null)
                        idsToDelete.Add(Convert.ToInt32(row.Cells["message_id"].Value));
                }
            }

            DialogResult result;

            if (idsToDelete.Count == 0)
            {
                result = MessageBox.Show(
                    "Вы уверены, что хотите полностью очистить корзину?\nТакже вы можете отметить определённые сообщения для удаления",
                    "Подтверждение очистки корзины",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }
            else
            {
                result = MessageBox.Show(
                    "Вы уверены, что хотите удалить отмеченные сообщения из корзины?",
                    "Подтверждение удаления",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                if (idsToDelete.Count > 0)
                {
                    // Удаляем только помеченные
                    string query = "DELETE FROM messages WHERE id = @id AND recipient = @recipient AND is_deleted = 1";

                    foreach (int id in idsToDelete)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@recipient", currentUser);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    // Удаляем всё из корзины
                    string query = "DELETE FROM messages WHERE recipient = @recipient AND is_deleted = 1";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@recipient", currentUser);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Сообщения были удалены из корзины!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Обновляем таблицу
            button5_Click(null, null);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;

            действияСпочтойToolStripMenuItem.Enabled = false;
            действияСКорзинойToolStripMenuItem.Enabled = false;
            действияСЧерновикамиToolStripMenuItem.Enabled = true;
            восстановитьУдалённоеToolStripMenuItem.Enabled = true;
            удалитьЧерновикиToolStripMenuItem.Enabled = true;
            переслатьСообщенияToolStripMenuItem.Enabled = false;

            // Очистка таблицы
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Добавляем колонку с чекбоксом
            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            dataGridView1.Columns.Add(checkboxColumn);

            // Добавляем скрытую колонку для ID черновика
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "draft_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            // Основные колонки
            dataGridView1.Columns.Add("recipient", "Получатель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_created", "Дата");
            dataGridView1.Columns.Add("time_created", "Время");

            // Колонка с кнопками "Открыть"
            DataGridViewButtonColumn openButtonColumn = new DataGridViewButtonColumn();
            openButtonColumn.Name = "open_button";
            openButtonColumn.HeaderText = "Открыть";
            openButtonColumn.Text = "Открыть";
            openButtonColumn.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(openButtonColumn);

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();
                    string query = @"
SELECT 
    d.id,
    d.recipient,
    dep.name AS recipient_department,
    ud.phone AS recipient_phone,
    d.subject,
    d.priority,
    d.date_created,
    d.time_created
FROM drafts d
LEFT JOIN users u ON d.recipient = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments dep ON ud.department_id = dep.id
WHERE 
    d.sender = @sender 
    AND (d.is_sent IS NULL OR d.is_sent = 0)
    AND (d.is_deleted IS NULL OR d.is_deleted = 0)
ORDER BY d.date_created DESC, d.time_created DESC;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sender", currentUser);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            MessageBox.Show("У вас нет неотправленных черновиков", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        while (reader.Read())
                        {
                            int draftId = reader.GetInt32("id");
                            string recipient = reader.IsDBNull(reader.GetOrdinal("recipient")) ? "-" : reader.GetString("recipient");
                            string subject = reader.IsDBNull(reader.GetOrdinal("subject")) ? "-" : reader.GetString("subject");
                            string priority = reader.IsDBNull(reader.GetOrdinal("priority")) ? "-" : reader.GetString("priority");
                            string date = reader.IsDBNull(reader.GetOrdinal("date_created")) ? "-" : reader.GetString("date_created");
                            string time = reader.IsDBNull(reader.GetOrdinal("time_created"))
                                ? "-"
                                : ((TimeSpan)reader["time_created"]).ToString(@"hh\:mm\:ss");

                            string department = reader.IsDBNull(reader.GetOrdinal("recipient_department")) ? "-" : reader.GetString("recipient_department");
                            string phone = reader.IsDBNull(reader.GetOrdinal("recipient_phone")) ? "-" : reader.GetString("recipient_phone");

                            int rowIndex = dataGridView1.Rows.Add(false, draftId, recipient, department, phone, subject, priority, date, time);

                            // Раскрашиваем приоритет
                            DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                            switch (priority)
                            {
                                case "Не срочно":
                                    priorityCell.Style.BackColor = Color.Green;
                                    break;
                                case "Обычное сообщение":
                                    priorityCell.Style.BackColor = Color.Yellow;
                                    break;
                                case "Срочно!":
                                    priorityCell.Style.BackColor = Color.Red;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке черновиков: " + ex.Message);
            }
        }
        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            // Проверяем, что клик был по ячейке в пределах данных
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // Проверяем, есть ли колонка с кнопкой "Открыть" только в таблице черновиков
                if (dataGridView1.Columns.Contains("open_button") && e.ColumnIndex == dataGridView1.Columns["open_button"].Index)
                {
                    // Получаем ID черновика
                    int draftId = 0;
                    if (dataGridView1.Rows[e.RowIndex].Cells["draft_id"].Value != null)
                    {
                        draftId = (int)dataGridView1.Rows[e.RowIndex].Cells["draft_id"].Value;
                    }

                    // Открываем Form7 и передаем только ID
                    Form7 form7 = new Form7(currentUser);
                    form7.LoadDraftForEditing(draftId);
                    form7.Show();
                }
            }
            // Проверяем, что клик был по кнопке "Открыть"
            //if (e.ColumnIndex == dataGridView1.Columns["open_button"].Index && e.RowIndex >= 0)
            //{
            //    // Получаем ID черновика
            //    int draftId = (int)dataGridView1.Rows[e.RowIndex].Cells["draft_id"].Value;

            //    // Открываем Form7 и передаем только ID
            //    Form7 form7 = new Form7(currentUser);
            //    form7.LoadDraftForEditing(draftId);
            //    form7.Show();
            //}
            else
            {
                // Если клик был по другим ячейкам, например по чекбоксам или другим столбцам, ничего не делаем
                // В случае необходимости, сюда можно добавить обработку других типов кликов.
            }
        }

        public void LoadDraftMessages()
        {
            dataGridView1.MouseDown -= dataGridView1_MouseDown;
            dataGridView1.MouseDown -= dataGridView1_MouseDown1;

            действияСпочтойToolStripMenuItem.Enabled = false;
            действияСКорзинойToolStripMenuItem.Enabled = false;

            // Очистка таблицы
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Добавляем колонку с чекбоксом
            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            dataGridView1.Columns.Add(checkboxColumn);

            // Добавляем скрытую колонку для ID черновика
            DataGridViewTextBoxColumn idColumn = new DataGridViewTextBoxColumn();
            idColumn.Name = "draft_id";
            idColumn.Visible = false;
            dataGridView1.Columns.Add(idColumn);

            // Основные колонки
            dataGridView1.Columns.Add("recipient", "Получатель");
            dataGridView1.Columns.Add("department", "Отдел");
            dataGridView1.Columns.Add("phone", "Телефон");
            dataGridView1.Columns.Add("subject", "Тема");
            dataGridView1.Columns.Add("priority", "Приоритет");
            dataGridView1.Columns.Add("date_created", "Дата");
            dataGridView1.Columns.Add("time_created", "Время");

            // Колонка с кнопками "Открыть"
            DataGridViewButtonColumn openButtonColumn = new DataGridViewButtonColumn();
            openButtonColumn.Name = "open_button";
            openButtonColumn.HeaderText = "Открыть";
            openButtonColumn.Text = "Открыть";
            openButtonColumn.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(openButtonColumn);

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();
                    string query = @"
SELECT 
    d.id,
    d.recipient,
    dep.name AS recipient_department,
    ud.phone AS recipient_phone,
    d.subject,
    d.priority,
    d.date_created,
    d.time_created
FROM drafts d
LEFT JOIN users u ON d.recipient = u.username
LEFT JOIN user_details ud ON u.id = ud.user_id
LEFT JOIN departments dep ON ud.department_id = dep.id
WHERE 
    d.sender = @sender 
    AND (d.is_sent IS NULL OR d.is_sent = 0)
    AND (d.is_deleted IS NULL OR d.is_deleted = 0)
ORDER BY d.date_created DESC, d.time_created DESC;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sender", currentUser);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            MessageBox.Show("У вас нет неотправленных черновиков", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        while (reader.Read())
                        {
                            int draftId = reader.GetInt32("id");
                            string recipient = reader.IsDBNull(reader.GetOrdinal("recipient")) ? "-" : reader.GetString("recipient");
                            string subject = reader.IsDBNull(reader.GetOrdinal("subject")) ? "-" : reader.GetString("subject");
                            string priority = reader.IsDBNull(reader.GetOrdinal("priority")) ? "-" : reader.GetString("priority");
                            string date = reader.IsDBNull(reader.GetOrdinal("date_created")) ? "-" : reader.GetString("date_created");
                            string time = reader.IsDBNull(reader.GetOrdinal("time_created"))
                                ? "-"
                                : ((TimeSpan)reader["time_created"]).ToString(@"hh\:mm\:ss");

                            string department = reader.IsDBNull(reader.GetOrdinal("recipient_department")) ? "-" : reader.GetString("recipient_department");
                            string phone = reader.IsDBNull(reader.GetOrdinal("recipient_phone")) ? "-" : reader.GetString("recipient_phone");

                            int rowIndex = dataGridView1.Rows.Add(false, draftId, recipient, department, phone, subject, priority, date, time);

                            // Раскрашиваем приоритет
                            DataGridViewCell priorityCell = dataGridView1.Rows[rowIndex].Cells["priority"];
                            switch (priority)
                            {
                                case "Не срочно":
                                    priorityCell.Style.BackColor = Color.Green;
                                    break;
                                case "Обычное сообщение":
                                    priorityCell.Style.BackColor = Color.Yellow;
                                    break;
                                case "Срочно!":
                                    priorityCell.Style.BackColor = Color.Red;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке черновиков: " + ex.Message);
            }
        }

        private void восстановитьУдалённоеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    // Сначала проверяем есть ли удалённые черновики
                    string checkQuery = "SELECT COUNT(*) FROM drafts WHERE sender = @sender AND is_deleted = 1";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@sender", currentUser);
                        int deletedCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (deletedCount == 0)
                        {
                            MessageBox.Show("Удалённых черновиков нет для восстановления", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }

                    // Предупреждение пользователю
                    DialogResult result = MessageBox.Show(
                        "Вы уверены, что хотите восстановить все удалённые черновики?",
                        "Подтверждение",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Восстановление всех удалённых черновиков
                        string restoreQuery = "UPDATE drafts SET is_deleted = 0 WHERE sender = @sender AND is_deleted = 1";
                        using (MySqlCommand restoreCmd = new MySqlCommand(restoreQuery, conn))
                        {
                            restoreCmd.Parameters.AddWithValue("@sender", currentUser);
                            restoreCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Удалённые черновики успешно восстановлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Перезагрузка списка черновиков
                        LoadDraftMessages();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при восстановлении черновиков: " + ex.Message);
            }
        }

        private void удалитьЧерновикиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Нет черновиков для удаления", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

            List<int> selectedDraftIds = new List<int>();

            // Сбор ID выбранных черновиков
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(row.Cells[0].Value) == true) // Колонка с чекбоксами — индекс 0
                {
                    if (row.Cells["draft_id"].Value != null)
                    {
                        selectedDraftIds.Add(Convert.ToInt32(row.Cells["draft_id"].Value));
                    }
                }
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    if (selectedDraftIds.Count > 0)
                    {
                        // Пользователь выбрал строки
                        DialogResult confirm = MessageBox.Show(
                            "Вы уверены, что хотите удалить выбранные черновики?",
                            "Подтверждение удаления",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (confirm == DialogResult.Yes)
                        {
                            foreach (int draftId in selectedDraftIds)
                            {
                                string deleteQuery = "UPDATE drafts SET is_deleted = 1 WHERE id = @id";
                                using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", draftId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            MessageBox.Show("Выбранные черновики успешно удалены", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDraftMessages();
                        }
                    }
                    else
                    {
                        // Пользователь ничего не выбрал — удаляем все черновики
                        DialogResult confirmAll = MessageBox.Show(
                            "Вы не выбрали черновики.\nУдалить все черновики, которые сейчас отображаются? Также вы можете отметить определённые черновики для удаления",
                            "Подтверждение удаления всех",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (confirmAll == DialogResult.Yes)
                        {
                            string deleteAllQuery = @"
UPDATE drafts
SET is_deleted = 1
WHERE sender = @sender AND (is_sent IS NULL OR is_sent = 0) AND is_deleted = 0
";
                            using (MySqlCommand cmd = new MySqlCommand(deleteAllQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@sender", currentUser);
                                cmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("Все черновики успешно удалены", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadDraftMessages();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении черновиков: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ответитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

                string originalSender = selectedRow.Cells["sender"].Value.ToString();
                string originalSubject = selectedRow.Cells["subject"].Value.ToString();
                string originalDate = selectedRow.Cells["date_sent"].Value.ToString();
                string originalTime = selectedRow.Cells["time_sent"].Value.ToString();
                int messageId = Convert.ToInt32(selectedRow.Cells["message_id"].Value);

                string originalBody = GetMessageBodyById(messageId); // <-- получаем текст через отдельный метод

                Form7 replyForm = new Form7(currentUser); // currentUser — это тот, кто отвечает

                // Устанавливаем получателя, тему и оригинальный текст
                replyForm.SetReplyMode(
                    recipient: originalSender,
                    subject: originalSubject,
                    originalBody: originalBody,
                    originalSender: originalSender,
                    originalDate: originalDate,
                    originalTime: originalTime,
                    messageId: messageId
                );

                replyForm.Show();
            }
        }
        private string GetMessageBodyById(int messageId)
        {
            string body = "";

            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();

                string query = "SELECT body FROM messages WHERE id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", messageId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        body = reader.IsDBNull(reader.GetOrdinal("body")) ? "" : reader.GetString("body");
                    }
                }
            }

            return body;
        }

        private void ответитьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

                string originalSender = selectedRow.Cells["sender"].Value.ToString();
                string originalSubject = selectedRow.Cells["subject"].Value.ToString();
                string originalDate = selectedRow.Cells["date_sent"].Value.ToString();
                string originalTime = selectedRow.Cells["time_sent"].Value.ToString();
                int messageId = Convert.ToInt32(selectedRow.Cells["message_id"].Value);

                string originalBody = GetMessageBodyById(messageId); // <-- получаем текст через отдельный метод

                Form7 replyForm = new Form7(currentUser); // currentUser — это тот, кто отвечает

                // Устанавливаем получателя, тему и оригинальный текст
                replyForm.SetReplyMode(
                    recipient: originalSender,
                    subject: originalSubject,
                    originalBody: originalBody,
                    originalSender: originalSender,
                    originalDate: originalDate,
                    originalTime: originalTime,
                    messageId: messageId
                );

                // Помечаем оригинальное письмо как прочитанное
                //MarkMessageAsRead(messageId);

                replyForm.Show();
            }
        }

        private void переслатьСообщенияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit();
            dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

            List<int> selectedMessageIds = new List<int>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0] is DataGridViewCheckBoxCell checkbox &&
                    checkbox.Value != null &&
                    Convert.ToBoolean(checkbox.Value))
                {
                    if (row.Cells["message_id"].Value != null)
                        selectedMessageIds.Add(Convert.ToInt32(row.Cells["message_id"].Value));
                }
            }

            if (selectedMessageIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одно сообщение для пересылки", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Получаем список пользователей
            List<string> users = new List<string>();
            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT username FROM users", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            users.Add(reader.GetString("username"));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при получении списка пользователей: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Мини-форма выбора получателя
            string selectedRecipient = ShowRecipientSelectDialog(users, currentUser);
            if (string.IsNullOrEmpty(selectedRecipient))
            {
                MessageBox.Show("Получатель не выбран. Пересылка отменена", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string currentUserLogin = currentUser; // <- Замени на актуального пользователя
            if (selectedRecipient == currentUserLogin)
            {
                MessageBox.Show("Нельзя пересылать сообщения самому себе", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    foreach (int id in selectedMessageIds)
                    {
                        string selectQuery = "SELECT sender, subject, body, priority, is_read, is_sent, is_deleted FROM messages WHERE id = @id";
                        using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn))
                        {
                            selectCmd.Parameters.AddWithValue("@id", id);
                            using (MySqlDataReader reader = selectCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string originalSender = reader.GetString("sender");
                                    string subject = reader.GetString("subject");
                                    string body = reader.GetString("body");
                                    string priority = reader.GetString("priority");
                                    bool is_read = reader.GetBoolean("is_read");
                                    bool is_sent = reader.GetBoolean("is_sent");
                                    bool is_deleted = reader.GetBoolean("is_deleted");

                                    // Формируем тело пересланного сообщения
                                    body = $"--- Пересланное сообщение ---\n\nОт: {originalSender}\nТема: {subject}\nТекст:\n{body}\n\n";

                                    reader.Close();

                                    // Проверка, не пересылалось ли уже такое сообщение этому же получателю
                                    string checkQuery = @"SELECT COUNT(*) FROM messages 
                                              WHERE sender = @sender 
                                              AND recipient = @recipient 
                                              AND subject = @subject 
                                              AND body = @body 
                                              AND is_deleted = 0";
                                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                                    {
                                        checkCmd.Parameters.AddWithValue("@sender", currentUserLogin);
                                        checkCmd.Parameters.AddWithValue("@recipient", selectedRecipient);
                                        checkCmd.Parameters.AddWithValue("@subject", subject);
                                        checkCmd.Parameters.AddWithValue("@body", body);

                                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                                        // Если сообщение уже существует в базе, пропускаем пересылку
                                        if (count > 0)
                                        {
                                            MessageBox.Show("Это сообщение уже было переслано данному пользователю", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            return; // Пропускаем пересылку этого сообщения
                                        }
                                    }

                                    // Вставка пересланного сообщения с текущей датой/временем
                                    string insertQuery = @"INSERT INTO messages 
                                                (sender, recipient, subject, body, priority, date_sent, time_sent, is_read, is_sent, is_deleted) 
                                                VALUES 
                                                (@sender, @recipient, @subject, @body, @priority, @date_sent, @time_sent, @is_read, @is_sent, @is_deleted)";
                                    using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn))
                                    {
                                        insertCmd.Parameters.AddWithValue("@sender", currentUserLogin);
                                        insertCmd.Parameters.AddWithValue("@recipient", selectedRecipient);
                                        insertCmd.Parameters.AddWithValue("@subject", subject);
                                        insertCmd.Parameters.AddWithValue("@body", body);
                                        insertCmd.Parameters.AddWithValue("@priority", priority);
                                        insertCmd.Parameters.AddWithValue("@date_sent", DateTime.Now.ToString("dd.MM.yyyy"));
                                        insertCmd.Parameters.AddWithValue("@time_sent", DateTime.Now.ToString("HH:mm:ss"));
                                        insertCmd.Parameters.AddWithValue("@is_read", false); // Новое сообщение — непрочитанное
                                        insertCmd.Parameters.AddWithValue("@is_sent", true);  // Переслано — считается отправленным
                                        insertCmd.Parameters.AddWithValue("@is_deleted", false);

                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                    MessageBox.Show("Сообщения успешно пересланы!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Обновление данных в зависимости от текущей вкладки
                    if (currentView == "read")
                    {
                        LoadReadMessages(); // Метод для обновления прочитанных сообщений
                    }
                    else
                    {
                        LoadIncomingMessages(); // Метод для обновления входящих сообщений
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при пересылке сообщений: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // Метод выбора получателя через встроенную мини-форму
        private string ShowRecipientSelectDialog(List<string> users, string currentUser)
        {
            // Исключаем администратора и текущего пользователя
            var filteredUsers = users
                .Where(u =>
                    !string.Equals(u, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(u, currentUser, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Form prompt = new Form()
            {
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Выбор получателя",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label()
            {
                Left = 10,
                Top = 10,
                Text = "Введите имя пользователя или выберите из списка:",
                AutoSize = true
            };

            System.Windows.Forms.ComboBox comboBox = new System.Windows.Forms.ComboBox()
            {
                Left = 10,
                Top = 35,
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                ForeColor = Color.Gray,
                Text = "Поиск..."
            };

            comboBox.Items.AddRange(filteredUsers.ToArray());

            comboBox.GotFocus += (s, e) =>
            {
                if (comboBox.Text == "Поиск...")
                {
                    comboBox.Text = "";
                    comboBox.ForeColor = Color.Black;
                }
            };

            comboBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    comboBox.Text = "Поиск...";
                    comboBox.ForeColor = Color.Gray;
                }
            };

            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button()
            {
                Text = "OK",
                Left = 180,
                Width = 65,
                Top = 75,
                DialogResult = DialogResult.OK
            };

            System.Windows.Forms.Button cancel = new System.Windows.Forms.Button()
            {
                Text = "Отмена",
                Left = 255,
                Width = 65,
                Top = 75,
                DialogResult = DialogResult.Cancel
            };

            prompt.Controls.Add(label);
            prompt.Controls.Add(comboBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                string selected = comboBox.Text.Trim();
                if (filteredUsers.Contains(selected))
                    return selected;
            }

            return null;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string searchText = textBox1.Text.Trim();

            // Проверка на пустоту DataGridView
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("Таблица пуста, поиск невозможен", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Проверка на пустое значение в поле ввода
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Пожалуйста, введите имя пользователя для поиска!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool userFound = false;

            // Проходим по всем строкам и ищем имя в столбце "Отправитель"
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["sender"].Value != null && row.Cells["sender"].Value.ToString().Equals(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    // Пользователь найден, выделяем ячейки в столбце "Отправитель"
                    row.Cells["sender"].Style.BackColor = Color.Yellow;
                    userFound = true;
                }
                else
                {
                    // Если строка не содержит имя пользователя, сбрасываем выделение
                    row.Cells["sender"].Style.BackColor = Color.White;
                }
            }

            if (!userFound)
            {
                MessageBox.Show("Пользователь не найден в таблице", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Перемещаем на первую строку с найденным отправителем
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells["sender"].Style.BackColor == Color.Yellow)
                    {
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
        }

        private void отправитьВсемToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            List<string> selectedDepartments = ShowDepartmentSelectDialog(currentUser);
            if (selectedDepartments != null && selectedDepartments.Count > 0)
            {
                Form8 form8 = new Form8(currentUser, selectedDepartments);
                form8.ShowDialog();
            }
        }

        private List<string> ShowDepartmentSelectDialog(string senderUsername)
        {
            List<string> departmentNames = new List<string>();

            // Подгружаем список отделов из БД
            try
            {
                using (MySqlConnection con = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT name FROM departments ORDER BY name", con))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departmentNames.Add(reader.GetString("name"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке отделов: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (departmentNames.Count == 0)
            {
                MessageBox.Show("Нет доступных отделов", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            // Создаём мини-форму
            Form prompt = new Form()
            {
                Width = 370,
                Height = 300,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Выбор отделов",
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label()
            {
                Left = 10,
                Top = 10,
                Text = "Отметьте один или несколько отделов:",
                AutoSize = true
            };

            CheckedListBox checkedListBox = new CheckedListBox()
            {
                Left = 10,
                Top = 35,
                Width = 330,
                Height = 180
            };
            checkedListBox.Items.AddRange(departmentNames.ToArray());

            System.Windows.Forms.Button buttonOk = new System.Windows.Forms.Button()
            {
                Text = "OK",
                Left = 170,
                Width = 80,
                Top = 230
            };

            System.Windows.Forms.Button buttonCancel = new System.Windows.Forms.Button()
            {
                Text = "Отмена",
                Left = 260,
                Width = 80,
                Top = 230,
                DialogResult = DialogResult.Cancel
            };

            List<string> selectedDepartments = null;

            buttonOk.Click += (sender, e) =>
            {
                var selected = checkedListBox.CheckedItems.Cast<string>().ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("Пожалуйста, выберите хотя бы один отдел", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Проверка наличия хотя бы одного пользователя в выбранных отделах
                try
                {
                    using (MySqlConnection con = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                    {
                        con.Open();
                        bool hasUsers = false;
                        foreach (string dept in selected)
                        {
                            using (var cmd = new MySqlCommand(@"
                        SELECT COUNT(*) 
                        FROM users u 
                        JOIN user_details d ON u.id = d.user_id 
                        JOIN departments dep ON dep.id = d.department_id 
                        WHERE dep.name = @name AND u.username != @sender", con))
                            {
                                cmd.Parameters.AddWithValue("@name", dept);
                                cmd.Parameters.AddWithValue("@sender", senderUsername);
                                long count = (long)cmd.ExecuteScalar();
                                if (count > 0)
                                {
                                    hasUsers = true;
                                    break;
                                }
                            }
                        }

                        if (!hasUsers)
                        {
                            MessageBox.Show("Нет доступных получателей в выбранных отделах", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        selectedDepartments = selected;
                        prompt.DialogResult = DialogResult.OK;
                        prompt.Close();
                    }
                }
                catch (Exception exCheck)
                {
                    MessageBox.Show("Ошибка при проверке отделов: " + exCheck.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            prompt.Controls.Add(label);
            prompt.Controls.Add(checkedListBox);
            prompt.Controls.Add(buttonOk);
            prompt.Controls.Add(buttonCancel);
            prompt.AcceptButton = buttonOk;
            prompt.CancelButton = buttonCancel;

            var result = prompt.ShowDialog();
            return result == DialogResult.OK ? selectedDepartments : null;
        }

        private void прочитатьToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                string senderUsername = dataGridView1.SelectedRows[0].Cells["sender"].Value.ToString();
                string subject = dataGridView1.SelectedRows[0].Cells["subject"].Value.ToString();

                using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
                {
                    conn.Open();

                    // Считываем тело письма без условия is_read = 0
                    string query = "SELECT body FROM messages WHERE sender = @sender AND recipient = @recipient AND subject = @subject LIMIT 1";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sender", senderUsername);
                    cmd.Parameters.AddWithValue("@recipient", currentUser);
                    cmd.Parameters.AddWithValue("@subject", subject);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string body = reader.GetString("body");

                            Form7 readForm = new Form7(currentUser);
                            readForm.LoadReadOnlyMessage(subject, body, senderUsername);
                            readForm.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось загрузить текст сообщения", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        //private void MarkMessageAsRead(int messageId)
        //{
        //    try
        //    {
        //        using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
        //        {
        //            conn.Open();
        //            string query = "UPDATE messages SET is_read = 1 WHERE id = @id";
        //            MySqlCommand cmd = new MySqlCommand(query, conn);
        //            cmd.Parameters.AddWithValue("@id", messageId);
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Ошибка при обновлении статуса письма: " + ex.Message);
        //    }
        //}

    }
}
