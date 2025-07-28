[Русская версия](#разработка-по-для-автоматизации-документооборота-и-внутренней-коммуникации) | [English Version](#software-development-for-document-workflow-and-internal-communication-automation)

---

# Разработка ПО для автоматизации документооборота и внутренней коммуникации

**Дипломный проект**: разработка программного обеспечения для автоматизации документооборота и внутренней коммуникации в филиале ЗАО «АТЛАНТ» - Барановичский станкостроительный завод.

## О проекте

Программа создана на языке **C# (Windows Forms)** с использованием базы данных **MySQL**. Основная цель проекта — упростить обмен сообщениями и документами между сотрудниками предприятия из разных отделов, а также предоставить инструменты для управления документами.

### Основной функционал

- Обмен сообщениями и документами между сотрудниками;
- Операции с документами и сообщениями: сохранение, удаление, восстановление;
- Регистрация и авторизация пользователей;
- Разделение ролей (пользователь/администратор);
- Шифрование паролей в базе данных.

> **Примечание:** Это мой первый полноценный программный продукт. Он не лишён недостатков, но отражает мои навыки как начинающего разработчика.

---

## Технологии

- **Язык**: C# (Windows Forms)
- **СУБД**: MySQL 8+
- **Среда разработки**: Visual Studio 2019/2022

---

## Установка и запуск

### 1. Необходимое ПО
- Установленная **Visual Studio 2019** или **Visual Studio 2022**  
  (с пакетом для разработки классических приложений C# — Windows Forms).
- Установленный **MySQL Workbench** версии 8 или выше.

### 2. Настройка базы данных
1. Создайте базу данных с названием `document_system`.
2. Откройте файл **MySQL.txt** из проекта, скопируйте все SQL-запросы (`Ctrl + A → Ctrl + C`) и вставьте их в новый SQL-файл в MySQL Workbench.
3. Выполните запросы для создания таблиц.

### 3. Настройка подключения
В проекте используется следующая строка подключения:

server=localhost;database=document_system;uid=root;pwd=1111;


Убедитесь, что настройки **пользователя и пароля** MySQL совпадают с указанными.

### 4. Запуск программы
- Перейдите в папку проекта → `bin\Debug`
- Запустите `.exe`-файл

> При первом запуске база данных будет пустой — заполняйте её через интерфейс программы.

---

## Статус проекта

Проект выполнен в рамках дипломной работы. Код не идеален и требует доработки, но демонстрирует мой первый опыт создания прикладного ПО с использованием C# и MySQL.

---

# Software Development for Document Workflow and Internal Communication Automation

**Graduation project**: software development for automation of document flow and internal communications in the branch of JSC «ATLANT» - Baranovichi Machine-Tool Plant.

## About the Project

The program is built using **C# (Windows Forms)** and a **MySQL** database. Its main goal is to simplify the exchange of messages and documents between employees of different departments and provide tools for managing documents.

### Key Features

- Exchange of messages and documents between employees;
- Operations with messages and documents: save, delete, restore;
- User registration and authentication;
- Role-based functionality (user/admin);
- Password encryption in the database.

> **Note:** This is my first full-fledged software product. It is not perfect but reflects my skills as an entry-level developer.

---

## Technologies

- **Language**: C# (Windows Forms)
- **Database**: MySQL 8+
- **Development environment**: Visual Studio 2019/2022

---

## Installation and Launch

### 1. Required Software
- Installed **Visual Studio 2019** or **Visual Studio 2022**  
  (with the workload for developing classic C# applications — Windows Forms).
- Installed **MySQL Workbench** version 8 or higher.

### 2. Database Setup
1. Create a database named `document_system`.
2. Open the **MySQL.txt** file from the project, copy all SQL queries (`Ctrl + A → Ctrl + C`) and paste them into a new SQL file in MySQL Workbench.
3. Execute the queries to create the tables.

### 3. Connection String Setup
The project uses the following connection string:

server=localhost;database=document_system;uid=root;pwd=1111;


Make sure that your **MySQL username and password** match these settings.

### 4. Running the Program
- Go to the project folder → `bin\Debug`
- Run the `.exe` file

> On the first run, the database will be empty — fill it through the program’s interface.

---

## Project Status

The project was created as part of my diploma work. The code is not perfect and may require improvements, but it demonstrates my first experience in building desktop software using C# and MySQL.

---