using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace KeeperPRO
{
    public partial class EmployeeDepartmentWindow : Window
    {
        private int _employeeId;
        private string _employeeName;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private DataTable _allRequests;

        public EmployeeDepartmentWindow(int employeeId, string employeeName)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _employeeName = employeeName;

            if (UserInfoText != null)
                UserInfoText.Text = "Сотрудник: " + employeeName;

            LoadDepartments();
            LoadRequests();
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
                if (StatusText != null)
                {
                    StatusText.Text = "Ошибка: " + ex.Message;
                    StatusText.Foreground = Brushes.Red;
                }
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
                    if (TypeFilterCombo?.SelectedItem is ComboBoxItem selectedType && selectedType.Content.ToString() != "Все")
                    {
                        type = selectedType.Content.ToString();
                    }

                    string status = null;
                    if (StatusFilterCombo?.SelectedItem is ComboBoxItem selectedStatus && selectedStatus.Content.ToString() != "Все")
                    {
                        status = selectedStatus.Content.ToString();
                    }

                    string department = null;
                    if (DepartmentFilterCombo?.SelectedValue != null)
                    {
                        int deptId = (int)DepartmentFilterCombo.SelectedValue;
                        using (var cmd = new NpgsqlCommand("SELECT название FROM подразделения WHERE код_подразделения = @id", conn))
                        {
                            cmd.Parameters.AddWithValue("@id", deptId);
                            department = cmd.ExecuteScalar()?.ToString();
                        }
                    }

                    string query = @"
                        SELECT 
                            'личная' as Тип,
                            v.код_посещения as ID,
                            p.фамилия || ' ' || p.имя as Посетитель,
                            v.дата_посещения as Дата,
                            COALESCE(v.цель_визита, '') as Цель,
                            COALESCE(s.название, 'на проверке') as Статус,
                            COALESCE(подр.название, 'Не указано') as Подразделение
                        FROM посещения v
                        JOIN посетители p ON v.код_посетителя = p.код_посетителя
                        LEFT JOIN статусы_посещений s ON v.код_статуса = s.код_статуса
                        LEFT JOIN подразделения подр ON v.код_подразделения = подр.код_подразделения
                        
                        UNION ALL
                        
                        SELECT 
                            'групповая' as Тип,
                            g.код_заявки as ID,
                            p.фамилия || ' ' || p.имя as Посетитель,
                            g.дата_начала as Дата,
                            COALESCE(g.цель_посещения, '') as Цель,
                            g.статус as Статус,
                            COALESCE(подр.название, 'Не указано') as Подразделение
                        FROM групповые_заявки g
                        JOIN посетители p ON g.код_посетителя = p.код_посетителя
                        LEFT JOIN подразделения подр ON g.код_подразделения = подр.код_подразделения
                        
                        ORDER BY Дата DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            _allRequests = new DataTable();
                            adapter.Fill(_allRequests);

                            // Применяем фильтры
                            var filtered = _allRequests.AsEnumerable();
                            if (type != null)
                                filtered = filtered.Where(r => r["Тип"].ToString() == type);
                            if (status != null)
                                filtered = filtered.Where(r => r["Статус"].ToString() == status);
                            if (department != null)
                                filtered = filtered.Where(r => r["Подразделение"].ToString() == department);

                            DataTable result = filtered.Any() ? filtered.CopyToDataTable() : _allRequests.Clone();

                            if (RequestsGrid != null)
                                RequestsGrid.ItemsSource = result.DefaultView;
                            if (StatusText != null)
                            {
                                StatusText.Text = "Найдено заявок: " + result.Rows.Count;
                                StatusText.Foreground = Brushes.Green;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (StatusText != null)
                {
                    StatusText.Text = "Ошибка: " + ex.Message;
                    StatusText.Foreground = Brushes.Red;
                }
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => LoadRequests();

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (TypeFilterCombo != null)
                TypeFilterCombo.SelectedIndex = 0;
            if (StatusFilterCombo != null)
                StatusFilterCombo.SelectedIndex = 0;
            if (DepartmentFilterCombo != null)
                DepartmentFilterCombo.SelectedIndex = -1;
            LoadRequests();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => LoadRequests();

        private void ReportsBtn_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reportsWindow = new ReportsWindow();
            reportsWindow.Owner = this;
            reportsWindow.ShowDialog();
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            EmployeeLoginWindow loginWindow = new EmployeeLoginWindow();
            loginWindow.Show();
            this.Close();
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

                // ВЫЗОВ КОНСТРУКТОРА С 8 ПАРАМЕТРАМИ
                var detailWindow = new RequestDetailWindow(requestId, requestType, visitorName, date, purpose, status, _employeeId, _employeeName);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadRequests();
            }
        }
    }
}