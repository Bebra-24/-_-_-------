using System;
using System.Windows;
using Npgsql;

namespace KeeperPRO
{
    public partial class EmployeeLoginWindow : Window
    {
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public EmployeeLoginWindow()
        {
            InitializeComponent();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeBox.Password;

            if (string.IsNullOrEmpty(code))
            {
                StatusText.Text = "Введите код сотрудника";
                return;
            }

            if (!int.TryParse(code, out int employeeId))
            {
                StatusText.Text = "Код должен быть числовым";
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT код_сотрудника, фамилия, имя FROM сотрудники WHERE код_сотрудника = @id AND активно = true";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", employeeId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var terminalWindow = new EmployeeTerminalWindow(employeeId);
                                terminalWindow.Show();
                                this.Close();
                                return;
                            }
                        }
                    }
                }
                StatusText.Text = "Неверный код сотрудника";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}