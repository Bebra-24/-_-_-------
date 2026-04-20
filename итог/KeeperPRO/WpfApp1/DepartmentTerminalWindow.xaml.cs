using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace WpfApp1
{
    public partial class DepartmentTerminalWindow : Window
    {
        private int _employeeId;
        private string _employeeName;
        private int? _departmentId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private DataTable _allRequests;

        public DepartmentTerminalWindow(int employeeId, string employeeName, int? departmentId)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _employeeName = employeeName;
            _departmentId = departmentId;

            if (UserInfoText != null)
                UserInfoText.Text = "Сотрудник: " + employeeName;

            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            v.код_посещения as ID,
                            p.фамилия || ' ' || p.имя as Посетитель,
                            v.дата_посещения as Дата,
                            COALESCE(v.цель_визита, '') as Цель,
                            COALESCE(s.название, 'на проверке') as Статус
                        FROM посещения v
                        JOIN посетители p ON v.код_посетителя = p.код_посетителя
                        LEFT JOIN статусы_посещений s ON v.код_статуса = s.код_статуса
                        WHERE v.код_статуса = 2
                        ORDER BY v.дата_посещения DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            _allRequests = new DataTable();
                            adapter.Fill(_allRequests);

                            if (RequestsGrid != null)
                                RequestsGrid.ItemsSource = _allRequests.DefaultView;

                            if (StatusText != null)
                                StatusText.Text = "Найдено заявок: " + _allRequests.Rows.Count;
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

        private void ApplyFilters()
        {
            if (_allRequests == null || _allRequests.Rows.Count == 0)
            {
                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = null;
                if (StatusText != null)
                    StatusText.Text = "Нет данных";
                return;
            }

            DataTable filtered = _allRequests.Copy();

            DateTime? filterDate = null;
            if (DateFilterPicker != null)
                filterDate = DateFilterPicker.SelectedDate;

            if (filterDate.HasValue)
            {
                var rows = filtered.AsEnumerable()
                    .Where(r => ((DateTime)r["Дата"]).Date == filterDate.Value.Date);
                filtered = rows.Any() ? rows.CopyToDataTable() : filtered.Clone();
            }

            if (RequestsGrid != null)
                RequestsGrid.ItemsSource = filtered.DefaultView;
            if (StatusText != null)
                StatusText.Text = "Найдено заявок: " + filtered.Rows.Count;
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (DateFilterPicker != null)
                DateFilterPicker.SelectedDate = null;
            ApplyFilters();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => LoadRequests();

        private void RequestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RequestsGrid != null && RequestsGrid.SelectedItem != null)
            {
                var row = (DataRowView)RequestsGrid.SelectedItem;
                int requestId = Convert.ToInt32(row["ID"]);
                string visitorName = row["Посетитель"].ToString();
                string date = row["Дата"].ToString();
                string purpose = row["Цель"].ToString();

                DepartmentRequestWorkWindow workWindow = new DepartmentRequestWorkWindow(requestId, visitorName, date, purpose, _employeeId, _employeeName);
                workWindow.Owner = this;
                workWindow.ShowDialog();
                LoadRequests();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            SecurityLoginWindow loginWindow = new SecurityLoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}