using System;
using System.Windows;
using Npgsql;

namespace KeeperPRO
{
    public partial class LoginWindow : Window
    {
        private MainWindow _mainWindow;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public LoginWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // Заполняем тестовыми данными для удобства
            LoginBox.Text = "test@mail.ru";
            PasswordBox.Password = "12345678";

            // Проверяем подключение при загрузке окна
            TestConnection();
        }

        private void TestConnection()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    DebugText.Text = " Подключение к БД успешно";
                    StatusText.Text = "Введите логин и пароль";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                DebugText.Text = $"Ошибка подключения к БД: {ex.Message}";
                StatusText.Text = " Нет подключения к базе данных!";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
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

        // 1. SQL запрос с подробной диагностикой
        private void LoginSQLBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                StatusText.Text = " Введите логин и пароль!";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                DebugText.Text = "Поля логина или пароля пусты";
                return;
            }

            DebugText.Text = $"Проверка пользователя: {login}";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    DebugText.Text += $"\nПодключено к БД: {conn.Database}";

                    // Сначала проверим, существует ли пользователь
                    string checkQuery = "SELECT код_посетителя, логин, пароль, активно FROM посетители WHERE логин = @login";
                    using (var cmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                StatusText.Text = " Пользователь с таким логином не найден!";
                                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                                DebugText.Text += $"\nПользователь '{login}' не найден в БД";
                                return;
                            }

                            int userId = reader.GetInt32(0);
                            string dbLogin = reader.GetString(1);
                            string storedHash = reader.GetString(2);
                            bool isActive = reader.GetBoolean(3);

                            DebugText.Text += $"\n Пользователь найден: ID={userId}, Активен={isActive}";
                            DebugText.Text += $"\nХеш в БД: {storedHash}";

                            if (!isActive)
                            {
                                StatusText.Text = " Учётная запись заблокирована!";
                                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                                DebugText.Text += $"\n Пользователь заблокирован (активно=false)";
                                return;
                            }

                            // Вычисляем хеш введённого пароля
                            string computedHash = HashPassword(password, login.Length.ToString());
                            DebugText.Text += $"\nВычисленный хеш: {computedHash}";

                            if (computedHash == storedHash)
                            {
                                DebugText.Text += $"\n Пароль совпадает! Вход разрешён.";
                                _mainWindow.SetCurrentUser(userId, login);
                                DialogResult = true;
                                Close();
                                return;
                            }
                            else
                            {
                                StatusText.Text = " Неверный пароль!";
                                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                                DebugText.Text += $"\n Пароль не совпадает!";
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $" Ошибка при входе!";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                DebugText.Text = $" Исключение: {ex.Message}\n{ex.StackTrace}";
            }
        }

        // 2. Хранимая процедура с подробной диагностикой
        private void LoginProcBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                StatusText.Text = " Введите логин и пароль!";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                DebugText.Text = "Поля логина или пароля пусты";
                return;
            }

            DebugText.Text = $"Проверка через процедуру: {login}";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    DebugText.Text += $"\n Подключено к БД: {conn.Database}";

                    // Проверяем, существует ли процедура
                    string checkProcQuery = "SELECT COUNT(*) FROM pg_proc WHERE proname = 'login_visitor'";
                    using (var cmd = new NpgsqlCommand(checkProcQuery, conn))
                    {
                        long procCount = (long)cmd.ExecuteScalar();
                        if (procCount == 0)
                        {
                            DebugText.Text += "\nХранимая процедура 'login_visitor' не найдена!";
                            StatusText.Text = " Хранимая процедура не найдена! Используйте SQL вход.";
                            StatusText.Foreground = System.Windows.Media.Brushes.Red;
                            return;
                        }
                        DebugText.Text += "\nХранимая процедура найдена";
                    }

                    using (var cmd = new NpgsqlCommand("SELECT * FROM login_visitor(@login, @password)", conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        var result = cmd.ExecuteScalar();
                        DebugText.Text += $"\nРезультат процедуры: {result}";

                        if (result != null && result != DBNull.Value && Convert.ToInt32(result) > 0)
                        {
                            int userId = Convert.ToInt32(result);
                            DebugText.Text += $"\nВход успешен! ID={userId}";
                            _mainWindow.SetCurrentUser(userId, login);
                            DialogResult = true;
                            Close();
                            return;
                        }
                        else
                        {
                            StatusText.Text = " Неверный логин или пароль (процедура)!";
                            StatusText.Foreground = System.Windows.Media.Brushes.Red;
                            DebugText.Text += "\n Процедура вернула 0 - неверные данные";
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $" Ошибка при вызове процедуры!";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                DebugText.Text = $" Исключение: {ex.Message}\n{ex.StackTrace}";
            }
        }
    }
}