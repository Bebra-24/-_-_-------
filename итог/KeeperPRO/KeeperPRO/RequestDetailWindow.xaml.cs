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
        private string _visitorName;
        private string _date;
        private string _purpose;
        private string _status;
        private int _employeeId;
        private string _employeeName;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private bool _isInBlacklist = false;

        public RequestDetailWindow(int requestId, string requestType, string visitorName,
            string date, string purpose, string status, int employeeId, string employeeName)
        {
            InitializeComponent();

            _requestId = requestId;
            _requestType = requestType;
            _visitorName = visitorName;
            _date = date;
            _purpose = purpose;
            _status = status;
            _employeeId = employeeId;
            _employeeName = employeeName;

            TitleText.Text = "Заявка №" + requestId;
            TypeText.Text = requestType == "личная" ? "Личная" : "Групповая";
            VisitorText.Text = visitorName;
            DateText.Text = date;
            PurposeText.Text = purpose;
            StatusText.Text = GetStatusDisplay(status);

            // Если заявка групповая, загружаем участников
            if (requestType == "групповая")
            {
                LoadGroupMembers();
            }

            // Проверяем черный список
            CheckBlacklist();

            // Если статус не "на проверке", отключаем кнопки
            if (status != "на проверке")
            {
                ApproveBtn.IsEnabled = false;
                RejectBtn.IsEnabled = false;
                ApproveBtn.Background = Brushes.Gray;
                RejectBtn.Background = Brushes.Gray;
            }
        }

        private void LoadGroupMembers()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT фамилия || ' ' || имя as ФИО, телефон, email
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
                            MembersGrid.ItemsSource = dt.DefaultView;
                            MembersBorder.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки участников: " + ex.Message);
            }
        }

        private void CheckBlacklist()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    // Получаем код посетителя по ФИО
                    string[] nameParts = _visitorName.Split(' ');
                    string lastName = nameParts[0];

                    string getVisitorQuery = @"
                        SELECT код_посетителя 
                        FROM посетители 
                        WHERE фамилия = @lastName
                        LIMIT 1";

                    int visitorId = 0;
                    using (var cmd = new NpgsqlCommand(getVisitorQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@lastName", lastName);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                            visitorId = Convert.ToInt32(result);
                    }

                    if (visitorId > 0)
                    {
                        string checkQuery = "SELECT COUNT(*) FROM черный_список WHERE код_посетителя = @visitorId";
                        using (var cmd = new NpgsqlCommand(checkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@visitorId", visitorId);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            _isInBlacklist = count > 0;
                        }
                    }

                    if (_isInBlacklist)
                    {
                        BlacklistBorder.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210));
                        BlacklistStatusText.Text = "ВНИМАНИЕ! Посетитель находится в черном списке!";
                        BlacklistStatusText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));

                        // Автоматически отклоняем заявку
                        if (_status == "на проверке")
                        {
                            AutoRejectDueToBlacklist();
                        }
                    }
                    else
                    {
                        BlacklistBorder.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));
                        BlacklistStatusText.Text = "Проверка пройдена: посетитель не найден в черном списке";
                        BlacklistStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    }
                }
            }
            catch (Exception ex)
            {
                BlacklistStatusText.Text = "Ошибка проверки: " + ex.Message;
            }
        }

        private void AutoRejectDueToBlacklist()
        {
            string message = "Заявка на посещение объекта КИИ отклонена в связи с нахождением в черном списке";

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

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
                }

                StatusText.Text = GetStatusDisplay("не одобрена");
                StatusText.Foreground = Brushes.Red;
                ReasonBorder.Visibility = Visibility.Visible;
                ReasonText.Text = message;

                // Отключаем кнопки
                ApproveBtn.IsEnabled = false;
                RejectBtn.IsEnabled = false;
                ApproveBtn.Background = Brushes.Gray;
                RejectBtn.Background = Brushes.Gray;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка автоотклонения: " + ex.Message);
            }
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

                        string message = $"Заявка одобрена. Дата посещения: {dialog.VisitDate:dd.MM.yyyy}, время: {dialog.VisitTime:hh\\:mm}";

                        if (_requestType == "личная")
                        {
                            string updateQuery = @"
                                UPDATE посещения 
                                SET код_статуса = (SELECT код_статуса FROM статусы_посещений WHERE название = 'одобрена'),
                                    дата_посещения = @date,
                                    примечание = @message
                                WHERE код_посещения = @id";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@date", dialog.VisitDate);
                                cmd.Parameters.AddWithValue("@message", message);
                                cmd.Parameters.AddWithValue("@id", _requestId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string updateQuery = @"
                                UPDATE групповые_заявки 
                                SET статус = 'одобрена',
                                    дата_начала = @date,
                                    примечание = @message
                                WHERE код_заявки = @id";

                            using (var cmd = new NpgsqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@date", dialog.VisitDate);
                                cmd.Parameters.AddWithValue("@message", message);
                                cmd.Parameters.AddWithValue("@id", _requestId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    MessageBox.Show("Заявка одобрена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
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

                        string message = $"Заявка отклонена. Причина: {dialog.RejectionReason}";

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
                    }

                    MessageBox.Show("Заявка отклонена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        private string GetStatusDisplay(string status)
        {
            switch (status)
            {
                case "на проверке": return "⏳ На проверке";
                case "одобрена": return "✅ Одобрена";
                case "не одобрена": return "❌ Не одобрена";
                default: return status;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}