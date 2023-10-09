﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BurnOutSharp;
using MPF.Core;
using MPF.Core.Converters;
using MPF.Core.Data;
using MPF.Core.Utilities;
using MPF.Core.UI.ComboBoxItems;
using MPF.UI.Core.UserControls;
using MPF.UI.Core.Windows;
using SabreTools.RedumpLib.Data;
using WPFCustomMessageBox;
using WinForms = System.Windows.Forms;

namespace MPF.UI.Core.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        /// <summary>
        /// Parent MainWindow object
        /// </summary>
        public MainWindow Parent { get; private set; }

        /// <summary>
        /// LogViewModel associated with the parent window
        /// </summary>
        public LogOutput Logger { get; private set; }

        /// <summary>
        /// Access to the current options
        /// </summary>
        public MPF.Core.Data.Options Options
        {
            get => _options;
            set
            {
                _options = value;
                OptionsLoader.SaveToConfig(_options);
            }
        }

        /// <summary>
        /// Current dumping environment
        /// </summary>
        public DumpEnvironment Env { get; set; }

        /// <summary>
        /// Indicates if SelectionChanged events can be executed
        /// </summary>
        public bool CanExecuteSelectionChanged { get; private set; } = false;

        /// <summary>
        /// Internal reference to Options
        /// </summary>
        private MPF.Core.Data.Options _options;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Currently selected system value
        /// </summary>
        public RedumpSystem? CurrentSystem
        {
            get => _currentSystem;
            set
            {
                _currentSystem = value;
                TriggerPropertyChanged("CurrentSystem");
            }
        }
        private RedumpSystem? _currentSystem;

        /// <summary>
        /// Currently selected media type value
        /// </summary>
        public MediaType? CurrentMediaType
        {
            get => _currentMediaType;
            set
            {
                _currentMediaType = value;
                TriggerPropertyChanged("CurrentMediaType");
            }
        }
        private MediaType? _currentMediaType;

        /// <summary>
        /// Currently provided output path
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                _outputPath = value;
                TriggerPropertyChanged("OutputPath");
            }
        }
        private string _outputPath;

        /// <summary>
        /// Currently selected drive value
        /// </summary>
        public Drive CurrentDrive
        {
            get => _currentDrive;
            set
            {
                _currentDrive = value;
                TriggerPropertyChanged("CurrentDrive");
            }
        }
        private Drive _currentDrive;

        /// <summary>
        /// Currently selected drive speed value
        /// </summary>
        public int DriveSpeed
        {
            get => _driveSpeed;
            set
            {
                _driveSpeed = value;
                TriggerPropertyChanged("DriveSpeed");
            }
        }
        private int _driveSpeed;

        /// <summary>
        /// Currently selected dumping program
        /// </summary>
        public InternalProgram CurrentProgram
        {
            get => _currentProgram;
            set
            {
                _currentProgram = value;
                TriggerPropertyChanged("CurrentProgram");
            }
        }
        private InternalProgram _currentProgram;

        /// <summary>
        /// Currently provided parameters
        /// </summary>
        public string Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                TriggerPropertyChanged("Parameters");
            }
        }
        private string _parameters;

        /// <summary>
        /// Currently displayed status
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                TriggerPropertyChanged("Status");
            }
        }
        private string _status;

        #endregion

        #region List Properties

        /// <summary>
        /// Current list of drives
        /// </summary>
        public List<Drive> Drives
        {
            get => _drives;
            set
            {
                _drives = value;
                TriggerPropertyChanged("Drives");
            }
        }
        private List<Drive> _drives;

        /// <summary>
        /// Current list of supported media types
        /// </summary>
        public List<Element<MediaType>> MediaTypes
        {
            get => _mediaTypes;
            set
            {
                _mediaTypes = value;
                TriggerPropertyChanged("MediaTypes");
            }
        }
        private List<Element<MediaType>> _mediaTypes;

        /// <summary>
        /// Current list of supported system profiles
        /// </summary>
        public List<RedumpSystemComboBoxItem> Systems
        {
            get => _systems;
            set
            {
                _systems = value;
                TriggerPropertyChanged("Systems");
            }
        }
        private List<RedumpSystemComboBoxItem> _systems;

        /// <summary>
        /// List of available internal programs
        /// </summary>
        public List<Element<InternalProgram>> InternalPrograms
        {
            get => _internalPrograms;
            set
            {
                _internalPrograms = value;
                TriggerPropertyChanged("InternalPrograms");
            }
        }
        private List<Element<InternalProgram>> _internalPrograms;

        #endregion

        /// <summary>
        /// Generic constructor
        /// </summary>
        public MainViewModel()
        {
            _options = OptionsLoader.LoadFromConfig();
            MediaTypes = new List<Element<MediaType>>();
            Systems = RedumpSystemComboBoxItem.GenerateElements().ToList();
            InternalPrograms = new List<Element<InternalProgram>>();
        }

        /// <summary>
        /// Initialize the main window after loading
        /// </summary>
        public void Init(MainWindow parent)
        {
            // Set the parent window
            this.Parent = parent;
            this.Logger = parent.LogOutput;

            // Finish initializing the rest of the values
            InitializeUIValues(removeEventHandlers: false, rescanDrives: true);

            // Check for updates, if necessary
            if (this.Options.CheckForUpdatesOnStartup)
                CheckForUpdates(showIfSame: false);
        }

        #region Property Updates

        /// <summary>
        /// Trigger a property changed event
        /// </summary>
        private void TriggerPropertyChanged(string propertyName)
        {
            // Disable event handlers temporarily
            CanExecuteSelectionChanged = false;

            // If the property change event is initialized
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

            // Reenable event handlers
            CanExecuteSelectionChanged = true;
        }

        #endregion

        #region Population

        /// <summary>
        /// Get a complete list of active disc drives and fill the combo box
        /// </summary>
        /// <remarks>TODO: Find a way for this to periodically run, or have it hook to a "drive change" event</remarks>
        private void PopulateDrives()
        {
            // Disable other UI updates
            CanExecuteSelectionChanged = false;

            if (this.Options.VerboseLogging)
                this.Logger.VerboseLogLn("Scanning for drives..");

            // Always enable the media scan
            this.Parent.MediaScanButton.IsEnabled = true;
            this.Parent.UpdateVolumeLabel.IsEnabled = true;

            // If we have a selected drive, keep track of it
            char? lastSelectedDrive = this.CurrentDrive?.Letter;

            // Populate the list of drives and add it to the combo box
            Drives = Drive.CreateListOfDrives(this.Options.IgnoreFixedDrives);

            if (Drives.Count > 0)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"Found {Drives.Count} drives: {string.Join(", ", Drives.Select(d => d.Letter))}");

                // Check for the last selected drive, if possible
                int index = -1;
                if (lastSelectedDrive != null)
                    index = Drives.FindIndex(d => d.MarkedActive && d.Letter == lastSelectedDrive);

                // Check for active optical drives
                if (index == -1)
                    index = Drives.FindIndex(d => d.MarkedActive && d.InternalDriveType == InternalDriveType.Optical);

                // Check for active floppy drives
                if (index == -1)
                    index = Drives.FindIndex(d => d.MarkedActive && d.InternalDriveType == InternalDriveType.Floppy);

                // Check for any active drives
                if (index == -1)
                    index = Drives.FindIndex(d => d.MarkedActive);

                // Set the selected index
                CurrentDrive = (index != -1 ? Drives[index] : Drives[0]);
                this.Status = "Valid drive found! Choose your Media Type";
                this.Parent.CopyProtectScanButton.IsEnabled = true;

                // Get the current system type
                if (index != -1)
                    DetermineSystemType();

                // Only enable the start/stop if we don't have the default selected
                this.Parent.StartStopButton.IsEnabled = ShouldEnableDumpingButton();
            }
            else
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn("Found no drives");
                this.CurrentDrive = null;
                this.Status = "No valid drive found!";
                this.Parent.StartStopButton.IsEnabled = false;
                this.Parent.CopyProtectScanButton.IsEnabled = false;
            }

            // Reenable UI updates
            CanExecuteSelectionChanged = true;

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Populate media type according to system type
        /// </summary>
        private void PopulateMediaType()
        {
            // Disable other UI updates
            CanExecuteSelectionChanged = false;

            if (this.CurrentSystem != null)
            {
                var mediaTypeValues = this.CurrentSystem.MediaTypes();
                int index = mediaTypeValues.FindIndex(m => m == this.CurrentMediaType);
                if (this.CurrentMediaType != null && index == -1 && this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"Disc of type '{CurrentMediaType.LongName()}' found, but the current system does not support it!");

                MediaTypes = Element<MediaType>.GenerateElements().Where(m => mediaTypeValues.Contains(m.Value)).ToList();

                //this.Parent.MediaTypeComboBox.IsEnabled = MediaTypes.Count > 1;
                this.Parent.MediaTypeComboBox.IsEnabled = true;

                this.CurrentMediaType = (index > -1 ? MediaTypes[index] : MediaTypes[0]);
                this.Parent.MediaTypeComboBox.SelectedIndex = (index > -1 ? index : 0);
            }
            else
            {
                this.Parent.MediaTypeComboBox.IsEnabled = false;
                this.Parent.MediaTypeComboBox.ItemsSource = null;
                this.Parent.MediaTypeComboBox.SelectedIndex = -1;
            }

            // Reenable UI updates
            CanExecuteSelectionChanged = true;

            // Ensure the UI gets updated
            this.ChangeMediaType(null);
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Populate media type according to system type
        /// </summary>
        private void PopulateInternalPrograms()
        {
            // Disable other UI updates
            CanExecuteSelectionChanged = false;

            // Get the current internal program
            InternalProgram internalProgram = this.Options.InternalProgram;

            // Create a static list of supported programs, not everything
            var internalPrograms = new List<InternalProgram> { InternalProgram.DiscImageCreator, InternalProgram.Aaru, InternalProgram.Redumper };
            InternalPrograms = internalPrograms.Select(ip => new Element<InternalProgram>(ip)).ToList();

            // Select the current default dumping program
            int currentIndex = InternalPrograms.FindIndex(m => m == internalProgram);
            this.CurrentProgram = (currentIndex > -1 ? InternalPrograms[currentIndex].Value : InternalPrograms[0].Value);

            // Reenable UI updates
            CanExecuteSelectionChanged = true;

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        #endregion

        #region UI Commands

        /// <summary>
        /// Change the currently selected dumping program
        /// </summary>
        public void ChangeDumpingProgram()
        {
            if (this.Options.VerboseLogging)
                this.Logger.VerboseLogLn($"Changed dumping program to: {((InternalProgram?)this.CurrentProgram).LongName()}");
            EnsureDiscInformation();
            GetOutputNames(false);
        }

        /// <summary>
        /// Change the currently selected media type
        /// </summary>
        public void ChangeMediaType(SelectionChangedEventArgs e)
        {
            // Only change the media type if the selection and not the list has changed
            if (e == null || (e.RemovedItems.Count == 1 && e.AddedItems.Count == 1))
            {
                SetSupportedDriveSpeed();
            }

            GetOutputNames(false);
            EnsureDiscInformation();
        }

        /// <summary>
        /// Change the currently selected system
        /// </summary>
        public void ChangeSystem()
        {
            if (this.Options.VerboseLogging)
                this.Logger.VerboseLogLn($"Changed system to: {this.CurrentSystem.LongName()}");
            PopulateMediaType();
            GetOutputNames(false);
            EnsureDiscInformation();
        }

        /// <summary>
        /// Check for available updates
        /// </summary>
        /// <param name="showIfSame">True to show the box even if it's the same, false to only show if it's different</param>
        public void CheckForUpdates(bool showIfSame)
        {
            (bool different, string message, string url) = Tools.CheckForNewVersion();

            // If we have a new version, put it in the clipboard
            if (different)
                Clipboard.SetText(url);

            this.Logger.SecretLogLn(message);
            if (url == null)
                message = "An exception occurred while checking for versions, please try again later. See the log window for more details.";

            if (showIfSame || different)
                CustomMessageBox.Show(message, "Version Update Check", MessageBoxButton.OK, different ? MessageBoxImage.Exclamation : MessageBoxImage.Information);
        }

        /// <summary>
        /// Shutdown the current application
        /// </summary>
        public static void ExitApplication() => Application.Current.Shutdown();

        /// <summary>
        /// Set the output path from a dialog box
        /// </summary>
        public void SetOutputPath()
        {
            BrowseFile();
            EnsureDiscInformation();
        }

        /// <summary>
        /// Show the About text popup
        /// </summary>
        public void ShowAboutText()
        {
            string aboutText = $"Media Preservation Frontend (MPF)"
                + $"{Environment.NewLine}"
                + $"{Environment.NewLine}A community preservation frontend developed in C#."
                + $"{Environment.NewLine}Supports Redumper, Aaru, and DiscImageCreator."
                + $"{Environment.NewLine}Originally created to help the Redump project."
                + $"{Environment.NewLine}"
                + $"{Environment.NewLine}Thanks to everyone who has supported this project!"
                + $"{Environment.NewLine}"
                + $"{Environment.NewLine}Version {Tools.GetCurrentVersion()}";

            this.Logger.SecretLogLn(aboutText);
            CustomMessageBox.Show(aboutText, "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Build a dummy SubmissionInfo and display it for testing
        /// </summary>
        public void ShowDebugDiscInfoWindow()
        {
            var submissionInfo = new SubmissionInfo()
            {
                SchemaVersion = 1,
                FullyMatchedID = 3,
                PartiallyMatchedIDs = new List<int> { 0, 1, 2, 3 },
                Added = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,

                CommonDiscInfo = new CommonDiscInfoSection()
                {
                    System = SabreTools.RedumpLib.Data.RedumpSystem.IBMPCcompatible,
                    Media = DiscType.BD128,
                    Title = "Game Title",
                    ForeignTitleNonLatin = "Foreign Game Title",
                    DiscNumberLetter = "1",
                    DiscTitle = "Install Disc",
                    Category = DiscCategory.Games,
                    Region = Region.World,
                    Languages = new Language?[] { Language.English, Language.Spanish, Language.French },
                    LanguageSelection = new LanguageSelection?[] { LanguageSelection.BiosSettings },
                    Serial = "Disc Serial",
                    Layer0MasteringRing = "L0 Mastering Ring",
                    Layer0MasteringSID = "L0 Mastering SID",
                    Layer0ToolstampMasteringCode = "L0 Toolstamp",
                    Layer0MouldSID = "L0 Mould SID",
                    Layer0AdditionalMould = "L0 Additional Mould",
                    Layer1MasteringRing = "L1 Mastering Ring",
                    Layer1MasteringSID = "L1 Mastering SID",
                    Layer1ToolstampMasteringCode = "L1 Toolstamp",
                    Layer1MouldSID = "L1 Mould SID",
                    Layer1AdditionalMould = "L1 Additional Mould",
                    Layer2MasteringRing = "L2 Mastering Ring",
                    Layer2MasteringSID = "L2 Mastering SID",
                    Layer2ToolstampMasteringCode = "L2 Toolstamp",
                    Layer3MasteringRing = "L3 Mastering Ring",
                    Layer3MasteringSID = "L3 Mastering SID",
                    Layer3ToolstampMasteringCode = "L3 Toolstamp",
                    RingWriteOffset = "+12",
                    Barcode = "UPC Barcode",
                    EXEDateBuildDate = "19xx-xx-xx",
                    ErrorsCount = "0",
                    Comments = "Comment data line 1\r\nComment data line 2",
#if NET48
                    CommentsSpecialFields = new Dictionary<SiteCode?, string>()
#else
                    CommentsSpecialFields = new Dictionary<SiteCode, string>()
#endif
                    {
                        [SiteCode.ISBN] = "ISBN",
                    },
                    Contents = "Special contents 1\r\nSpecial contents 2",
#if NET48
                    ContentsSpecialFields = new Dictionary<SiteCode?, string>()
#else
                    ContentsSpecialFields = new Dictionary<SiteCode, string>()
#endif
                    {
                        [SiteCode.PlayableDemos] = "Game Demo 1",
                    },
                },

                VersionAndEditions = new VersionAndEditionsSection()
                {
                    Version = "Original",
                    VersionDatfile = "Alt",
                    CommonEditions = new string[] { "Taikenban" },
                    OtherEditions = "Rerelease",
                },

                EDC = new EDCSection()
                {
                    EDC = YesNo.Yes,
                },

                ParentCloneRelationship = new ParentCloneRelationshipSection()
                {
                    ParentID = "12345",
                    RegionalParent = false,
                },

                Extras = new ExtrasSection()
                {
                    PVD = "PVD with a stupidly long line and nothing else but a little more\nPVD with a stupidly long line and nothing else but a little more\nPVD with a stupidly long line and nothing else but a little more\nPVD with a stupidly long line and nothing else but a little more\nPVD with a stupidly long line and nothing else but a little more\nPVD with a stupidly long line and nothing else but a little more\n",
                    DiscKey = "Disc key",
                    DiscID = "Disc ID",
                    PIC = "PIC",
                    Header = "Header",
                    BCA = "BCA",
                    SecuritySectorRanges = "SSv1 Ranges",
                },

                CopyProtection = new CopyProtectionSection()
                {
                    AntiModchip = YesNo.Yes,
                    LibCrypt = YesNo.No,
                    LibCryptData = "LibCrypt data",
                    Protection = "List of protections",
                    SecuROMData = "SecuROM data",
                },

                DumpersAndStatus = new DumpersAndStatusSection()
                {
                    Status = DumpStatus.TwoOrMoreGreen,
                    Dumpers = new string[] { "Dumper1", "Dumper2" },
                    OtherDumpers = "Dumper3",
                },

                TracksAndWriteOffsets = new TracksAndWriteOffsetsSection()
                {
                    ClrMameProData = "Datfile",
                    Cuesheet = "Cuesheet",
                    CommonWriteOffsets = new int[] { 0, 12, -12 },
                    OtherWriteOffsets = "-2",
                },

                SizeAndChecksums = new SizeAndChecksumsSection()
                {
                    Layerbreak = 0,
                    Layerbreak2 = 1,
                    Layerbreak3 = 2,
                    Size = 12345,
                    CRC32 = "CRC32",
                    MD5 = "MD5",
                    SHA1 = "SHA1",
                },

                DumpingInfo = new DumpingInfoSection()
                {
                    DumpingProgram = "DiscImageCreator 20500101",
                    DumpingDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Manufacturer = "ATAPI",
                    Model = "Optical Drive",
                    Firmware = "1.23",
                    ReportedDiscType = "CD-R",
                },

                Artifacts = new Dictionary<string, string>()
                {
                    ["Sample Artifact"] = "Sample Data",
                },
            };

            var result = ShowDiscInformationWindow(submissionInfo);
            InfoTool.ProcessSpecialFields(result.Item2);
        }

        /// <summary>
        /// Show the Options window
        /// </summary>
        public void ShowOptionsWindow()
        {
            var optionsWindow = new OptionsWindow(this.Options)
            {
                Focusable = true,
                Owner = this.Parent,
                ShowActivated = true,
                ShowInTaskbar = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            optionsWindow.Closed += OnOptionsUpdated;
            optionsWindow.Show();
        }

        /// <summary>
        /// Toggle the Start/Stop button
        /// </summary>
        public async void ToggleStartStop()
        {
            // Dump or stop the dump
            if ((string)this.Parent.StartStopButton.Content == Interface.StartDumping)
            {
                StartDumping();
            }
            else if ((string)this.Parent.StartStopButton.Content == Interface.StopDumping)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn("Canceling dumping process...");
                Env.CancelDumping();
                this.Parent.CopyProtectScanButton.IsEnabled = true;

                if (Env.Options.EjectAfterDump == true)
                {
                    if (this.Options.VerboseLogging)
                        this.Logger.VerboseLogLn($"Ejecting disc in drive {Env.Drive.Letter}");
                    await Env.EjectDisc();
                }

                if (this.Options.DICResetDriveAfterDump)
                {
                    if (this.Options.VerboseLogging)
                        this.Logger.VerboseLogLn($"Resetting drive {Env.Drive.Letter}");
                    await Env.ResetDrive();
                }
            }

            // Reset the progress bar
            this.Logger.ResetProgressBar();
        }

        /// <summary>
        /// Update the internal options from a closed OptionsWindow
        /// </summary>
        /// <param name="optionsWindow">OptionsWindow to copy back data from</param>
        public void UpdateOptions(OptionsWindow optionsWindow)
        {
            if (optionsWindow?.OptionsViewModel.SavedSettings == true)
            {
                this.Options = new MPF.Core.Data.Options(optionsWindow.OptionsViewModel.Options);
                InitializeUIValues(removeEventHandlers: true, rescanDrives: true);
            }
        }

        #endregion

        #region UI Functionality

        /// <summary>
        /// Performs UI value setup end to end
        /// </summary>
        /// <param name="removeEventHandlers">Whether event handlers need to be removed first</param>
        /// <param name="rescanDrives">Whether drives should be rescanned or not</param>
        public async void InitializeUIValues(bool removeEventHandlers, bool rescanDrives)
        {
            // Disable the dumping button
            this.Parent.StartStopButton.IsEnabled = false;

            // Safely uncheck the parameters box, just in case
            if (this.Parent.EnableParametersCheckBox.IsChecked == true)
            {
                this.Parent.EnableParametersCheckBox.Checked -= Parent.EnableParametersCheckBoxClick;
                this.Parent.EnableParametersCheckBox.IsChecked = false;
                this.Parent.ParametersTextBox.IsEnabled = false;
                this.Parent.EnableParametersCheckBox.Checked += Parent.EnableParametersCheckBoxClick;
            }

            // Set the UI color scheme according to the options
            if (this.Options.EnableDarkMode)
                EnableDarkMode();
            else
                EnableLightMode();

            // Force the UI to reload after applying the theme
            this.Parent.UpdateLayout();

            // Remove event handlers to ensure ordering
            if (removeEventHandlers)
                DisableEventHandlers();

            // Populate the list of drives and determine the system
            if (rescanDrives)
            {
                this.Status = "Creating drive list, please wait!";
                await this.Parent.Dispatcher.InvokeAsync(() => PopulateDrives());
            }
            else
            {
                await this.Parent.Dispatcher.InvokeAsync(() => DetermineSystemType());
            }

            // Determine current media type, if possible
            await this.Parent.Dispatcher.InvokeAsync(() => PopulateMediaType());
            CacheCurrentDiscType();
            SetCurrentDiscType();

            // Set the dumping program
            await this.Parent.Dispatcher.InvokeAsync(() => PopulateInternalPrograms());

            // Set the initial environment and UI values
            SetSupportedDriveSpeed();
            Env = DetermineEnvironment();
            GetOutputNames(true);
            EnsureDiscInformation();

            // Enable event handlers
            EnableEventHandlers();

            // Enable the dumping button, if necessary
            this.Parent.StartStopButton.IsEnabled = ShouldEnableDumpingButton();
        }

        /// <summary>
        /// Performs a fast update of the output path while skipping disc checks
        /// </summary>
        /// <param name="removeEventHandlers">Whether event handlers need to be removed first</param>
        public void FastUpdateLabel(bool removeEventHandlers)
        {
            // Disable the dumping button
            this.Parent.StartStopButton.IsEnabled = false;

            // Safely uncheck the parameters box, just in case
            if (this.Parent.EnableParametersCheckBox.IsChecked == true)
            {
                this.Parent.EnableParametersCheckBox.Checked -= Parent.EnableParametersCheckBoxClick;
                this.Parent.EnableParametersCheckBox.IsChecked = false;
                this.Parent.ParametersTextBox.IsEnabled = false;
                this.Parent.EnableParametersCheckBox.Checked += Parent.EnableParametersCheckBoxClick;
            }

            // Remove event handlers to ensure ordering
            if (removeEventHandlers)
                DisableEventHandlers();

            // Refresh the drive info
            this.CurrentDrive?.RefreshDrive();

            // Set the initial environment and UI values
            Env = DetermineEnvironment();
            GetOutputNames(true);
            EnsureDiscInformation();

            // Enable event handlers
            EnableEventHandlers();

            // Enable the dumping button, if necessary
            this.Parent.StartStopButton.IsEnabled = ShouldEnableDumpingButton();
        }

        /// <summary>
        /// Enable all textbox and combobox event handlers
        /// </summary>
        private void EnableEventHandlers()
        {
            CanExecuteSelectionChanged = true;
        }

        /// <summary>
        /// Disable all textbox and combobox event handlers
        /// </summary>
        private void DisableEventHandlers()
        {
            CanExecuteSelectionChanged = false;
        }

        /// <summary>
        /// Recolor all UI elements for light mode
        /// </summary>
        private static void EnableLightMode()
        {
            var theme = new LightModeTheme();
            theme.Apply();
        }

        /// <summary>
        /// Recolor all UI elements for dark mode
        /// </summary>
        private static void EnableDarkMode()
        {
            var theme = new DarkModeTheme();
            theme.Apply();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Browse for an output file path
        /// </summary>
        private void BrowseFile()
        {
            // Get the current path, if possible
            string currentPath = this.OutputPath;
            if (string.IsNullOrWhiteSpace(currentPath))
                currentPath = Path.Combine(this.Options.DefaultOutputPath, "track.bin");
            if (string.IsNullOrWhiteSpace(currentPath))
                currentPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "track.bin");

            // Get the full path
            currentPath = Path.GetFullPath(currentPath);

            // Get the directory
            string directory = Path.GetDirectoryName(currentPath);

            // Get the filename
            string filename = Path.GetFileName(currentPath);

            WinForms.FileDialog fileDialog = new WinForms.SaveFileDialog
            {
                FileName = filename,
                InitialDirectory = directory,
            };
            WinForms.DialogResult result = fileDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                this.OutputPath = fileDialog.FileName;
            }
        }

        /// <summary>
        /// Cache the current disc type to internal variable
        /// </summary>
        private void CacheCurrentDiscType()
        {
            // If the selected item is invalid, we just skip
            if (this.CurrentDrive == null)
                return;

            // Get reasonable default values based on the current system
            MediaType? defaultMediaType = this.CurrentSystem.MediaTypes().FirstOrDefault() ?? MediaType.CDROM;
            if (defaultMediaType == MediaType.NONE)
                defaultMediaType = MediaType.CDROM;

            // If we're skipping detection, set the default value
            if (this.Options.SkipMediaTypeDetection)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"Media type detection disabled, defaulting to {defaultMediaType.LongName()}.");
                CurrentMediaType = defaultMediaType;
            }
            // If the drive is marked active, try to read from it
            else if (this.CurrentDrive.MarkedActive)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLog($"Trying to detect media type for drive {this.CurrentDrive.Letter} [{this.CurrentDrive.DriveFormat}] using size and filesystem.. ");
                (MediaType? detectedMediaType, string errorMessage) = this.CurrentDrive.GetMediaType(this.CurrentSystem);

                // If we got an error message, post it to the log
                if (errorMessage != null && this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"Message from detecting media type: {errorMessage}");

                // If we got either an error or no media, default to the current System default
                if (detectedMediaType == null)
                {
                    if (this.Options.VerboseLogging)
                        this.Logger.VerboseLogLn($"Unable to detect, defaulting to {defaultMediaType.LongName()}.");
                    CurrentMediaType = defaultMediaType;
                }
                else
                {
                    if (this.Options.VerboseLogging)
                        this.Logger.VerboseLogLn($"Detected {detectedMediaType.LongName()}.");
                    CurrentMediaType = detectedMediaType;
                }
            }

            // All other cases, just use the default
            else
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"Drive marked as empty, defaulting to {defaultMediaType.LongName()}.");
                CurrentMediaType = defaultMediaType;
            }

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Create a DumpEnvironment with all current settings
        /// </summary>
        /// <returns>Filled DumpEnvironment this.Parent</returns>
        private DumpEnvironment DetermineEnvironment()
        {
            return new DumpEnvironment(
                this.Options,
                this.OutputPath,
                this.CurrentDrive,
                this.CurrentSystem,
                this.CurrentMediaType,
                this.CurrentProgram,
                this.Parameters);
        }

        /// <summary>
        /// Determine and set the current system type, if allowed
        /// </summary>
        private void DetermineSystemType()
        {
            if (Drives == null || Drives.Count == 0 || this.CurrentDrive == null)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn("Skipping system type detection because no valid drives found!");
            }
            else if (this.CurrentDrive?.MarkedActive != true)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn("Skipping system type detection because drive not marked as active!");
            }
            else if (!this.Options.SkipSystemDetection)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLog($"Trying to detect system for drive {this.CurrentDrive.Letter}.. ");
                var currentSystem = this.CurrentDrive?.GetRedumpSystem(this.Options.DefaultSystem) ?? this.Options.DefaultSystem;
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn(currentSystem == null ? "unable to detect." : ($"detected {currentSystem.LongName()}."));

                if (currentSystem != null)
                {
                    int sysIndex = Systems.FindIndex(s => s == currentSystem);
                    this.Parent.SystemTypeComboBox.SelectedIndex = sysIndex;
                }
            }
            else if (this.Options.SkipSystemDetection && this.Options.DefaultSystem != null)
            {
                var currentSystem = this.Options.DefaultSystem;
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"System detection disabled, setting to default of {currentSystem.LongName()}.");
                int sysIndex = Systems.FindIndex(s => s == currentSystem);
                this.Parent.SystemTypeComboBox.SelectedIndex = sysIndex;
            }

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Ensure information is consistent with the currently selected disc type
        /// </summary>
        public void EnsureDiscInformation()
        {
            // Get the current environment information
            Env = DetermineEnvironment();

            // Get the status to write out
            Result result = Tools.GetSupportStatus(Env.System, Env.Type);
            this.Status = result.Message;

            // Set the index for the current disc type
            SetCurrentDiscType();

            // Enable or disable the button
            this.Parent.StartStopButton.IsEnabled = result && ShouldEnableDumpingButton();

            // If we're in a type that doesn't support drive speeds
            this.Parent.DriveSpeedComboBox.IsEnabled = Env.Type.DoesSupportDriveSpeed();

            // If input params are not enabled, generate the full parameters from the environment
            if (!this.Parent.ParametersTextBox.IsEnabled)
            {
                string generated = Env.GetFullParameters((int?)this.Parent.DriveSpeedComboBox.SelectedItem);
                if (generated != null)
                    this.Parameters = generated;
            }

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Get the default output directory name from the currently selected drive
        /// </summary>
        /// <param name="driveChanged">Force an updated name if the drive letter changes</param>
        public void GetOutputNames(bool driveChanged)
        {
            if (Drives == null || Drives.Count == 0 || this.CurrentDrive == null)
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLog("Skipping output name building because no valid drives found!");
                return;
            }

            // Get the extension for the file for the next two statements
            string extension = Env?.Parameters?.GetDefaultExtension(this.CurrentMediaType);

            // Set the output filename, if it's not already
            if (string.IsNullOrEmpty(this.OutputPath))
            {
                string label = this.CurrentDrive?.FormattedVolumeLabel ?? this.CurrentSystem.LongName();
                string directory = this.Options.DefaultOutputPath;
                string filename = $"{label}{extension ?? ".bin"}";

                // If the path ends with the label already
                if (directory.EndsWith(label, StringComparison.OrdinalIgnoreCase))
                    directory = Path.GetDirectoryName(directory);

                this.OutputPath = Path.Combine(directory, label, filename);
            }

            // Set the output filename, if we changed drives
            else if (driveChanged)
            {
                string label = this.CurrentDrive?.FormattedVolumeLabel ?? this.CurrentSystem.LongName();
                string oldPath = InfoTool.NormalizeOutputPaths(this.OutputPath, false);
                string oldFilename = Path.GetFileNameWithoutExtension(oldPath);
                string directory = Path.GetDirectoryName(oldPath);
                string filename = $"{label}{extension ?? ".bin"}";

                // If the previous path included the label
                if (directory.EndsWith(oldFilename, StringComparison.OrdinalIgnoreCase))
                    directory = Path.GetDirectoryName(directory);

                // If the path ends with the label already
                if (directory.EndsWith(label, StringComparison.OrdinalIgnoreCase))
                    directory = Path.GetDirectoryName(directory);

                this.OutputPath = Path.Combine(directory, label, filename);
            }

            // Otherwise, reset the extension of the currently set path
            else
            {
                string oldPath = InfoTool.NormalizeOutputPaths(this.OutputPath, false);
                string filename = Path.GetFileNameWithoutExtension(oldPath);
                string directory = Path.GetDirectoryName(oldPath);
                filename = $"{filename}{extension ?? ".bin"}";

                this.OutputPath = Path.Combine(directory, filename);
            }

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Process the current custom parameters back into UI values
        /// </summary>
        public void ProcessCustomParameters()
        {
            Env.SetParameters(this.Parameters);
            if (Env.Parameters == null)
                return;

            // Catch this in case there's an input path issue
            try
            {
                int driveIndex = Drives.Select(d => d.Letter).ToList().IndexOf(Env.Parameters.InputPath[0]);
                this.CurrentDrive = (driveIndex != -1 ? Drives[driveIndex] : Drives[0]);
            }
            catch { }

            int driveSpeed = Env.Parameters.Speed ?? -1;
            if (driveSpeed > 0)
                this.DriveSpeed = driveSpeed;
            else
                Env.Parameters.Speed = this.DriveSpeed;

            // Disable change handling
            DisableEventHandlers();

            this.OutputPath = InfoTool.NormalizeOutputPaths(Env.Parameters.OutputPath, true);

            MediaType? mediaType = Env.Parameters.GetMediaType();
            int mediaTypeIndex = MediaTypes.FindIndex(m => m == mediaType);
            this.CurrentMediaType = (mediaTypeIndex > -1 ? MediaTypes[mediaTypeIndex] : MediaTypes[0]);

            // Reenable change handling
            EnableEventHandlers();
        }

        /// <summary>
        /// Scan and show copy protection for the current disc
        /// </summary>
        public async void ScanAndShowProtection()
        {
            // Determine current environment, just in case
            if (Env == null)
                Env = DetermineEnvironment();

            // Pull the drive letter from the UI directly, just in case
            if (this.CurrentDrive != null && this.CurrentDrive.Letter != default(char))
            {
                if (this.Options.VerboseLogging)
                    this.Logger.VerboseLogLn($"Scanning for copy protection in {this.CurrentDrive.Letter}");

                var tempContent = this.Status;
                this.Status = "Scanning for copy protection... this might take a while!";
                this.Parent.StartStopButton.IsEnabled = false;
                this.Parent.MediaScanButton.IsEnabled = false;
                this.Parent.UpdateVolumeLabel.IsEnabled = false;
                this.Parent.CopyProtectScanButton.IsEnabled = false;

                var progress = new Progress<ProtectionProgress>();
                progress.ProgressChanged += ProgressUpdated;
                (var protections, string error) = await Protection.RunProtectionScanOnPath(this.CurrentDrive.Letter + ":\\", this.Options, progress);
                string output = Protection.FormatProtections(protections);

                // If SmartE is detected on the current disc, remove `/sf` from the flags for DIC only -- Disabled until further notice
                //if (Env.InternalProgram == InternalProgram.DiscImageCreator && output.Contains("SmartE"))
                //{
                //    ((Modules.DiscImageCreator.Parameters)Env.Parameters)[Modules.DiscImageCreator.FlagStrings.ScanFileProtect] = false;
                //    if (this.Options.VerboseLogging)
                //        this.Logger.VerboseLogLn($"SmartE detected, removing {Modules.DiscImageCreator.FlagStrings.ScanFileProtect} from parameters");
                //}

                if (!this.Parent.LogPanel.IsExpanded)
                {
                    if (string.IsNullOrEmpty(error))
                        CustomMessageBox.Show(output, "Detected Protection(s)", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        CustomMessageBox.Show("An exception occurred, see the log for details", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (string.IsNullOrEmpty(error))
                    this.Logger.LogLn($"Detected the following protections in {this.CurrentDrive.Letter}:\r\n\r\n{output}");
                else
                    this.Logger.ErrorLogLn($"Path could not be scanned! Exception information:\r\n\r\n{error}");

                this.Status = tempContent;
                this.Parent.StartStopButton.IsEnabled = ShouldEnableDumpingButton();
                this.Parent.MediaScanButton.IsEnabled = true;
                this.Parent.UpdateVolumeLabel.IsEnabled = true;
                this.Parent.CopyProtectScanButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Set the current disc type in the combo box
        /// </summary>
        private void SetCurrentDiscType()
        {
            // If we have an invalid current type, we don't care and return
            if (CurrentMediaType == null || CurrentMediaType == MediaType.NONE)
                return;

            // Now set the selected item, if possible
            int index = MediaTypes.FindIndex(kvp => kvp.Value == CurrentMediaType);
            if (this.CurrentMediaType != null && index == -1 && this.Options.VerboseLogging)
                this.Logger.VerboseLogLn($"Disc of type '{CurrentMediaType.LongName()}' found, but the current system does not support it!");

            this.CurrentMediaType = (index > -1 ? MediaTypes[index] : MediaTypes[0]);

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Set the drive speed based on reported maximum and user-defined option
        /// </summary>
        public void SetSupportedDriveSpeed()
        {
            // Set the drive speed list that's appropriate
            var values = Interface.GetSpeedsForMediaType(CurrentMediaType);
            this.Parent.DriveSpeedComboBox.ItemsSource = values;
            if (this.Options.VerboseLogging)
                this.Logger.VerboseLogLn($"Supported media speeds: {string.Join(", ", values)}");

            // Set the selected speed
            int speed;
            switch (this.CurrentMediaType)
            {
                case MediaType.CDROM:
                case MediaType.GDROM:
                    speed = this.Options.PreferredDumpSpeedCD;
                    break;
                case MediaType.DVD:
                case MediaType.NintendoGameCubeGameDisc:
                case MediaType.NintendoWiiOpticalDisc:
                    speed = this.Options.PreferredDumpSpeedDVD;
                    break;
                case MediaType.HDDVD:
                    speed = this.Options.PreferredDumpSpeedHDDVD;
                    break;
                case MediaType.BluRay:
                    speed = this.Options.PreferredDumpSpeedBD;
                    break;
                default:
                    speed = this.Options.PreferredDumpSpeedCD;
                    break;
            }

            if (this.Options.VerboseLogging)
                this.Logger.VerboseLogLn($"Setting drive speed to: {speed}");
            this.DriveSpeed = speed;

            // Ensure the UI gets updated
            this.Parent.UpdateLayout();
        }

        /// <summary>
        /// Determine if the dumping button should be enabled
        /// </summary>
        private bool ShouldEnableDumpingButton()
        {
            return Drives != null
                && Drives.Count > 0
                && this.CurrentSystem != null
                && !string.IsNullOrEmpty(this.Parameters);
        }

        /// <summary>
        /// Show the disc information window
        /// </summary>
        /// <param name="submissionInfo">SubmissionInfo object to display and possibly change</param>
        /// <returns>Dialog open result</returns>
        private (bool?, SubmissionInfo) ShowDiscInformationWindow(SubmissionInfo submissionInfo)
        {
            if (this.Options.ShowDiscEjectReminder)
                CustomMessageBox.Show("It is now safe to eject the disc", "Eject", MessageBoxButton.OK, MessageBoxImage.Information);

            var discInformationWindow = new DiscInformationWindow(this.Options, submissionInfo)
            {
                Focusable = true,
                Owner = this.Parent,
                ShowActivated = true,
                ShowInTaskbar = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            bool? result = discInformationWindow.ShowDialog();

            // Copy back the submission info changes, if necessary
            if (result == true)
                submissionInfo = discInformationWindow.DiscInformationViewModel.SubmissionInfo.Clone() as SubmissionInfo;

            return (result, submissionInfo);
        }

        /// <summary>
        /// Begin the dumping process using the given inputs
        /// </summary>
        public async void StartDumping()
        {
            // One last check to determine environment, just in case
            Env = DetermineEnvironment();

            // Force an internal drive refresh in case the user entered things manually
            Env.Drive.RefreshDrive();

            // If still in custom parameter mode, check that users meant to continue or not
            if (this.Parent.EnableParametersCheckBox.IsChecked == true)
            {
                MessageBoxResult result = CustomMessageBox.Show("It looks like you have custom parameters that have not been saved. Would you like to apply those changes before starting to dump?", "Custom Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    this.Parent.EnableParametersCheckBox.IsChecked = false;
                    this.Parent.ParametersTextBox.IsEnabled = false;
                    ProcessCustomParameters();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                // If "No", then we continue with the current known environment
            }

            // Run path adjustments for DiscImageCreator -- Disabled until further notice
            //Env.AdjustPathsForDiscImageCreator();

            try
            {
                // Run pre-dumping validation checks
                if (!ValidateBeforeDumping())
                    return;

                // Disable all UI elements apart from dumping button
                this.Parent.DisableAllUIElements();

                // Refresh the drive, if it wasn't null
                Env.Drive?.RefreshDrive();

                // Output to the label and log
                this.Status = "Starting dumping process... Please wait!";
                this.Logger.LogLn("Starting dumping process... Please wait!");
                if (this.Options.ToolsInSeparateWindow)
                    this.Logger.LogLn("Look for the separate command window for more details");
                else
                    this.Logger.LogLn("Program outputs may be slow to populate in the log window");

                // Get progress indicators
                var resultProgress = new Progress<Result>();
                resultProgress.ProgressChanged += ProgressUpdated;
                var protectionProgress = new Progress<ProtectionProgress>();
                protectionProgress.ProgressChanged += ProgressUpdated;
                Env.ReportStatus += ProgressUpdated;

                // Run the program with the parameters
                Result result = await Env.Run(resultProgress);
                this.Logger.ResetProgressBar();

                // If we didn't execute a dumping command we cannot get submission output
                if (!Env.Parameters.IsDumpingCommand())
                {
                    this.Logger.LogLn("No dumping command was run, submission information will not be gathered.");
                    this.Status = "Execution complete!";

                    // Reset all UI elements
                    this.Parent.EnableAllUIElements();
                    return;
                }

                // Verify dump output and save it
                if (result)
                {
                    result = await Env.VerifyAndSaveDumpOutput(resultProgress, protectionProgress, ShowDiscInformationWindow);
                }
                else
                {
                    this.Logger.ErrorLogLn(result.Message);
                    this.Status = "Execution failed!";
                }
            }
            catch (Exception ex)
            {
                this.Logger.ErrorLogLn(ex.ToString());
                this.Status = "An exception occurred!";
            }
            finally
            {
                // Reset all UI elements
                this.Parent.EnableAllUIElements();
            }
        }

        /// <summary>
        /// Perform validation, including user input, before attempting to start dumping
        /// </summary>
        /// <returns>True if dumping should start, false otherwise</returns>
        private bool ValidateBeforeDumping()
        {
            // Validate that we have an output path of any sort
            if (string.IsNullOrWhiteSpace(Env.OutputPath))
            {
                _ = CustomMessageBox.Show("No output path was provided so dumping cannot continue.", "Missing Path", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.Logger.LogLn("Dumping aborted!");
                return false;
            }

            // Validate that the user explicitly wants an inactive drive to be considered for dumping
            if (!Env.Drive.MarkedActive)
            {
                string message = "The currently selected drive does not appear to contain a disc! "
                    + (!Env.System.DetectedByWindows() ? $"This is normal for {Env.System.LongName()} as the discs may not be readable on Windows. " : string.Empty)
                    + "Do you want to continue?";

                MessageBoxResult mbresult = CustomMessageBox.Show(message, "No Disc Detected", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (mbresult == MessageBoxResult.No || mbresult == MessageBoxResult.Cancel || mbresult == MessageBoxResult.None)
                {
                    this.Logger.LogLn("Dumping aborted!");
                    return false;
                }
            }

            // Pre-split the output path
            string outputDirectory = Path.GetDirectoryName(Env.OutputPath);
            string outputFilename = Path.GetFileName(Env.OutputPath);

            // If a complete dump already exists
            (bool foundFiles, List<string> _) = InfoTool.FoundAllFiles(outputDirectory, outputFilename, Env.Parameters, true);
            if (foundFiles)
            {
                MessageBoxResult mbresult = CustomMessageBox.Show("A complete dump already exists! Are you sure you want to overwrite?", "Overwrite?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (mbresult == MessageBoxResult.No || mbresult == MessageBoxResult.Cancel || mbresult == MessageBoxResult.None)
                {
                    this.Logger.LogLn("Dumping aborted!");
                    return false;
                }
            }

            // Validate that at least some space exists
            // TODO: Tie this to the size of the disc, type of disc, etc.
            string fullPath = Path.GetFullPath(outputDirectory);
            var driveInfo = new DriveInfo(Path.GetPathRoot(fullPath));
            if (driveInfo.AvailableFreeSpace < Math.Pow(2, 30))
            {
                MessageBoxResult mbresult = CustomMessageBox.Show("There is less than 1gb of space left on the target drive. Are you sure you want to continue?", "Low Space", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (mbresult == MessageBoxResult.No || mbresult == MessageBoxResult.Cancel || mbresult == MessageBoxResult.None)
                {
                    this.Logger.LogLn("Dumping aborted!");
                    return false;
                }
            }

            // If nothing above fails, we want to continue
            return true;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler for OptionsWindow OnUpdated event
        /// </summary>
        private void OnOptionsUpdated(object sender, EventArgs e) =>
            UpdateOptions(sender as OptionsWindow);

        #region Progress Reporting

        /// <summary>
        /// Handler for Result ProgressChanged event
        /// </summary>
        private void ProgressUpdated(object sender, string value)
        {
            try
            {
                value = value ?? string.Empty;
                this.Logger.LogLn(value);
            }
            catch { }
        }

        /// <summary>
        /// Handler for Result ProgressChanged event
        /// </summary>
        private void ProgressUpdated(object sender, Result value)
        {
            string message = value?.Message;

            // Update the label with only the first line of output
            if (message.Contains("\n"))
                this.Status = value.Message.Split('\n')[0] + " (See log output)";
            else
                this.Status = value.Message;

            // Log based on success or failure
            if (value && this.Options.VerboseLogging)
                this.Logger.VerboseLogLn(message);
            else if (!value)
                this.Logger.ErrorLogLn(message);
        }

        /// <summary>
        /// Handler for ProtectionProgress ProgressChanged event
        /// </summary>
        private void ProgressUpdated(object sender, ProtectionProgress value)
        {
            string message = $"{value.Percentage * 100:N2}%: {value.Filename} - {value.Protection}";
            this.Status = message;
            if (this.Options.VerboseLogging)
                this.Logger.VerboseLogLn(message);
        }

        #endregion

        #endregion // Event Handlers
    }
}
