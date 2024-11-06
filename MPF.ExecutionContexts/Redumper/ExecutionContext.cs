using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SabreTools.RedumpLib.Data;

namespace MPF.ExecutionContexts.Redumper
{
    /// <summary>
    /// Represents a generic set of Redumper parameters
    /// </summary>
    public sealed class ExecutionContext : BaseExecutionContext
    {
        #region Generic Dumping Information

        /// <inheritdoc/>
        public override string? InputPath => DriveValue?.Trim('"');

        /// <inheritdoc/>
        public override string? OutputPath => Path.Combine(
                ImagePathValue?.Trim('"') ?? string.Empty,
                ImageNameValue?.Trim('"') ?? string.Empty)
            + GetDefaultExtension(this.Type);

        /// <inheritdoc/>
        public override int? Speed => SpeedValue;

        #endregion

        #region Flag Values

        /// <summary>
        /// List of all modes being run
        /// </summary>
        public List<string>? ModeValues { get; set; }

        #region General

        /// <summary>
        /// Drive to use, first available drive with disc, if not provided
        /// </summary>
        public string? DriveValue { get; set; }

        /// <summary>
        /// Drive read speed, optimal drive speed will be used if not provided
        /// </summary>
        public int? SpeedValue { get; set; }

        /// <summary>
        /// Number of sector retries in case of SCSI/C2 error (default: 0)
        /// </summary>
        public int? RetriesValue { get; set; }

        /// <summary>
        /// Dump files base directory
        /// </summary>
        public string? ImagePathValue { get; set; }

        /// <summary>
        /// Dump files prefix, autogenerated in dump mode, if not provided
        /// </summary>
        public string? ImageNameValue { get; set; }

        #endregion

        #region Drive Configuration

        /// <summary>
        /// Override drive type, possible values: GENERIC, PLEXTOR, LG_ASUS
        /// </summary>
        public string? DriveTypeValue { get; set; }

        /// <summary>
        /// Override drive read offset
        /// </summary>
        public int? DriveReadOffsetValue { get; set; }

        /// <summary>
        /// Override drive C2 shift
        /// </summary>
        public int? DriveC2ShiftValue { get; set; }

        /// <summary>
        /// Override drive pre-gap start LBA
        /// </summary>
        public int? DrivePregapStartValue { get; set; }

        /// <summary>
        /// Override drive read method, possible values: BE, D8, BE_CDDA
        /// </summary>
        public string? DriveReadMethodValue { get; set; }

        /// <summary>
        /// Override drive sector order, possible values: DATA_C2_SUB, DATA_SUB_C2
        /// </summary>
        public string? DriveSectorOrderValue { get; set; }

        #endregion

        #region Offset

        /// <summary>
        /// Override offset autodetection and use supplied value
        /// </summary>
        public int? ForceOffsetValue { get; set; }

        /// <summary>
        /// Maximum absolute sample value to treat it as silence (default: 32)
        /// </summary>
        public int? AudioSilenceThresholdValue { get; set; }

        #endregion

        #region Split

        /// <summary>
        /// Fill byte value for skipped sectors (default: 0x55)
        /// </summary>
        public byte? SkipFillValue { get; set; }

        #endregion

        #region Miscellaneous

        /// <summary>
        /// LBA to start dumping from
        /// </summary>
        public int? LBAStartValue { get; set; }

        /// <summary>
        /// LBA to stop dumping at (everything before the value), useful for discs with fake TOC
        /// </summary>
        public int? LBAEndValue { get; set; }

        /// <summary>
        /// LBA ranges of sectors to skip
        /// </summary>
        public string? SkipValue { get; set; }

        /// <summary>
        /// Write offset for dumps when reading as data
        /// </summary>
        public int? DumpWriteOffsetValue { get; set; }

        /// <summary>
        /// Number of sectors to read at once on initial dump, DVD only (Default 32)
        /// </summary>
        public int? DumpReadSizeValue { get; set; }

        /// <summary>
        /// Maximum number of lead-in retries per session (Default 4)
        /// </summary>
        public int? PlextorLeadinRetriesValue { get; set; }

        #endregion

        #endregion

        /// <inheritdoc/>
        public ExecutionContext(string? parameters) : base(parameters) { }

        /// <inheritdoc/>
        public ExecutionContext(RedumpSystem? system, MediaType? type, string? drivePath, string filename, int? driveSpeed, Dictionary<string, string?> options)
            : base(system, type, drivePath, filename, driveSpeed, options)
        {
        }

        #region BaseExecutionContext Implementations

        /// <inheritdoc/>
        /// <remarks>Command support is irrelevant for redumper</remarks>
        public override Dictionary<string, List<string>> GetCommandSupport()
        {
            return new Dictionary<string, List<string>>()
            {
                [CommandStrings.NONE] =
                [
                    // General
                    FlagStrings.HelpLong,
                    FlagStrings.HelpShort,
                    FlagStrings.Version,
                    FlagStrings.Verbose,
                    FlagStrings.AutoEject,
                    FlagStrings.Debug,
                    FlagStrings.Drive,
                    FlagStrings.Speed,
                    FlagStrings.Retries,
                    FlagStrings.ImagePath,
                    FlagStrings.ImageName,
                    FlagStrings.Overwrite,

                    // Drive Configuration
                    FlagStrings.DriveType,
                    FlagStrings.DriveReadOffset,
                    FlagStrings.DriveC2Shift,
                    FlagStrings.DrivePregapStart,
                    FlagStrings.DriveReadMethod,
                    FlagStrings.DriveSectorOrder,

                    // Drive Specific
                    FlagStrings.PlextorSkipLeadin,
                    FlagStrings.PlextorLeadinRetries,
                    FlagStrings.AsusSkipLeadout,

                    // Offset
                    FlagStrings.ForceOffset,
                    FlagStrings.AudioSilenceThreshold,
                    FlagStrings.CorrectOffsetShift,
                    FlagStrings.OffsetShiftRelocate,

                    // Split
                    FlagStrings.ForceSplit,
                    FlagStrings.LeaveUnchanged,
                    FlagStrings.ForceQTOC,
                    FlagStrings.SkipFill,
                    FlagStrings.ISO9660Trim,

                    // Miscellaneous
                    FlagStrings.LBAStart,
                    FlagStrings.LBAEnd,
                    FlagStrings.RefineSubchannel,
                    FlagStrings.Skip,
                    FlagStrings.DumpWriteOffset,
                    FlagStrings.DumpReadSize,
                    FlagStrings.OverreadLeadout,
                    FlagStrings.ForceUnscrambled,
                    FlagStrings.LegacySubs,
                    FlagStrings.DisableCDText,
                ],
            };
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Redumper is unique in that the base command can be multiple
        /// modes all listed together. It is also unique in that "all
        /// flags are supported for everything" and it filters out internally
        /// </remarks>
        public override string GenerateParameters()
        {
            var parameters = new List<string>();

            ModeValues ??= [CommandStrings.NONE];

            // Modes
            parameters.AddRange(ModeValues);

            #region General

            // Help
            if (this[FlagStrings.HelpLong] == true)
                parameters.Add(FlagStrings.HelpLong);

            // Version
            if (this[FlagStrings.Version] == true)
                parameters.Add(FlagStrings.Version);

            // Verbose
            if (this[FlagStrings.Verbose] == true)
                parameters.Add(FlagStrings.Verbose);

            // Auto Eject
            if (this[FlagStrings.AutoEject] == true)
                parameters.Add(FlagStrings.AutoEject);

            // Debug
            if (this[FlagStrings.Debug] == true)
                parameters.Add(FlagStrings.Debug);

            // Drive
            if (this[FlagStrings.Drive] == true)
            {
                if (DriveValue != null)
                {
                    if (DriveValue.Contains(' '))
                        parameters.Add($"{FlagStrings.Drive}=\"{DriveValue}\"");
                    else
                        parameters.Add($"{FlagStrings.Drive}={DriveValue}");
                }
            }

            // Speed
            if (this[FlagStrings.Speed] == true)
            {
                if (SpeedValue != null)
                    parameters.Add($"{FlagStrings.Speed}={SpeedValue}");
            }

            // Retries
            if (this[FlagStrings.Retries] == true)
            {
                if (RetriesValue != null)
                    parameters.Add($"{FlagStrings.Retries}={RetriesValue}");
            }

            // Image Path
            if (this[FlagStrings.ImagePath] == true)
            {
                if (ImagePathValue != null)
                    parameters.Add($"{FlagStrings.ImagePath}={ImagePathValue}");
            }

            // Image Name
            if (this[FlagStrings.ImageName] == true)
            {
                if (ImageNameValue != null)
                    parameters.Add($"{FlagStrings.ImageName}={ImageNameValue}");
            }

            // Overwrite
            if (this[FlagStrings.Overwrite] == true)
                parameters.Add(FlagStrings.Overwrite);

            #endregion

            #region Drive Configuration

            // Drive Type
            if (this[FlagStrings.DriveType] == true)
            {
                if (DriveTypeValue != null)
                    parameters.Add($"{FlagStrings.DriveType}={DriveTypeValue}");
            }

            // Drive Read Offset
            if (this[FlagStrings.DriveReadOffset] == true)
            {
                if (DriveReadOffsetValue != null)
                    parameters.Add($"{FlagStrings.DriveReadOffset}={DriveReadOffsetValue}");
            }

            // Drive C2 Shift
            if (this[FlagStrings.DriveC2Shift] == true)
            {
                if (DriveC2ShiftValue != null)
                    parameters.Add($"{FlagStrings.DriveC2Shift}={DriveC2ShiftValue}");
            }

            // Drive Pregap Start
            if (this[FlagStrings.DrivePregapStart] == true)
            {
                if (DrivePregapStartValue != null)
                    parameters.Add($"{FlagStrings.DrivePregapStart}={DrivePregapStartValue}");
            }

            // Drive Read Method
            if (this[FlagStrings.DriveReadMethod] == true)
            {
                if (DriveReadMethodValue != null)
                    parameters.Add($"{FlagStrings.DriveReadMethod}={DriveReadMethodValue}");
            }

            // Drive Sector Order
            if (this[FlagStrings.DriveSectorOrder] == true)
            {
                if (DriveSectorOrderValue != null)
                    parameters.Add($"{FlagStrings.DriveSectorOrder}={DriveSectorOrderValue}");
            }

            #endregion

            #region Drive Specific

            // Plextor Leadin Skip
            if (this[FlagStrings.PlextorSkipLeadin] == true)
                parameters.Add(FlagStrings.PlextorSkipLeadin);

            // Plextor Leadin Retries
            if (this[FlagStrings.PlextorLeadinRetries] == true)
            {
                if (PlextorLeadinRetriesValue != null)
                    parameters.Add($"{FlagStrings.PlextorLeadinRetries}={PlextorLeadinRetriesValue}");
            }

            // Asus Skip Leadout
            if (this[FlagStrings.AsusSkipLeadout] == true)
                parameters.Add(FlagStrings.AsusSkipLeadout);

            #endregion

            #region Offset

            // Force Offset
            if (this[FlagStrings.ForceOffset] == true)
            {
                if (ForceOffsetValue != null)
                    parameters.Add($"{FlagStrings.ForceOffset}={ForceOffsetValue}");
            }

            // Audio Silence Threshold
            if (this[FlagStrings.AudioSilenceThreshold] == true)
            {
                if (AudioSilenceThresholdValue != null)
                    parameters.Add($"{FlagStrings.AudioSilenceThreshold}={AudioSilenceThresholdValue}");
            }

            // Correct Offset Shift
            if (this[FlagStrings.CorrectOffsetShift] == true)
                parameters.Add(FlagStrings.CorrectOffsetShift);

            // Offset Shift Relocate
            if (this[FlagStrings.OffsetShiftRelocate] == true)
                parameters.Add(FlagStrings.OffsetShiftRelocate);

            #endregion

            #region Split

            // Force Split
            if (this[FlagStrings.ForceSplit] == true)
                parameters.Add(FlagStrings.ForceSplit);

            // Leave Unchanged
            if (this[FlagStrings.LeaveUnchanged] == true)
                parameters.Add(FlagStrings.LeaveUnchanged);

            // Force QTOC
            if (this[FlagStrings.ForceQTOC] == true)
                parameters.Add(FlagStrings.ForceQTOC);

            // Skip Fill
            if (this[FlagStrings.SkipFill] == true)
            {
                if (SkipFillValue != null)
                    parameters.Add($"{FlagStrings.SkipFill}={SkipFillValue:x}");
            }

            // ISO9660 Trim
            if (this[FlagStrings.ISO9660Trim] == true)
                parameters.Add(FlagStrings.ISO9660Trim);

            #endregion

            #region Miscellaneous

            // LBA Start
            if (this[FlagStrings.LBAStart] == true)
            {
                if (LBAStartValue != null)
                    parameters.Add($"{FlagStrings.LBAStart}={LBAStartValue}");
            }

            // LBA End
            if (this[FlagStrings.LBAEnd] == true)
            {
                if (LBAEndValue != null)
                    parameters.Add($"{FlagStrings.LBAEnd}={LBAEndValue}");
            }

            // Refine Subchannel
            if (this[FlagStrings.RefineSubchannel] == true)
                parameters.Add(FlagStrings.RefineSubchannel);

            // Skip
            if (this[FlagStrings.Skip] == true)
            {
                if (!string.IsNullOrEmpty(SkipValue))
                    parameters.Add($"{FlagStrings.Skip}={SkipValue}");
            }

            // Dump Write Offset
            if (this[FlagStrings.DumpWriteOffset] == true)
            {
                if (DumpWriteOffsetValue != null)
                    parameters.Add($"{FlagStrings.DumpWriteOffset}={DumpWriteOffsetValue}");
            }

            // Dump Read Size
            if (this[FlagStrings.DumpReadSize] == true)
            {
                if (DumpReadSizeValue != null && DumpReadSizeValue > 0)
                    parameters.Add($"{FlagStrings.DumpReadSize}={DumpReadSizeValue}");
            }

            // Overread Leadout
            if (this[FlagStrings.OverreadLeadout] == true)
                parameters.Add(FlagStrings.OverreadLeadout);

            // Force Unscrambled
            if (this[FlagStrings.ForceUnscrambled] == true)
                parameters.Add(FlagStrings.ForceUnscrambled);

            // Legacy Subs
            if (this[FlagStrings.LegacySubs] == true)
                parameters.Add(FlagStrings.LegacySubs);

            // Disable CD Text
            if (this[FlagStrings.DisableCDText] == true)
                parameters.Add(FlagStrings.DisableCDText);

            #endregion

            return string.Join(" ", [.. parameters]);
        }

        /// <inheritdoc/>
        public override string? GetDefaultExtension(MediaType? mediaType) => Converters.Extension(mediaType);

        /// <inheritdoc/>
        public override MediaType? GetMediaType() => null;

        /// <inheritdoc/>
        public override bool IsDumpingCommand()
        {
            return this.BaseCommand == CommandStrings.NONE
                || this.BaseCommand?.Contains(CommandStrings.CD) == true
                || this.BaseCommand?.Contains(CommandStrings.DVD) == true
                || this.BaseCommand?.Contains(CommandStrings.BluRay) == true
                || this.BaseCommand?.Contains(CommandStrings.SACD) == true
                || this.BaseCommand?.Contains(CommandStrings.New) == true
                || this.BaseCommand?.Contains(CommandStrings.Dump) == true
                || this.BaseCommand?.Contains(CommandStrings.DumpNew) == true;
        }

        /// <inheritdoc/>
        protected override void ResetValues()
        {
            BaseCommand = CommandStrings.NONE;

            flags = [];

            // General
            DriveValue = null;
            SpeedValue = null;
            RetriesValue = null;
            ImagePathValue = null;
            ImageNameValue = null;

            // Drive Configuration
            DriveTypeValue = null;
            DriveReadOffsetValue = null;
            DriveC2ShiftValue = null;
            DrivePregapStartValue = null;
            DriveReadMethodValue = null;
            DriveSectorOrderValue = null;

            // Offset
            ForceOffsetValue = null;
            AudioSilenceThresholdValue = null;

            // Split
            SkipFillValue = null;

            // Miscellaneous
            LBAStartValue = null;
            LBAEndValue = null;
            SkipValue = null;
            DumpReadSizeValue = null;
        }

        /// <inheritdoc/>
        protected override void SetDefaultParameters(string? drivePath, string filename, int? driveSpeed, Dictionary<string, string?> options)
        {
            // If we don't have a CD, DVD, HD-DVD, or BD, we can't dump using redumper
            if (this.Type != MediaType.CDROM
                && this.Type != MediaType.DVD
                && this.Type != MediaType.HDDVD
                && this.Type != MediaType.BluRay)
            {
                return;
            }

            BaseCommand = CommandStrings.NONE;
            switch (this.Type)
            {
                case MediaType.CDROM:
                    ModeValues = this.System switch
                    {
                        RedumpSystem.SuperAudioCD => [CommandStrings.SACD],
                        _ => [CommandStrings.CD],
                    };
                    break;
                case MediaType.DVD:
                    ModeValues = [CommandStrings.DVD];
                    break;
                case MediaType.HDDVD: // TODO: Keep in sync if another command string shows up
                    ModeValues = [CommandStrings.DVD];
                    break;
                case MediaType.BluRay:
                    ModeValues = [CommandStrings.BluRay];
                    break;
                default:
                    BaseCommand = null;
                    return;
            }

            this[FlagStrings.Drive] = true;
            DriveValue = drivePath;

            this[FlagStrings.Speed] = true;
            SpeedValue = driveSpeed;

            // Set user-defined options
            if (GetBooleanSetting(options, SettingConstants.EnableVerbose, SettingConstants.EnableVerboseDefault))
                this[FlagStrings.Verbose] = true;
            if (GetBooleanSetting(options, SettingConstants.EnableDebug, SettingConstants.EnableDebugDefault))
                this[FlagStrings.Debug] = true;

            string? readMethod = GetStringSetting(options, SettingConstants.ReadMethod, SettingConstants.ReadMethodDefault);
            
            if (!string.IsNullOrEmpty(readMethod) && readMethod != ReadMethod.NONE.ToString())
            {
                this[FlagStrings.DriveReadMethod] = true;
                DriveReadMethodValue = readMethod;
            }

            string? sectorOrder = GetStringSetting(options, SettingConstants.SectorOrder, SettingConstants.SectorOrderDefault);
            if (!string.IsNullOrEmpty(sectorOrder) && sectorOrder != SectorOrder.NONE.ToString())
            {
                this[FlagStrings.DriveSectorOrder] = true;
                DriveSectorOrderValue = sectorOrder;
            }

            if (GetBooleanSetting(options, SettingConstants.UseGenericDriveType, SettingConstants.UseGenericDriveTypeDefault))
            {
                this[FlagStrings.DriveType] = true;
                DriveTypeValue = "GENERIC";
            }

            // Set the output paths
            if (!string.IsNullOrEmpty(filename))
            {
                var imagePath = Path.GetDirectoryName(filename);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    this[FlagStrings.ImagePath] = true;
                    ImagePathValue = $"\"{imagePath}\"";
                }

                string imageName = Path.GetFileNameWithoutExtension(filename);
                if (!string.IsNullOrEmpty(imageName))
                {
                    this[FlagStrings.ImageName] = true;
                    ImageNameValue = $"\"{imageName}\"";
                }
            }

            this[FlagStrings.Retries] = true;
            RetriesValue = GetInt32Setting(options, SettingConstants.RereadCount, SettingConstants.RereadCountDefault);

            if (GetBooleanSetting(options, SettingConstants.EnableLeadinRetry, SettingConstants.EnableLeadinRetryDefault))
            {
                this[FlagStrings.PlextorLeadinRetries] = true;
                PlextorLeadinRetriesValue = GetInt32Setting(options, SettingConstants.LeadinRetryCount, SettingConstants.LeadinRetryCountDefault);
            }
        }

        /// <inheritdoc/>
        protected override bool ValidateAndSetParameters(string? parameters)
        {
            BaseCommand = CommandStrings.NONE;

            // The string has to be valid by itself first
            if (string.IsNullOrEmpty(parameters))
                return false;

            // Now split the string into parts for easier validation
            // https://stackoverflow.com/questions/14655023/split-a-string-that-has-white-spaces-unless-they-are-enclosed-within-quotes
            parameters = parameters!.Trim();
            List<string> parts = Regex.Matches(parameters, @"([a-zA-Z\-]*=)?[\""].+?[\""]|[^ ]+", RegexOptions.Compiled)
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();

            // Setup the modes
            ModeValues = [];

            // All modes should be cached separately
            int index = 0;
            for (; index < parts.Count; index++)
            {
                // Flag to see if we have a flag
                bool isFlag = false;

                string part = parts[index];
                switch (part)
                {
                    case CommandStrings.CD:
                    case CommandStrings.DVD:
                    case CommandStrings.BluRay:
                    case CommandStrings.SACD:
                    case CommandStrings.New: // Temporary command, to be removed later
                    case CommandStrings.Rings:
                    case CommandStrings.Dump:
                    case CommandStrings.DumpNew: // Temporary command, to be removed later
                    case CommandStrings.Refine:
                    case CommandStrings.RefineNew: // Temporary command, to be removed later
                    case CommandStrings.Verify:
                    case CommandStrings.DVDKey:
                    case CommandStrings.Eject:
                    case CommandStrings.DVDIsoKey:
                    case CommandStrings.Protection:
                    case CommandStrings.Split:
                    case CommandStrings.Hash:
                    case CommandStrings.Info:
                    case CommandStrings.Skeleton:
                    case CommandStrings.Debug:
                    //case CommandStrings.FixMSF:
                        ModeValues.Add(part);
                        break;

                    // Default is either a flag or an invalid mode
                    default:
                        if (part.StartsWith("-"))
                        {
                            isFlag = true;
                            break;
                        }
                        else
                        {
                            return false;
                        }
                }

                // If we had a flag, break out
                if (isFlag)
                    break;
            }

            // Loop through all auxiliary flags, if necessary
            for (int i = index; i < parts.Count; i++)
            {
                // Flag read-out values
                byte? byteValue = null;
                int? intValue = null;
                string? stringValue = null;

                #region General

                // Help
                ProcessFlagParameter(parts, FlagStrings.HelpShort, FlagStrings.HelpLong, ref i);

                // Version
                ProcessFlagParameter(parts, FlagStrings.Version, ref i);

                // Verbose
                ProcessFlagParameter(parts, FlagStrings.Verbose, ref i);

                // Debug
                ProcessFlagParameter(parts, FlagStrings.Debug, ref i);

                // Drive
                stringValue = ProcessStringParameter(parts, FlagStrings.Drive, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    DriveValue = stringValue;

                // Speed
                intValue = ProcessInt32Parameter(parts, FlagStrings.Speed, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    SpeedValue = intValue;

                // Retries
                intValue = ProcessInt32Parameter(parts, FlagStrings.Retries, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    RetriesValue = intValue;

                // Image Path
                stringValue = ProcessStringParameter(parts, FlagStrings.ImagePath, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    ImagePathValue = $"\"{stringValue!.Trim('"')}\"";

                // Image Name
                stringValue = ProcessStringParameter(parts, FlagStrings.ImageName, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    ImageNameValue = $"\"{stringValue!.Trim('"')}\"";

                // Overwrite
                ProcessFlagParameter(parts, FlagStrings.Overwrite, ref i);

                #endregion

                #region Drive Configuration

                // Drive Type
                stringValue = ProcessStringParameter(parts, FlagStrings.DriveType, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    DriveTypeValue = stringValue;

                // Drive Read Offset
                intValue = ProcessInt32Parameter(parts, FlagStrings.DriveReadOffset, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    DriveReadOffsetValue = intValue;

                // Drive C2 Shift
                intValue = ProcessInt32Parameter(parts, FlagStrings.DriveC2Shift, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    DriveC2ShiftValue = intValue;

                // Drive Pregap Start
                intValue = ProcessInt32Parameter(parts, FlagStrings.DrivePregapStart, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    DrivePregapStartValue = intValue;

                // Drive Read Method
                stringValue = ProcessStringParameter(parts, FlagStrings.DriveReadMethod, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    DriveReadMethodValue = stringValue;

                // Drive Sector Order
                stringValue = ProcessStringParameter(parts, FlagStrings.DriveSectorOrder, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    DriveSectorOrderValue = stringValue;

                #endregion

                #region Drive Specific

                // Plextor Skip Leadin
                ProcessFlagParameter(parts, FlagStrings.PlextorSkipLeadin, ref i);

                // Plextor Leadin Retries
                intValue = ProcessInt32Parameter(parts, FlagStrings.PlextorLeadinRetries, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    PlextorLeadinRetriesValue = intValue;

                // Asus Skip Leadout
                ProcessFlagParameter(parts, FlagStrings.AsusSkipLeadout, ref i);

                #endregion

                #region Offset

                // Force Offset
                intValue = ProcessInt32Parameter(parts, FlagStrings.ForceOffset, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    ForceOffsetValue = intValue;

                // Audio Silence Threshold
                intValue = ProcessInt32Parameter(parts, FlagStrings.AudioSilenceThreshold, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    AudioSilenceThresholdValue = intValue;

                // Correct Offset Shift
                ProcessFlagParameter(parts, FlagStrings.CorrectOffsetShift, ref i);

                // Correct Shift Relocate
                ProcessFlagParameter(parts, FlagStrings.OffsetShiftRelocate, ref i);

                #endregion

                #region Split

                // Force Split
                ProcessFlagParameter(parts, FlagStrings.ForceSplit, ref i);

                // Leave Unchanged
                ProcessFlagParameter(parts, FlagStrings.LeaveUnchanged, ref i);

                // Force QTOC
                ProcessFlagParameter(parts, FlagStrings.ForceQTOC, ref i);

                // Skip Fill
                byteValue = ProcessUInt8Parameter(parts, FlagStrings.SkipFill, ref i);
                if (byteValue != null && byteValue != Byte.MinValue)
                    SkipFillValue = byteValue;

                // ISO9660 Trim
                ProcessFlagParameter(parts, FlagStrings.ISO9660Trim, ref i);

                #endregion

                #region Miscellaneous

                // LBA Start
                intValue = ProcessInt32Parameter(parts, FlagStrings.LBAStart, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    LBAStartValue = intValue;

                // LBA End
                intValue = ProcessInt32Parameter(parts, FlagStrings.LBAEnd, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    LBAEndValue = intValue;

                // Refine Subchannel
                ProcessFlagParameter(parts, FlagStrings.RefineSubchannel, ref i);

                // Skip
                stringValue = ProcessStringParameter(parts, FlagStrings.Skip, ref i);
                if (!string.IsNullOrEmpty(stringValue))
                    SkipValue = stringValue;

                // Dump Write Offset
                intValue = ProcessInt32Parameter(parts, FlagStrings.DumpWriteOffset, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    DumpWriteOffsetValue = intValue;

                // Dump Read Size
                intValue = ProcessInt32Parameter(parts, FlagStrings.DumpReadSize, ref i);
                if (intValue != null && intValue != Int32.MinValue)
                    DumpReadSizeValue = intValue;

                // Overread Leadout
                ProcessFlagParameter(parts, FlagStrings.OverreadLeadout, ref i);

                // Force Unscrambled
                ProcessFlagParameter(parts, FlagStrings.ForceUnscrambled, ref i);

                // Legacy Subs
                ProcessFlagParameter(parts, FlagStrings.LegacySubs, ref i);

                // Disable CD Text
                ProcessFlagParameter(parts, FlagStrings.DisableCDText, ref i);

                #endregion
            }

            // If the image name was not set, set it with a default value
            if (string.IsNullOrEmpty(this.ImageNameValue))
                this.ImageNameValue = "track";

            return true;
        }

        #endregion
    }
}
