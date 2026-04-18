using System;
using System.Windows;
using Npgsql;

namespace KeeperPRO
{
    public partial class RequestWindow : Window
    {
        private int _userId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public RequestWindow(int userId, MainWindow mainWindow)
        {
            InitializeComponent();
            _userId = userId;
            VisitDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            BirthDatePicker.SelectedDate = DateTime.Now.AddYears(-20);
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = "SELECT фамилия, имя, email, номер_телефона, серия_паспорта, номер_паспорта, дата_рождения FROM посетители WHERE код_посетителя = @id";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                LastNameBox.Text = reader.GetString(0);
                                FirstNameBox.Text = reader.GetString(1);
                                EmailBox.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                PhoneBox.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                PassportSeriesBox.Text = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                PassportNumberBox.Text = reader.IsDBNull(5) ? "" : reader.GetString(5);
                                if (!reader.IsDBNull(6))
                                    BirthDatePicker.SelectedDate = reader.GetDateTime(6);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (!VisitDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату посещения");
                return;
            }

            if (string.IsNullOrWhiteSpace(LastNameBox.Text) || string.IsNullOrWhiteSpace(FirstNameBox.Text))
            {
                MessageBox.Show("Введите фамилию и имя");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailBox.Text))
            {
                MessageBox.Show("Введите email");
                return;
            }

            if (string.IsNullOrWhiteSpace(PassportSeriesBox.Text) || PassportSeriesBox.Text.Length != 4)
            {
                MessageBox.Show("Серия паспорта должна содержать 4 цифры");
                return;
            }

            if (string.IsNullOrWhiteSpace(PassportNumberBox.Text) || PassportNumberBox.Text.Length != 6)
            {
                MessageBox.Show("Номер паспорта должен содержать 6 цифр");
                return;
            }

            if (!BirthDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату рождения");
                return;
            }

            if (string.IsNullOrWhiteSpace(NoteBox.Text))
            {
                MessageBox.Show("Введите примечание");
                return;
            }

            if (string.IsNullOrWhiteSpace(PurposeBox.Text))
            {
                MessageBox.Show("Введите цель посещения");
                return;
            }

            DateTime visitDate = VisitDatePicker.SelectedDate.Value;
            if (visitDate < DateTime.Now.AddDays(1))
            {
                MessageBox.Show("Дата должна быть не раньше завтрашнего дня");
                return;
            }
            if (visitDate > DateTime.Now.AddDays(15))
            {
                MessageBox.Show("Дата не может быть позже чем через 15 дней");
                return;
            }

            DateTime birthDate = BirthDatePicker.SelectedDate.Value;
            int age = DateTime.Now.Year - birthDate.Year;
            if (birthDate > DateTime.Now.AddYears(-age)) age--;
            if (age < 16)
            {
                MessageBox.Show("Посетитель должен быть не моложе 16 лет");
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    // Обновляем данные посетителя
                    string updateQuery = @"UPDATE посетители SET 
                        фамилия = @lastName, 
                        имя = @firstName, 
                        email = @email, 
                        номер_телефона = @phone, 
                        серия_паспорта = @series, 
                        номер_паспорта = @number,
                        дата_рождения = @birthDate
                        WHERE код_посетителя = @id";
                    using (var cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@lastName", LastNameBox.Text);
                        cmd.Parameters.AddWithValue("@firstName", FirstNameBox.Text);
                        cmd.Parameters.AddWithValue("@email", EmailBox.Text);
                        cmd.Parameters.AddWithValue("@phone", string.IsNullOrEmpty(PhoneBox.Text) ? DBNull.Value : (object)PhoneBox.Text);
                        cmd.Parameters.AddWithValue("@series", PassportSeriesBox.Text);
                        cmd.Parameters.AddWithValue("@number", PassportNumberBox.Text);
                        cmd.Parameters.AddWithValue("@birthDate", birthDate);
                        cmd.Parameters.AddWithValue("@id", _userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Создаем заявку
                    string insertQuery = @"INSERT INTO посещения 
                        (код_посетителя, код_сотрудника, код_типа, код_статуса, дата_посещения, цель_визита, примечание)
                        VALUES (@visitorId, 1, 1, 1, @date, @purpose, @note)";
                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@visitorId", _userId);
                        cmd.Parameters.AddWithValue("@date", visitDate);
                        cmd.Parameters.AddWithValue("@purpose", PurposeBox.Text);
                        cmd.Parameters.AddWithValue("@note", NoteBox.Text);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Заявка успешно отправлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заявки: {ex.Message}");
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}