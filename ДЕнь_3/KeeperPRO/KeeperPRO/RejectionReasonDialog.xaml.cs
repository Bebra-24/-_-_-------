using System.Windows;

namespace KeeperPRO
{
    public partial class RejectionReasonDialog : Window
    {
        public string RejectionReason { get; private set; }

        public RejectionReasonDialog()
        {
            InitializeComponent();
            ReasonCombo.SelectedIndex = 0;
            RejectionReason = "";
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ReasonCombo.SelectedItem == null || string.IsNullOrWhiteSpace(ReasonCombo.Text))
            {
                MessageBox.Show("Укажите причину отказа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RejectionReason = ReasonCombo.Text;
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