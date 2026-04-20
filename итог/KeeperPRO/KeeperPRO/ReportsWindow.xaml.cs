using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;
using OfficeOpenXml;

namespace KeeperPRO
{
    public partial class ReportsWindow : Window
    {
        private string connString = "Host=localhost;Port=5432;Database=сесия;Username=postgres;Password=1;";
        private string reportsFolder;
        private Timer autoReportTimer;

        public ReportsWindow()
        {
            InitializeComponent();
            InitializeReportsFolder();
            StartAutoReportTimer();
            if (ReportDatePicker != null)
                ReportDatePicker.SelectedDate = DateTime.Now;
        }

        private void InitializeReportsFolder()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            reportsFolder = Path.Combine(documentsPath, "Отчеты ТБ");

            if (!Directory.Exists(reportsFolder))
            {
                Directory.CreateDirectory(reportsFolder);
            }

            if (ReportsFolderText != null)
                ReportsFolderText.Text = reportsFolder;
        }

        private void StartAutoReportTimer()
        {
            autoReportTimer = new Timer(CreateAutoReport, null, TimeSpan.Zero, TimeSpan.FromHours(3));
            if (AutoReportStatus != null)
            {
                AutoReportStatus.Text = "Статус: Активен (отчёты каждые 3 часа)";
                AutoReportStatus.Foreground = Brushes.Green;
            }
        }

        private void CreateAutoReport(object state)
        {
            Dispatcher.Invoke(() =>
            {
                CreateHourlyReport();
            });
        }

        private void CreateHourlyReport()
        {
            try
            {
                DateTime now = DateTime.Now;
                string dateFolder = Path.Combine(reportsFolder, now.ToString("dd_MM_yyyy"));

                if (!Directory.Exists(dateFolder))
                {
                    Directory.CreateDirectory(dateFolder);
                }

                string fileName = Path.Combine(dateFolder, $"Отчет_{now:HH}_часов.xlsx");

                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            COALESCE(подр.название, 'Не указано') as Подразделение,
                            COUNT(*) as Количество_посетителей
                        FROM посещения v
                        JOIN сотрудники с ON v.код_сотрудника = с.код_сотрудника
                        LEFT JOIN подразделения подр ON с.код_подразделения = подр.код_подразделения
                        WHERE v.код_статуса = 2
                          AND v.дата_посещения = CURRENT_DATE
                          AND v.время_входа <= CURRENT_TIME
                        GROUP BY подр.название
                        ORDER BY COUNT(*) DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var adapter = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        using (var package = new ExcelPackage())
                        {
                            var worksheet = package.Workbook.Worksheets.Add($"Отчет_{now:HH}_часов");

                            worksheet.Cells[1, 1].Value = "Отчёт о количестве посетителей";
                            worksheet.Cells[1, 2].Value = $"Дата: {now:dd.MM.yyyy}";
                            worksheet.Cells[1, 3].Value = $"Время: {now:HH}:00";

                            worksheet.Cells[3, 1].Value = "Подразделение";
                            worksheet.Cells[3, 2].Value = "Количество посетителей";

                            using (var range = worksheet.Cells[3, 1, 3, 2])
                            {
                                range.Style.Font.Bold = true;
                                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            }

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                worksheet.Cells[i + 4, 1].Value = dt.Rows[i]["Подразделение"].ToString();
                                worksheet.Cells[i + 4, 2].Value = Convert.ToInt32(dt.Rows[i]["Количество_посетителей"]);
                            }

                            worksheet.Cells.AutoFitColumns();
                            package.SaveAs(new FileInfo(fileName));
                        }
                    }
                }

                if (AutoReportStatus != null)
                {
                    AutoReportStatus.Text = $"Статус: Отчёт создан в {now:HH:mm}";
                    AutoReportStatus.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                if (AutoReportStatus != null)
                {
                    AutoReportStatus.Text = $"Ошибка: {ex.Message}";
                    AutoReportStatus.Foreground = Brushes.Red;
                }
            }
        }

        private void Period_Changed(object sender, RoutedEventArgs e)
        {
            GenerateReport();
        }

        private void ReportDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            GenerateReport();
        }

        private void GenerateReportBtn_Click(object sender, RoutedEventArgs e)
        {
            GenerateReport();
        }

        private void GenerateReport()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    DateTime selectedDate = DateTime.Now;
                    if (ReportDatePicker != null && ReportDatePicker.SelectedDate.HasValue)
                        selectedDate = ReportDatePicker.SelectedDate.Value;

                    string dateCondition = "";

                    if (DayRadio != null && DayRadio.IsChecked == true)
                    {
                        dateCondition = $"v.дата_посещения = '{selectedDate:yyyy-MM-dd}'";
                    }
                    else if (MonthRadio != null && MonthRadio.IsChecked == true)
                    {
                        dateCondition = $"DATE_TRUNC('month', v.дата_посещения) = DATE_TRUNC('month', '{selectedDate:yyyy-MM-dd}'::date)";
                    }
                    else
                    {
                        dateCondition = $"DATE_TRUNC('year', v.дата_посещения) = DATE_TRUNC('year', '{selectedDate:yyyy-MM-dd}'::date)";
                    }

                    string query = $@"
                        SELECT 
                            COALESCE(подр.название, 'Не указано') as Подразделение,
                            COUNT(*) as Количество_посещений
                        FROM посещения v
                        JOIN сотрудники с ON v.код_сотрудника = с.код_сотрудника
                        LEFT JOIN подразделения подр ON с.код_подразделения = подр.код_подразделения
                        WHERE v.код_статуса = 2 AND {dateCondition}
                        GROUP BY подр.название
                        ORDER BY COUNT(*) DESC";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var adapter = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (VisitsGrid != null)
                            VisitsGrid.ItemsSource = dt.DefaultView;
                        if (StatusText != null)
                        {
                            StatusText.Text = $"Найдено записей: {dt.Rows.Count}";
                            StatusText.Foreground = Brushes.Green;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (StatusText != null)
                {
                    StatusText.Text = $"Ошибка: {ex.Message}";
                    StatusText.Foreground = Brushes.Red;
                }
            }
        }

        private void RefreshCurrentBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connString))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            COALESCE(подр.название, 'Не указано') as Подразделение,
                            p.фамилия || ' ' || p.имя as Посетитель,
                            v.дата_посещения as Дата,
                            v.время_входа as Время_входа,
                            v.цель_визита as Цель
                        FROM посещения v
                        JOIN посетители p ON v.код_посетителя = p.код_посетителя
                        JOIN сотрудники с ON v.код_сотрудника = с.код_сотрудника
                        LEFT JOIN подразделения подр ON с.код_подразделения = подр.код_подразделения
                        WHERE v.код_статуса = 4
                          AND v.время_входа IS NOT NULL
                          AND v.время_выхода IS NULL
                        ORDER BY подр.название, p.фамилия";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var adapter = new NpgsqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (CurrentVisitorsGrid != null)
                            CurrentVisitorsGrid.ItemsSource = dt.DefaultView;
                        if (StatusText != null)
                        {
                            StatusText.Text = $"На территории: {dt.Rows.Count} человек";
                            StatusText.Foreground = Brushes.Green;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (StatusText != null)
                {
                    StatusText.Text = $"Ошибка: {ex.Message}";
                    StatusText.Foreground = Brushes.Red;
                }
            }
        }

        private void ExportExcelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VisitsGrid != null)
                ExportDataGridToExcel(VisitsGrid, "Отчет_посещения");
        }

        private void ExportCurrentExcelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentVisitorsGrid != null)
                ExportDataGridToExcel(CurrentVisitorsGrid, "Список_на_территории");
        }

        private void ExportDataGridToExcel(DataGrid grid, string fileName)
        {
            if (grid.ItemsSource == null || grid.Items.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта!");
                return;
            }

            try
            {
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                saveDialog.FileName = $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (saveDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Отчёт");

                        DataTable dt = ((DataView)grid.ItemsSource).ToTable();

                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = dt.Columns[i].ColumnName;
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                worksheet.Cells[i + 2, j + 1].Value = dt.Rows[i][j]?.ToString();
                            }
                        }

                        worksheet.Cells.AutoFitColumns();
                        package.SaveAs(new FileInfo(saveDialog.FileName));
                    }

                    MessageBox.Show($"Отчёт сохранён: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}");
            }
        }

        private void CreateManualReportBtn_Click(object sender, RoutedEventArgs e)
        {
            CreateHourlyReport();
        }

        private void OpenReportsFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(reportsFolder))
            {
                System.Diagnostics.Process.Start("explorer.exe", reportsFolder);
            }
            else
            {
                MessageBox.Show("Папка с отчётами не найдена!");
            }
        }
    }

    public static class DataViewExtensions
    {
        public static DataTable ToTable(this DataView dataView)
        {
            DataTable dt = dataView.Table.Clone();
            foreach (DataRowView rowView in dataView)
            {
                DataRow newRow = dt.NewRow();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    newRow[i] = rowView[i];
                }
                dt.Rows.Add(newRow);
            }
            return dt;
        }
    }
}