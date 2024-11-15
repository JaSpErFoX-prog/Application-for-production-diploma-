using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace NetworkStoress
{
    public partial class Form1 : Form
    {
        private SqlConnection connection;
        private string connectionString = "Server=localhost\\SQLEXPRESS; Database=StoreNetwork; Trusted_Connection=True;";

        public Form1()
        {
            InitializeComponent();
            connection = new SqlConnection(connectionString);
        }

        private string currentTableName = string.Empty;  // Храним текущее имя таблицы

        // Метод для установки текущей таблицы
        private void SetCurrentTable(string tableName)
        {
            currentTableName = tableName;
        }

        // Метод для получения текущего имени таблицы
        private string GetTableName()
        {
            return currentTableName;
        }

        // Метод для выполнения запроса и вывода данных в DataGridView
        private void ExecuteQueryAndDisplayData(string query)
        {
            try
            {
                //dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                connection.Open();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);
                dataGridView1.DataSource = dataTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            SetCurrentTable("Owners");
            string query = "SELECT * FROM Owners";
            ExecuteQueryAndDisplayData(query);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SetCurrentTable("Stores");
            string query = "SELECT * FROM Stores";
            ExecuteQueryAndDisplayData(query);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SetCurrentTable("Suppliers");
            string query = "SELECT * FROM Suppliers";
            ExecuteQueryAndDisplayData(query);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы действительно хотите выйти из программы?",
                                                  "Подтверждение выхода",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }


        private void button8_Click(object sender, EventArgs e)
        {
            bool changesSaved = false;

            // Проверка на то, пуста ли таблица
            if (dataGridView1.Rows.Count == 0 || (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
            {
                MessageBox.Show("Нет данных для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Перебираем все строки в DataGridView и сохраняем изменения
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Пропускаем пустую строку (она будет отмечена как новая)
                if (row.IsNewRow) continue;

                // Получаем значения из каждой ячейки строки
                int id = row.Cells[0].Value != null ? Convert.ToInt32(row.Cells[0].Value) : 0; // Получаем ID, если оно пустое - ID будет равно 0
                string columnName = GetTableName(); // Имя текущей таблицы

                // Работа с таблицей Owners
                if (columnName == "Owners")
                {
                    string fullName = row.Cells["FullName"]?.Value?.ToString();
                    string address = row.Cells["Address"]?.Value?.ToString();
                    string phone = row.Cells["Phone"]?.Value?.ToString();
                    decimal investment = row.Cells["Investment"]?.Value != DBNull.Value ? Convert.ToDecimal(row.Cells["Investment"].Value) : 0;
                    string registrationNumber = row.Cells["RegistrationNumber"]?.Value?.ToString();
                    DateTime registrationDate = row.Cells["RegistrationDate"]?.Value != DBNull.Value ? Convert.ToDateTime(row.Cells["RegistrationDate"].Value) : DateTime.MinValue;

                    if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(address)) // Пример проверки на пустые поля
                    {
                        MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Если запись новая (ID == 0), вставляем новую запись
                    string query = id == 0
                        ? "INSERT INTO Owners (FullName, Address, Phone, Investment, RegistrationNumber, RegistrationDate) VALUES (@FullName, @Address, @Phone, @Investment, @RegistrationNumber, @RegistrationDate)"
                        : "UPDATE Owners SET FullName = @FullName, Address = @Address, Phone = @Phone, Investment = @Investment, RegistrationNumber = @RegistrationNumber, RegistrationDate = @RegistrationDate WHERE OwnerID = @OwnerID";

                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@Phone", phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Investment", investment);
                        cmd.Parameters.AddWithValue("@RegistrationNumber", registrationNumber ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RegistrationDate", registrationDate == DateTime.MinValue ? (object)DBNull.Value : registrationDate);
                        if (id != 0)
                            cmd.Parameters.AddWithValue("@OwnerID", id); // Только для обновления

                        int rowsAffected = cmd.ExecuteNonQuery(); // Выполнение запроса

                        if (rowsAffected > 0) // Проверяем, были ли изменения в базе
                        {
                            changesSaved = true;
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при сохранении данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                // Работа с таблицей Stores
                else if (columnName == "Stores")
                {
                    string name = row.Cells["Name"]?.Value?.ToString();
                    string address = row.Cells["Address"]?.Value?.ToString();
                    string phone = row.Cells["Phone"]?.Value?.ToString();
                    decimal authorizedCapital = row.Cells["AuthorizedCapital"]?.Value != DBNull.Value ? Convert.ToDecimal(row.Cells["AuthorizedCapital"].Value) : 0;
                    string profile = row.Cells["Profile"]?.Value?.ToString();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(address)) // Пример проверки на пустые поля
                    {
                        MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Если запись новая (ID == 0), вставляем новую запись
                    string query = id == 0
                        ? "INSERT INTO Stores (Name, Address, Phone, AuthorizedCapital, Profile) VALUES (@Name, @Address, @Phone, @AuthorizedCapital, @Profile)"
                        : "UPDATE Stores SET Name = @Name, Address = @Address, Phone = @Phone, AuthorizedCapital = @AuthorizedCapital, Profile = @Profile WHERE StoreID = @StoreID";

                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@Phone", phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AuthorizedCapital", authorizedCapital);
                        cmd.Parameters.AddWithValue("@Profile", profile ?? (object)DBNull.Value);
                        if (id != 0)
                            cmd.Parameters.AddWithValue("@StoreID", id); // Только для обновления

                        int rowsAffected = cmd.ExecuteNonQuery(); // Выполнение запроса

                        if (rowsAffected > 0) // Проверяем, были ли изменения в базе
                        {
                            changesSaved = true;
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при сохранении данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
                // Работа с таблицей Suppliers
                else if (columnName == "Suppliers")
                {
                    string fullName = row.Cells["FullName"]?.Value?.ToString();
                    string address = row.Cells["Address"]?.Value?.ToString();
                    string phone = row.Cells["Phone"]?.Value?.ToString();
                    decimal supplyCost = row.Cells["SupplyCost"]?.Value != DBNull.Value ? Convert.ToDecimal(row.Cells["SupplyCost"].Value) : 0;

                    if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(address)) // Пример проверки на пустые поля
                    {
                        MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Если запись новая (ID == 0), вставляем новую запись
                    string query = id == 0
                        ? "INSERT INTO Suppliers (FullName, Address, Phone, SupplyCost) VALUES (@FullName, @Address, @Phone, @SupplyCost)"
                        : "UPDATE Suppliers SET FullName = @FullName, Address = @Address, Phone = @Phone, SupplyCost = @SupplyCost WHERE SupplierID = @SupplierID";

                    try
                    {
                        connection.Open();
                        SqlCommand cmd = new SqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@FullName", fullName);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@Phone", phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SupplyCost", supplyCost);
                        if (id != 0)
                            cmd.Parameters.AddWithValue("@SupplierID", id); // Только для обновления

                        int rowsAffected = cmd.ExecuteNonQuery(); // Выполнение запроса

                        if (rowsAffected > 0) // Проверяем, были ли изменения в базе
                        {
                            changesSaved = true;
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при сохранении данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            // Если были сохранены изменения
            if (changesSaved)
            {
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }




        private void button4_Click(object sender, EventArgs e)
        {
            string query = @"
    SELECT TOP 1 O.FullName, O.RegistrationDate, S.Address
    FROM Owners O
    JOIN Ownerships Ow ON O.OwnerID = Ow.OwnerID
    JOIN Stores S ON Ow.StoreID = S.StoreID
    WHERE S.Address LIKE '%Киевский%'
    ORDER BY O.RegistrationDate DESC"; // Самый молодой предприниматель
            ExecuteQueryAndDisplayData(query);

        }

        private void button5_Click(object sender, EventArgs e)
        {
            string query = @"
        SELECT O.FullName, O.RegistrationDate
        FROM Owners O
        WHERE DATEDIFF(YEAR, O.RegistrationDate, GETDATE()) < 18"; // Владели магазинами до 18 лет
            ExecuteQueryAndDisplayData(query);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string query = @"
        SELECT O.FullName, S.Name AS StoreName, S.AuthorizedCapital, O.Investment
        FROM Owners O
        JOIN Ownerships Ow ON O.OwnerID = Ow.OwnerID
        JOIN Stores S ON Ow.StoreID = S.StoreID
        WHERE S.AuthorizedCapital * 0.5 < O.Investment AND O.Address NOT LIKE S.Address"; // Капитал больше 50%
            ExecuteQueryAndDisplayData(query);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string query = @"
    SELECT S.Profile, S.AuthorizedCapital, O.FullName
    FROM Owners O
    JOIN Ownerships Ow ON O.OwnerID = Ow.OwnerID
    JOIN Stores S ON Ow.StoreID = S.StoreID
    WHERE O.FullName LIKE '%Кузнецов%' 
    ORDER BY S.AuthorizedCapital DESC"; // Профили магазинов Кузнецова
            ExecuteQueryAndDisplayData(query);

        }

        // Обновление данных
        private void button9_Click(object sender, EventArgs e)
        {
            // Проверяем, пуста ли таблица
            if (dataGridView1.Rows.Count == 0 || (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
            {
                MessageBox.Show("Таблица пуста. Невозможно обновить данные.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Обновляем данные в таблице
            try
            {
                string query = $"SELECT * FROM {GetTableName()}"; // Замените GetTableName() на название текущей таблицы
                ExecuteQueryAndDisplayData(query);
                MessageBox.Show("Таблица успешно обновлена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void button10_Click(object sender, EventArgs e)
        {
            // Проверяем, что таблица не пуста
            if (dataGridView1.Rows.Count == 0 || (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
            {
                MessageBox.Show("Таблица пуста. Невозможно удалить запись.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для удаления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем ID выбранной записи
            int selectedId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells[0].Value);
            string tableName = GetTableName();  // Получаем имя текущей таблицы

            // Диалоговое окно с подтверждением удаления
            DialogResult dialogResult = MessageBox.Show("Удалить эту запись?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.No)
            {
                return; // Если пользователь нажал "Нет", отменяем операцию
            }

            try
            {
                connection.Open();

                string query = string.Empty;

                // Логика удаления в зависимости от текущей таблицы
                if (tableName == "Owners")
                {
                    // Сначала удаляем записи из таблицы Ownerships (связь владельцев с магазинами)
                    query = "DELETE FROM Ownerships WHERE OwnerID = @id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", selectedId);
                    command.ExecuteNonQuery();

                    // Теперь удаляем владельца
                    query = "DELETE FROM Owners WHERE OwnerID = @id";
                }
                else if (tableName == "Stores")
                {
                    // Сначала удаляем записи из таблицы StoreSupplies (связь магазинов с поставщиками)
                    query = "DELETE FROM StoreSupplies WHERE StoreID = @id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", selectedId);
                    command.ExecuteNonQuery();

                    // Теперь удаляем записи из таблицы Ownerships (связь магазинов с владельцами)
                    query = "DELETE FROM Ownerships WHERE StoreID = @id";
                    command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", selectedId);
                    command.ExecuteNonQuery();

                    // Теперь удаляем магазин
                    query = "DELETE FROM Stores WHERE StoreID = @id";
                }
                else if (tableName == "Suppliers")
                {
                    // Сначала удаляем записи из таблицы StoreSupplies (связь поставщиков с магазинами)
                    query = "DELETE FROM StoreSupplies WHERE SupplierID = @id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@id", selectedId);
                    command.ExecuteNonQuery();

                    // Теперь удаляем поставщика
                    query = "DELETE FROM Suppliers WHERE SupplierID = @id";
                }
                else if (tableName == "Ownerships")
                {
                    // Удаляем запись из таблицы Ownerships
                    query = "DELETE FROM Ownerships WHERE OwnershipID = @id";
                }
                else if (tableName == "StoreSupplies")
                {
                    // Удаляем запись из таблицы StoreSupplies
                    query = "DELETE FROM StoreSupplies WHERE StoreSupplyID = @id";
                }

                // Выполнение запроса удаления
                SqlCommand deleteCommand = new SqlCommand(query, connection);
                deleteCommand.Parameters.AddWithValue("@id", selectedId);
                deleteCommand.ExecuteNonQuery();

                MessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // После удаления больше не обновляем DataGridView, это будет делать кнопка обновления
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Закрываем соединение
                connection.Close();
            }
        }

    }
}
