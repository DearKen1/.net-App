    using System;
    using System.Data;
    using System.Data.SQLite;
    using System.Windows.Forms;
    using System.Drawing;
    using System.Windows.Forms.DataVisualization.Charting;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;

namespace final_work1
{
    public partial class Form1 : Form
    {
        private SQLiteConnection SQLiteConn;
        private string fileName;
        private SQLiteDataAdapter adapter;
        private DataTable tableData;
        private string currentTableName;
        private Dictionary<string, List<string>> blockPoints;
        private List<string> blockNames;

        public Form1()
        {
            InitializeComponent();

            buttonOpen.Enabled = false;
            button_addRow.Enabled = false;
            button_removeRow.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button9.Enabled = false;
            radioButton1.Enabled = false;
            radioButton2.Enabled = false;
            radioButton3.Enabled = false;
            radioButton4.Enabled = false;
            radioButton5.Enabled = false;
            radioButton6.Enabled = false;
            radioButton7.Enabled = false;
            radioButton8.Enabled = false;
            radioButton9.Enabled = false;
            radioButton10.Enabled = false;
            radioButton11.Enabled = false;
            radioButton12.Enabled = false;
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            comboBox3.Enabled = false;
            checkedListBox1.Enabled = false;
            checkedListBox2.Enabled = false;
            checkedListBox3.Enabled = false;
            textBox_error.Enabled = false;
            textBox_block.Enabled = false;
            textBox_Exp.Enabled = false;

            blockPoints = new Dictionary<string, List<string>>();
            blockNames = new List<string>();
        }

        private void InitializeComboBox2()
        {
            comboBox2.Items.Clear();
            blockNames.Clear();
            blockPoints.Clear(); // Очищаем словарь точек

            // Проверяем, есть ли валидное положительное число в textBox_block
            if (int.TryParse(textBox_block.Text.Trim(), out int numBlocks) && numBlocks > 0)
            {
                for (int i = 0; i < numBlocks; i++)
                {
                    string blockName = ((char)('А' + i)).ToString(); // Генерируем имена: А, Б, В, ...
                    blockNames.Add(blockName);
                    comboBox2.Items.Add(blockName);
                    blockPoints[blockName] = new List<string>(); // Инициализируем пустой список для точек
                }
            }

            // Устанавливаем выбранный элемент, если есть элементы
            if (comboBox2.Items.Count > 0)
            {
                comboBox2.SelectedIndex = 0;
            }
        }

        private void InitializeComboBox3()
        {
            comboBox3.Items.Clear();
            comboBox3.Items.Add("Система");
            foreach (var blockName in blockNames)
            {
                comboBox3.Items.Add(blockName);
            }
            comboBox3.SelectedIndex = 0;
            UpdateCheckedListBox3();
        }

        private void LoadListBox1Items()
        {
            listBox1.Items.Clear();
            if (tableData != null && tableData.Columns.Count > 1)
            {
                // Собираем все точки
                var pointsInBlocks = blockPoints.Values.SelectMany(points => points).ToHashSet();

                // Получаем имена столбцов, начиная со второго (пропускаем "Эпоха")
                var columns = tableData.Columns
                    .Cast<DataColumn>()
                    .Select(col => col.ColumnName)
                    .Skip(1) // Пропускаем первый столбец ("Эпоха")
                    .Where(name => !pointsInBlocks.Contains(name))
                    .ToList();

                foreach (var columnName in columns)
                {
                    listBox1.Items.Add(columnName);
                }
            }
            else
            {
                Console.WriteLine("tableData пуста или содержит менее 2 столбцов.");
            }
        }

        private void LoadCheckedListBoxItems(CheckedListBox checkedListBox)
        {
            checkedListBox.Items.Clear();
            string[] options = { "M+", "M", "M-" };
            checkedListBox.Items.AddRange(options);
        }

        public bool OpenDatabaseFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                openFileDialog.Filter = "SQLite files (*.sqlite;*.db)|*.sqlite;*.db|All files (*.*)|*.*";

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = openFileDialog.FileName;

                try
                {
                    if (SQLiteConn != null && SQLiteConn.State != ConnectionState.Closed)
                        SQLiteConn.Close();

                    SQLiteConn = new SQLiteConnection($"Data Source={fileName};Version=3;");
                    SQLiteConn.Open();

                    using (var cmd = new SQLiteCommand(
                        @"CREATE TABLE IF NOT EXISTS Дополнительные_данные (
                            E TEXT,
                            Блоки TEXT,
                            [Экспон сглаживание] TEXT
                            );", SQLiteConn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла базы данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }
        }

        private void LoadDatabaseTables()
        {
            try
            {
                using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", SQLiteConn))
                using (var tempAdapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable tables = new DataTable();
                    tempAdapter.Fill(tables);

                    comboBox1.Items.Clear();
                    foreach (DataRow row in tables.Rows)
                    {
                        string tableName = row["name"].ToString();
                        if (tableName != "Дополнительные_данные")
                        {
                            comboBox1.Items.Add(tableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке таблиц: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTableData(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                MessageBox.Show("Не выбрана таблица для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string connectionString = $"Data Source={fileName};Version=3;";
                SQLiteConnection conn = new SQLiteConnection(connectionString);
                conn.Open();

                string query = $"SELECT * FROM [{tableName}]";
                adapter = new SQLiteDataAdapter(query, conn);
                SQLiteCommandBuilder commandBuilder = new SQLiteCommandBuilder(adapter);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);


                conn.Close();

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row["Эпоха"] != DBNull.Value)
                    {
                        if (!int.TryParse(row["Эпоха"].ToString(), out int epochValue))
                        {
                            MessageBox.Show($"Некорректное значение в столбце 'Эпоха': {row["Эпоха"]}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                dataGridViewOpenDB.DataSource = dataTable;

                // Настраиваем столбцы
                foreach (DataGridViewColumn column in dataGridViewOpenDB.Columns)
                {
                    if (column.Name == "Эпоха")
                    {
                        column.ValueType = typeof(int);
                        column.ReadOnly = true; // Делаем столбец "Эпоха" только для чтения
                    }
                    else
                    {
                        column.ValueType = typeof(double);
                    }
                }

                tableData = dataTable;
                currentTableName = tableName;

                PopulateDataGridView2();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в LoadTableData: {ex.Message}");
            }
        }

        private void LoadAdditionalData()
        {
            try
            {
                if (SQLiteConn == null || SQLiteConn.State != ConnectionState.Open)
                {
                    MessageBox.Show("Соединение с базой данных не открыто.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string query = "SELECT * FROM Дополнительные_данные LIMIT 1;";
                using (SQLiteCommand command = new SQLiteCommand(query, SQLiteConn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            textBox_error.Text = reader[0]?.ToString() ?? "0"; // Нулевая колонка
                            textBox_block.Text = reader[1]?.ToString() ?? "0"; // Первая колонка
                            textBox_Exp.Text = reader[2]?.ToString() ?? "0"; // Вторая колонка
                        }
                        else
                        {
                            textBox_error.Text = "0";
                            textBox_block.Text = "0";
                            textBox_Exp.Text = "0";
                            blockPoints.Clear();
                        }
                    }
                }
                InitializeComboBox2();
                InitializeComboBox3();
                LoadListBox1Items(); // Обновляем listBox1 после загрузки блоков
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке дополнительных данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в LoadAdditionalData: {ex.Message}");
            }
        }

        private void SaveAdditionalData()
        {
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Файл базы данных не выбран.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Открываем соединение
                if (SQLiteConn == null || SQLiteConn.State != ConnectionState.Open)
                {
                    SQLiteConn = new SQLiteConnection($"Data Source={fileName};Version=3;");
                    SQLiteConn.Open();
                    Console.WriteLine("Соединение открыто в SaveAdditionalData.");
                }

                string additionalTable = "Дополнительные_данные";

                // Очищаем таблицу
                using (var cmd = new SQLiteCommand($"DELETE FROM {additionalTable};", SQLiteConn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Вставка по индексам столбцов
                using (var cmd = new SQLiteCommand(
                    $"INSERT INTO {additionalTable} VALUES (@col0, @col1, @col2);", SQLiteConn))
                {
                    cmd.Parameters.AddWithValue("@col0", textBox_error.Text.Replace(',', '.'));
                    cmd.Parameters.AddWithValue("@col1", textBox_block.Text);
                    cmd.Parameters.AddWithValue("@col2", textBox_Exp.Text.Replace(',', '.'));
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"Сохранено в БД: Погрешность = '{textBox_error.Text}', Блоки = '{textBox_block}', Экспоненциональное сглаживание = '{textBox_Exp.Text}'");

                PopulateDataGridView2();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении дополнительных данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в SaveAdditionalData: {ex.Message}");
            }
            finally
            {
                if (SQLiteConn != null && SQLiteConn.State == ConnectionState.Open)
                {
                    SQLiteConn.Close();
                    Console.WriteLine("Соединение закрыто в SaveAdditionalData.");
                }
            }
        }

        private void UpdateChart()
        {
            chart1.Series.Clear();

            List<double> allXValues = new List<double>();
            List<double> allYValues = new List<double>();

            // Получаем список эпох из tableData
            List<string> epochs = tableData.AsEnumerable().Select(row => row["Эпоха"].ToString()).ToList();

            foreach (var item in checkedListBox1.CheckedItems)
            {
                string seriesName = item.ToString();
                Series series = new Series(seriesName)
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6
                };

                string xColumn = seriesName == "M+" ? "M+" : seriesName == "M-" ? "M-" : "M";
                string yColumn = seriesName == "M+" ? "a+" : seriesName == "M-" ? "a-" : "a";

                var fullValues = dataGridView2.Tag as dynamic;
                List<double> xValuesFull = xColumn == "M+" ? fullValues.MPlusFull : xColumn == "M-" ? fullValues.MMinusFull : fullValues.MFull;
                List<double> yValuesFull = yColumn == "a+" ? fullValues.APlusFull : yColumn == "a-" ? fullValues.AMinusFull : fullValues.AFull;

                for (int i = 0; i < xValuesFull.Count; i++)
                {
                    if (!double.IsNaN(xValuesFull[i]) && !double.IsNaN(yValuesFull[i]))
                    {
                        series.Points.AddXY(xValuesFull[i], yValuesFull[i]);
                        // Устанавливаем метку в зависимости от эпохи или "Прогноз"
                        string label = (i == xValuesFull.Count - 1) ? "Прогноз" : epochs[i];
                        series.Points[series.Points.Count - 1].Label = label;
                        allXValues.Add(xValuesFull[i]);
                        allYValues.Add(yValuesFull[i]);
                    }
                }

                if (series.Points.Count > 0)
                {
                    chart1.Series.Add(series);
                    Console.WriteLine($"Добавлена серия: {seriesName}, точек: {series.Points.Count}");
                }
            }

            chart1.ChartAreas[0].AxisX.Title = "Модуль (M)";
            chart1.ChartAreas[0].AxisY.Title = "Угол (a)";
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "F4";
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "F6";

            if (allXValues.Any() && allYValues.Any())
            {
                double xMin = allXValues.Min();
                double xMax = allXValues.Max();
                double xMargin = (xMax - xMin) * 0.1;
                if (xMargin == 0) xMargin = xMin * 0.1 != 0 ? xMin * 0.1 : 1.0;

                double yMin = allYValues.Min();
                double yMax = allYValues.Max();
                double yMargin = (yMax - yMin) * 0.1;
                if (yMargin == 0) yMargin = yMin * 0.1 != 0 ? yMin * 0.1 : 1.0;

                chart1.ChartAreas[0].AxisX.Minimum = xMin - xMargin;
                chart1.ChartAreas[0].AxisX.Maximum = xMax + xMargin;
                chart1.ChartAreas[0].AxisY.Minimum = yMin - yMargin;
                chart1.ChartAreas[0].AxisY.Maximum = yMax + yMargin;
            }
            else
            {
                chart1.ChartAreas[0].AxisX.Minimum = double.NaN;
                chart1.ChartAreas[0].AxisX.Maximum = double.NaN;
                chart1.ChartAreas[0].AxisY.Minimum = double.NaN;
                chart1.ChartAreas[0].AxisY.Maximum = double.NaN;
            }
        }

        private void UpdateChart2()
        {
            chart2.Series.Clear();

            if (tableData == null || tableData.Rows.Count == 0)
            {
                Console.WriteLine("UpdateChart2: tableData пуст или не инициализирован.");
                return;
            }

            var tag = dataGridView2.Tag;
            if (tag == null)
            {
                Console.WriteLine("UpdateChart2: dataGridView2.Tag равен null.");
                return;
            }

            var fullValues = new
            {
                MPlusFull = (tag as dynamic).MPlusFull as List<double> ?? new List<double>(),
                MFull = (tag as dynamic).MFull as List<double> ?? new List<double>(),
                MMinusFull = (tag as dynamic).MMinusFull as List<double> ?? new List<double>(),
                APlusFull = (tag as dynamic).APlusFull as List<double> ?? new List<double>(),
                AFull = (tag as dynamic).AFull as List<double> ?? new List<double>(),
                AMinusFull = (tag as dynamic).AMinusFull as List<double> ?? new List<double>(),
                SmoothedMPlus = (tag as dynamic).SmoothedMPlus as List<List<double>> ?? new List<List<double>>(),
                SmoothedM = (tag as dynamic).SmoothedM as List<List<double>> ?? new List<List<double>>(),
                SmoothedMMinus = (tag as dynamic).SmoothedMMinus as List<List<double>> ?? new List<List<double>>(),
                SmoothedAPlus = (tag as dynamic).SmoothedAPlus as List<List<double>> ?? new List<List<double>>(),
                SmoothedA = (tag as dynamic).SmoothedA as List<List<double>> ?? new List<List<double>>(),
                SmoothedAMinus = (tag as dynamic).SmoothedAMinus as List<List<double>> ?? new List<List<double>>(),
                FixedAlphas = (tag as dynamic).FixedAlphas as List<double> ?? new List<double>()
            };

            Console.WriteLine($"UpdateChart2: MPlusFull.Count={fullValues.MPlusFull.Count}, SmoothedMPlus.Count={fullValues.SmoothedMPlus.Count}, tableName={currentTableName}");

            string yColumn = "";
            bool isAngle = false;
            if (radioButton1.Checked) { yColumn = "M-"; }
            else if (radioButton2.Checked) { yColumn = "M"; }
            else if (radioButton3.Checked) { yColumn = "M+"; }
            else if (radioButton4.Checked) { yColumn = "a-"; isAngle = true; }
            else if (radioButton5.Checked) { yColumn = "a"; isAngle = true; }
            else if (radioButton6.Checked) { yColumn = "a+"; isAngle = true; }
            else
            {
                Console.WriteLine("UpdateChart2: Не выбрана радиокнопка для построения графика.");
                return;
            }

            int startIndex = isAngle ? 1 : 0;
            int dataCount = fullValues.MFull.Count - startIndex;
            if (dataCount <= 0)
            {
                Console.WriteLine("UpdateChart2: Недостаточно данных для построения графика (dataCount <= 0).");
                return;
            }

            List<double> yValues;
            List<List<double>> smoothedValues;
            if (yColumn == "M+") { yValues = fullValues.MPlusFull; smoothedValues = fullValues.SmoothedMPlus; }
            else if (yColumn == "M") { yValues = fullValues.MFull; smoothedValues = fullValues.SmoothedM; }
            else if (yColumn == "M-") { yValues = fullValues.MMinusFull; smoothedValues = fullValues.SmoothedMMinus; }
            else if (yColumn == "a+") { yValues = fullValues.APlusFull; smoothedValues = fullValues.SmoothedAPlus; }
            else if (yColumn == "a") { yValues = fullValues.AFull; smoothedValues = fullValues.SmoothedA; }
            else if (yColumn == "a-") { yValues = fullValues.AMinusFull; smoothedValues = fullValues.SmoothedAMinus; }
            else { yValues = new List<double>(); smoothedValues = new List<List<double>>(); }

            // Обрезаем yValues с учетом startIndex
            yValues = yValues.Skip(startIndex).Take(dataCount).ToList();

            // Получаем список эпох
            List<string> epochs = tableData.AsEnumerable().Select(row => row["Эпоха"].ToString()).Skip(startIndex).Take(dataCount).ToList();

            Console.WriteLine($"UpdateChart2: yValues.Count={yValues.Count}, epochs.Count={epochs.Count}");

            // Базовая серия
            Series baseSeries = new Series(yColumn)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 6,
                LegendText = yColumn
            };

            for (int i = 0; i < yValues.Count && i < epochs.Count; i++)
            {
                if (!double.IsNaN(yValues[i]))
                {
                    baseSeries.Points.AddXY(i, yValues[i]);
                    baseSeries.Points[baseSeries.Points.Count - 1].Label = epochs[i];
                }
            }

            if (baseSeries.Points.Count > 0)
            {
                chart2.Series.Add(baseSeries);
                Console.WriteLine($"UpdateChart2: Добавлена базовая серия: {baseSeries.LegendText}, точек: {baseSeries.Points.Count}");
            }

            List<double> allYValues = new List<double>(yValues.Where(x => !double.IsNaN(x)));

            // Сглаженные серии
            if (smoothedValues != null && smoothedValues.Count > 0)
            {
                for (int aIndex = 0; aIndex < fullValues.FixedAlphas.Count && aIndex < smoothedValues.Count; aIndex++)
                {
                    double a = fullValues.FixedAlphas[aIndex];
                    List<double> smoothed = smoothedValues[aIndex];
                    if (smoothed == null || smoothed.Count == 0)
                    {
                        Console.WriteLine($"UpdateChart2: Сглаженная серия для alpha={a} пуста.");
                        continue;
                    }

                    allYValues.AddRange(smoothed.Where(x => !double.IsNaN(x)));

                    string seriesName = $"A={a}";
                    Series smoothedSeries = new Series(seriesName)
                    {
                        ChartType = SeriesChartType.Line,
                        BorderWidth = 2,
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerSize = 6,
                        LegendText = seriesName
                    };

                    for (int i = 0; i < smoothed.Count && i < epochs.Count + 1; i++)
                    {
                        if (!double.IsNaN(smoothed[i]))
                        {
                            smoothedSeries.Points.AddXY(i, smoothed[i]);
                            smoothedSeries.Points[smoothedSeries.Points.Count - 1].Label = i == smoothed.Count - 1 ? "Прогноз" : epochs[i];
                        }
                    }

                    if (smoothedSeries.Points.Count > 0)
                    {
                        chart2.Series.Add(smoothedSeries);
                        Console.WriteLine($"UpdateChart2: Добавлена серия: {smoothedSeries.LegendText}, точек: {smoothedSeries.Points.Count}");
                    }
                }
            }

            if (allYValues.Any())
            {
                double yMin = allYValues.Min();
                double yMax = allYValues.Max();
                double margin = (yMax - yMin) * 0.1;
                if (margin == 0)
                    margin = yMin * 0.1 != 0 ? yMin * 0.1 : 1.0;

                chart2.ChartAreas[0].AxisY.Minimum = yMin - margin;
                chart2.ChartAreas[0].AxisY.Maximum = yMax + margin;
                chart2.ChartAreas[0].AxisY.LabelStyle.Format = isAngle ? "F6" : "F4";
            }
            else
            {
                chart2.ChartAreas[0].AxisY.Minimum = double.NaN;
                chart2.ChartAreas[0].AxisY.Maximum = double.NaN;
            }

            chart2.ChartAreas[0].AxisX.Title = "Эпоха";
            chart2.ChartAreas[0].AxisY.Title = yColumn;
        }

        private void UpdateChart3()
        {
            chart3.Series.Clear();

            if (!checkedListBox2.CheckedItems.Cast<object>().Any())
            {
                Console.WriteLine("Нет выбранных элементов в checkedListBox2.");
                return;
            }

            var fullValues = dataGridView1.Tag as dynamic;
            if (fullValues == null)
            {
                Console.WriteLine("dataGridView1.Tag пуст.");
                return;
            }

            // Получаем список эпох из tableData
            List<string> epochs = tableData.AsEnumerable().Select(row => row["Эпоха"].ToString()).ToList();

            List<double> allXValues = new List<double>();
            List<double> allYValues = new List<double>();

            foreach (var item in checkedListBox2.CheckedItems)
            {
                string seriesName = item.ToString();
                Console.WriteLine($"Обрабатывается серия: {seriesName}");

                Series series = new Series(seriesName)
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6
                };

                string xColumn = seriesName == "M+" ? "M+" : seriesName == "M-" ? "M-" : "M";
                string yColumn = seriesName == "M+" ? "a+" : seriesName == "M-" ? "a-" : "a";

                List<double> xValuesFull = xColumn == "M+" ? fullValues.MPlusFull : xColumn == "M-" ? fullValues.MMinusFull : fullValues.MFull;
                List<double> yValuesFull = yColumn == "a+" ? fullValues.APlusFull : yColumn == "a-" ? fullValues.AMinusFull : fullValues.AFull;

                int pointCount = 0;
                for (int i = 0; i < xValuesFull.Count; i++)
                {
                    if (!double.IsNaN(xValuesFull[i]) && !double.IsNaN(yValuesFull[i]))
                    {
                        series.Points.AddXY(xValuesFull[i], yValuesFull[i]);
                        string label = (i == xValuesFull.Count - 1) ? "Прогноз" : epochs[i];
                        series.Points[series.Points.Count - 1].Label = label;
                        allXValues.Add(xValuesFull[i]);
                        allYValues.Add(yValuesFull[i]);
                        pointCount++;
                    }
                }

                Console.WriteLine($"Добавлено точек для серии {seriesName}: {pointCount}");
                if (pointCount > 0)
                {
                    chart3.Series.Add(series);
                }
            }

            chart3.ChartAreas[0].AxisX.Title = "Модуль (M)";
            chart3.ChartAreas[0].AxisY.Title = "Угол (a)";
            chart3.ChartAreas[0].AxisX.LabelStyle.Format = "F4";
            chart3.ChartAreas[0].AxisY.LabelStyle.Format = "F6";

            if (allXValues.Any() && allYValues.Any())
            {
                double xMin = allXValues.Min();
                double xMax = allXValues.Max();
                double xMargin = (xMax - xMin) * 0.1;
                if (xMargin == 0) xMargin = xMin * 0.1 != 0 ? xMin * 0.1 : 1.0;

                double yMin = allYValues.Min();
                double yMax = allYValues.Max();
                double yMargin = (yMax - yMin) * 0.1;
                if (yMargin == 0) yMargin = yMin * 0.1 != 0 ? yMin * 0.1 : 1.0;

                chart3.ChartAreas[0].AxisX.Minimum = xMin - xMargin;
                chart3.ChartAreas[0].AxisX.Maximum = xMax + xMargin;
                chart3.ChartAreas[0].AxisY.Minimum = yMin - yMargin;
                chart3.ChartAreas[0].AxisY.Maximum = yMax + yMargin;
            }
            else
            {
                chart3.ChartAreas[0].AxisX.Minimum = double.NaN;
                chart3.ChartAreas[0].AxisX.Maximum = double.NaN;
                chart3.ChartAreas[0].AxisY.Minimum = double.NaN;
                chart3.ChartAreas[0].AxisY.Maximum = double.NaN;
            }
        }

        private void UpdateChart4()
        {
            chart4.Series.Clear();

            var tag = dataGridView1.Tag;
            if (tag == null)
            {
                Console.WriteLine("UpdateChart4: Данные в dataGridView1.Tag отсутствуют.");
                return;
            }

            if (tableData == null || tableData.Rows.Count == 0)
            {
                Console.WriteLine("UpdateChart4: tableData пуст или не инициализирован.");
                return;
            }

            var fullValues = new
            {
                MPlusFull = (tag as dynamic).MPlusFull as List<double> ?? new List<double>(),
                MFull = (tag as dynamic).MFull as List<double> ?? new List<double>(),
                MMinusFull = (tag as dynamic).MMinusFull as List<double> ?? new List<double>(),
                APlusFull = (tag as dynamic).APlusFull as List<double> ?? new List<double>(),
                AFull = (tag as dynamic).AFull as List<double> ?? new List<double>(),
                AMinusFull = (tag as dynamic).AMinusFull as List<double> ?? new List<double>()
            };

            // Проверяем наличие данных
            if (!fullValues.MFull.Any() || !fullValues.MPlusFull.Any() || !fullValues.MMinusFull.Any() ||
                !fullValues.APlusFull.Any() || !fullValues.AFull.Any() || !fullValues.AMinusFull.Any())
            {
                Console.WriteLine("UpdateChart4: Один или несколько списков данных пусты.");
                return;
            }

            string yColumn = "";
            bool isAngle = false;
            if (radioButton7.Checked) { yColumn = "M-"; }
            else if (radioButton8.Checked) { yColumn = "M"; }
            else if (radioButton9.Checked) { yColumn = "M+"; }
            else if (radioButton10.Checked) { yColumn = "a-"; isAngle = true; }
            else if (radioButton11.Checked) { yColumn = "a"; isAngle = true; }
            else if (radioButton12.Checked) { yColumn = "a+"; isAngle = true; }
            else
            {
                Console.WriteLine("UpdateChart4: Не выбрана радиокнопка для построения графика.");
                return;
            }

            // Получаем список эпох
            List<string> epochs = tableData.AsEnumerable().Select(row => row["Эпоха"].ToString()).ToList();

            // Определяем начальный индекс и количество данных
            int startIndex = isAngle ? 1 : 0;

            int dataCount = Math.Min(fullValues.MFull.Count, epochs.Count) - startIndex;
            if (dataCount <= 0)
            {
                Console.WriteLine($"UpdateChart4: Недостаточно данных для построения графика (dataCount={dataCount}, MFull.Count={fullValues.MFull.Count}, epochs.Count={epochs.Count}).");
                return;
            }

            List<double> yValues;
            if (yColumn == "M+") { yValues = fullValues.MPlusFull; }
            else if (yColumn == "M") { yValues = fullValues.MFull; }
            else if (yColumn == "M-") { yValues = fullValues.MMinusFull; }
            else if (yColumn == "a+") { yValues = fullValues.APlusFull; }
            else if (yColumn == "a") { yValues = fullValues.AFull; }
            else if (yColumn == "a-") { yValues = fullValues.AMinusFull; }
            else
            {
                Console.WriteLine("UpdateChart4: Неверное значение yColumn.");
                return;
            }

            // Проверяем, что yValues содержит достаточно данных
            if (yValues.Count < startIndex + dataCount)
            {
                Console.WriteLine($"UpdateChart4: yValues.Count ({yValues.Count}) меньше необходимого ({startIndex + dataCount}).");
                return;
            }

            // Обрезаем yValues и epochs
            yValues = yValues.Skip(startIndex).Take(dataCount).ToList();
            List<string> dataEpochs = epochs.Skip(startIndex).Take(dataCount).ToList();

            Console.WriteLine($"UpdateChart4: yValues.Count={yValues.Count}, dataEpochs.Count={dataEpochs.Count}, isAngle={isAngle}, tableName={currentTableName}");

            // Базовая серия
            Series baseSeries = new Series(yColumn)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 6,
                LegendText = yColumn
            };

            for (int i = 0; i < yValues.Count && i < dataEpochs.Count; i++)
            {
                if (!double.IsNaN(yValues[i]))
                {
                    baseSeries.Points.AddXY(i, yValues[i]);
                    baseSeries.Points[baseSeries.Points.Count - 1].Label = dataEpochs[i];
                }
            }

            if (baseSeries.Points.Count > 0)
            {
                chart4.Series.Add(baseSeries);
                Console.WriteLine($"UpdateChart4: Добавлена базовая серия: {baseSeries.LegendText}, точек: {baseSeries.Points.Count}");
            }

            // Сглаженные серии
            List<double> fixedAlphas = new List<double> { 0.1, 0.4, 0.7, 0.9 };
            List<double> allYValues = new List<double>(yValues.Where(x => !double.IsNaN(x)));

            foreach (double a in fixedAlphas)
            {
                List<double> smoothed = CalculateExponentialSmoothing(yValues, a);
                if (smoothed.Count == 0)
                {
                    Console.WriteLine($"UpdateChart4: Сглаженная серия для alpha={a} пуста.");
                    continue;
                }

                // Добавляем прогнозную точку
                double avgSmoothed = smoothed.Where(x => !double.IsNaN(x)).Any() ? smoothed.Where(x => !double.IsNaN(x)).Average() : 0;
                double lastSmoothed = smoothed.Last();
                double forecastPoint = a * avgSmoothed + (1 - a) * lastSmoothed;
                smoothed.Add(forecastPoint);

                allYValues.AddRange(smoothed.Where(x => !double.IsNaN(x)));

                string seriesName = $"A={a}";
                Series smoothedSeries = new Series(seriesName)
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6,
                    LegendText = seriesName
                };

                for (int i = 0; i < smoothed.Count && i < dataEpochs.Count + 1; i++)
                {
                    if (!double.IsNaN(smoothed[i]))
                    {
                        smoothedSeries.Points.AddXY(i, smoothed[i]);
                        smoothedSeries.Points[smoothedSeries.Points.Count - 1].Label = i == smoothed.Count - 1 ? "Прогноз" : dataEpochs[i];
                    }
                }

                if (smoothedSeries.Points.Count > 0)
                {
                    chart4.Series.Add(smoothedSeries);
                    Console.WriteLine($"UpdateChart4: Добавлена серия: {smoothedSeries.LegendText}, точек: {smoothedSeries.Points.Count}");
                }
            }

            // Настраиваем оси
            if (allYValues.Any())
            {
                double yMin = allYValues.Min();
                double yMax = allYValues.Max();
                double margin = (yMax - yMin) * 0.1;
                if (margin == 0)
                    margin = yMin * 0.1 != 0 ? yMin * 0.1 : 1.0;

                chart4.ChartAreas[0].AxisY.Minimum = yMin - margin;
                chart4.ChartAreas[0].AxisY.Maximum = yMax + margin;
                chart4.ChartAreas[0].AxisY.LabelStyle.Format = isAngle ? "F6" : "F4";
            }
            else
            {
                chart4.ChartAreas[0].AxisY.Minimum = double.NaN;
                chart4.ChartAreas[0].AxisY.Maximum = double.NaN;
            }

            chart4.ChartAreas[0].AxisX.Title = "Эпоха";
            chart4.ChartAreas[0].AxisY.Title = yColumn;
        }

        private void UpdateChart5()
        {
            chart5.Series.Clear();

            string selectedMode = comboBox3.SelectedItem?.ToString();
            List<string> columnNames = tableData.Columns.Cast<DataColumn>()
                .Where(col => col.ColumnName != "Эпоха")
                .Select(col => col.ColumnName)
                .ToList();

            // Получаем список эпох из tableData
            List<string> epochs = tableData.AsEnumerable().Select(row => row["Эпоха"].ToString()).ToList();

            List<double> allYValues = new List<double>();

            foreach (var item in checkedListBox3.CheckedItems)
            {
                string columnName;
                if (selectedMode == "Система")
                {
                    if (int.TryParse(item.ToString(), out int index) && index > 0 && index <= columnNames.Count)
                    {
                        columnName = columnNames[index - 1];
                    }
                    else
                    {
                        Console.WriteLine($"Неверный индекс: {item}");
                        continue;
                    }
                }
                else
                {
                    columnName = item.ToString();
                }

                Series series = new Series(columnName)
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 6,
                    LegendText = columnName
                };

                List<double> yValues = new List<double>();
                int pointCount = 0;
                foreach (DataRow row in tableData.Rows)
                {
                    if (row[columnName] != DBNull.Value && double.TryParse(row[columnName].ToString(), out double y))
                    {
                        yValues.Add(y);
                        pointCount++;
                    }
                    else
                    {
                        yValues.Add(double.NaN);
                    }
                }

                for (int i = 0; i < yValues.Count; i++)
                {
                    if (!double.IsNaN(yValues[i]))
                    {
                        series.Points.AddXY(i, yValues[i]);
                        series.Points[series.Points.Count - 1].Label = epochs[i];
                        allYValues.Add(yValues[i]);
                    }
                }

                Console.WriteLine($"Добавлено точек для серии {columnName}: {pointCount}");
                if (pointCount > 0)
                {
                    chart5.Series.Add(series);
                }
            }

            if (allYValues.Any())
            {
                double yMin = allYValues.Min();
                double yMax = allYValues.Max();
                double margin = (yMax - yMin) * 0.1;
                if (margin == 0)
                    margin = yMin * 0.1 != 0 ? yMin * 0.1 : 1.0;

                chart5.ChartAreas[0].AxisY.Minimum = yMin - margin;
                chart5.ChartAreas[0].AxisY.Maximum = yMax + margin;
                chart5.ChartAreas[0].AxisY.LabelStyle.Format = "F4";
            }
            else
            {
                chart5.ChartAreas[0].AxisY.Minimum = double.NaN;
                chart5.ChartAreas[0].AxisY.Maximum = double.NaN;
            }

            chart5.ChartAreas[0].AxisX.Title = "Эпоха";
            chart5.ChartAreas[0].AxisY.Title = "M";
        }

        private void UpdateCheckedListBox3()
        {
            checkedListBox3.Items.Clear();
            string selectedMode = comboBox3.SelectedItem?.ToString();

            if (selectedMode == "Система")
            {
                if (tableData != null && tableData.Columns.Count > 1)
                {
                    int index = 1;
                    foreach (DataColumn column in tableData.Columns)
                    {
                        if (column.ColumnName != "Эпоха")
                        {
                            checkedListBox3.Items.Add(index.ToString());
                            index++;
                        }
                    }
                }
            }
            else if (blockPoints.ContainsKey(selectedMode))
            {
                foreach (var point in blockPoints[selectedMode])
                {
                    checkedListBox3.Items.Add(point);
                }
            }

            Console.WriteLine($"checkedListBox3 обновлен для режима {selectedMode}, элементов: {checkedListBox3.Items.Count}");
        }

        private void AddCalculatedRow()
        {
            if (tableData == null || tableData.Rows.Count < 1)
            {
                MessageBox.Show("Недостаточно данных для расчета.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Random random = new Random();
                DataRow newRow = tableData.NewRow();

                // Генерируем уникальное значение для столбца "Эпоха"
                int maxEpoch = tableData.AsEnumerable()
                    .Where(row => row["Эпоха"] != DBNull.Value)
                    .Select(row => Convert.ToInt32(row["Эпоха"]))
                    .DefaultIfEmpty(0)
                    .Max();
                int newEpoch = maxEpoch + 1;
                newRow["Эпоха"] = newEpoch;

                // Заполняем остальные столбцы
                for (int colIndex = 1; colIndex < tableData.Columns.Count; colIndex++) // Пропускаем столбец "Эпоха"
                {
                    DataColumn column = tableData.Columns[colIndex];
                    if (tableData.Rows[0][column] is DBNull)
                    {
                        newRow[column] = DBNull.Value;
                        continue;
                    }

                    double maxDifference = 0;
                    for (int i = 1; i < tableData.Rows.Count; i++)
                    {
                        if (tableData.Rows[i][column] is DBNull || tableData.Rows[i - 1][column] is DBNull)
                            continue;

                        double prevValue = Convert.ToDouble(tableData.Rows[i - 1][column]);
                        double currentValue = Convert.ToDouble(tableData.Rows[i][column]);
                        double difference = Math.Abs(currentValue - prevValue);

                        if (difference > maxDifference)
                        {
                            maxDifference = difference;
                        }
                    }

                    double randomChange = (random.NextDouble() * 2 - 1) * maxDifference;
                    double lastValueColumn = Convert.ToDouble(tableData.Rows[tableData.Rows.Count - 1][column]);
                    double newValue = Math.Max(0, lastValueColumn + randomChange);
                    newRow[column] = Math.Round(newValue, 4);
                }

                tableData.Rows.Add(newRow);
                Console.WriteLine($"Добавлена новая строка в tableData с Эпоха={newEpoch}.");

                // Открываем соединение
                if (SQLiteConn == null || SQLiteConn.State != ConnectionState.Open)
                {
                    SQLiteConn = new SQLiteConnection($"Data Source={fileName};Version=3;");
                    SQLiteConn.Open();
                    Console.WriteLine("Соединение открыто в AddCalculatedRow.");
                }

                // Формируем SQL-запрос для вставки новой строки
                string columns = string.Join(", ", tableData.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]"));
                string values = string.Join(", ", tableData.Columns.Cast<DataColumn>().Select(c => $"@p{c.Ordinal}"));
                string sql = $"INSERT INTO [{currentTableName}] ({columns}) VALUES ({values})";

                using (SQLiteCommand command = new SQLiteCommand(sql, SQLiteConn))
                {
                    for (int i = 0; i < tableData.Columns.Count; i++)
                    {
                        object value = newRow[i];
                        command.Parameters.AddWithValue($"@p{i}", value ?? DBNull.Value);
                    }
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Вставлено строк: {rowsAffected}, SQL: {sql}");
                }

                tableData.AcceptChanges();
                Console.WriteLine($"Новая строка сохранена в базе данных: Эпоха={newEpoch}");

                LoadTableData(currentTableName);
                PopulateDataGridView2();
                UpdateCheckedListBox3();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении строки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в AddCalculatedRow: {ex.Message}\nСтек вызовов: {ex.StackTrace}");
            }
            finally
            {
                // Закрываем соединение
                if (SQLiteConn != null && SQLiteConn.State == ConnectionState.Open)
                {
                    SQLiteConn.Close();
                    Console.WriteLine("Соединение закрыто в AddCalculatedRow.");
                }
            }
        }

        private void SaveCellChangeToDatabase(int rowIndex, int columnIndex, object newValue)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Файл базы данных не выбран.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Ошибка: fileName не установлен.");
                return;
            }

            try
            {
                // Открываем соединение, если оно закрыто
                if (SQLiteConn == null || SQLiteConn.State != ConnectionState.Open)
                {
                    SQLiteConn = new SQLiteConnection($"Data Source={fileName};Version=3;");
                    SQLiteConn.Open();
                    Console.WriteLine("Соединение открыто в SaveCellChangeToDatabase.");
                }

                if (tableData == null || string.IsNullOrEmpty(currentTableName))
                {
                    MessageBox.Show("Таблица не загружена.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("Ошибка: tableData или currentTableName не инициализированы.");
                    return;
                }

                string columnName = tableData.Columns[columnIndex].ColumnName;
                string epochValue = tableData.Rows[rowIndex]["Эпоха"].ToString();
                object valueToSave = newValue;


                if (valueToSave != DBNull.Value && !double.TryParse(valueToSave?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    MessageBox.Show($"Значение в столбце '{columnName}' должно быть числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Console.WriteLine($"SaveCellChangeToDatabase: Входное значение: {newValue}, Столбец: {columnName}, Эпоха: {epochValue}");

                string sql = $"UPDATE {currentTableName} SET [{columnName}] = @Value WHERE Эпоха = @Epoch";
                using (SQLiteCommand command = new SQLiteCommand(sql, SQLiteConn))
                {
                    command.Parameters.AddWithValue("@Value", valueToSave ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Epoch", epochValue);

                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Обновлено строк: {rowsAffected}, SQL: {sql}, Value: {valueToSave}, Epoch: {epochValue}");
                }

                tableData.Rows[rowIndex][columnIndex] = valueToSave;
                tableData.AcceptChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменения ячейки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в SaveCellChangeToDatabase: {ex.Message}");
            }
            finally
            {
                // Закрываем соединение
                if (SQLiteConn != null && SQLiteConn.State == ConnectionState.Open)
                {
                    SQLiteConn.Close();
                    Console.WriteLine("Соединение закрыто в SaveCellChangeToDatabase.");
                }
            }
        }

        private void SaveChanges()
        {
            try
            {
                if (adapter != null && tableData != null && !string.IsNullOrEmpty(currentTableName))
                {
                    DataTable changes = tableData.GetChanges();
                    if (changes != null && changes.Rows.Count > 0)
                    {
                        Console.WriteLine($"Обнаружено {changes.Rows.Count} измененных строк для сохранения.");

                        // Убедимся, что соединение открыто
                        if (adapter.SelectCommand.Connection.State != ConnectionState.Open)
                        {
                            adapter.SelectCommand.Connection.Open();
                        }

                        // Явно создаем команды
                        SQLiteCommandBuilder commandBuilder = new SQLiteCommandBuilder(adapter);
                        adapter.UpdateCommand = commandBuilder.GetUpdateCommand();
                        adapter.InsertCommand = commandBuilder.GetInsertCommand();
                        adapter.DeleteCommand = commandBuilder.GetDeleteCommand();

                        adapter.Update(tableData);
                        tableData.AcceptChanges();
                        Console.WriteLine("Изменения успешно сохранены в базе данных.");
                    }
                    else
                    {
                        Console.WriteLine("Нет изменений для сохранения.");
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка: adapter, tableData или currentTableName не инициализированы.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
            finally
            {
                if (adapter != null && adapter.SelectCommand != null && adapter.SelectCommand.Connection != null && adapter.SelectCommand.Connection.State == ConnectionState.Open)
                {
                    adapter.SelectCommand.Connection.Close();
                }
            }
        }

        public void DeleteCycle()
        {
            if (dataGridViewOpenDB.CurrentCell == null || dataGridViewOpenDB.RowCount == 0)
            {
                MessageBox.Show("Не выбрана ячейка для удаления или таблица пуста.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Проверяем и открываем соединение, если оно закрыто
                if (SQLiteConn == null || SQLiteConn.State != ConnectionState.Open)
                {
                    SQLiteConn = new SQLiteConnection($"Data Source={fileName};Version=3;");
                    SQLiteConn.Open();
                    Console.WriteLine("Соединение открыто в DeleteCycle.");
                }

                int rowIndex = dataGridViewOpenDB.CurrentCell.RowIndex;
                string epochValue = dataGridViewOpenDB.Rows[rowIndex].Cells[0].Value.ToString();

                using (SQLiteCommand command = new SQLiteCommand($"DELETE FROM {currentTableName} WHERE Эпоха = @Epoch", SQLiteConn))
                {
                    command.Parameters.AddWithValue("@Epoch", epochValue);
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Удалено строк: {rowsAffected}, Эпоха: {epochValue}");
                }

                tableData.Rows.RemoveAt(rowIndex);
                SaveChanges();
                LoadTableData(currentTableName);

                PopulateDataGridView2();
                UpdateCheckedListBox3();

                // Очищаем dataGridView1.Tag перед обновлением
                dataGridView1.Tag = null;
                Console.WriteLine($"DeleteCycle: Очищен dataGridView1.Tag, tableData.Rows.Count={tableData.Rows.Count}");

                // Обновляем dataGridView1 и dataGridView3, если блоки инициализированы
                if (comboBox2.SelectedItem != null && blockPoints.ContainsKey(comboBox2.SelectedItem.ToString()) && blockPoints[comboBox2.SelectedItem.ToString()].Any())
                {
                    PopulateDataGridView1And3();
                    Console.WriteLine($"DeleteCycle: Вызван PopulateDataGridView1And3 для блока {comboBox2.SelectedItem}");
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в DeleteCycle: {ex.Message}");
            }
            finally
            {
                // Закрываем соединение, если оно открыто
                if (SQLiteConn != null && SQLiteConn.State == ConnectionState.Open)
                {
                    SQLiteConn.Close();
                    Console.WriteLine("Соединение закрыто в DeleteCycle.");
                }
            }
        }

        private double Round(double value, int decimalPlaces)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value;
            return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
        }

        private void PopulateDataGridView2()
        {
            if (tableData == null || tableData.Rows.Count == 0)
            {
                MessageBox.Show("Исходная таблица пуста.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем значение ошибки
            double errorValue = double.TryParse(textBox_error.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out double tempError) ? tempError : 0;

            // Получаем значение alpha для экспоненциального сглаживания
            if (!double.TryParse(textBox_Exp.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out double alpha) || alpha < 0 || alpha > 1)
            {
                MessageBox.Show($"Введите число между 0 и 1 для экспоненциального сглаживания (используйте '{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}' как десятичный разделитель).",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_Exp.Text = "0,9";
                alpha = 0.9;
            }

            DataTable calculatedTable = new DataTable();

            calculatedTable.Columns.Add("Эпоха");
            calculatedTable.Columns.Add("M+", typeof(double));
            calculatedTable.Columns.Add("M", typeof(double));
            calculatedTable.Columns.Add("M-", typeof(double));
            calculatedTable.Columns.Add("a+", typeof(double));
            calculatedTable.Columns.Add("a", typeof(double));
            calculatedTable.Columns.Add("a-", typeof(double));
            calculatedTable.Columns.Add("R", typeof(double));
            calculatedTable.Columns.Add("L", typeof(double));
            calculatedTable.Columns.Add("Результат");

            List<double[]> originalData = tableData.AsEnumerable()
                .Select(row => row.ItemArray.Skip(1).Select(x => x == DBNull.Value ? 0.0 : Convert.ToDouble(x)).ToArray())
                .ToList();
            List<string> epochs = tableData.AsEnumerable()
                .Select(row => row[0].ToString())
                .ToList();

            double firstMagnitude = 0;
            List<double> mPlusValuesFull = new List<double>(); // Округленные для отображения
            List<double> mValuesFull = new List<double>();
            List<double> mMinusValuesFull = new List<double>();
            List<double> mPlusValuesFullCalc = new List<double>(); // Неокругленные для вычислений
            List<double> mValuesFullCalc = new List<double>();
            List<double> mMinusValuesFullCalc = new List<double>();
            List<double> aPlusValuesFull = new List<double>();
            List<double> aValuesFull = new List<double>();
            List<double> aMinusValuesFull = new List<double>();

            // Сохраняем сглаженные значения для фиксированных alpha
            List<List<double>> smoothedMPlus = new List<List<double>>();
            List<List<double>> smoothedM = new List<List<double>>();
            List<List<double>> smoothedMMinus = new List<List<double>>();
            List<List<double>> smoothedAPlus = new List<List<double>>();
            List<List<double>> smoothedA = new List<List<double>>();
            List<List<double>> smoothedAMinus = new List<List<double>>();
            List<double> fixedAlphas = new List<double> { 0.1, 0.4, 0.7, 0.9 };

            // Вычисляем модуль первого вектора
            double[] firstRow = originalData[0];
            firstMagnitude = Math.Sqrt(firstRow.Sum(x => x * x));

            for (int i = 0; i < originalData.Count; i++)
            {
                double[] rowData = originalData[i];
                double sumSquares = rowData.Sum(x => x * x);
                double magnitude = Math.Sqrt(sumSquares);

                double magnitudePlus = Math.Sqrt(rowData.Select(x => (x + errorValue) * (x + errorValue)).Sum());
                double magnitudeMinus = Math.Sqrt(rowData.Select(x => (x - errorValue) * (x - errorValue)).Sum());

                double aPlus = 0, a = 0, aMinus = 0;

                if (rowData.All(x => x == 0) || firstRow.All(x => x == 0))
                {
                    Console.WriteLine($"Предупреждение: вектор в эпохе {epochs[i]} или первый вектор нулевой. Углы установлены в 0.");
                }
                else if (i > 0)
                {
                    double dotProductPlus = firstRow.Zip(rowData.Select(x => x + errorValue), (x, y) => x * y).Sum();
                    double dotProduct = firstRow.Zip(rowData, (x, y) => x * y).Sum();
                    double dotProductMinus = firstRow.Zip(rowData.Select(x => x - errorValue), (x, y) => x * y).Sum();

                    double cosPlus = 0, cosA = 0, cosMinus = 0;
                    if (firstMagnitude > 1e-10 && magnitudePlus > 1e-10)
                    {
                        cosPlus = dotProductPlus / (firstMagnitude * magnitudePlus);
                        cosPlus = Math.Max(-1, Math.Min(1, cosPlus));
                    }
                    if (firstMagnitude > 1e-10 && magnitude > 1e-10)
                    {
                        cosA = dotProduct / (firstMagnitude * magnitude);
                        cosA = Math.Max(-1, Math.Min(1, cosA));
                    }
                    if (firstMagnitude > 1e-10 && magnitudeMinus > 1e-10)
                    {
                        cosMinus = dotProductMinus / (firstMagnitude * magnitudeMinus);
                        cosMinus = Math.Max(-1, Math.Min(1, cosMinus));
                    }

                    aPlus = double.IsNaN(cosPlus) ? 0 : Math.Acos(cosPlus);
                    a = double.IsNaN(cosA) ? 0 : Math.Acos(cosA);
                    aMinus = double.IsNaN(cosMinus) ? 0 : Math.Acos(cosMinus);

                }

                // Округляем значения только для отображения
                double displayMPlus = Round(magnitudePlus, 4);
                double displayM = Round(magnitude, 4);
                double displayMMinus = Round(magnitudeMinus, 4);
                double displayAPlus = Round(aPlus, 6);
                double displayA = Round(a, 6);
                double displayAMinus = Round(aMinus, 6);

                double R = Math.Abs(magnitudePlus - magnitudeMinus) / 2;
                double L = i == 0 ? 0 : Math.Abs(magnitude - firstMagnitude);
                string result = L < R ? "+" : "-";

                // Сохраняем неокругленные значения для вычислений
                mPlusValuesFullCalc.Add(magnitudePlus);
                mValuesFullCalc.Add(magnitude);
                mMinusValuesFullCalc.Add(magnitudeMinus);
                // Сохраняем округленные значения для отображения
                mPlusValuesFull.Add(displayMPlus);
                mValuesFull.Add(displayM);
                mMinusValuesFull.Add(displayMMinus);
                aPlusValuesFull.Add(displayAPlus);
                aValuesFull.Add(displayA);
                aMinusValuesFull.Add(displayAMinus);

                calculatedTable.Rows.Add(
                    epochs[i],
                    displayMPlus,
                    displayM,
                    displayMMinus,
                    displayAPlus,
                    displayA,
                    displayAMinus,
                    Round(R, 4),
                    Round(L, 4),
                    result
                );
            }

            // Вычисляем сглаженные значения для фиксированных alpha на основе неокругленных значений
            foreach (double fixedAlpha in fixedAlphas)
            {
                smoothedMPlus.Add(CalculateExponentialSmoothing(mPlusValuesFullCalc, fixedAlpha));
                smoothedM.Add(CalculateExponentialSmoothing(mValuesFullCalc, fixedAlpha));
                smoothedMMinus.Add(CalculateExponentialSmoothing(mMinusValuesFullCalc, fixedAlpha));

                var aPlusTemp = aPlusValuesFull.Skip(1).ToList();
                var aTemp = aValuesFull.Skip(1).ToList();
                var aMinusTemp = aMinusValuesFull.Skip(1).ToList();

                smoothedAPlus.Add(aPlusTemp.Any() ? CalculateExponentialSmoothing(aPlusTemp, fixedAlpha) : new List<double>());
                smoothedA.Add(aTemp.Any() ? CalculateExponentialSmoothing(aTemp, fixedAlpha) : new List<double>());
                smoothedAMinus.Add(aMinusTemp.Any() ? CalculateExponentialSmoothing(aMinusTemp, fixedAlpha) : new List<double>());
            }

            // Прогноз на основе неокругленных значений
            List<List<double>> forecastValues = new List<List<double>>();
            try
            {
                forecastValues = new List<List<double>>
                    {
                        CalculateForecast(mPlusValuesFullCalc, alpha),
                        CalculateForecast(mValuesFullCalc, alpha),
                        CalculateForecast(mMinusValuesFullCalc, alpha),
                        CalculateForecast(aPlusValuesFull.Skip(1).ToList(), alpha),
                        CalculateForecast(aValuesFull.Skip(1).ToList(), alpha),
                        CalculateForecast(aMinusValuesFull.Skip(1).ToList(), alpha)
                    };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при вычислении прогноза: {ex.Message}");
                MessageBox.Show("Ошибка при вычислении прогноза. Проверьте входные данные.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (forecastValues.Any(f => !f.Any()))
            {
                Console.WriteLine("Один или несколько списков прогноза пусты.");
                MessageBox.Show("Невозможно построить прогноз из-за отсутствия данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Округляем прогнозные значения для отображения
            double forecastAPlus = Round(forecastValues[3].Last(), 6);
            double forecastA = Round(forecastValues[4].Last(), 6);
            double forecastAMinus = Round(forecastValues[5].Last(), 6);
            double forecastMPlus = Round(forecastValues[0].Last(), 4);
            double forecastM = Round(forecastValues[1].Last(), 4);
            double forecastMMinus = Round(forecastValues[2].Last(), 4);

            double forecastR = Math.Abs(forecastValues[0].Last() - forecastValues[2].Last()) / 2; // Используем неокругленные для R
            double forecastL = Math.Abs(forecastValues[1].Last() - firstMagnitude); // Используем неокругленные для L
            string forecastResult = forecastL < forecastR ? "+" : "-";

            calculatedTable.Rows.Add(
                "Прогноз",
                forecastMPlus,
                forecastM,
                forecastMMinus,
                forecastAPlus,
                forecastA,
                forecastAMinus,
                Round(forecastR, 4),
                Round(forecastL, 4),
                forecastResult
            );

            // Добавляем прогнозные значения в списки
            mPlusValuesFullCalc.Add(forecastValues[0].Last());
            mValuesFullCalc.Add(forecastValues[1].Last());
            mMinusValuesFullCalc.Add(forecastValues[2].Last());
            mPlusValuesFull.Add(forecastMPlus);
            mValuesFull.Add(forecastM);
            mMinusValuesFull.Add(forecastMMinus);
            aPlusValuesFull.Add(forecastAPlus);
            aValuesFull.Add(forecastA);
            aMinusValuesFull.Add(forecastAMinus);

            // Добавляем прогнозные точки в сглаженные списки
            for (int i = 0; i < fixedAlphas.Count; i++)
            {
                double fixedAlpha = fixedAlphas[i];
                smoothedMPlus[i].Add(CalculateForecast(mPlusValuesFullCalc.Take(mPlusValuesFullCalc.Count - 1).ToList(), fixedAlpha).Last());
                smoothedM[i].Add(CalculateForecast(mValuesFullCalc.Take(mValuesFullCalc.Count - 1).ToList(), fixedAlpha).Last());
                smoothedMMinus[i].Add(CalculateForecast(mMinusValuesFullCalc.Take(mMinusValuesFullCalc.Count - 1).ToList(), fixedAlpha).Last());

                var aPlusTemp = aPlusValuesFull.Skip(1).Take(aPlusValuesFull.Count - 2).ToList();
                var aTemp = aValuesFull.Skip(1).Take(aValuesFull.Count - 2).ToList();
                var aMinusTemp = aMinusValuesFull.Skip(1).Take(aMinusValuesFull.Count - 2).ToList();

                smoothedAPlus[i].Add(aPlusTemp.Any() ? CalculateForecast(aPlusTemp, fixedAlpha).Last() : double.NaN);
                smoothedA[i].Add(aTemp.Any() ? CalculateForecast(aTemp, fixedAlpha).Last() : double.NaN);
                smoothedAMinus[i].Add(aMinusTemp.Any() ? CalculateForecast(aMinusTemp, fixedAlpha).Last() : double.NaN);
            }

            dataGridView2.DataSource = calculatedTable;
            dataGridView2.Tag = new
            {
                MPlusFull = mPlusValuesFull, // Округленные для отображения
                MFull = mValuesFull,
                MMinusFull = mMinusValuesFull,
                MPlusFullCalc = mPlusValuesFullCalc, // Неокругленные для вычислений
                MFullCalc = mValuesFullCalc,
                MMinusFullCalc = mMinusValuesFullCalc,
                APlusFull = aPlusValuesFull,
                AFull = aValuesFull,
                AMinusFull = aMinusValuesFull,
                APlusFullCalc = aPlusValuesFull, // Углы уже округлены до 6 знаков
                AFullCalc = aValuesFull,
                AMinusFullCalc = aMinusValuesFull,
                SmoothedMPlus = smoothedMPlus,
                SmoothedM = smoothedM,
                SmoothedMMinus = smoothedMMinus,
                SmoothedAPlus = smoothedAPlus,
                SmoothedA = smoothedA,
                SmoothedAMinus = smoothedAMinus,
                FixedAlphas = fixedAlphas
            };

            Console.WriteLine($"PopulateDataGridView2: alpha={alpha}, mPlusValuesFullCalc.Count={mPlusValuesFullCalc.Count}, smoothedMPlus.Count={smoothedMPlus.Count}, tableName={currentTableName}");

            UpdateChart();
            UpdateChart2();
        }

        private void PopulateDataGridView1And3()
        {
            // Проверяем наличие исходной таблицы
            if (tableData == null || tableData.Rows.Count == 0)
            {
                MessageBox.Show("Исходная таблица пуста.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Проверяем выбор блока
            string selectedBlock = comboBox2.SelectedItem?.ToString();

            List<string> selectedColumns = blockPoints[selectedBlock];

            label6.Text = selectedBlock;

            // Создаем таблицы
            DataTable calculatedTable = new DataTable();
            calculatedTable.Columns.Add("Эпоха");
            foreach (var col in selectedColumns)
            {
                calculatedTable.Columns.Add(col, typeof(double));
            }
            calculatedTable.Columns.Add("M+", typeof(double));
            calculatedTable.Columns.Add("M", typeof(double));
            calculatedTable.Columns.Add("M-", typeof(double));
            calculatedTable.Columns.Add("a+", typeof(double));
            calculatedTable.Columns.Add("a", typeof(double));
            calculatedTable.Columns.Add("a-", typeof(double));

            DataTable resultTable = new DataTable();
            resultTable.Columns.Add("Эпоха");
            resultTable.Columns.Add("R", typeof(double));
            resultTable.Columns.Add("L", typeof(double));
            resultTable.Columns.Add("Состояние");

            // Получаем параметры
            double errorValue = double.TryParse(textBox_error.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out double tempError) ? tempError : 0;
            double alpha = double.TryParse(textBox_Exp.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out double expValue) && expValue >= 0 && expValue <= 1 ? expValue : 0.9;

            if (alpha != expValue)
            {
                MessageBox.Show($"Введите число между 0 и 1 для экспоненциального сглаживания (используйте '{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}' как десятичный разделитель).",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_Exp.Text = "0,9";
            }

            // Получаем эпохи и данные
            List<string> epochs = tableData.AsEnumerable().Select(row => row[0].ToString()).ToList();
            List<double[]> originalData = tableData.AsEnumerable()
                .Select(row => selectedColumns.Select(col => row[col] == DBNull.Value ? 0.0 : Convert.ToDouble(row[col])).ToArray())
                .ToList();

            // Списки для хранения вычисленных значений
            List<double> mPlusValuesFullCalc = new List<double>();
            List<double> mValuesFullCalc = new List<double>();
            List<double> mMinusValuesFullCalc = new List<double>();
            List<double> aPlusValuesFull = new List<double>();
            List<double> aValuesFull = new List<double>();
            List<double> aMinusValuesFull = new List<double>();
            List<double> mPlusValuesFull = new List<double>();
            List<double> mValuesFull = new List<double>();
            List<double> mMinusValuesFull = new List<double>();

            // Вычисляем первый вектор для углов
            double[] firstRow = originalData[0];
            double firstMagnitude = Math.Sqrt(firstRow.Sum(x => x * x));

            // Вычисляем значения для каждой эпохи
            for (int i = 0; i < originalData.Count; i++)
            {
                double[] rowData = originalData[i];
                double sumSquares = rowData.Sum(x => x * x);
                double magnitude = Math.Sqrt(sumSquares);

                // Вычисляем M+, M, M-
                double magnitudePlus = Math.Sqrt(rowData.Select(x => (x + errorValue) * (x + errorValue)).Sum());
                double magnitudeMinus = Math.Sqrt(rowData.Select(x => (x - errorValue) * (x - errorValue)).Sum());

                // Вычисляем углы a+, a, a-
                double aPlus = 0, a = 0, aMinus = 0;
                if (!rowData.All(x => x == 0) && !firstRow.All(x => x == 0) && i > 0)
                {
                    double dotProductPlus = firstRow.Zip(rowData.Select(x => x + errorValue), (x, y) => x * y).Sum();
                    double dotProduct = firstRow.Zip(rowData, (x, y) => x * y).Sum();
                    double dotProductMinus = firstRow.Zip(rowData.Select(x => x - errorValue), (x, y) => x * y).Sum();

                    double cosPlus = firstMagnitude > 1e-10 && magnitudePlus > 1e-10 ? Math.Max(-1, Math.Min(1, dotProductPlus / (firstMagnitude * magnitudePlus))) : 0;
                    double cosA = firstMagnitude > 1e-10 && magnitude > 1e-10 ? Math.Max(-1, Math.Min(1, dotProduct / (firstMagnitude * magnitude))) : 0;
                    double cosMinus = firstMagnitude > 1e-10 && magnitudeMinus > 1e-10 ? Math.Max(-1, Math.Min(1, dotProductMinus / (firstMagnitude * magnitudeMinus))) : 0;

                    aPlus = double.IsNaN(cosPlus) ? 0 : Math.Acos(cosPlus);
                    a = double.IsNaN(cosA) ? 0 : Math.Acos(cosA);
                    aMinus = double.IsNaN(cosMinus) ? 0 : Math.Acos(cosMinus);

                }

                // Округляем для отображения
                double displayMPlus = Round(magnitudePlus, 4);
                double displayM = Round(magnitude, 4);
                double displayMMinus = Round(magnitudeMinus, 4);
                double displayAPlus = Round(aPlus, 6);
                double displayA = Round(a, 6);
                double displayAMinus = Round(aMinus, 6);

                // Сохраняем значения
                mPlusValuesFullCalc.Add(magnitudePlus);
                mValuesFullCalc.Add(magnitude);
                mMinusValuesFullCalc.Add(magnitudeMinus);
                mPlusValuesFull.Add(displayMPlus);
                mValuesFull.Add(displayM);
                mMinusValuesFull.Add(displayMMinus);
                aPlusValuesFull.Add(displayAPlus);
                aValuesFull.Add(displayA);
                aMinusValuesFull.Add(displayAMinus);

                // Заполняем calculatedTable
                DataRow newRow = calculatedTable.NewRow();
                newRow["Эпоха"] = epochs[i];
                for (int j = 0; j < selectedColumns.Count; j++)
                {
                    newRow[selectedColumns[j]] = rowData[j];
                }
                newRow["M+"] = displayMPlus;
                newRow["M"] = displayM;
                newRow["M-"] = displayMMinus;
                newRow["a+"] = displayAPlus;
                newRow["a"] = displayA;
                newRow["a-"] = displayAMinus;
                calculatedTable.Rows.Add(newRow);

                // Заполняем resultTable
                double R = Math.Abs(magnitudePlus - magnitudeMinus) / 2;
                double L = i == 0 ? 0 : Math.Abs(magnitude - firstMagnitude);
                string state = L < R ? "+" : "-";

                resultTable.Rows.Add(epochs[i], Round(R, 4), Round(L, 4), state);
            }

            // Прогноз
            List<List<double>> forecastValues;
            try
            {
                forecastValues = new List<List<double>>
                {
                    CalculateForecast(mPlusValuesFullCalc, alpha),
                    CalculateForecast(mValuesFullCalc, alpha),
                    CalculateForecast(mMinusValuesFullCalc, alpha),
                    CalculateForecast(aPlusValuesFull.Skip(1).ToList(), alpha),
                    CalculateForecast(aValuesFull.Skip(1).ToList(), alpha),
                    CalculateForecast(aMinusValuesFull.Skip(1).ToList(), alpha)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка прогноза: {ex.Message}");
                MessageBox.Show("Ошибка при вычислении прогноза.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (forecastValues.Any(f => !f.Any()))
            {
                MessageBox.Show("Невозможно построить прогноз из-за отсутствия данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Добавляем прогноз в calculatedTable
            DataRow forecastRow = calculatedTable.NewRow();
            forecastRow["Эпоха"] = "Прогноз";
            for (int j = 0; j < selectedColumns.Count; j++)
            {
                forecastRow[selectedColumns[j]] = DBNull.Value;
            }
            forecastRow["M+"] = Round(forecastValues[0].Last(), 4);
            forecastRow["M"] = Round(forecastValues[1].Last(), 4);
            forecastRow["M-"] = Round(forecastValues[2].Last(), 4);
            forecastRow["a+"] = Round(forecastValues[3].Last(), 6);
            forecastRow["a"] = Round(forecastValues[4].Last(), 6);
            forecastRow["a-"] = Round(forecastValues[5].Last(), 6);
            calculatedTable.Rows.Add(forecastRow);

            // Обновляем списки с прогнозом
            mPlusValuesFullCalc.Add(forecastValues[0].Last());
            mValuesFullCalc.Add(forecastValues[1].Last());
            mMinusValuesFullCalc.Add(forecastValues[2].Last());
            mPlusValuesFull.Add(Round(forecastValues[0].Last(), 4));
            mValuesFull.Add(Round(forecastValues[1].Last(), 4));
            mMinusValuesFull.Add(Round(forecastValues[2].Last(), 4));
            aPlusValuesFull.Add(Round(forecastValues[3].Last(), 6));
            aValuesFull.Add(Round(forecastValues[4].Last(), 6));
            aMinusValuesFull.Add(Round(forecastValues[5].Last(), 6));

            // Прогноз для resultTable
            double forecastMPlus = forecastValues[0].Last();
            double forecastM = forecastValues[1].Last();
            double forecastMMinus = forecastValues[2].Last();
            double forecastR = Math.Abs(forecastMPlus - forecastMMinus) / 2;
            double forecastL = Math.Abs(forecastM - firstMagnitude);
            string forecastState = forecastL < forecastR ? "+" : "-";

            resultTable.Rows.Add("Прогноз", Round(forecastR, 4), Round(forecastL, 4), forecastState);

            // Устанавливаем источники данных
            dataGridView1.DataSource = calculatedTable;
            dataGridView3.DataSource = resultTable;

            // Сохраняем данные в dataGridView1.Tag
            dataGridView1.Tag = new
            {
                MPlusFull = mPlusValuesFull,
                MFull = mValuesFull,
                MMinusFull = mMinusValuesFull,
                MPlusFullCalc = mPlusValuesFullCalc,
                MFullCalc = mValuesFullCalc,
                MMinusFullCalc = mMinusValuesFullCalc,
                APlusFull = aPlusValuesFull,
                AFull = aValuesFull,
                AMinusFull = aMinusValuesFull
            };

            Console.WriteLine($"PopulateDataGridView1And3: Строк в calculatedTable={calculatedTable.Rows.Count}, resultTable={resultTable.Rows.Count}");
            UpdateChart3();
            UpdateChart4();
            UpdateCheckedListBox3();
        }

        private List<double> CalculateExponentialSmoothing(List<double> values, double alpha)
        {
            List<double> smoothed = new List<double>();
            if (values.Count == 0 || values.All(x => double.IsNaN(x)))
            {
                Console.WriteLine("Значения для сглаживания отсутствуют или содержат только NaN.");
                return smoothed;
            }

            // Первое значение: A * первое_значение + (1-A) * СРЗНАЧ(всех_значений)
            double avg = values.Where(x => !double.IsNaN(x)).Average();
            smoothed.Add(alpha * values[0] + (1 - alpha) * avg);

            // Остальные значения
            for (int i = 1; i < values.Count; i++)
            {
                if (!double.IsNaN(values[i]) && !double.IsNaN(smoothed[i - 1]))
                {
                    smoothed.Add(alpha * values[i] + (1 - alpha) * smoothed[i - 1]);
                }
                else
                {
                    smoothed.Add(double.NaN);
                }
            }

            return smoothed;
        }

        private List<double> CalculateForecast(List<double> baseValues, double alpha)
        {
            List<double> forecast = new List<double>();
            if (!baseValues.Any() || baseValues.All(x => double.IsNaN(x)))
            {
                Console.WriteLine("CalculateForecast: Базовые значения для прогноза отсутствуют или содержат только NaN.");
                return forecast;
            }

            Console.WriteLine($"CalculateForecast: baseValues.Count={baseValues.Count}, alpha={alpha}, lastValue={baseValues.LastOrDefault()}");

            forecast = CalculateExponentialSmoothing(baseValues, alpha);

            if (!forecast.Any())
            {
                Console.WriteLine("CalculateForecast: Сглаженные значения пусты.");
                return forecast;
            }

            double avgSmoothed = forecast.Where(x => !double.IsNaN(x)).Average();
            double lastSmoothed = forecast.Last();
            double forecastPoint = alpha * avgSmoothed + (1 - alpha) * lastSmoothed;
            forecast.Add(forecastPoint);

            return forecast;
        }

        private void ResetBlocksAndPoints()
        {
            dataGridView1.DataSource = null;
            dataGridView3.DataSource = null;
            chart3.Series.Clear();
            chart4.Series.Clear();
            dataGridView1.Tag = null; // Очищаем Tag
            Console.WriteLine("Таблицы dataGridView1 и dataGridView3 очищены.");

            listBox2.Items.Clear();
            blockPoints.Clear();
            LoadListBox1Items();
            Console.WriteLine("blockPoints очищен, точки возвращены в listBox1.");

            for (int i = 0; i < checkedListBox2.Items.Count; i++)
            {
                checkedListBox2.SetItemChecked(i, false);
            }

            radioButton7.Checked = false;
            radioButton8.Checked = false;
            radioButton9.Checked = false;
            radioButton10.Checked = false;
            radioButton11.Checked = false;
            radioButton12.Checked = false;
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            BeginInvoke(new Action(UpdateChart));
        }

        private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            Console.WriteLine($"Изменен элемент checkedListBox2: {checkedListBox2.Items[e.Index]}, новое состояние: {(e.NewValue == CheckState.Checked ? "Включен" : "Выключен")}");
            BeginInvoke(new Action(UpdateChart3));
        }

        private void checkedListBox3_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            Console.WriteLine($"Изменен элемент checkedListBox3: {checkedListBox3.Items[e.Index]}, новое состояние: {(e.NewValue == CheckState.Checked ? "Включен" : "Выключен")}");
        }

        private void DataGridViewOpenDB_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                Console.WriteLine($"Ячейка изменена: строка {e.RowIndex}, столбец {e.ColumnIndex}");
                if (tableData != null && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    object newValue = dataGridViewOpenDB.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    SaveCellChangeToDatabase(e.RowIndex, e.ColumnIndex, newValue);

                    PopulateDataGridView2();
                    UpdateCheckedListBox3();
                }
                else
                {
                    Console.WriteLine("Ошибка: tableData не инициализирована или неверные индексы.");
                }

                if (comboBox2.SelectedItem != null && blockPoints.ContainsKey(comboBox2.SelectedItem.ToString()) && blockPoints[comboBox2.SelectedItem.ToString()].Any())
                {
                    PopulateDataGridView1And3();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении изменений: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в CellValueChanged: {ex.Message}");
            }
        }

        private void dataGridViewOpenDB_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("Введено недопустимое значение. Пожалуйста, введите число, разделенной запятой.", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true; // отменяет редактирование ячейки
        }

        private void button_OpenDB_Click(object sender, EventArgs e)
        {
            if (OpenDatabaseFile())
            {
                // Активируем элементы для выбора таблицы
                comboBox1.Enabled = true;
                buttonOpen.Enabled = true;
                button9.Enabled = true;

                LoadDatabaseTables();
                LoadAdditionalData();
                LoadListBox1Items();
                LoadCheckedListBoxItems(checkedListBox1);
                LoadCheckedListBoxItems(checkedListBox2);
                InitializeComboBox2(); // Инициализация comboBox2 после открытия БД
                InitializeComboBox3(); // Обновляем comboBox3
                UpdateCheckedListBox3();

                // Очищаем выбор таблицы, чтобы данные не отображались автоматически
                comboBox1.SelectedIndex = -1;
                dataGridViewOpenDB.DataSource = null; // Очищаем таблицу
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {

                string selectedTable = comboBox1.SelectedItem.ToString();
                LoadTableData(selectedTable);

                button1.Enabled = true;
                checkedListBox1.Enabled = true;
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                radioButton3.Enabled = true;
                radioButton4.Enabled = true;
                radioButton5.Enabled = true;
                radioButton6.Enabled = true;
                comboBox2.Enabled = true;
                button5.Enabled = true;
                button3.Enabled = true;
                checkedListBox2.Enabled = true;
                radioButton7.Enabled = true;
                radioButton8.Enabled = true;
                radioButton9.Enabled = true;
                radioButton10.Enabled = true;
                radioButton11.Enabled = true;
                radioButton12.Enabled = true;
                button4.Enabled = true;
                comboBox3.Enabled = true;
                button6.Enabled = true;
                checkedListBox3.Enabled = true;

                button_addRow.Enabled = true;
                button_removeRow.Enabled = true;
                textBox_error.Enabled = true;
                textBox_block.Enabled = true;
                textBox_Exp.Enabled = true;
                button7.Enabled = true;
                button2.Enabled = true;

                LoadListBox1Items();
                UpdateCheckedListBox3();
            }
        }

        private void button_addRow_Click(object sender, EventArgs e)
        {
            if (tableData != null && tableData.Rows.Count > 1)
            {
                // Устанавливаем фокус на первую ячейку столбца "Эпоха" в последней строке
                if (dataGridViewOpenDB.CurrentCell == null || dataGridViewOpenDB.CurrentCell.ColumnIndex != 0)
                {
                    int lastRowIndex = dataGridViewOpenDB.Rows.Count - 1;
                    if (lastRowIndex >= 0)
                    {
                        dataGridViewOpenDB.CurrentCell = dataGridViewOpenDB.Rows[lastRowIndex].Cells[0];
                    }
                }

                AddCalculatedRow();

                // Проверяем, есть ли выбранный блок и точки в нем
                if (comboBox2.SelectedItem != null && blockPoints.ContainsKey(comboBox2.SelectedItem.ToString()) && blockPoints[comboBox2.SelectedItem.ToString()].Any())
                {
                    PopulateDataGridView1And3(); // Обновляем dataGridView1 и dataGridView3
                    UpdateChart4(); // Обновляем график chart4
                    Console.WriteLine($"button_addRow_Click: Вызван PopulateDataGridView1And3 и UpdateChart4 для блока {comboBox2.SelectedItem}");
                }
                else
                {
                    // Очищаем dataGridView1, dataGridView3 и chart4, если блоки не выбраны
                    dataGridView1.DataSource = null;
                    dataGridView3.DataSource = null;
                    chart4.Series.Clear();
                    dataGridView1.Tag = null;
                    Console.WriteLine("button_addRow_Click: Очищены dataGridView1, dataGridView3 и chart4, так как блоки не выбраны.");
                }
            }
            else
            {
                MessageBox.Show("Недостаточно данных для расчета.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            chart5.Series.Clear();
        }

        private void button_removeRow_Click(object sender, EventArgs e)
        {
            try
            {
                DeleteCycle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении строки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Ошибка в button_removeRow_Click: {ex.Message}");
            }
            chart5.Series.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage5;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            tabControl3.SelectedTab = tabPage6;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage11;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Проверяем, что все блоки содержат одинаковое количество точек
            int pointCount = -1;
            foreach (var block in blockPoints)
            {
                if (pointCount == -1)
                    pointCount = block.Value.Count;
                else if (block.Value.Count != pointCount)
                {
                    MessageBox.Show("Ошибка: Все блоки должны содержать одинаковое количество точек.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Проверяем, что есть хотя бы одна точка в блоках
            if (!blockPoints.Any(kvp => kvp.Value.Any()))
            {
                MessageBox.Show("Не выбраны точки для обработки в блоках.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            tabControl3.SelectedTab = tabPage7;
            PopulateDataGridView1And3();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            UpdateChart5();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            // Проверяем textBox_Exp
            string expText = textBox_Exp.Text.Trim();
            if (!double.TryParse(expText, NumberStyles.Any, CultureInfo.CurrentCulture, out double expValue) || expValue < 0 || expValue > 1)
            {
                MessageBox.Show($"Введите число между 0 и 1 для экспоненциального сглаживания (используйте '{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}' как десятичный разделитель).",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_Exp.Text = "0,9";
                SaveAdditionalData();
                return;
            }

            // Проверяем textBox_error
            string errorText = textBox_error.Text.Trim();
            if (!double.TryParse(errorText, NumberStyles.Any, CultureInfo.CurrentCulture, out double errorValue))
            {
                MessageBox.Show($"Введите действительное число для ошибки (используйте '{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}' как десятичный разделитель).",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_error.Text = "0,0017";
                SaveAdditionalData();
                return;
            }

            // Проверяем textBox_block и определяем, нужно ли сбрасывать блоки
            string blockText = textBox_block.Text.Trim();
            bool resetBlocks = false;
            int numBlocks = 2; // Значение по умолчанию
            if (int.TryParse(blockText, out int parsedNumBlocks) && parsedNumBlocks >= 1)
            {
                // Проверяем, изменилось ли количество блоков
                int currentNumBlocks = blockNames.Count;
                if (parsedNumBlocks != currentNumBlocks)
                {
                    resetBlocks = true;
                    numBlocks = parsedNumBlocks;
                }
            }
            else
            {
                MessageBox.Show("Введите положительное целое число для количества блоков.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_block.Text = "2";
                SaveAdditionalData();
                return;
            }

            // Сбрасываем блоки и точки только если изменилось значение в textBox_block
            if (resetBlocks)
            {
                ResetBlocksAndPoints();

                // Создаем новые блоки
                blockNames.Clear();
                for (int i = 0; i < numBlocks; i++)
                {
                    string blockName = ((char)('А' + i)).ToString();
                    blockNames.Add(blockName);
                    blockPoints[blockName] = new List<string>(); // Пустой список точек
                }

                InitializeComboBox2();
                InitializeComboBox3();

                label6.Text = null;
            }

            // Сохраняем дополнительные данные и обновляем таблицы
            SaveAdditionalData();
            PopulateDataGridView2();

            // Обновляем dataGridView1 и dataGridView3, если выбран блок и есть точки
            if (comboBox2.SelectedItem != null && blockPoints.ContainsKey(comboBox2.SelectedItem.ToString()) && blockPoints[comboBox2.SelectedItem.ToString()].Any())
            {
                PopulateDataGridView1And3();
                Console.WriteLine($"button7_Click: Вызван PopulateDataGridView1And3 для блока {comboBox2.SelectedItem}");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Show();
            form3.textBox1.SelectionStart = form3.textBox1.Text.Length;
            form3.textBox1.SelectionLength = 0;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                openFileDialog.Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Все файлы (*.*)|*.*";
                openFileDialog.Title = "Выберите изображение для pictureBox1 и pictureBox2";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Очищаем текущие изображения
                        pictureBox1.Image?.Dispose();
                        pictureBox2.Image?.Dispose();
                        pictureBox1.Image = null;
                        pictureBox2.Image = null;

                        // Загружаем новое изображение
                        Image image = Image.FromFile(openFileDialog.FileName);

                        // Устанавливаем изображение в оба PictureBox
                        pictureBox1.Image = image;
                        pictureBox2.Image = image;

                        Console.WriteLine($"Изображение {openFileDialog.FileName} загружено в pictureBox1 и pictureBox2.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Console.WriteLine($"Ошибка в button9_Click: {ex.Message}");
                    }
                }
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null && comboBox2.SelectedItem != null)
            {
                string selectedPoint = listBox1.SelectedItem.ToString();
                string selectedBlock = comboBox2.SelectedItem.ToString();

                // Проверяем, что точка не назначена ни одному блоку
                if (!blockPoints.Any(kvp => kvp.Value.Contains(selectedPoint)))
                {
                    blockPoints[selectedBlock].Add(selectedPoint);
                    listBox2.Items.Add(selectedPoint);
                    listBox1.Items.Remove(selectedPoint);
                    UpdateCheckedListBox3();
                }
                else
                {
                    MessageBox.Show($"Точка {selectedPoint} уже назначена одному из блоков.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null) return;

            string selectedPoint = listBox2.SelectedItem.ToString();
            string selectedBlock = comboBox2.SelectedItem?.ToString();

            if (blockPoints.ContainsKey(selectedBlock))
            {
                blockPoints[selectedBlock].Remove(selectedPoint);
                if (!blockPoints.Any(kvp => kvp.Value.Contains(selectedPoint)))
                {
                    listBox1.Items.Add(selectedPoint);
                }
                listBox2.Items.Remove(selectedPoint);
                UpdateCheckedListBox3();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedBlock = comboBox2.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedBlock)) return;

            listBox2.Items.Clear();
            if (blockPoints.ContainsKey(selectedBlock))
            {
                foreach (string point in blockPoints[selectedBlock])
                {
                    listBox2.Items.Add(point);
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCheckedListBox3();
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
                if (new[] { radioButton1, radioButton2, radioButton3, radioButton4, radioButton5, radioButton6 }.Contains(radioButton))
                {
                    UpdateChart2();
                }
                else if (new[] { radioButton7, radioButton8, radioButton9, radioButton10, radioButton11, radioButton12 }.Contains(radioButton))
                {
                    UpdateChart4();
                }
            }
        }
    }
}    