using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace KeeperPRO
{
    public partial class MyRequestsWindow : Window
    {
        private int _userId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private DataTable? _allRequests;

        public MyRequestsWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserInfo();
            LoadRequests();
        }

        private void LoadUserInfo()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT фамилия, имя, отчество, email FROM посетители WHERE код_посетителя = @id";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string fio = $"{reader.GetString(0)} {reader.GetString(1)}";
                                if (!reader.IsDBNull(2)) fio += " " + reader.GetString(2);
                                if (UserInfoText != null)
                                    UserInfoText.Text = $"Пользователь: {fio} | Email: {reader.GetString(3)}";
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

        private void LoadRequests()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    // Правильный запрос с русскими именами колонок
                    string query = @"
                        SELECT 
                            '' as Тип, 
                            v.код_посещения as ID, 
                            v.дата_посещения as ДатаПосещения,
                            v.дата_посещения as ДатаПодачи,
                            COALESCE(v.цель_визита, '') as Цель,
                            COALESCE(s.название, 'на проверке') as Статус
                        FROM посещения v
                        LEFT JOIN статусы_посещений s ON v.код_статуса = s.код_статуса
                        WHERE v.код_посетителя = @userId
                        
                        UNION ALL
                        
                        SELECT 
                            '' as Тип,
                            g.код_заявки as ID,
                            g.дата_начала as ДатаПосещения,
                            g.дата_создания as ДатаПодачи,
                            COALESCE(g.цель_посещения, '') as Цель,
                            CASE 
                                WHEN g.статус = 'на проверке' THEN ' На проверке'
                                WHEN g.статус = 'одобрена' THEN ' Одобрена'
                                WHEN g.статус = 'не одобрена' THEN ' Не одобрена'
                                ELSE COALESCE(g.статус, 'на проверке')
                            END as Статус
                        FROM групповые_заявки g
                        WHERE g.код_посетителя = @userId
                        
                        ORDER BY ДатаПодачи DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", _userId);
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            _allRequests = new DataTable();
                            adapter.Fill(_allRequests);

                            if (RequestsGrid != null)
                                RequestsGrid.ItemsSource = _allRequests.DefaultView;

                            if (CountText != null)
                                CountText.Text = $"Всего заявок: {_allRequests.Rows.Count}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // Создаём пустую таблицу с сообщением об ошибке
                _allRequests = new DataTable();
                _allRequests.Columns.Add("Тип", typeof(string));
                _allRequests.Columns.Add("ID", typeof(int));
                _allRequests.Columns.Add("ДатаПосещения", typeof(DateTime));
                _allRequests.Columns.Add("ДатаПодачи", typeof(DateTime));
                _allRequests.Columns.Add("Цель", typeof(string));
                _allRequests.Columns.Add("Статус", typeof(string));

                DataRow row = _allRequests.NewRow();
                row["Тип"] = "⚠️";
                row["ID"] = 0;
                row["ДатаПосещения"] = DateTime.Now;
                row["ДатаПодачи"] = DateTime.Now;
                row["Цель"] = ex.Message;
                row["Статус"] = "Ошибка";
                _allRequests.Rows.Add(row);

                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = _allRequests.DefaultView;

                if (CountText != null)
                    CountText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allRequests == null || _allRequests.Rows.Count == 0) return;
            if (StatusFilterCombo == null) return;

            string? statusFilter = (StatusFilterCombo.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(statusFilter) || statusFilter == "Все заявки")
            {
                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = _allRequests.DefaultView;
                if (CountText != null)
                    CountText.Text = $"Всего заявок: {_allRequests.Rows.Count}";
                return;
            }

            var filtered = _allRequests.AsEnumerable();

            if (statusFilter.Contains("На проверке"))
                filtered = filtered.Where(row => row.Field<string>("Статус").Contains("На проверке"));
            else if (statusFilter.Contains("Одобрена"))
                filtered = filtered.Where(row => row.Field<string>("Статус").Contains("Одобрена"));
            else if (statusFilter.Contains("Не одобрена"))
                filtered = filtered.Where(row => row.Field<string>("Статус").Contains("Не одобрена"));

            var result = filtered.CopyToDataTable();

            if (RequestsGrid != null)
                RequestsGrid.ItemsSource = result.DefaultView;

            if (CountText != null)
                CountText.Text = $"Показано: {result.Rows.Count} из {_allRequests.Rows.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allRequests == null || _allRequests.Rows.Count == 0) return;
            if (SearchBox == null) return;

            string searchText = SearchBox.Text?.ToLower() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = _allRequests.DefaultView;
                if (CountText != null)
                    CountText.Text = $"Всего заявок: {_allRequests.Rows.Count}";
                return;
            }

            var filtered = _allRequests.AsEnumerable()
                .Where(row => (row.Field<string>("Цель")?.ToLower().Contains(searchText) == true) ||
                              (row.Field<string>("Статус")?.ToLower().Contains(searchText) == true));

            if (filtered.Any())
            {
                var result = filtered.CopyToDataTable();
                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = result.DefaultView;
                if (CountText != null)
                    CountText.Text = $"Найдено: {result.Rows.Count} из {_allRequests.Rows.Count}";
            }
            else
            {
                if (RequestsGrid != null)
                    RequestsGrid.ItemsSource = null;
                if (CountText != null)
                    CountText.Text = $"Ничего не найдено из {_allRequests.Rows.Count}";
            }
        }

        private void ClearFilterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SearchBox != null)
                SearchBox.Text = "";

            if (StatusFilterCombo != null && StatusFilterCombo.Items.Count > 0)
                StatusFilterCombo.SelectedIndex = 0;

            if (_allRequests != null && RequestsGrid != null)
                RequestsGrid.ItemsSource = _allRequests.DefaultView;

            if (CountText != null && _allRequests != null)
                CountText.Text = $"Всего заявок: {_allRequests.Rows.Count}";
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadRequests();
        }
    }
}