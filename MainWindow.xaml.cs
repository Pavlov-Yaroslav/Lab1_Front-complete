using Microsoft.Win32;
using System.IO;
using System.Windows;
using TMPLAB1;

namespace Lab1_Front
{
    public partial class MainWindow : Window
    {
        private PRD currentPrdFile;
        private PRS currentPrsFile;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PRD files (*.prd)|*.prd|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    currentPrdFile = new PRD(openFileDialog.FileName);
                    currentPrdFile.Open();

                    string directory = Path.GetDirectoryName(openFileDialog.FileName) ?? string.Empty;
                    string prsFileName = Path.ChangeExtension(openFileDialog.FileName, ".prs");

                    currentPrsFile = new PRS(prsFileName);

                    if (File.Exists(prsFileName))
                    {
                        currentPrsFile.Open();
                    }
                    else
                    {
                        currentPrsFile.Create();
                        currentPrsFile.Open();
                    }

                    MessageBox.Show($"Файлы успешно загружены",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenComponents_Click(object sender, RoutedEventArgs e)
        {
            if ((currentPrdFile == null) || !currentPrdFile.IsOpen)
            {
                MessageBox.Show("Сначала откройте файл через меню 'Открыть'",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ComponentsWindow window = new ComponentsWindow(currentPrdFile);
            window.Show();
        }

        private void OpenSpecification_Click(object sender, RoutedEventArgs e)
        {
            if ((currentPrdFile == null) || !currentPrdFile.IsOpen || (currentPrsFile == null) || !currentPrsFile.IsOpen)
            {
                MessageBox.Show("Сначала откройте файл через меню 'Открыть'",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SpecificationWindow window = new SpecificationWindow(currentPrdFile, currentPrsFile);
            window.Show();
        }
    }
}