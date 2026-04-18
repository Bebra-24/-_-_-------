using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace KeeperPRO
{
    public partial class RequestDetailWindow : Window
    {
        private int _requestId;
        private string _requestType;
        private int _employeeId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private bool _isInBlacklist = false;
        private string _currentStatus = "";
        private string _rejectionReason = "";

        public RequestDetailWindow(int requestId, string requestType, int employeeId)
        {
            InitializeComponent();
            _requestId = requestId;
            _requestType = requestType;
            _employeeId = employeeId;

            if (TitleText != null)
                TitleText.Text = $"Информация о заявке №{_requestId}";

            LoadRequestDetails();
        }

        private void LoadRequestDetails()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    if (_requestType == "личная")
                    {
                        LoadPersonalRequest(conn);
                    }
                    else
                    {
                        LoadGroupRequest(conn);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadPersonalRequest(NpgsqlConnection conn)
        {
            string query = @"
                SELECT 
                    v.*,
                    p.фамилия, p.имя, p.отчество, p.серия_паспорта, p.номер_паспорта,
                    p.email, p.номер_телефона, s.название as статус_название
                FROM посещения v
                JOIN посетители p ON v.код_посетителя = p.код_посетителя
                LEFT JOIN статусы_посещений s ON v.код_статуса = s.код_статуса
                WHERE v.код_посещения = @id";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", _requestId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (RequestTypeText != null)
                            RequestTypeText.Text = "Личная";

                        _currentStatus = reader["статус_название"]?.ToString() ?? "на проверке";

                        if (StatusText != null)
                            StatusText.Text = GetStatusDisplay(_currentStatus);

                        if (StartDateText != null)
                            StartDateText.Text = Convert.ToDateTime(reader["дата_посещения"]).ToString("dd.MM.yyyy");

                        if (EndDateText != null)
                            EndDateText.Text = StartDateText.Text;

                        if (PurposeText != null)
                            PurposeText.Text = reader["цель_визита"]?.ToString() ?? "";

                        string fio = $"{reader["фамилия"]} {reader["имя"]}";
                        if (reader["отчество"] != DBNull.Value) fio += " " + reader["отчество"];

                        if (FioText != null)
                            FioText.Text = fio;

                        string passport = $"{reader["серия_паспорта"]} {reader["номер_паспорта"]}";

                        if (PassportText != null)
                            PassportText.Text = passport;

                        if (EmailText != null)
                            EmailText.Text = reader["email"]?.ToString() ?? "";

                        if (PhoneText != null)
                            PhoneText.Text = reader["номер_телефона"]?.ToString() ?? "";

                        if (reader["причина_отказа"] != DBNull.Value)
                        {
                            _rejectionReason = reader["причина_отказа"].ToString();
                            if (ReasonText != null)
                                ReasonText.Text = _rejectionReason;
                            if (ReasonBorder != null)
                                ReasonBorder.Visibility = Visibility.Visible;
                        }
                    }
                }
            }

            // Проверка черного списка
            CheckBlacklist(conn);
        }

        private void LoadGroupRequest(NpgsqlConnection conn)
        {
            string query = @"
                SELECT 
                    g.*,
                    p.фамилия, p.имя, p.отчество, p.email, p.номер_телефона
                FROM групповые_заявки g
                JOIN посетители p ON g.код_посетителя = p.код_посетителя
                WHERE g.код_заявки = @id";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", _requestId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (RequestTypeText != null)
                            RequestTypeText.Text = "Групповая";

                        _currentStatus = reader["статус"]?.ToString() ?? "на проверке";

                        if (StatusText != null)
                            StatusText.Text = GetStatusDisplay(_currentStatus);

                        if (StartDateText != null)
                            StartDateText.Text = Convert.ToDateTime(reader["дата_начала"]).ToString("dd.MM.yyyy");

                        if (EndDateText != null)
                            EndDateText.Text = Convert.ToDateTime(reader["дата_окончания"]).ToString("dd.MM.yyyy");

                        if (PurposeText != null)
                            PurposeText.Text = reader["цель_посещения"]?.ToString() ?? "";

                        string fio = $"{reader["фамилия"]} {reader["имя"]}";
                        if (reader["отчество"] != DBNull.Value) fio += " " + reader["отчество"];

                        if (FioText != null)
                            FioText.Text = fio;

                        if (EmailText != null)
                            EmailText.Text = reader["email"]?.ToString() ?? "";

                        if (PhoneText != null)
                            PhoneText.Text = reader["номер_телефона"]?.ToString() ?? "";

                        if (reader["причина_отказа"] != DBNull.Value)
                        {
                            _rejectionReason = reader["причина_отказа"].ToString();
                            if (ReasonText != null)
                                ReasonText.Text = _rejectionReason;
                            if (ReasonBorder != null)
                                ReasonBorder.Visibility = Visibility.Visible;
                        }
                    }
                }
            }

            // Загрузка участников группы
            LoadGroupMembers(conn);

            // Проверка черного списка для всех участников
            CheckBlacklistForGroup(conn);
        }

        private void LoadGroupMembers(NpgsqlConnection conn)
        {
            string query = @"
                SELECT номер_по_порядку, фамилия, имя, отчество, телефон, email, серия_паспорта, номер_паспорта
                FROM участники_групповой_заявки
                WHERE код_заявки = @id
                ORDER BY номер_по_порядку";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", _requestId);
                using (var adapter = new NpgsqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (MembersGrid != null)
                        MembersGrid.ItemsSource = dt.DefaultView;
                    if (MembersBorder != null)
                        MembersBorder.Visibility = Visibility.Visible;
                }
            }
        }

        private void CheckBlacklist(NpgsqlConnection conn)
        {
            // Получаем код посетителя
            string getVisitorQuery = @"
                SELECT код_посетителя FROM посещения WHERE код_посещения = @id";

            int visitorId = 0;
            using (var cmd = new NpgsqlCommand(getVisitorQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", _requestId);
                visitorId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Проверяем черный список
            string checkQuery = "SELECT COUNT(*) FROM черный_список WHERE код_посетителя = @visitorId";
            using (var cmd = new NpgsqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@visitorId", visitorId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                _isInBlacklist = count > 0;
            }

            UpdateUIAfterBlacklistCheck(conn);
        }

        private void CheckBlacklistForGroup(NpgsqlConnection conn)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM участники_групповой_заявки у
                JOIN черный_список ч ON у.серия_паспорта = ч.серия_паспорта AND у.номер_паспорта = ч.номер_паспорта
                WHERE у.код_заявки = @id";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", _requestId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                _isInBlacklist = count > 0;
            }

            UpdateUIAfterBlacklistCheck(conn);
        }

        private void UpdateUIAfterBlacklistCheck(NpgsqlConnection conn)
        {
            if (_isInBlacklist)
            {
                if (BlacklistBorder != null)
                    BlacklistBorder.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210));
                if (BlacklistStatusText != null)
                {
                    BlacklistStatusText.Text = "ВНИМАНИЕ! Посетитель находится в черном списке!";
                    BlacklistStatusText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                }

                // Автоматически отклоняем заявку
                if (_currentStatus == "на проверке")
                {
                    AutoRejectDueToBlacklist(conn);
                }

                // Запрещаем редактирование
                if (ApproveBtn != null)
                    ApproveBtn.Visibility = Visibility.Collapsed;
                if (RejectBtn != null)
                    RejectBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (BlacklistBorder != null)
                    BlacklistBorder.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                if (BlacklistStatusText != null)
                {
                    BlacklistStatusText.Text = "Проверка пройдена: посетитель не найден в черном списке";
                    BlacklistStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                }

                // Показываем кнопки только если заявка на проверке
                if (_currentStatus == "на проверке")
                {
                    if (ApproveBtn != null)
                        ApproveBtn.Visibility = Visibility.Visible;
                    if (RejectBtn != null)
                        RejectBtn.Visibility = Visibility.Visible;
                }
            }
        }

        private void AutoRejectDueToBlacklist(NpgsqlConnection conn)
        {
            string message = "Заявка на посещение объекта КИИ отклонена в связи с нарушением Федерального закона от 26.07.2017 № 187-ФЗ «О безопасности критической информационной инфраструктуры Российской Федерации»";

            if (_requestType == "личная")
            {
                string updateQuery = @"
                    UPDATE посещения 
                    SET код_статуса = (SELECT код_статуса FROM статусы_посещений WHERE название = 'не одобрена'),
                        причина_отказа = @reason
                    WHERE код_посещения = @id";

                using (var cmd = new NpgsqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@reason", message);
                    cmd.Parameters.AddWithValue("@id", _requestId);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                string updateQuery = @"
                    UPDATE групповые_заявки 
                    SET статус = 'не одобрена',
                        причина_отказа = @reason
                    WHERE код_заявки = @id";

                using (var cmd = new NpgsqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@reason", message);
                    cmd.Parameters.AddWithValue("@id", _requestId);
                    cmd.ExecuteNonQuery();
                }
            }

            // Сохраняем уведомление
            SaveNotification(conn, message);

            _currentStatus = "не одобрена";
            if (StatusText != null)
                StatusText.Text = GetStatusDisplay(_currentStatus);
            if (ReasonText != null)
                ReasonText.Text = message;
            if (ReasonBorder != null)
                ReasonBorder.Visibility = Visibility.Visible;
        }

        private void ApproveBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SetVisitDateTimeDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        conn.Open();

                        string message = $"Заявка на посещение объекта КИИ одобрена, дата посещения: {dialog.VisitDate:dd.MM.yyyy}, время посещения: {dialog.VisitTime:hh\\:mm}";

                        if (_requestType == "личная")
                        {
                            string updateQuery = @"
                                UPDATE посещения 
                                SET код_статуса = (SELECT код_статуса FROM статусы_посещений WHERE название = 'одобрена'),
                                    дата_посещения = @date,
                                    время_входа = @time
                                WHERE код_посещения = @id";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@date", dialog.VisitDate);
                                cmd.Parameters.AddWithValue("@time", dialog.VisitTime);
                                cmd.Parameters.AddWithValue("@id", _requestId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string updateQuery = @"
                                UPDATE групповые_заявки 
                                SET статус = 'одобрена',
                                    дата_начала = @date
                                WHERE код_заявки = @id";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@date", dialog.VisitDate);
                                cmd.Parameters.AddWithValue("@id", _requestId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        SaveNotification(conn, message);
                    }

                    MessageBox.Show("Заявка одобрена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void RejectBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RejectionReasonDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.RejectionReason))
            {
                try
                {
                    using (var conn = new NpgsqlConnection(connString))
                    {
                        conn.Open();

                        string message = $"Заявка на посещение объекта КИИ отклонена. Причина: {dialog.RejectionReason}";

                        if (_requestType == "личная")
                        {
                            string updateQuery = @"
                                UPDATE посещения 
                                SET код_статуса = (SELECT код_статуса FROM статусы_посещений WHERE название = 'не одобрена'),
                                    причина_отказа = @reason
                                WHERE код_посещения = @id";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@reason", message);
                                cmd.Parameters.AddWithValue("@id", _requestId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string updateQuery = @"
                                UPDATE групповые_заявки 
                                SET статус = 'не одобрена',
                                    причина_отказа = @reason
                                WHERE код_заявки = @id";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@reason", message);
                                cmd.Parameters.AddWithValue("@id", _requestId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        SaveNotification(conn, message);
                    }

                    MessageBox.Show("Заявка отклонена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private void SaveNotification(NpgsqlConnection conn, string message)
        {
            // Получаем код посетителя
            int visitorId = 0;
            if (_requestType == "личная")
            {
                string query = "SELECT код_посетителя FROM посещения WHERE код_посещения = @id";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", _requestId);
                    visitorId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            else
            {
                string query = "SELECT код_посетителя FROM групповые_заявки WHERE код_заявки = @id";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", _requestId);
                    visitorId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            string insertQuery = @"
                INSERT INTO уведомления (код_посетителя, код_заявки, тип_заявки, сообщение)
                VALUES (@visitorId, @requestId, @type, @message)";

            using (var cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@visitorId", visitorId);
                cmd.Parameters.AddWithValue("@requestId", _requestId);
                cmd.Parameters.AddWithValue("@type", _requestType);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.ExecuteNonQuery();
            }
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "на проверке" => "⏳ На проверке",
                "одобрена" => "✅ Одобрена",
                "не одобрена" => "❌ Не одобрена",
                _ => status
            };
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}