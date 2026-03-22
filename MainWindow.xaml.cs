using Microsoft.Win32;
using System.IO;
using System.Windows;
using TMPLAB1;

namespace Lab1_Front
{
    /// <summary>
    /// Главное окно приложения.
    /// Отвечает за открытие файлов и переход к окнам компонентов и спецификаций.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Текущий PRD-файл (компоненты).
        /// </summary>
        private PRD currentPrdFile;

        /// <summary>
        /// Текущий PRS-файл (спецификации).
        /// </summary>
        private PRS currentPrsFile;

        /// <summary>
        /// Инициализация главного окна.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик открытия PRD-файла через диалог выбора файла.
        /// При отсутствии PRS-файла создаёт его автоматически.
        /// </summary>
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

                    // Если файл спецификаций существует — открываем, иначе создаём новый
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

        /// <summary>
        /// Открывает окно компонентов.
        /// Требует предварительно открытого PRD-файла.
        /// </summary>
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

        /// <summary>
        /// Открывает окно спецификаций.
        /// Требует открытых PRD и PRS файлов.
        /// </summary>
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