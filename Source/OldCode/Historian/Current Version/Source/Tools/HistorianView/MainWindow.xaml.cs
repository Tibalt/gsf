﻿
//******************************************************************************************************
//  MainWindow.xaml.cs - Gbtc
//
//  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  08/10/2010 - Stephen C. Wills
//       Generated original version of source code.
//  12/18/2011 - J. Ritchie Carroll
//       Set likely default archive locations on initial startup and removed disabled points from the
//       display list.
//  09/29/2012 - J. Ritchie Carroll
//       Updated to code to use roll-over yielding ArchiveReader.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Microsoft.Win32;
using TVA.Collections;
using TVA.Configuration;
using TVA.Historian;
using TVA.Historian.Files;
using TVA.IO;
using System.Globalization;

namespace HistorianView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region [ Members ]

        // Nested Types

        /// <summary>
        /// This is a wrapper around a MetadataRecord that allows auto-generation
        /// of columns in the data grid based on the public properties of this class.
        /// </summary>
        public class MetadataWrapper : INotifyPropertyChanged
        {
            #region [ Members ]

            // Events

            /// <summary>
            /// Event triggered when a property is changed.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            // Fields
            private MetadataRecord m_metadata;
            private bool m_export;

            #endregion

            #region [ Constructors ]

            /// <summary>
            /// Creates a new instance of the <see cref="MetadataWrapper"/> class.
            /// </summary>
            /// <param name="metadata">The <see cref="MetadataRecord"/> to be wrapped.</param>
            public MetadataWrapper(MetadataRecord metadata)
            {
                m_metadata = metadata;
            }

            #endregion

            #region [ Properties ]

            /// <summary>
            /// Determines whether the measurement represented by this metadata record
            /// should be exported to CSV or displayed on the graph by the Historian Data Viewer.
            /// </summary>
            public bool Export
            {
                get
                {
                    return m_export;
                }
                set
                {
                    m_export = value;
                    OnPropertyChanged("Export");
                }
            }

            /// <summary>
            /// Gets the point number of the measurement.
            /// </summary>
            public int PointNumber
            {
                get
                {
                    return m_metadata.HistorianID;
                }
            }

            /// <summary>
            /// Gets the name of the measurement.
            /// </summary>
            public string Name
            {
                get
                {
                    return m_metadata.Name;
                }
            }

            /// <summary>
            /// Gets the description of the measurement.
            /// </summary>
            public string Description
            {
                get
                {
                    return m_metadata.Description;
                }
            }

            /// <summary>
            /// Gets the first alternate name for the measurement.
            /// </summary>
            public string Synonym1
            {
                get
                {
                    return m_metadata.Synonym1;
                }
            }

            /// <summary>
            /// Gets the second alternate name for the measurement.
            /// </summary>
            public string Synonym2
            {
                get
                {
                    return m_metadata.Synonym2;
                }
            }

            /// <summary>
            /// Gets the third alternate name for the measurement.
            /// </summary>
            public string Synonym3
            {
                get
                {
                    return m_metadata.Synonym3;
                }
            }

            /// <summary>
            /// Gets the system name.
            /// </summary>
            public string System
            {
                get
                {
                    return m_metadata.SystemName;
                }
            }

            /// <summary>
            /// Gets the low range of the measurement.
            /// </summary>
            public Single LowRange
            {
                get
                {
                    return m_metadata.Summary.LowRange;
                }
            }

            /// <summary>
            /// Gets the high range of the measurement.
            /// </summary>
            public Single HighRange
            {
                get
                {
                    return m_metadata.Summary.HighRange;
                }
            }

            /// <summary>
            /// Gets the engineering units used to measure the values.
            /// </summary>
            public string EngineeringUnits
            {
                get
                {
                    return m_metadata.AnalogFields.EngineeringUnits;
                }
            }

            /// <summary>
            /// Gets the unit number of the measurement.
            /// </summary>
            public int Unit
            {
                get
                {
                    return m_metadata.UnitNumber;
                }
            }

            /// <summary>
            /// Returns the wrapped <see cref="MetadataRecord"/>.
            /// </summary>
            /// <returns>The wrapped metadata record.</returns>
            public MetadataRecord GetMetadata()
            {
                return m_metadata;
            }

            #endregion

            #region [ Methods ]

            // Triggers the PropertyChanged event.
            private void OnPropertyChanged(string propertyName)
            {
                if ((object)PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }

        // Fields

        private string m_currentSessionPath;
        private ICollection<ArchiveReader> m_archiveReaders;
        private List<MetadataWrapper> m_metadata;
        private DateTime m_startTime;
        private DateTime m_endTime;
        private string[] m_tokens;

        private ICollection<MenuItem> m_contextMenuItems;
        private ICollection<string> m_visibleColumns;
        private ChartWindow m_chartWindow;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            m_archiveReaders = new List<ArchiveReader>();
            m_metadata = new List<MetadataWrapper>();
            m_contextMenuItems = new List<MenuItem>();
            m_visibleColumns = new HashSet<string>();

            InitializeComponent();
            InitializeChartWindow();

            StartTime = TimeTag.Parse("*-5M").ToDateTime();
            EndTime = TimeTag.Parse("*").ToDateTime();
            m_currentTimeCheckBox.IsChecked = true;

            string[] lastArchiveLocations = ConfigurationFile.Current.Settings.General["ArchiveLocations", true].ValueAs("").Split('|').Where(archiveLocation => !string.IsNullOrWhiteSpace(archiveLocation) && File.Exists(archiveLocation)).ToArray();

            if (lastArchiveLocations.Length == 0)
            {
                // See if a local archive folder exists with a valid archive
                string defaultArchiveLocation = FilePath.GetAbsolutePath("Archive");

                if (Directory.Exists(defaultArchiveLocation))
                    lastArchiveLocations = Directory.GetFiles(defaultArchiveLocation, "*_archive.d");

                if (lastArchiveLocations.Length == 0)
                {
                    // See if a local statistics folder exists with a valid archive
                    defaultArchiveLocation = FilePath.GetAbsolutePath("Statistics");

                    if (Directory.Exists(defaultArchiveLocation))
                        lastArchiveLocations = Directory.GetFiles(defaultArchiveLocation, "*_archive.d");
                }
            }

            if (lastArchiveLocations.Length > 0)
                OpenArchives(lastArchiveLocations);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets a boolean that determines whether an archive is open or not.
        /// </summary>
        public bool ArchiveIsOpen
        {
            get
            {
                return m_saveButton.IsEnabled;
            }
            set
            {
                m_saveButton.IsEnabled = value;
                m_saveMenuItem.IsEnabled = value;
                m_saveAsMenuItem.IsEnabled = value;

                m_trendButton.IsEnabled = value;
                m_trendMenuItem.IsEnabled = value;

                m_exportButton.IsEnabled = value;
                m_exportMenuItem.IsEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the start time to be displayed on the graph or exported to CSV.
        /// </summary>
        public DateTime StartTime
        {
            get
            {
                return m_startTime;
            }
            set
            {
                m_startTime = value;
                m_startTimeDatePicker.SelectedDate = value;
                m_startTimeTextBox.Text = value.ToString("HH:mm:ss.fff");
            }
        }

        /// <summary>
        /// Gets or sets the end time to be displayed on the graph or exported to CSV.
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                return m_endTime;
            }
            set
            {
                m_endTime = value;
                m_endTimeDatePicker.SelectedDate = value;
                m_endTimeTextBox.Text = value.ToString("HH:mm:ss.fff");
            }
        }

        #endregion

        #region [ Methods ]

        // Initializes the chart window.
        private void InitializeChartWindow()
        {
            if (m_chartWindow == null)
            {
                m_chartWindow = new ChartWindow();
                m_chartWindow.ChartUpdated += ChartWindow_ChartUpdated;
                m_chartWindow.Closing += ChildWindow_Closing;
                TrySetChartInterval(null);
            }
        }

        // Gets a string representation of the start time to be used when reading from the archive.
        private string GetStartTime()
        {
            StringBuilder startTimeBuilder = new StringBuilder();

            if (!m_currentTimeCheckBox.IsChecked.Value)
                startTimeBuilder.Append(m_startTime.ToString("MM/dd/yyyy HH:mm:ss.fff"));
            else
            {
                startTimeBuilder.Append("*-");
                startTimeBuilder.Append(m_currentTimeTextBox.Text);
                startTimeBuilder.Append(m_currentTimeComboBox.SelectionBoxItem.ToString()[0]);
            }

            return startTimeBuilder.ToString();
        }

        // Gets a string representation of the end time to be used when reading from the archive.
        private string GetEndTime()
        {
            StringBuilder endTimeBuilder = new StringBuilder();

            if (m_currentTimeCheckBox.IsChecked.Value)
                return "*";
            else
                return m_endTime.ToString("MM/dd/yyyy HH:mm:ss.fff");
        }

        private void OpenArchive(string fileName)
        {
            OpenArchives(new string[] { fileName });
        }

        private void OpenArchives(string[] fileNames)
        {
            ClearArchives();

            foreach (string fileName in fileNames)
            {
                if (File.Exists(fileName))
                    m_archiveReaders.Add(OpenArchiveReader(fileName));
            }

            foreach (ArchiveReader reader in m_archiveReaders)
            {
                foreach (MetadataRecord record in reader.MetadataFile.Read())
                {
                    if (record.GeneralFlags.Enabled)
                        m_metadata.Add(new MetadataWrapper(record));
                }
            }

            ArchiveIsOpen = true;
            FilterBySearchResults();
            m_chartWindow.ArchiveReaders = m_archiveReaders;
            TrySetChartInterval(null);
            m_chartWindow.UpdateChart();
        }

        // Prompts the user to open one or more archive files.
        private void ShowOpenArchiveFileDialog()
        {
            OpenFileDialog archiveDialog = new OpenFileDialog();

            archiveDialog.Filter = "Archive files|*_archive.d";
            archiveDialog.CheckFileExists = true;
            archiveDialog.Multiselect = true;

            if (archiveDialog.ShowDialog() == true)
                OpenArchives(archiveDialog.FileNames);
        }

        // Prompts the user to open a previously saved session file.
        private void ShowOpenSessionDialog()
        {
            OpenFileDialog openSessionDialog = new OpenFileDialog();

            openSessionDialog.Filter = "Historian Data Viewer files|*.hdv";
            openSessionDialog.CheckFileExists = true;

            if (openSessionDialog.ShowDialog() == true)
                OpenSessionFile(openSessionDialog.FileName);
        }

        // Prompts the user for the location to save the current session.
        private void ShowSaveSessionDialog()
        {
            string errorMessage = "Unable to save current time interval."
                + " Please enter a start time that is less than or equal to the end time.";

            if (TrySetChartInterval(errorMessage))
            {
                SaveFileDialog saveSessionDialog = new SaveFileDialog();

                saveSessionDialog.Filter = "Historian Data Viewer files|*.hdv";
                saveSessionDialog.DefaultExt = "hdv";
                saveSessionDialog.AddExtension = true;
                saveSessionDialog.CheckPathExists = true;

                if (saveSessionDialog.ShowDialog() == true)
                    SaveCurrentSession(saveSessionDialog.FileName);

                this.Focus();
            }
        }

        // Opens an archive reader and returns the ArchiveReader object.
        private ArchiveReader OpenArchiveReader(string fileName)
        {
            ArchiveReader file = new ArchiveReader();

            file.Open(fileName);

            return file;
        }

        // Opens a previously saved session file.
        private void OpenSessionFile(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode root;
            IEnumerable<XmlElement> metadataElements;

            ClearArchives();
            doc.Load(filePath);
            root = doc.SelectSingleNode("historianDataViewer");
            StartTime = DateTime.Parse(root.Attributes["startTime"].Value);
            EndTime = DateTime.Parse(root.Attributes["endTime"].Value);
            metadataElements = root.ChildNodes.Cast<XmlElement>();
            m_archiveReaders = metadataElements.Select(element => element.Attributes["archivePath"].Value).Distinct().Select(archivePath => OpenArchiveReader(archivePath)).ToList();

            foreach (ArchiveReader reader in m_archiveReaders)
            {
                IEnumerable<MetadataWrapper> records = reader.MetadataFile.Read().Select(record => new MetadataWrapper(record));

                foreach (MetadataWrapper record in records)
                {
                    XmlElement metadataElement = metadataElements.SingleOrDefault(element => element.Attributes["historianId"].Value == record.GetMetadata().HistorianID.ToString() && element.Attributes["archivePath"].Value == reader.FileName);

                    if (metadataElement != null)
                        record.Export = bool.Parse(metadataElement.Attributes["export"].Value);

                    m_metadata.Add(record);
                }
            }

            m_currentSessionPath = filePath;
            this.Title = Path.GetFileName(filePath) + " - Historian Data Viewer";
            ArchiveIsOpen = true;
            FilterBySearchResults();
            m_chartWindow.ArchiveReaders = m_archiveReaders;
            TrySetChartInterval(null);
            m_chartWindow.UpdateChart();
        }

        // Saves the current session file with the current session's file path
        // or prompts the user if the current file path has not been set.
        private void SaveCurrentSession()
        {
            string errorMessage = "Unable to save current time interval."
                + " Please enter a start time that is less than or equal to the end time.";

            if (m_currentSessionPath == null)
                ShowSaveSessionDialog();
            else if (TrySetChartInterval(errorMessage))
                SaveCurrentSession(m_currentSessionPath);
        }

        // Saves the current session to a session file.
        //svk_5/29/12:the earlier version crashes while saving a historian data viewer file as a null object was tried to be referenced,
        //a fix for this error has been added.
        private void SaveCurrentSession(string filePath)
        {
            string[] attributeNames = { "archivePath", "historianId", "export" };
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("historianDataViewer");
            XmlAttribute startTime = doc.CreateAttribute("startTime");
            XmlAttribute endTime = doc.CreateAttribute("endTime");

            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", string.Empty));
            doc.AppendChild(root);

            startTime.Value = StartTime.ToString("MM/dd/yyyy HH:mm:ss.fff");
            endTime.Value = EndTime.ToString("MM/dd/yyyy HH:mm:ss.fff");
            root.Attributes.Append(startTime);
            root.Attributes.Append(endTime);

            foreach (ArchiveReader reader in m_archiveReaders)
            {
                foreach (MetadataRecord record in reader.MetadataFile.Read())
                {
                    //The earlier version used m_metadata.Single which would throw an exception(when GetMetadata returns a null) causing the application to crash
                    //When m_metadata.SingleOrDefault is used it returns a null(default) value when the comparison is false
                    MetadataWrapper wrapper = m_metadata.SingleOrDefault(wrap => wrap.GetMetadata() == record);//svk_modified from Single


                    if (wrapper != null)//svk_5/25/12//condition to ensure that an null referenced object is not accessed
                    {
                        if (wrapper.Export)
                        {
                            string[] attributeValues = { reader.FileName, record.HistorianID.ToString(), wrapper.Export.ToString() };
                            XmlElement metadataElement = doc.CreateElement("metadata");

                            for (int i = 0; i < attributeNames.Length; i++)
                            {
                                XmlAttribute attribute = doc.CreateAttribute(attributeNames[i]);
                                attribute.Value = attributeValues[i];
                                metadataElement.Attributes.Append(attribute);
                            }

                            root.AppendChild(metadataElement);
                        }
                    }

                }
            }

            doc.Save(filePath);
            m_currentSessionPath = filePath;
            this.Title = Path.GetFileName(filePath) + " - Historian Data Viewer";
        }

        // Creates an item for the data grid header area's context menu.
        private MenuItem CreateContextMenuItem(string header, bool isChecked)
        {
            MenuItem item = new MenuItem();

            item.Header = header;
            item.IsCheckable = true;
            item.IsChecked = isChecked;
            item.Click += ColumnContextMenuItem_Click;

            return item;
        }

        // Converts an automatically generated data grid header to a more human readable header.
        private string ToProperHeader(string header)
        {
            if (header == "PointNumber")
                return "Pt.#";

            StringBuilder properHeader = new StringBuilder();

            foreach (char c in header)
            {
                if (properHeader.Length != 0 && (char.IsUpper(c) || char.IsDigit(c)))
                    properHeader.Append(' ');

                properHeader.Append(c);
            }

            return properHeader.ToString();
        }

        // Preemptively updates the corresponding item's property when a data grid check box is checked or unchecked.
        private void DataGridCheckBoxCheckedChanged(DataGridCell cell, bool isChecked)
        {
            DataGridCellInfo cellInfo = new DataGridCellInfo(cell);
            string currentColumnHeader = cellInfo.Column.Header.ToString();
            PropertyInfo property = cellInfo.Item.GetType().GetProperties().Single(propertyInfo => propertyInfo.Name == currentColumnHeader);
            property.SetValue(cellInfo.Item, isChecked, null);
        }

        // Filters the data grid rows by the results of the user's search.
        //The earlier version of this function produces a output grid with spaces whenever a record was empty
        //These empty records are filtered by removing the records which do not have a measurement name.
        //The records are also arranged in Ascending order. 
        private void FilterBySearchResults()
        {
            m_tokens = m_searchBox.Text.Split(' ');

            m_dataGrid.ItemsSource = m_metadata.Where(metadata => !string.IsNullOrEmpty(metadata.Name))
                .Select(metadata => new Tuple<MetadataWrapper, bool>(metadata, SearchProperties(metadata, m_tokens)))
                .Where(tuple => tuple.Item1.Export || tuple.Item2)
                .OrderByDescending(tuple => tuple.Item2)
                .ThenBy(tuple => tuple.Item1.Export)
                .ThenBy(tuple => tuple.Item1.PointNumber)
                .Select(tuple => tuple.Item1)
                .ToList();
        }

        // Selects or deselects the export field for all records in the data grid.
        private void SetExportForAll(bool selected)
        {
            m_dataGrid.ItemsSource.Cast<MetadataWrapper>().ToList()
                .ForEach(metadata => metadata.Export = selected);
        }

        // Searches the non-boolean properties of an object to determine if their string representations collectively contain a set of tokens.
        private bool SearchProperties(object obj, string[] tokens)
        {
            IEnumerable<PropertyInfo> properties = obj.GetType().GetProperties().Where(property => property.PropertyType != typeof(bool));
            IEnumerable<string> propertyValues = properties.Select(property => property.GetValue(obj, null).ToString());
            return tokens.All(token => propertyValues.Any(propertyValue => propertyValue.ToLower().Contains(token.ToLower())));
        }

        // Displays the chart window.
        private void TrendRequested()
        {
            string chartIntervalErrorMessage = "Unable to set x-axis boundaries for the chart."
                   + " Please enter a start time that is less than or equal to the end time.";

            if (TrySetChartInterval(chartIntervalErrorMessage))
            {
                SetChartResolution();
                m_chartWindow.VisiblePoints = m_metadata.Where(wrapper => wrapper.Export).Select(wrapper => wrapper.GetMetadata()).ToList();
                m_chartWindow.UpdateChart();
                m_chartWindow.Show();
                m_chartWindow.Focus();
            }
        }

        // Exports measurements to a csv file.
        private void ExportRequested()
        {
            string csvFilePath = GetCsvFilePath();

            if (csvFilePath != null)
            {
                TextWriter writer = null;
                StringBuilder line = null;

                try
                {
                    int index = 0;

                    Dictionary<MetadataWrapper, ArchiveReader> metadata = m_metadata
                        .Where(wrapper => wrapper.Export)
                        .ToDictionary(
                            wrapper => wrapper,
                            wrapper => m_archiveReaders.Single(archive => archive.MetadataFile.Read().Any(record => wrapper.GetMetadata() == record))
                        );

                    Dictionary<TimeTag, List<string[]>> data = new Dictionary<TimeTag, List<string[]>>();
                    List<double> averages = new List<double>();
                    List<double> maximums = new List<double>();
                    List<double> minimums = new List<double>();

                    foreach (MetadataWrapper wrapper in metadata.Keys)
                    {
                        double min = double.NaN;
                        double max = double.NaN;
                        double total = 0.0;
                        int count = 0;

                        foreach (IDataPoint point in metadata[wrapper].ReadData(wrapper.GetMetadata().HistorianID, GetStartTime(), GetEndTime()))
                        {
                            TimeTag time = point.Time;
                            List<string[]> rowList;
                            string[] row;
                            int rowIndex;

                            // Get or create the list of rows for the timetag of the current point.
                            if (!data.TryGetValue(time, out rowList))
                            {
                                rowList = new List<string[]>();
                                data.Add(time, rowList);
                            }

                            // Attempt to add the value of the current point to an existing list.
                            for (rowIndex = 0; rowIndex < rowList.Count; rowIndex++)
                            {
                                row = rowList[rowIndex];

                                if (row[index] == null)
                                {
                                    row[index] = point.Value.ToString();
                                    break;
                                }
                            }

                            // If all rows were already occupied with
                            // a value for this point, add a new row.
                            if (rowIndex >= rowList.Count)
                            {
                                row = new string[metadata.Count];
                                row[index] = point.Value.ToString();
                                rowList.Add(row);
                            }

                            // Determine if this is the new minimum value.
                            if (!(point.Value >= min))
                                min = point.Value;

                            // Determine if this is the new maximum value.
                            if (!(point.Value <= max))
                                max = point.Value;

                            // Increase total and count for average calculation.
                            total += point.Value;
                            count++;
                        }

                        averages.Add(total / count);
                        maximums.Add(max);
                        minimums.Add(min);

                        index++;
                    }

                    writer = new StreamWriter(new FileStream(csvFilePath, FileMode.Create, FileAccess.Write));
                    line = new StringBuilder();

                    // Write time interval to the CSV file.
                    writer.Write("Historian Data Viewer Export: ");
                    writer.Write(GetStartTime());
                    writer.Write(" to ");
                    writer.Write(GetEndTime());
                    writer.WriteLine();
                    writer.WriteLine();

                    // Write average, min, and max for each measurement to the CSV file.
                    line.Append("Average,");
                    foreach (double average in averages)
                    {
                        line.Append(average);
                        line.Append(',');
                    }
                    line.Remove(line.Length - 1, 1);
                    writer.WriteLine(line.ToString());
                    line.Clear();

                    line.Append("Maximum,");
                    foreach (double max in maximums)
                    {
                        line.Append(max);
                        line.Append(',');
                    }
                    line.Remove(line.Length - 1, 1);
                    writer.WriteLine(line.ToString());
                    line.Clear();

                    line.Append("Minimum,");
                    foreach (double min in minimums)
                    {
                        line.Append(min);
                        line.Append(',');
                    }
                    line.Remove(line.Length - 1, 1);
                    writer.WriteLine(line.ToString());
                    line.Clear();

                    // Write header for the data points to the CSV file.
                    writer.WriteLine();
                    line.Append("Time,");
                    foreach (MetadataRecord record in metadata.Keys.Select(wrapper => wrapper.GetMetadata()))
                    {
                        line.Append(record.Name);
                        line.Append(' ');
                        line.Append(record.Description.Replace(',', '-'));
                        line.Append(',');
                    }
                    line.Remove(line.Length - 1, 1);
                    writer.WriteLine(line.ToString());
                    line.Clear();

                    // Write data to the CSV file.
                    foreach (KeyValuePair<TimeTag, List<string[]>> pair in data.OrderBy(p => p.Key))
                    {
                        TimeTag time = pair.Key;

                        foreach (string[] row in pair.Value)
                        {
                            line.Append(time);
                            line.Append(',');

                            foreach (string value in row)
                            {
                                line.Append(value ?? double.NaN.ToString());
                                line.Append(',');
                            }

                            line.Remove(line.Length - 1, 1);
                            writer.WriteLine(line.ToString());
                            line.Clear();
                        }
                    }
                }
                finally
                {
                    if (writer != null)
                        writer.Close();
                }
            }
        }

        // Sets the x-axis boundaries of the chart based on the current start time and end time.
        private bool TrySetChartInterval(string errorMessage)
        {
            try
            {
                bool canSetInterval = m_currentTimeCheckBox.IsChecked.Value || m_startTime < m_endTime;

                if (canSetInterval)
                    m_chartWindow.SetInterval(GetStartTime(), GetEndTime());
                else if (errorMessage != null)
                    MessageBox.Show(errorMessage);

                return canSetInterval;
            }
            catch (OverflowException)
            {
                m_currentTimeTextBox.Text = int.MaxValue.ToString();
                return TrySetChartInterval(errorMessage);
            }
            catch (ArgumentOutOfRangeException)
            {
                m_chartWindow.SetInterval(new DateTime().ToString(), DateTime.Now.ToString());
                return true;
            }
        }

        // Sets the chart resolution or displays an error message if user input is invalid.
        private void SetChartResolution()
        {
            int resolution;

            if (int.TryParse(m_chartResolutionTextBox.Text, out resolution))
                m_chartWindow.ChartResolution = resolution;
            else
            {
                m_chartResolutionTextBox.Text = int.MaxValue.ToString();
                m_chartWindow.ChartResolution = int.MaxValue;
            }
        }

        // Gets the file path of the CSV file in which to export measurements.
        private string GetCsvFilePath()
        {
            SaveFileDialog csvDialog = new SaveFileDialog();

            csvDialog.Filter = "CSV files|*.csv";
            csvDialog.DefaultExt = "csv";
            csvDialog.AddExtension = true;
            csvDialog.CheckPathExists = true;

            if (csvDialog.ShowDialog() == true)
                return csvDialog.FileName;
            else
                return null;
        }

        // Updates the visibility of the data grid's columns.
        private void UpdateColumns()
        {
            foreach (DataGridColumn column in m_dataGrid.Columns)
            {
                if (m_visibleColumns.Contains(column.Header))
                    column.Visibility = Visibility.Visible;
                else
                    column.Visibility = Visibility.Collapsed;
            }
        }

        // Closes the archive files and clears the archive file collection.
        private void ClearArchives()
        {
            foreach (ArchiveReader reader in m_archiveReaders)
                reader.Dispose();

            m_archiveReaders.Clear();
            m_metadata.Clear();
        }

        // Occurs when the DatePickers are initialized.
        private void DatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            DatePicker picker = sender as DatePicker;

            if (picker != null)
                picker.SelectedDate = DateTime.Today;
        }

        // Added for improvisation Occurs when the starting date is changed.
        private void StartTimeDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            StringBuilder dateString = new StringBuilder();

            dateString.Append(m_startTimeDatePicker.SelectedDate.Value.ToString("MM/dd/yyyy"));
            dateString.Append(' ');
            dateString.Append(m_startTime.ToString("HH:mm:ss.fff"));
            
            // Converts any date format style to US format and clubs both in dateString
            String format = "MM/dd/yyyy HH:mm:ss.fff";
            m_startTime = DateTime.ParseExact(dateString.ToString(), format, CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None);

        }


        // Occurs when the ending date is changed.
        private void EndTimeDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            StringBuilder dateString = new StringBuilder();

            dateString.Append(m_endTimeDatePicker.SelectedDate.Value.ToString("MM/dd/yyyy"));
            dateString.Append(' ');
            dateString.Append(m_endTime.ToString("HH:mm:ss.fff"));

            // Converts any date format style to US format and clubs both in dateString
            String format = "MM/dd/yyyy HH:mm:ss.fff";
            m_endTime = DateTime.ParseExact(dateString.ToString(), format, CultureInfo.CreateSpecificCulture("en-US"), DateTimeStyles.None);
        }

        // Occurs when the user changes the starting time.
        private void StartTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            StringBuilder dateString = new StringBuilder();
            DateTime startTime;

            dateString.Append(m_startTime.ToString("MM/dd/yyyy"));
            dateString.Append(' ');
            dateString.Append(m_startTimeTextBox.Text);

            if (DateTime.TryParse(dateString.ToString(), out startTime))
                m_startTime = startTime;
        }

        // Occurs when the user changes the ending time.
        private void EndTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            StringBuilder dateString = new StringBuilder();
            DateTime endTime;

            dateString.Append(m_endTime.ToString("MM/dd/yyyy"));
            dateString.Append(' ');
            dateString.Append(m_endTimeTextBox.Text);

            if (DateTime.TryParse(dateString.ToString(), out endTime))
                m_endTime = endTime;
        }

        // Filters text input so that only numbers can be entered into the text box.
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int num;

            if (!int.TryParse(e.Text, out num))
                e.Handled = true;
        }

        // Allows pasting into a numeric text box.
        private void NumericTextBox_PasteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        // Only numeric input can be pasted into a numeric text box.
        private void NumericTextBox_PasteCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextBox numericTextBox = sender as TextBox;

            if (numericTextBox != null)
            {
                string text = Clipboard.GetText();
                int num;

                if (int.TryParse(text, out num))
                    numericTextBox.Paste();

                e.Handled = true;
            }
        }

        // Occurs when the user selects a control that indicates they are ready to select archive files.
        private void OpenArchiveControl_Click(object sender, RoutedEventArgs e)
        {
            ShowOpenArchiveFileDialog();
        }

        // Occurs when the user selects a control that indicates they are ready to open a previously saved session.
        private void OpenControl_Click(object sender, RoutedEventArgs e)
        {
            ShowOpenSessionDialog();
        }

        // Occurs when the user selects a control that indicates they are ready to save the current session.
        private void SaveControl_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentSession();
        }

        // Occurs when the user selects the "Save as..." menu item from the file menu.
        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowSaveSessionDialog();
        }

        // Checks for Ctrl+O key combination to prompt the user to open archive files.
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool altDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            bool ctrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool shiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            e.Handled = true;

            if (!altDown && ctrlDown && !shiftDown && e.Key == Key.R)
                ShowOpenArchiveFileDialog();
            else if (!altDown && ctrlDown && !shiftDown && e.Key == Key.O)
                ShowOpenSessionDialog();
            else if (!altDown && ctrlDown && !shiftDown && e.Key == Key.S && ArchiveIsOpen)
                SaveCurrentSession();
            else if (!altDown && ctrlDown && shiftDown && e.Key == Key.S && ArchiveIsOpen)
                ShowSaveSessionDialog();
            else
                e.Handled = false;
        }

        // Occurs when the context menu for the data grid header area has been initialized.
        private void ColumnContextMenu_Initialized(object sender, EventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;

            if (menu != null)
                menu.ItemsSource = m_contextMenuItems;
        }

        // Sets the visibility of the data grid columns based on context menu selections made by the user.
        private void ColumnContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

            if (item != null)
            {
                if (item.IsChecked)
                    m_visibleColumns.Add(item.Header.ToString());
                else
                    m_visibleColumns.Remove(item.Header.ToString());
            }

            UpdateColumns();
        }

        // Fixes headers and visibility of columns in the data grid when they are created.
        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string header = ToProperHeader(e.Column.Header.ToString());

            switch (header)
            {
                case "Export":
                case "Display":
                    m_visibleColumns.Add(header);
                    break;

                case "Name":
                case "Description":
                case "Pt.#":
                    if (!m_visibleColumns.Contains(header))
                        m_visibleColumns.Add(header);

                    if (!m_contextMenuItems.Any(menuItem => menuItem.Header.ToString() == header))
                        m_contextMenuItems.Add(CreateContextMenuItem(header, true));

                    break;

                default:
                    if (!m_contextMenuItems.Any(menuItem => menuItem.Header.ToString() == header))
                        m_contextMenuItems.Add(CreateContextMenuItem(header, false));
                    break;
            }

            e.Column.Header = header;
        }

        // Sets the visibility of columns after they've all been generated.
        private void DataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            UpdateColumns();
        }

        // Focuses and selects cells ahead of time to allow single-click editing of data grid cells.
        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;

            if (cell != null && !cell.IsFocused)
            {
                cell.Focus();
                cell.IsSelected = true;
            }
        }

        // Updates the value of the object's property as soon as the check box is checked.
        private void DataGridCell_CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;

            if (cell != null)
                DataGridCheckBoxCheckedChanged(cell, true);
        }

        // Updates the value of the object's property as soon as the check box is unchecked.
        private void DataGridCell_CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;

            if (cell != null)
                DataGridCheckBoxCheckedChanged(cell, false);
        }

        // Occurs when the user chooses the start time relative to current time.
        private void CurrentTimeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            m_currentTimeStackPanel.Visibility = Visibility.Visible;
            m_historicTimeGrid.Visibility = Visibility.Collapsed;
        }

        // Occurs when the user chooses the start time not relative to current time.
        private void CurrentTimeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            m_currentTimeStackPanel.Visibility = Visibility.Collapsed;
            m_historicTimeGrid.Visibility = Visibility.Visible;
        }

        // Occurs when the user enters text into the search box.
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterBySearchResults();
        }

        // Selects the export field for all records in the data grid.
        private void SelectAllButton_Clicked(object sender, RoutedEventArgs e)
        {
            SetExportForAll(true);
        }

        // Deselects the export field for all records in the data grid.
        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetExportForAll(false);
        }

        // Displays the chart window.
        private void TrendControl_Click(object sender, RoutedEventArgs e)
        {
            TrendRequested();
        }

        // Exports measurements to a CSV file.
        private void ExportControl_Click(object sender, RoutedEventArgs e)
        {
            ExportRequested();
        }

        // Displays the chart window.
        private void ChartResolutionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ArchiveIsOpen)
                TrendRequested();
        }

        // Updates the start time and end time based on the latest chart boundaries.
        private void ChartWindow_ChartUpdated(object sender, EventArgs e)
        {
            StartTime = m_chartWindow.StartTime;
            EndTime = m_chartWindow.EndTime;
        }

        // Hides the child window rather than closing it so it can be shown again later.
        private void ChildWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Window child = sender as Window;

            if (child != null)
            {
                e.Cancel = true;
                child.Hide();
            }
        }

        // Closes the window.
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Clears the archives and closes child windows.
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            string lastArchiveLocations = "";

            if (m_archiveReaders != null && m_archiveReaders.Count > 0)
                lastArchiveLocations = m_archiveReaders.Select(file => file.FileName).ToDelimitedString();

            ConfigurationFile.Current.Settings.General["ArchiveLocations", true].Value = lastArchiveLocations;
            ConfigurationFile.Current.Save();

            ClearArchives();
            m_chartWindow.Closing -= ChildWindow_Closing;
            m_chartWindow.Close();
        }

        #endregion
    }
}