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

            label1.Text = $"Почта пользователя {currentUser}";
            label2.Text = ""; // Сброс уведомлений
            label2.ForeColor = Color.Red;
            label2.Font = new Font(label2.Font, FontStyle.Bold);

            ShowNotificationCount(); // загрузка уведомлений
                                     //dataGridView1.MouseDown += dataGridView1_MouseDown;
            восстановитьПомеченноеToolStripMenuItem.Enabled = false;
            восстановитьВсёToolStripMenuItem.Enabled = false;
            очиститьКорзинуToolStripMenuItem.Enabled = false;
            ПоместитьToolStripMenuItem.Enabled = false;
            ПрочитанноеToolStripMenuItem1.Enabled = false;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            Form7 sendMail = new Form7(currentUser);
            sendMail.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            currentView = "inbox";
            LoadIncomingMessages();
            восстановитьПомеченноеToolStripMenuItem.Enabled = false;
            восстановитьВсёToolStripMenuItem.Enabled = false;
            очиститьКорзинуToolStripMenuItem.Enabled = false;
            ПоместитьToolStripMenuItem.Enabled = true;
            отправитьВсемToolStripMenuItem.Enabled = true;
            ПрочитанноеToolStripMenuItem1.Enabled = true;
        }


        private void ShowNotificationCount()
        {
            using (MySqlConnection conn = new MySqlConnection("server=localhost;user=root;password=1111;database=document_system;"))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM messages WHERE recipient = @username AND is_read = 0 AND is_deleted = 0"; // добавляем фильтр is_deleted
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

        private void LoadIncomingMessages()
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
            WHERE m.recipient = @username AND m.is_read = 0 AND m.is_deleted = 0
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
            восстановитьПомеченноеToolStripMenuItem.Enabled = false;
            восстановитьВсёToolStripMenuItem.Enabled = false;
            очиститьКорзинуToolStripMenuItem.Enabled = false;
            ПоместитьToolStripMenuItem.Enabled = true;
            отправитьВсемToolStripMenuItem.Enabled = false;
            ПрочитанноеToolStripMenuItem1.Enabled = false;

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
        WHERE m.recipient = @username AND m.is_read = 1 AND m.is_deleted = 0
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
        private void LoadReadMessages()
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
        WHERE m.recipient = @username AND m.is_read = 1 AND m.is_deleted = 0
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
       
        private void ПоместитьToolStripMenuItem_Click(object sender, EventArgs e)
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
            восстановитьПомеченноеToolStripMenuItem.Enabled = true;
            восстановитьВсёToolStripMenuItem.Enabled = true;
            очиститьКорзинуToolStripMenuItem.Enabled = true;
            ПоместитьToolStripMenuItem.Enabled = false;
            отправитьВсемToolStripMenuItem.Enabled = false;
            ПрочитанноеToolStripMenuItem1.Enabled = false;

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
            WHERE m.recipient = @username AND m.is_deleted = 1
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
                    dataGridView1.MouseDown += dataGridView1_MouseDown1;
                }
            }
        }

        private void ПрочитанноеToolStripMenuItem1_Click(object sender, EventArgs e)
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

        private void восстановитьПомеченноеToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void восстановитьВсёToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void очиститьКорзинуToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void button3_Click(object sender, EventArgs e)
        {
            восстановитьПомеченноеToolStripMenuItem.Enabled = false;
            восстановитьВсёToolStripMenuItem.Enabled = false;
            очиститьКорзинуToolStripMenuItem.Enabled = false;
            ПоместитьToolStripMenuItem.Enabled = false;
            ПрочитанноеToolStripMenuItem1.Enabled = false;
            отправитьВсемToolStripMenuItem.Enabled = false;
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
            WHERE m.sender = @sender AND m.is_read = 0 AND m.is_deleted = 0
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
    }
}
