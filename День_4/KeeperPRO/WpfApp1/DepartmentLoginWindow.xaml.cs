using System;
using System.Windows;
using Npgsql;

namespace WpfApp1
{
    public partial class DepartmentLoginWindow : Window
    {
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public DepartmentLoginWindow()
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

            if (!int.TryParse(code, out int employeeCode))
            {
                StatusText.Text = "Код должен быть числовым";
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT код_сотрудника, фамилия, имя, код_подразделения
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
                                int? departmentId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);

                                DepartmentTerminalWindow terminalWindow = new DepartmentTerminalWindow(employeeId, employeeName, departmentId);
                                terminalWindow.Show();
                                this.Close();
                                return;
                            }
                        }
                    }
                }
                StatusText.Text = "Неверный код сотрудника!";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}