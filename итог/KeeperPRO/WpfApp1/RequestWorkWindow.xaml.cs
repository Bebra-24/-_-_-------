using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace WpfApp1
{
    public partial class RequestWorkWindow : Window
    {
        private int _requestId;
        private string _requestType;
        private string _visitorName;
        private string[]? _participants;
        private DateTime? _currentEntryTime;
        private DateTime? _currentExitTime;
        private int _employeeId;
        private string _employeeName;
        private int _departmentId;
        private string _departmentName;
        private DepartmentTerminalWindow _parentWindow;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private TimeSpan _maxTravelTime = TimeSpan.FromMinutes(15); // Время перемещения от проходной до подразделения

        public RequestWorkWindow(
            int requestId,
            string requestType,
            string visitorName,
            string[]? participants,
            DateTime? entryTime,
            DateTime? exitTime,
            int employeeId,
            string employeeName,
            int departmentId,
            string departmentName,
            DepartmentTerminalWindow parentWindow)
        {
            InitializeComponent();

            _requestId = requestId;
            _requestType = requestType;
            _visitorName = visitorName;
            _participants = participants;
            _currentEntryTime = entryTime;
            _currentExitTime = exitTime;
            _employeeId = employeeId;
            _employeeName = employeeName;
            _departmentId = departmentId;
            _departmentName = departmentName;
            _parentWindow = parentWindow;

            TitleText.Text = $"Заявка №{_requestId} - {_requestType}";
            LoadRequestDetails();
            UpdateTimeDisplay();
        }

        private void LoadRequestDetails()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT цель, дата_начала, статус
                        FROM view_list_requests 
                        WHERE id = @id AND тип_заявки = @type";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _requestId);
                        cmd.Parameters.AddWithValue("@type", _requestType);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                TypeText.Text = _requestType == "личная" ? "Личная" : "Групповая";
                                PurposeText.Text = reader["цель"]?.ToString() ?? "";
                                DateText.Text = Convert.ToDateTime(reader["дата_начала"]).ToString("dd.MM.yyyy");
                            }
                        }
                    }
                }

                // Заполняем список участников для групповой заявки
                if (_requestType == "групповая" && _participants != null && _participants.Length > 0)
                {
                    VisitorsBorder.Visibility = Visibility.Visible;
                    foreach (var participant in _participants)
                    {
                        VisitorsList.Items.Add(participant);
                    }
                }
                else if (_requestType == "личная")
                {
                    // Для личной заявки показываем одного посетителя
                    VisitorsBorder.Visibility = Visibility.Visible;
                    VisitorsList.Items.Add(_visitorName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void UpdateTimeDisplay()
        {
            if (_currentEntryTime.HasValue)
            {
                EntryTimeText.Text = _currentEntryTime.Value.ToString("HH:mm:ss");
                EntryTimeBtn.IsEnabled = false;
                EntryTimeBtn.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                EntryTimeText.Text = "Не зафиксировано";
                EntryTimeBtn.IsEnabled = true;
                EntryTimeBtn.Background = System.Windows.Media.Brushes.Green;
            }

            if (_currentExitTime.HasValue)
            {
                ExitTimeText.Text = _currentExitTime.Value.ToString("HH:mm:ss");
                ExitTimeBtn.IsEnabled = false;
                ExitTimeBtn.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                ExitTimeText.Text = "Не зафиксировано";
                ExitTimeBtn.IsEnabled = true;
                ExitTimeBtn.Background = System.Windows.Media.Brushes.Orange;
            }
        }

        private void EntryTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка, разрешил ли сотрудник охраны доступ
                if (!IsAccessAllowedBySecurity())
                {
                    MessageBox.Show("Доступ не разрешен сотрудником охраны! Сначала необходимо разрешить доступ на проходной.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime entryTime = DateTime.Now;

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE посещения 
                        SET время_входа = @entryTime
                        WHERE код_посещения = @id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@entryTime", entryTime.TimeOfDay);
                        cmd.Parameters.AddWithValue("@id", _requestId);
                        cmd.ExecuteNonQuery();
                    }
                }

                _currentEntryTime = entryTime;
                UpdateTimeDisplay();

                // Проверка на превышение времени перемещения
                CheckTravelTimeViolation(entryTime);

                MessageBox.Show($"Время входа зафиксировано: {entryTime:HH:mm:ss}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фиксации времени входа: {ex.Message}");
            }
        }

        private void ExitTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime exitTime = DateTime.Now;

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE посещения 
                        SET время_выхода = @exitTime
                        WHERE код_посещения = @id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@exitTime", exitTime.TimeOfDay);
                        cmd.Parameters.AddWithValue("@id", _requestId);
                        cmd.ExecuteNonQuery();
                    }
                }

                _currentExitTime = exitTime;
                UpdateTimeDisplay();

                MessageBox.Show($"Время выхода зафиксировано: {exitTime:HH:mm:ss}", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фиксации времени выхода: {ex.Message}");
            }
        }

        private bool IsAccessAllowedBySecurity()
        {
            // Проверяем, разрешил ли сотрудник охраны доступ
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT время_входа 
                        FROM посещения 
                        WHERE код_посещения = @id AND время_входа IS NOT NULL";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _requestId);
                        var result = cmd.ExecuteScalar();
                        // Если время входа уже зафиксировано, значит доступ разрешён
                        return result != null && result != DBNull.Value;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void CheckTravelTimeViolation(DateTime entryTime)
        {
            // Проверяем, не превышено ли время перемещения от проходной до подразделения
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    // Получаем время, когда охрана разрешила доступ
                    string query = @"
                        SELECT время_разрешения 
                        FROM посещения 
                        WHERE код_посещения = @id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _requestId);
                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            TimeSpan accessTime = TimeSpan.Parse(result.ToString()!);
                            TimeSpan currentTime = entryTime.TimeOfDay;
                            TimeSpan travelTime = currentTime - accessTime;

                            if (travelTime > _maxTravelTime)
                            {
                                // Нарушение - отправляем оповещение
                                ViolationBorder.Visibility = Visibility.Visible;
                                ViolationText.Text = $"Превышено время перемещения от проходной до подразделения! " +
                                    $"Допустимое время: {_maxTravelTime.TotalMinutes} минут, " +
                                    $"фактическое: {travelTime.TotalMinutes:F0} минут.";

                                // Записываем оповещение в БД
                                string violationQuery = @"
                                    INSERT INTO оповещения_о_нарушениях 
                                    (код_посещения, тип_нарушения, сообщение, для_сотрудника_охраны, для_сотрудника_подразделения)
                                    VALUES (@visitId, 'Превышение времени перемещения', @message, true, true)";

                                using (var violationCmd = new NpgsqlCommand(violationQuery, conn))
                                {
                                    violationCmd.Parameters.AddWithValue("@visitId", _requestId);
                                    violationCmd.Parameters.AddWithValue("@message", ViolationText.Text);
                                    violationCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки времени: {ex.Message}");
            }
        }

        private void VisitorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработка выбора посетителя
        }

        private void AddToBlacklist_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var textBlock = contextMenu?.PlacementTarget as TextBlock;
            string selectedVisitor = textBlock?.Text ?? "";

            if (string.IsNullOrEmpty(selectedVisitor))
                return;

            // Открываем диалоговое окно для ввода причины
            var dialog = new AddToBlacklistDialog(selectedVisitor);
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.Reason))
            {
                AddVisitorToBlacklist(selectedVisitor, dialog.Reason);
            }
        }

        private void AddVisitorToBlacklist(string visitorName, string reason)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    // Получаем код посетителя по ФИО
                    string[] nameParts = visitorName.Split(' ');
                    string lastName = nameParts[0];
                    string firstName = nameParts.Length > 1 ? nameParts[1] : "";

                    string getVisitorQuery = @"
                        SELECT код_посетителя 
                        FROM посетители 
                        WHERE фамилия = @lastName AND (имя = @firstName OR @firstName = '')";

                    int visitorId = 0;
                    using (var cmd = new NpgsqlCommand(getVisitorQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@lastName", lastName);
                        cmd.Parameters.AddWithValue("@firstName", firstName);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            visitorId = Convert.ToInt32(result);
                    }

                    if (visitorId == 0)
                    {
                        MessageBox.Show("Посетитель не найден в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Проверяем, есть ли уже в черном списке
                    string checkQuery = "SELECT COUNT(*) FROM черный_список WHERE код_посетителя = @visitorId";
                    using (var cmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@visitorId", visitorId);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        if (count > 0)
                        {
                            MessageBox.Show("Посетитель уже находится в черном списке!", "Внимание",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Добавляем в черный список
                    string insertQuery = @"
                        INSERT INTO черный_список (код_посетителя, причина, дата_добавления, добавлен_вручную)
                        VALUES (@visitorId, @reason, @date, true)";

                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@visitorId", visitorId);
                        cmd.Parameters.AddWithValue("@reason", reason);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show($"Посетитель '{visitorName}' добавлен в черный список!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении в черный список: {ex.Message}");
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}