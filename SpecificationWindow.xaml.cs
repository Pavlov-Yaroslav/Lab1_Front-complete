using System.IO;
using System.Windows;
using System.Windows.Controls;
using TMPLAB1;

namespace Lab1_Front
{
    /// <summary>
    /// Окно для отображения и управления спецификациями (связями между компонентами).
    /// Формирует древовидную структуру на основе данных PRD и PRS файлов.
    /// </summary>
    public partial class SpecificationWindow : Window
    {
        /// <summary>
        /// Текущий PRD-файл (компоненты).
        /// </summary>
        private PRD currentPrdFile;

        /// <summary>
        /// Текущий PRS-файл (связи).
        /// </summary>
        private PRS currentPrsFile;

        /// <summary>
        /// Словарь компонентов (offset -> информация о компоненте).
        /// </summary>
        private Dictionary<int, ComponentInfo> components = new Dictionary<int, ComponentInfo>();

        /// <summary>
        /// Словарь связей (offset продукта -> список связей).
        /// </summary>
        private Dictionary<int, List<RelationInfo>> relations = new Dictionary<int, List<RelationInfo>>();

        /// <summary>
        /// Внутреннее представление компонента.
        /// </summary>
        private class ComponentInfo
        {
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
            public int P_FirstComp { get; set; }
        }

        /// <summary>
        /// Внутреннее представление связи между компонентами.
        /// </summary>
        private class RelationInfo
        {
            public int ProductOffset { get; set; }
            public int DetailOffset { get; set; }
            public ushort MultiOccurrence { get; set; }
            public bool IsDeleted { get; set; }
        }

        /// <summary>
        /// Инициализация окна спецификаций.
        /// </summary>
        public SpecificationWindow(PRD prdFile, PRS prsFile)
        {
            InitializeComponent();
            currentPrdFile = prdFile;
            currentPrsFile = prsFile;
            LoadSpecifications();
        }

        /// <summary>
        /// Загружает компоненты и связи, затем строит дерево спецификации.
        /// </summary>
        private void LoadSpecifications()
        {
            try
            {
                SpecTreeView.Items.Clear();
                components.Clear();
                relations.Clear();

                LoadComponents();
                LoadRelations();
                BuildTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке спецификаций: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                SpecTreeView.Items.Add(new TreeViewItem { Header = $"Ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Загружает компоненты из PRD-файла через обход бинарной структуры.
        /// </summary>
        private void LoadComponents()
        {
            using (FileStream fs = new FileStream(currentPrdFile.CurrentFileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Чтение параметров записи и обход связанного списка через p_Next
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

                    components[offset] = new ComponentInfo
                    {
                        Name = name,
                        IsDeleted = flag == 0xFF,
                        P_FirstComp = p_FirstComp
                    };

                    offset = p_Next;
                }
            }
        }

        /// <summary>
        /// Загружает связи между компонентами из PRS-файла.
        /// </summary>
        private void LoadRelations()
        {
            if (!File.Exists(currentPrsFile.CurrentFileName))
                return;

            using (FileStream fs = new FileStream(currentPrsFile.CurrentFileName, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int firstRecord = br.ReadInt32();
                br.ReadInt32(); // пропуск области свободного пространства

                int offset = firstRecord;
                while ((offset != -1) && (offset < fs.Length))
                {
                    fs.Seek(offset, SeekOrigin.Begin);

                    byte flag = br.ReadByte();
                    int p_Product = br.ReadInt32();
                    int p_Detail = br.ReadInt32();
                    ushort multiOccurrence = br.ReadUInt16();
                    int p_Next = br.ReadInt32();

                    // Добавляем только активные (не удалённые) связи
                    if (flag != 0xFF)
                    {
                        if (!relations.ContainsKey(p_Product))
                            relations[p_Product] = new List<RelationInfo>();

                        relations[p_Product].Add(new RelationInfo
                        {
                            ProductOffset = p_Product,
                            DetailOffset = p_Detail,
                            MultiOccurrence = multiOccurrence,
                            IsDeleted = false
                        });
                    }

                    offset = p_Next;
                }
            }
        }

        /// <summary>
        /// Формирует дерево спецификации на основе компонентов и связей.
        /// </summary>
        private void BuildTree()
        {
            HashSet<int> childOffsets = new HashSet<int>();

            // Определение всех дочерних элементов
            foreach (var relList in relations.Values)
            {
                foreach (var rel in relList)
                {
                    childOffsets.Add(rel.DetailOffset);
                }
            }

            var rootOffsets = new List<int>();

            // Поиск корневых элементов (не являются дочерними)
            foreach (var comp in components)
            {
                if (!comp.Value.IsDeleted && (comp.Value.P_FirstComp != -1) && !childOffsets.Contains(comp.Key))
                {
                    rootOffsets.Add(comp.Key);
                }
            }

            // Резервный вариант: если явных корней нет
            if (rootOffsets.Count == 0)
            {
                foreach (var comp in components)
                {
                    if ((!comp.Value.IsDeleted) && (comp.Value.P_FirstComp != -1))
                    {
                        rootOffsets.Add(comp.Key);
                    }
                }
            }

            if (rootOffsets.Count == 0)
            {
                SpecTreeView.Items.Add(new TreeViewItem
                {
                    Header = "Нет спецификаций"
                });
                return;
            }

            foreach (var rootOffset in rootOffsets)
            {
                var rootItem = CreateTreeItem(rootOffset);
                BuildTreeRecursive(rootItem, rootOffset);
                SpecTreeView.Items.Add(rootItem);
            }
        }

        /// <summary>
        /// Создаёт элемент дерева для компонента.
        /// </summary>
        private TreeViewItem CreateTreeItem(int offset)
        {
            if (!components.ContainsKey(offset))
            {
                return new TreeViewItem
                {
                    Header = "Неизвестный компонент"
                };
            }

            var comp = components[offset];
            TreeViewItem item = new TreeViewItem();
            item.Header = comp.Name;
            item.Tag = offset;
            item.ContextMenu = (ContextMenu)FindResource("TreeContextMenu");

            return item;
        }

        /// <summary>
        /// Рекурсивно строит дерево связей компонентов.
        /// </summary>
        private void BuildTreeRecursive(TreeViewItem parentItem, int parentOffset)
        {
            if (!relations.ContainsKey(parentOffset)) return;

            foreach (var rel in relations[parentOffset])
            {
                if (components.ContainsKey(rel.DetailOffset) && !components[rel.DetailOffset].IsDeleted)
                {
                    TreeViewItem childItem = CreateTreeItem(rel.DetailOffset);

                    // Отображение кратности связи
                    if (rel.MultiOccurrence > 1)
                    {
                        childItem.Header = $"{components[rel.DetailOffset].Name} (x{rel.MultiOccurrence})";
                    }

                    BuildTreeRecursive(childItem, rel.DetailOffset);
                    parentItem.Items.Add(childItem);
                }
            }
        }

        /// <summary>
        /// Добавление новой связи между компонентами.
        /// </summary>
        private void MenuItem_Add_Click(object sender, RoutedEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите связь в формате: ОсновнойКомпонент Деталь",
                "Добавить связь",
                "");

            if (string.IsNullOrWhiteSpace(input))
                return;

            try
            {
                if (!currentPrsFile.IsOpen)
                    currentPrsFile.Open();

                string message = currentPrsFile.Input($"({input})");
                MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSpecifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении связи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Удаление выбранной связи.
        /// </summary>
        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SpecTreeView.SelectedItem is not TreeViewItem selectedItem)
            {
                MessageBox.Show("Выберите элемент для удаления", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selectedItem.Parent is TreeViewItem parentItem)
            {
                string product = parentItem.Header.ToString().Split(' ')[0];
                string detail = selectedItem.Header.ToString().Split(' ')[0];
                string message;

                try
                {
                    if (!currentPrsFile.IsOpen)
                    {
                        currentPrsFile.Open();
                    }

                    message = currentPrsFile.Delete($"({product} {detail})");
                    MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSpecifications();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите дочерний элемент для удаления", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Заглушка для редактирования связи.
        /// </summary>
        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция изменения будет реализована позже",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Окончательное удаление помеченных записей (сжатие файла).
        /// </summary>
        private void BtnTruncate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!currentPrsFile.IsOpen) currentPrsFile.Open();

                currentPrsFile.Truncate();
                MessageBox.Show("Файл сжат. Удаленные записи окончательно удалены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSpecifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сжатии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Восстановление удалённых записей (всех или выбранной).
        /// </summary>
        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Восстановить ВСЕ помеченные записи?\nДа — все, Нет — конкретную связь",
                "Восстановление",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (!currentPrsFile.IsOpen) currentPrsFile.Open();

                    currentPrsFile.Restore("*");
                    MessageBox.Show("Все записи восстановлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSpecifications();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (result == MessageBoxResult.No)
            {
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "Введите связь для восстановления в формате: ОсновнойКомпонент Деталь",
                    "Восстановить конкретную запись",
                    "");

                if (string.IsNullOrWhiteSpace(input)) return;

                try
                {
                    if (!currentPrsFile.IsOpen) currentPrsFile.Open();

                    currentPrsFile.Restore($"({input})");
                    MessageBox.Show($"Связь '{input}' восстановлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadSpecifications();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}