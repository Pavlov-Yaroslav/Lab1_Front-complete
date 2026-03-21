using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using TMPLAB1;
using System.IO;

namespace Lab1_Front
{
    public partial class ComponentsWindow : Window
    {
        public ObservableCollection<Component> Components { get; set; } = new ObservableCollection<Component>();
        private PRD currentPrdFile;

        public ComponentsWindow(PRD prdFile)
        {
            InitializeComponent();
            currentPrdFile = prdFile;
            ComponentsGrid.ItemsSource = Components;

            LoadComponentsFromFile();
        }

        private void LoadComponentsFromFile()
        {
            try
            {
                Components.Clear();

                // Используем новый метод GetAllComponents() из PRD
                var allComponents = currentPrdFile.GetAllComponents();

                foreach (var comp in allComponents)
                {
                    Components.Add(new Component
                    {
                        Name = comp.Name,
                        Type = comp.Type,
                        IsDeleted = false
                    });
                }

                // Дополнительно загружаем удаленные компоненты
                LoadDeletedComponents();

                if (Components.Count == 0)
                {
                    MessageBox.Show("В файле нет компонентов",
                                  "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке компонентов: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDeletedComponents()
        {
            try
            {
                using (FileStream fs = new FileStream(currentPrdFile.CurrentFileName, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Читаем заголовок PRD
                    fs.Seek(2, SeekOrigin.Begin);
                    ushort recordLen = br.ReadUInt16();
                    int firstRecord = br.ReadInt32();

                    int offset = firstRecord;
                    while ((offset != -1) && (offset < fs.Length))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);

                        byte flag = br.ReadByte();
                        int p_FirstComp = br.ReadInt32();
                        int p_Next = br.ReadInt32();
                        byte[] nameBytes = br.ReadBytes(recordLen);
                        string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

                        // Проверяем, есть ли уже этот компонент в списке
                        bool exists = false;
                        foreach (var comp in Components)
                        {
                            if (comp.Name == name)
                            {
                                exists = true;
                                break;
                            }
                        }

                        // Если компонент удален и его нет в списке - добавляем
                        if ((flag == 0xFF) && !exists)
                        {
                            string type = p_FirstComp == -1 ? "Деталь" : "Узел/Изделие";
                            Components.Add(new Component
                            {
                                Name = name,
                                Type = type,
                                IsDeleted = true
                            });
                        }

                        offset = p_Next;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке удаленных компонентов: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            string type = TypeTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
            {
                MessageBox.Show("Введите оба поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем корректность типа
            if ((type != "Изделие") && (type != "Узел") && (type != "Деталь"))
            {
                MessageBox.Show("Тип должен быть: Изделие, Узел или Деталь",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!currentPrdFile.IsOpen)
                {
                    currentPrdFile.Open();
                }

                // Формируем аргумент для метода Input
                string inputArgument = $"({name}, {type})";
                currentPrdFile.Input(inputArgument);

                // Добавляем в таблицу
                Components.Add(new Component 
                { 
                    Name = name, 
                    Type = type, 
                    IsDeleted = false 
                });

                NameTextBox.Text = "";
                TypeTextBox.Text = "";

                MessageBox.Show($"Компонент '{name}' типа '{type}' успешно добавлен",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении компонента: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComponentsGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите компонент для удаления",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Component selected = (Component)ComponentsGrid.SelectedItem;

            if (selected.IsDeleted)
            {
                MessageBox.Show("Компонент уже помечен на удаление",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Удалить компонент '{selected.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (!currentPrdFile.IsOpen)
                    {
                        currentPrdFile.Open();
                    }

                    currentPrdFile.Delete(selected.Name);
                    selected.IsDeleted = true;
                    ComponentsGrid.Items.Refresh();

                    MessageBox.Show($"Компонент '{selected.Name}' помечен на удаление",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении компонента: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (ComponentsGrid.SelectedItem == null)
            {
                MessageBox.Show("Выберите компонент для восстановления",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Component selected = (Component)ComponentsGrid.SelectedItem;

            if (!selected.IsDeleted)
            {
                MessageBox.Show("Компонент не помечен на удаление",
                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!currentPrdFile.IsOpen)
                {
                    currentPrdFile.Open();
                }

                currentPrdFile.Restore(selected.Name);
                selected.IsDeleted = false;
                ComponentsGrid.Items.Refresh();

                MessageBox.Show($"Компонент '{selected.Name}' восстановлен",
                              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при восстановлении компонента: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TruncateButton_Click(object sender, RoutedEventArgs e)
        {
            int deletedCount = Components.Count(c => c.IsDeleted);

            if (deletedCount == 0)
            {
                MessageBox.Show("Нет компонентов, помеченных на удаление",
                              "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Окончательно удалить {deletedCount} помеченных компонент(ов)? Это действие нельзя отменить!",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (!currentPrdFile.IsOpen)
                    {
                        currentPrdFile.Open();
                    }

                    currentPrdFile.Truncate();
                    LoadComponentsFromFile();

                    MessageBox.Show("Файл успешно сжат. Удаленные записи очищены.",
                                  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сжатии файла: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!currentPrdFile.IsOpen)
                {
                    currentPrdFile.Open();
                }

                currentPrdFile.Restore("*");

                foreach (var comp in Components)
                {
                    comp.IsDeleted = false;
                }
                ComponentsGrid.Items.Refresh();

                MessageBox.Show("Все компоненты восстановлены",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при восстановлении компонентов: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class Component
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsDeleted { get; set; } = false;
        public SolidColorBrush TextColor => IsDeleted ? new SolidColorBrush(Colors.Gray) : new SolidColorBrush(Colors.Black);
    }
}