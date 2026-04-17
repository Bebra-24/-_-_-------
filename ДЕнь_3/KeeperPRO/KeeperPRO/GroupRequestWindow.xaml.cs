using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Npgsql;
using OfficeOpenXml;

namespace KeeperPRO
{
    public partial class GroupRequestWindow : Window
    {
        private int _userId;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private ObservableCollection<GroupMember> _members = new ObservableCollection<GroupMember>();
        private byte[]? _passportScan = null;
        private byte[]? _excelData = null;

        public GroupRequestWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            InitializeDates();
            LoadDepartments();

            if (MembersGrid != null)
                MembersGrid.ItemsSource = _members;
        }

        private void InitializeDates()
        {
            if (StartDatePicker != null)
                StartDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            if (EndDatePicker != null)
                EndDatePicker.SelectedDate = DateTime.Now.AddDays(2);
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
                        if (DepartmentCombo != null)
                        {
                            DepartmentCombo.ItemsSource = dt.DefaultView;
                            DepartmentCombo.DisplayMemberPath = "название";
                            DepartmentCombo.SelectedValuePath = "код_подразделения";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        private void DepartmentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentCombo == null || DepartmentCombo.SelectedValue == null) return;

            try
            {
                int deptId = (int)DepartmentCombo.SelectedValue;
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    string query = @"SELECT код_сотрудника, фамилия || ' ' || имя AS фио 
                                   FROM сотрудники WHERE код_подразделения = @deptId";
                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@deptId", deptId);
                        var dt = new DataTable();
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                            if (EmployeeCombo != null)
                            {
                                EmployeeCombo.ItemsSource = dt.DefaultView;
                                EmployeeCombo.DisplayMemberPath = "фио";
                                EmployeeCombo.SelectedValuePath = "код_сотрудника";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        private void DownloadTemplateBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "шаблон_списка_посетителей.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Список посетителей");

                    worksheet.Cells[1, 1].Value = "№";
                    worksheet.Cells[1, 2].Value = "Фамилия";
                    worksheet.Cells[1, 3].Value = "Имя";
                    worksheet.Cells[1, 4].Value = "Отчество";
                    worksheet.Cells[1, 5].Value = "Телефон";
                    worksheet.Cells[1, 6].Value = "Email";
                    worksheet.Cells[1, 7].Value = "Организация";
                    worksheet.Cells[1, 8].Value = "Дата рождения";
                    worksheet.Cells[1, 9].Value = "Серия паспорта";
                    worksheet.Cells[1, 10].Value = "Номер паспорта";
                    worksheet.Cells[1, 11].Value = "Примечание";

                    worksheet.Cells.AutoFitColumns();
                    package.SaveAs(new FileInfo(saveDialog.FileName));
                }
                MessageBox.Show("Шаблон скачан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UploadListBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var file = new FileInfo(openDialog.FileName);
                    _excelData = File.ReadAllBytes(openDialog.FileName);

                    using (var package = new ExcelPackage(file))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int row = 2;
                        _members.Clear();
                        int number = 1;

                        while (worksheet.Cells[row, 1].Value != null)
                        {
                            var member = new GroupMember
                            {
                                Номер = number++,
                                Фамилия = worksheet.Cells[row, 2].Value?.ToString() ?? "",
                                Имя = worksheet.Cells[row, 3].Value?.ToString() ?? "",
                                Отчество = worksheet.Cells[row, 4].Value?.ToString() ?? "",
                                Телефон = worksheet.Cells[row, 5].Value?.ToString() ?? "",
                                Email = worksheet.Cells[row, 6].Value?.ToString() ?? "",
                                Организация = worksheet.Cells[row, 7].Value?.ToString() ?? "",
                                СерияПаспорта = worksheet.Cells[row, 9].Value?.ToString() ?? "",
                                НомерПаспорта = worksheet.Cells[row, 10].Value?.ToString() ?? "",
                                Примечание = worksheet.Cells[row, 11].Value?.ToString() ?? ""
                            };

                            if (DateTime.TryParse(worksheet.Cells[row, 8].Value?.ToString(), out DateTime birthDate))
                                member.ДатаРождения = birthDate;
                            else
                                member.ДатаРождения = DateTime.Now.AddYears(-20);

                            _members.Add(member);
                            row++;
                        }
                    }
                    MessageBox.Show($"Загружено {_members.Count} участников", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки файла: {ex.Message}");
                }
            }
        }

        private void AddMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddMemberDialog();
            if (dialog.ShowDialog() == true)
            {
                _members.Add(new GroupMember
                {
                    Номер = _members.Count + 1,
                    Фамилия = dialog.LastName ?? "",
                    Имя = dialog.FirstName ?? "",
                    Отчество = dialog.MiddleName ?? "",
                    Телефон = dialog.Phone ?? "",
                    Email = dialog.Email ?? "",
                    Организация = dialog.Organization ?? "",
                    ДатаРождения = dialog.BirthDate,
                    СерияПаспорта = dialog.PassportSeries ?? "",
                    НомерПаспорта = dialog.PassportNumber ?? "",
                    Примечание = dialog.Note ?? ""
                });
            }
        }

        private void RemoveMemberBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MembersGrid != null && MembersGrid.SelectedItem != null)
            {
                _members.Remove((GroupMember)MembersGrid.SelectedItem);
                for (int i = 0; i < _members.Count; i++)
                    _members[i].Номер = i + 1;
            }
        }

        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var member = button?.Tag as GroupMember;

            if (member != null)
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.jpg)|*.jpg"
                };

                if (openDialog.ShowDialog() == true)
                {
                    member.Фотография = File.ReadAllBytes(openDialog.FileName);
                    MessageBox.Show("Фото добавлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void UploadPassportBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf"
            };

            if (openDialog.ShowDialog() == true)
            {
                _passportScan = File.ReadAllBytes(openDialog.FileName);
                if (PassportFileText != null)
                {
                    PassportFileText.Text = $"Файл: {Path.GetFileName(openDialog.FileName)}";
                    PassportFileText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
        }

        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            // Получаем даты с проверкой на null
            if (StartDatePicker == null || EndDatePicker == null)
            {
                MessageBox.Show("Ошибка инициализации элементов выбора даты");
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите даты начала и окончания");
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value;

            // Валидация дат
            if (startDate < DateTime.Now.AddDays(1))
            {
                MessageBox.Show("Дата начала должна быть не раньше завтрашнего дня");
                return;
            }
            if (startDate > DateTime.Now.AddDays(15))
            {
                MessageBox.Show("Дата начала не может быть позже чем через 15 дней");
                return;
            }
            if (endDate < startDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала");
                return;
            }
            if (endDate > startDate.AddDays(15))
            {
                MessageBox.Show("Дата окончания не может быть позже чем через 15 дней от даты начала");
                return;
            }

            // Проверка цели посещения
            if (PurposeBox == null || string.IsNullOrWhiteSpace(PurposeBox.Text))
            {
                MessageBox.Show("Введите цель посещения");
                return;
            }

            // Проверка выбора подразделения и сотрудника
            if (DepartmentCombo == null || DepartmentCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите подразделение");
                return;
            }

            if (EmployeeCombo == null || EmployeeCombo.SelectedValue == null)
            {
                MessageBox.Show("Выберите сотрудника");
                return;
            }

            // Проверка количества участников
            if (_members.Count < 5)
            {
                MessageBox.Show("В группе должно быть не менее 5 человек");
                return;
            }

            // Проверка скана паспорта
            if (_passportScan == null)
            {
                MessageBox.Show("Загрузите скан паспорта");
                return;
            }

            // Валидация каждого участника
            foreach (var member in _members)
            {
                if (string.IsNullOrWhiteSpace(member.Фамилия) || string.IsNullOrWhiteSpace(member.Имя))
                {
                    MessageBox.Show($"У участника №{member.Номер} не заполнены ФИО");
                    return;
                }
                if (string.IsNullOrWhiteSpace(member.Email) || !member.Email.Contains("@"))
                {
                    MessageBox.Show($"У участника №{member.Номер} некорректный email");
                    return;
                }
                if (string.IsNullOrWhiteSpace(member.СерияПаспорта) || member.СерияПаспорта.Length != 4)
                {
                    MessageBox.Show($"У участника №{member.Номер} неверная серия паспорта");
                    return;
                }
                if (string.IsNullOrWhiteSpace(member.НомерПаспорта) || member.НомерПаспорта.Length != 6)
                {
                    MessageBox.Show($"У участника №{member.Номер} неверный номер паспорта");
                    return;
                }

                int age = DateTime.Now.Year - member.ДатаРождения.Year;
                if (member.ДатаРождения > DateTime.Now.AddYears(-age)) age--;
                if (age < 16)
                {
                    MessageBox.Show($"Участник №{member.Номер} младше 16 лет");
                    return;
                }
            }

            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string insertQuery = @"INSERT INTO групповые_заявки 
                        (код_посетителя, код_сотрудника, дата_начала, дата_окончания, 
                         цель_посещения, статус, скан_паспорта, файл_список)
                        VALUES (@visitorId, @employeeId, @startDate, @endDate, @purpose, 'на проверке', @passportScan, @excelData)
                        RETURNING код_заявки";

                    int requestId;
                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@visitorId", _userId);
                        cmd.Parameters.AddWithValue("@employeeId", (int)EmployeeCombo.SelectedValue);
                        cmd.Parameters.AddWithValue("@startDate", startDate);
                        cmd.Parameters.AddWithValue("@endDate", endDate);
                        cmd.Parameters.AddWithValue("@purpose", PurposeBox.Text);
                        cmd.Parameters.AddWithValue("@passportScan", _passportScan);
                        cmd.Parameters.AddWithValue("@excelData", _excelData ?? (object)DBNull.Value);
                        requestId = (int)cmd.ExecuteScalar();
                    }

                    foreach (var member in _members)
                    {
                        string memberQuery = @"INSERT INTO участники_групповой_заявки 
                            (код_заявки, фамилия, имя, отчество, телефон, email, организация, 
                             дата_рождения, серия_паспорта, номер_паспорта, фотография, 
                             примечание, номер_по_порядку)
                            VALUES (@requestId, @lastName, @firstName, @middleName, @phone, @email, 
                                    @organization, @birthDate, @series, @number, @photo, @note, @numberOrder)";

                        using (var cmd = new NpgsqlCommand(memberQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@requestId", requestId);
                            cmd.Parameters.AddWithValue("@lastName", member.Фамилия ?? "");
                            cmd.Parameters.AddWithValue("@firstName", member.Имя ?? "");
                            cmd.Parameters.AddWithValue("@middleName", member.Отчество ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@phone", member.Телефон ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@email", member.Email ?? "");
                            cmd.Parameters.AddWithValue("@organization", member.Организация ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@birthDate", member.ДатаРождения);
                            cmd.Parameters.AddWithValue("@series", member.СерияПаспорта ?? "");
                            cmd.Parameters.AddWithValue("@number", member.НомерПаспорта ?? "");
                            cmd.Parameters.AddWithValue("@photo", member.Фотография ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@note", member.Примечание ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@numberOrder", member.Номер);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Групповая заявка успешно отправлена на проверку!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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

    // ============================================================
    // КЛАСС GroupMember
    // ============================================================
    public class GroupMember
    {
        public int Номер { get; set; }
        public string? Фамилия { get; set; }
        public string? Имя { get; set; }
        public string? Отчество { get; set; }
        public string? Телефон { get; set; }
        public string? Email { get; set; }
        public string? Организация { get; set; }
        public DateTime ДатаРождения { get; set; }
        public string? СерияПаспорта { get; set; }
        public string? НомерПаспорта { get; set; }
        public byte[]? Фотография { get; set; }
        public string? Примечание { get; set; }

        public GroupMember()
        {
            Фамилия = "";
            Имя = "";
            Отчество = "";
            Телефон = "";
            Email = "";
            Организация = "";
            СерияПаспорта = "";
            НомерПаспорта = "";
            Примечание = "";
            ДатаРождения = DateTime.Now.AddYears(-20);
        }
    }

    // ============================================================
    // КЛАСС AddMemberDialog
    // ============================================================
    public class AddMemberDialog : Window
    {
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Organization { get; set; }
        public DateTime BirthDate { get; set; }
        public string? PassportSeries { get; set; }
        public string? PassportNumber { get; set; }
        public string? Note { get; set; }

        public AddMemberDialog()
        {
            Title = "Добавление участника";
            Height = 580;
            Width = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            BirthDate = DateTime.Now.AddYears(-20);

            // Инициализация пустыми значениями
            LastName = "";
            FirstName = "";
            MiddleName = "";
            Phone = "";
            Email = "";
            Organization = "";
            PassportSeries = "";
            PassportNumber = "";
            Note = "";

            var mainPanel = new StackPanel { Margin = new Thickness(15) };

            // Фамилия
            mainPanel.Children.Add(new TextBlock { Text = "Фамилия *", Margin = new Thickness(0, 10, 0, 5), FontWeight = FontWeights.Bold });
            var lastNameBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32 };
            mainPanel.Children.Add(lastNameBox);

            // Имя
            mainPanel.Children.Add(new TextBlock { Text = "Имя *", Margin = new Thickness(0, 10, 0, 5), FontWeight = FontWeights.Bold });
            var firstNameBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32 };
            mainPanel.Children.Add(firstNameBox);

            // Отчество
            mainPanel.Children.Add(new TextBlock { Text = "Отчество", Margin = new Thickness(0, 10, 0, 5) });
            var middleNameBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32 };
            mainPanel.Children.Add(middleNameBox);

            // Телефон
            mainPanel.Children.Add(new TextBlock { Text = "Телефон", Margin = new Thickness(0, 10, 0, 5) });
            var phoneBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32 };
            mainPanel.Children.Add(phoneBox);

            // Email
            mainPanel.Children.Add(new TextBlock { Text = "Email *", Margin = new Thickness(0, 10, 0, 5), FontWeight = FontWeights.Bold });
            var emailBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32 };
            mainPanel.Children.Add(emailBox);

            // Организация
            mainPanel.Children.Add(new TextBlock { Text = "Организация", Margin = new Thickness(0, 10, 0, 5) });
            var orgBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32 };
            mainPanel.Children.Add(orgBox);

            // Дата рождения
            mainPanel.Children.Add(new TextBlock { Text = "Дата рождения *", Margin = new Thickness(0, 10, 0, 5), FontWeight = FontWeights.Bold });
            var birthPicker = new DatePicker { Margin = new Thickness(0, 0, 0, 5), Height = 32, SelectedDate = BirthDate };
            mainPanel.Children.Add(birthPicker);

            // Серия паспорта
            mainPanel.Children.Add(new TextBlock { Text = "Серия паспорта (4 цифры) *", Margin = new Thickness(0, 10, 0, 5), FontWeight = FontWeights.Bold });
            var seriesBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32, MaxLength = 4 };
            mainPanel.Children.Add(seriesBox);

            // Номер паспорта
            mainPanel.Children.Add(new TextBlock { Text = "Номер паспорта (6 цифр) *", Margin = new Thickness(0, 10, 0, 5), FontWeight = FontWeights.Bold });
            var numberBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 32, MaxLength = 6 };
            mainPanel.Children.Add(numberBox);

            // Примечание
            mainPanel.Children.Add(new TextBlock { Text = "Примечание", Margin = new Thickness(0, 10, 0, 5) });
            var noteBox = new TextBox { Margin = new Thickness(0, 0, 0, 5), Height = 60, TextWrapping = TextWrapping.Wrap };
            mainPanel.Children.Add(noteBox);

            // Кнопки
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 10) };
            var okBtn = new Button { Content = "OK", Width = 100, Height = 40, Margin = new Thickness(10), Background = System.Windows.Media.Brushes.LightGreen, FontWeight = FontWeights.Bold };
            var cancelBtn = new Button { Content = "Отмена", Width = 100, Height = 40, Margin = new Thickness(10), Background = System.Windows.Media.Brushes.LightCoral };
            buttonPanel.Children.Add(okBtn);
            buttonPanel.Children.Add(cancelBtn);
            mainPanel.Children.Add(buttonPanel);

            Content = mainPanel;

            okBtn.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(lastNameBox.Text) || string.IsNullOrWhiteSpace(firstNameBox.Text))
                {
                    MessageBox.Show("Заполните обязательные поля (Фамилия, Имя)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(emailBox.Text) || !emailBox.Text.Contains("@"))
                {
                    MessageBox.Show("Введите корректный email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(seriesBox.Text) || seriesBox.Text.Length != 4 || !seriesBox.Text.All(char.IsDigit))
                {
                    MessageBox.Show("Серия паспорта должна содержать 4 цифры", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(numberBox.Text) || numberBox.Text.Length != 6 || !numberBox.Text.All(char.IsDigit))
                {
                    MessageBox.Show("Номер паспорта должен содержать 6 цифр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LastName = lastNameBox.Text;
                FirstName = firstNameBox.Text;
                MiddleName = middleNameBox.Text;
                Phone = phoneBox.Text;
                Email = emailBox.Text;
                Organization = orgBox.Text;
                if (birthPicker.SelectedDate.HasValue)
                    BirthDate = birthPicker.SelectedDate.Value;
                PassportSeries = seriesBox.Text;
                PassportNumber = numberBox.Text;
                Note = noteBox.Text;

                DialogResult = true;
                Close();
            };

            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };
        }
    }
}