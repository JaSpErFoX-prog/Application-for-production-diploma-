
// Вызовы методов
GenerateAdminSql();
GenerateUsersSql();

// Методы
static void GenerateUsersSql()
{
    var users = new[]
    {
        new { Id = 6, Name = "Игорь", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
        new { Id = 13, Name = "Алла", Password = "1111", Role = "user", Phone = "3-96, 4-29", DepartmentId = 2 },
        new { Id = 18, Name = "Васильев Денис Олегович", Password = "1111", Role = "user", Phone = "6-11, 5-50", DepartmentId = 24 },
        new { Id = 19, Name = "Новикова Ольга Сергеевна", Password = "1111", Role = "user", Phone = "2-82, 3-82", DepartmentId = 36 },
        new { Id = 20, Name = "Фёдоров Артём Александрович", Password = "1111", Role = "user", Phone = "2-82, 3-82", DepartmentId = 36 },
        new { Id = 24, Name = "Соколов Павел Евгеньевич", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
        new { Id = 25, Name = "Волкова Мария Алексеевна", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
        new { Id = 29, Name = "Григорьева Виктория Романовна", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
        new { Id = 33, Name = "Белова Алина Геннадьевна", Password = "1111", Role = "user", Phone = "4-20", DepartmentId = 18 },
        new { Id = 35, Name = "Даник", Password = "2222", Role = "user", Phone = "4-04, 4-41", DepartmentId = 2 }
    };

    Console.WriteLine("SQL для вставки пользователей и их данных:\n");

    foreach (var user in users)
    {
        string hash = BCrypt.Net.BCrypt.HashPassword(user.Password);

        string sqlUser = $"INSERT INTO users (id, username, password, role) VALUES " +
                         $"({user.Id}, '{user.Name}', '{hash}', '{user.Role}') " +
                         $"ON DUPLICATE KEY UPDATE username=username;";
        Console.WriteLine(sqlUser);

        string sqlDetails = $"INSERT INTO user_details (user_id, phone, department_id) VALUES " +
                            $"({user.Id}, '{user.Phone}', {user.DepartmentId}) " +
                            $"ON DUPLICATE KEY UPDATE phone=VALUES(phone), department_id=VALUES(department_id);";
        Console.WriteLine(sqlDetails);
        Console.WriteLine();
    }

    Console.WriteLine("Нажмите любую клавишу для выхода...");
    Console.Read();
}

static void GenerateAdminSql()
{
    string username = "admin";
    string password = "admin";
    string role = "admin";

    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

    string sql = $"INSERT INTO users (id, username, password, role) VALUES " +
                 $"(1, '{username}', '{hashedPassword}', '{role}') " +
                 $"ON DUPLICATE KEY UPDATE username=username;";

    Console.WriteLine("SQL для вставки администратора:\n");
    Console.WriteLine(sql);
    Console.WriteLine("\nНажмите любую клавишу для выхода...");
    Console.Read();
}
