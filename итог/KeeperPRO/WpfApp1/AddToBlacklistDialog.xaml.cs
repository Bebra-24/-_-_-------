using System.Windows;

namespace WpfApp1
{
    public partial class AddToBlacklistDialog : Window
    {
        public string Reason { get; private set; } = "";

        public AddToBlacklistDialog(string visitorName)
        {
            InitializeComponent();
            VisitorNameText.Text = $"Посетитель: {visitorName}";
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReasonBox.Text))
            {
                MessageBox.Show("Укажите причину добавления в черный список", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ReasonBox.Text.Length > 5000)
            {
                MessageBox.Show("Причина не может превышать 5000 символов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Reason = ReasonBox.Text;
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