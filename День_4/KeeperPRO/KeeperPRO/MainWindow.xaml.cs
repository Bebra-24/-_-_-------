using System;
using System.Windows;
using Npgsql;

namespace KeeperPRO
{
    public partial class MainWindow : Window
    {
        private int? currentUserId = null;
        private string currentUserLogin = null;
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";

        public MainWindow()
        {
            InitializeComponent();
            TestConnection();
        }

        private void TestConnection()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();
                    ConnectionStatusText.Text = " Подключено к БД";
                    ConnectionStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                    UpdateUI();
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusText.Text = " Ошибка подключения к БД";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.LightCoral;
                WelcomeText.Text = $"Ошибка подключения к базе данных!\n{ex.Message}\n\n" +
                                   "Проверьте:\n" +
                                   "1. Запущен ли PostgreSQL\n" +
                                   "2. Существует ли база данных 'сесия'\n" +
                                   "3. Правильность пароля в строке подключения";
            }
        }

        public void UpdateUI()
        {
            if (currentUserId != null)
            {
                UserInfoText.Text = $" {currentUserLogin}";
                UserRoleText.Text = "Посетитель";
                LoginBtn.Visibility = Visibility.Collapsed;
                RegisterBtn.Visibility = Visibility.Collapsed;
                LogoutBtn.Visibility = Visibility.Visible;
                NewRequestBtn.Visibility = Visibility.Visible;
                GroupRequestBtn.Visibility = Visibility.Visible;
                MyRequestsBtn.Visibility = Visibility.Visible;

                WelcomeText.Text = $"Здравствуйте, {currentUserLogin}!\n\n" +
                                   "Выберите действие в меню слева:\n" +
                                   "• Новая заявка - оформление личного пропуска\n" +
                                   "• Групповая заявка - оформление экскурсионной группы\n" +
                                   "• Мои заявки - просмотр статуса поданных заявок";
            }
            else
            {
                UserInfoText.Text = "Не авторизован";
                UserRoleText.Text = "";
                LoginBtn.Visibility = Visibility.Visible;
                RegisterBtn.Visibility = Visibility.Visible;
                LogoutBtn.Visibility = Visibility.Collapsed;
                NewRequestBtn.Visibility = Visibility.Collapsed;
                GroupRequestBtn.Visibility = Visibility.Collapsed;
                MyRequestsBtn.Visibility = Visibility.Collapsed;

                WelcomeText.Text = "Добро пожаловать в систему ХранительПРО!\n\n" +
                                   "Для оформления пропуска на предприятие необходимо:\n" +
                                   "1. Нажмите ' Вход в систему' - если у вас уже есть учётная запись\n" +
                                   "2. Или ' Регистрация' - для создания новой учётной записи\n\n" +
                                   "После авторизации вам будут доступны функции оформления заявок на посещение.";
            }
        }

        public void SetCurrentUser(int id, string login)
        {
            currentUserId = id;
            currentUserLogin = login;
            UpdateUI();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow(this);
            loginWindow.ShowDialog();
        }

        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow(this);
            registerWindow.ShowDialog();
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                currentUserId = null;
                currentUserLogin = null;
                UpdateUI();
                MessageBox.Show("Вы вышли из системы", "Выход",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NewRequestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId == null) return;
            var requestWindow = new RequestWindow(currentUserId.Value, this);
            requestWindow.ShowDialog();
        }

        private void GroupRequestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId == null) return;
            var groupRequestWindow = new GroupRequestWindow(currentUserId.Value);
            groupRequestWindow.ShowDialog();
        }

        private void EmployeeLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new EmployeeLoginWindow();
            loginWindow.ShowDialog();
        }
        private void MyRequestsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentUserId == null) return;
            var requestsWindow = new MyRequestsWindow(currentUserId.Value);
            requestsWindow.ShowDialog();
        }
    }
}