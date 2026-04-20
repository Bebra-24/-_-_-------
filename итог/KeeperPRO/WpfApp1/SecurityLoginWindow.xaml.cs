using System;
using System.Windows;
using Npgsql;

namespace WpfApp1
{
    public partial class SecurityLoginWindow : Window
    {
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public SecurityLoginWindow()
        {
            InitializeComponent();
            TestConnection();
        }

        private void TestConnection()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    ConnectionStatus.Text = "✅ Подключено к БД";
                    ConnectionStatus.Foreground = System.Windows.Media.Brushes.Green;
                    StatusText.Text = "Введите код сотрудника";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus.Text = "❌ Ошибка подключения: " + ex.Message;
                ConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                StatusText.Text = "Ошибка подключения к БД!";
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeBox.Password;

            if (string.IsNullOrEmpty(code))
            {
                StatusText.Text = "❌ Введите код сотрудника!";
                return;
            }

            if (!int.TryParse(code, out int employeeCode))
            {
                StatusText.Text = "❌ Код должен быть числовым!";
                return;
            }

            DebugText.Text = "Поиск сотрудника с кодом: " + employeeCode;

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT код_сотрудника, фамилия, имя 
                        FROM сотрудники 
                        WHERE код_сотрудника = @code AND активно = true";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", employeeCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int employeeId = reader.GetInt32(0);
                                string employeeName = reader.GetString(1) + " " + reader.GetString(2);

                                DebugText.Text += "\n✅ Сотрудник найден: " + employeeName;

                                SecurityTerminalWindow terminalWindow = new SecurityTerminalWindow(employeeId, employeeName);
                                terminalWindow.Show();
                                this.Close();
                                return;
                            }
                            else
                            {
                                DebugText.Text += "\n❌ Сотрудник с кодом " + employeeCode + " не найден!";
                                StatusText.Text = "❌ Неверный код сотрудника!";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "❌ Ошибка: " + ex.Message;
                DebugText.Text = "Ошибка: " + ex.Message;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}