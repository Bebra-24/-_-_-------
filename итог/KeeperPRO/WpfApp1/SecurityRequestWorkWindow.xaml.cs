using System;
using System.Media;
using System.Windows;
using Npgsql;

namespace WpfApp1
{
    public partial class SecurityRequestWorkWindow : Window
    {
        private int _requestId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private bool _accessGranted = false;

        public SecurityRequestWorkWindow(int requestId, string visitorName, string date, string purpose, int employeeId, string employeeName)
        {
            InitializeComponent();

            _requestId = requestId;

            TitleText.Text = "Заявка №" + requestId;
            InfoText.Text = "Посетитель: " + visitorName + "\nДата: " + date + "\nЦель: " + purpose;
        }

        private void AllowAccessBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_accessGranted)
            {
                MessageBox.Show("Доступ уже был разрешён!");
                return;
            }

            try
            {
                SystemSounds.Beep.Play();
                DateTime accessTime = DateTime.Now;

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE посещения 
                        SET время_входа = @accessTime
                        WHERE код_посещения = @id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@accessTime", accessTime.TimeOfDay);
                        cmd.Parameters.AddWithValue("@id", _requestId);
                        cmd.ExecuteNonQuery();
                    }
                }

                _accessGranted = true;
                AccessMessageText.Text = "✅ Доступ разрешён в " + accessTime.ToString("HH:mm:ss");
                AccessMessageText.Foreground = System.Windows.Media.Brushes.Green;
                AllowAccessBtn.IsEnabled = false;
                AllowAccessBtn.Background = System.Windows.Media.Brushes.Gray;

                MessageBox.Show("Доступ РАЗРЕШЁН! Время: " + accessTime.ToString("HH:mm:ss"), "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void ExitTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_accessGranted)
            {
                MessageBox.Show("Сначала необходимо разрешить доступ!");
                return;
            }

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

                ExitTimeText.Text = "✅ Убытие зафиксировано в " + exitTime.ToString("HH:mm:ss");
                ExitTimeText.Foreground = System.Windows.Media.Brushes.Green;
                ExitTimeBtn.IsEnabled = false;
                ExitTimeBtn.Background = System.Windows.Media.Brushes.Gray;

                MessageBox.Show("Убытие зафиксировано: " + exitTime.ToString("HH:mm:ss"), "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}