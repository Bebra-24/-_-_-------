using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace KeeperPRO
{
    public partial class EmployeeTerminalWindow : Window
    {
        private int _employeeId;
        private string _employeeName;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private DataTable? _requests;

        public EmployeeTerminalWindow(int employeeId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _employeeName = "";
            LoadEmployeeInfo();
            LoadDepartments();
            LoadRequests();
        }

        private void LoadEmployeeInfo()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT фамилия, имя, отчество FROM сотрудники WHERE код_сотрудника = @id";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _employeeId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string fio = $"{reader.GetString(0)} {reader.GetString(1)}";
                                if (!reader.IsDBNull(2)) fio += " " + reader.GetString(2);
                                _employeeName = fio;
                                if (UserInfoText != null)
                                    UserInfoText.Text = $"Сотрудник: {fio} | Отдел: Общий отдел";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (UserInfoText != null)
                    UserInfoText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void LoadDepartments()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT код_подразделения, название FROM подразделения";
                    var dt = new DataTable();
                    using (var adapter = new NpgsqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
                        if (DepartmentFilterCombo != null)
                        {
                            DepartmentFilterCombo.ItemsSource = dt.DefaultView;
                            DepartmentFilterCombo.DisplayMemberPath = "название";
                            DepartmentFilterCombo.SelectedValuePath = "код_подразделения";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        private void LoadRequests()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string type = null;
                    if (TypeFilterCombo != null && TypeFilterCombo.SelectedItem is ComboBoxItem selectedType)
                    {
                        string typeContent = selectedType.Content.ToString();
                        if (typeContent != "Все")
                            type = typeContent;
                    }

                    string status = null;
                    if (StatusFilterCombo != null && StatusFilterCombo.SelectedItem is ComboBoxItem selectedStatus)
                    {
                        string statusContent = selectedStatus.Content.ToString();
                        if (statusContent != "Все")
                            status = statusContent;
                    }

                    string department = null;
                    if (DepartmentFilterCombo != null && DepartmentFilterCombo.SelectedValue != null)
                    {
                        int deptId = (int)DepartmentFilterCombo.SelectedValue;
                        using (var cmd = new NpgsqlCommand("SELECT название FROM подразделения WHERE код_подразделения = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", deptId);
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                                department = result.ToString();
                        }
                    }

                    using (var cmd = new NpgsqlCommand("SELECT * FROM filter_requests(@p_type, @p_department, @p_status)", conn))
                    {
                        cmd.Parameters.AddWithValue("@p_type", type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_department", department ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@p_status", status ?? (object)DBNull.Value);

                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            _requests = new DataTable();
                            adapter.Fill(_requests);
                            if (RequestsGrid != null)
                                RequestsGrid.ItemsSource = _requests.DefaultView;
                            if (CountText != null)
                                CountText.Text = $"Всего заявок: {_requests.Rows.Count}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadRequests();
        }

        private void ResetFiltersBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TypeFilterCombo != null)
                TypeFilterCombo.SelectedIndex = 0;
            if (StatusFilterCombo != null)
                StatusFilterCombo.SelectedIndex = 0;
            if (DepartmentFilterCombo != null)
                DepartmentFilterCombo.SelectedIndex = -1;
            LoadRequests();
        }

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsGrid?.SelectedItem != null)
            {
                var row = (DataRowView)RequestsGrid.SelectedItem;

                int requestId = Convert.ToInt32(row["ID"]);
                string requestType = row["Тип"]?.ToString() ?? "";
                string visitorName = row["Посетитель"]?.ToString() ?? "";
                string date = row["Дата"]?.ToString() ?? "";
                string purpose = row["Цель"]?.ToString() ?? "";
                string status = row["Статус"]?.ToString() ?? "";

                var detailWindow = new RequestDetailWindow(requestId, requestType, visitorName, date, purpose, status, _employeeId, _employeeName);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadRequests();
            }
        }

        // МЕТОД ДЛЯ ОТКРЫТИЯ ОКНА ОТЧЁТОВ
        private void ReportsBtn_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow();
            reportsWindow.Owner = this;
            reportsWindow.ShowDialog();
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }
}