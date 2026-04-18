using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace KeeperPRO
{
    public partial class SetVisitDateTimeDialog : Window
    {
        public DateTime VisitDate { get; private set; }
        public TimeSpan VisitTime { get; private set; }

        public SetVisitDateTimeDialog()
        {
            InitializeComponent();
            VisitDatePicker.SelectedDate = DateTime.Now.AddDays(1);
        }

        private bool ValidateTime(string time)
        {
            return Regex.IsMatch(time, @"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$");
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!VisitDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату посещения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(VisitTimeBox.Text) || !ValidateTime(VisitTimeBox.Text))
            {
                MessageBox.Show("Введите корректное время в формате ЧЧ:ММ (например, 14:30)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            VisitDate = VisitDatePicker.SelectedDate.Value;

            string[] parts = VisitTimeBox.Text.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
            {
                VisitTime = new TimeSpan(hours, minutes, 0);
            }
            else
            {
                VisitTime = new TimeSpan(10, 0, 0);
            }

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}