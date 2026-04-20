using System;
using System.Windows;
using Npgsql;

namespace WpfApp1
{
    public partial class DepartmentRequestWorkWindow : Window
    {
        private int _requestId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public DepartmentRequestWorkWindow(int requestId, string visitorName, string date, string purpose, int employeeId, string employeeName)
        {
            InitializeComponent();

            _requestId = requestId;

            TitleText.Text = "Заявка №" + requestId;
            InfoText.Text = "Посетитель: " + visitorName + "\nДата: " + date + "\nЦель: " + purpose;
        }

        private void EntryTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                EntryTimeText.Text = "Вход зафиксирован в " + entryTime.ToString("HH:mm:ss");
                EntryTimeText.Foreground = System.Windows.Media.Brushes.Green;
                EntryTimeBtn.IsEnabled = false;
                EntryTimeBtn.Background = System.Windows.Media.Brushes.Gray;

                MessageBox.Show("Вход зафиксирован: " + entryTime.ToString("HH:mm:ss"), "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
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

                ExitTimeText.Text = "Выход зафиксирован в " + exitTime.ToString("HH:mm:ss");
                ExitTimeText.Foreground = System.Windows.Media.Brushes.Green;
                ExitTimeBtn.IsEnabled = false;
                ExitTimeBtn.Background = System.Windows.Media.Brushes.Gray;

                MessageBox.Show("Выход зафиксирован: " + exitTime.ToString("HH:mm:ss"), "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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