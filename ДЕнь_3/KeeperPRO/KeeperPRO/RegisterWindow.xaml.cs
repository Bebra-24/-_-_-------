using System;
using System.Windows;
using Npgsql;

namespace KeeperPRO
{
    public partial class RegisterWindow : Window
    {
        private MainWindow _mainWindow;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public RegisterWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private string HashPassword(string password, string salt)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                string combined = password + salt;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(combined);
                byte[] hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private bool ValidatePassword(string password)
        {
            if (password.Length < 8) return false;
            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            }
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        // 1. SQL регистрация
        private void RegisterSQLBtn_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusText.Text = "Введите email и пароль";
                return;
            }

            if (!ValidatePassword(password))
            {
                StatusText.Text = "Пароль не соответствует требованиям";
                return;
            }

            try
            {
                string passwordHash = HashPassword(password, email.Length.ToString());
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = @"INSERT INTO посетители (фамилия, имя, логин, пароль, email, дата_регистрации, активно) 
                                     VALUES (@lastName, @firstName, @login, @password, @email, @regDate, true)";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@lastName", "Новый");
                        cmd.Parameters.AddWithValue("@firstName", "Пользователь");
                        cmd.Parameters.AddWithValue("@login", email);
                        cmd.Parameters.AddWithValue("@password", passwordHash);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@regDate", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        // 2. Хранимая процедура
        private void RegisterProcBtn_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                StatusText.Text = "Введите email и пароль";
                return;
            }

            if (!ValidatePassword(password))
            {
                StatusText.Text = "Пароль не соответствует требованиям";
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT register_visitor(@email, @password)", conn))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@password", password);
                        var result = cmd.ExecuteScalar();
                        if (result != null && Convert.ToInt32(result) > 0)
                        {
                            MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            DialogResult = true;
                            Close();
                            return;
                        }
                    }
                }
                StatusText.Text = "Ошибка регистрации";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }
    }
}