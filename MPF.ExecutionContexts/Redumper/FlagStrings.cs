namespace MPF.ExecutionContexts.Redumper
{
    /// <summary>
    /// Dumping flags for Redumper
    /// </summary>
    public static class FlagStrings
    {
        // General
        public const string HelpLong = "--help";
        public const string HelpShort = "-h";
        public const string Version = "--version";
        public const string Verbose = "--verbose";
        public const string AutoEject = "--auto-eject";
        public const string Debug = "--debug";
        public const string Drive = "--drive";
        public const string Speed = "--speed";
        public const string Retries = "--retries";
        public const string ImagePath = "--image-path";
        public const string ImageName = "--image-name";
        public const string Overwrite = "--overwrite";

        // Drive Configuration
        public const string DriveType = "--drive-type";
        public const string DriveReadOffset = "--drive-read-offset";
        public const string DriveC2Shift = "--drive-c2-shift";
        public const string DrivePregapStart = "--drive-pregap-start";
        public const string DriveReadMethod = "--drive-read-method";
        public const string DriveSectorOrder = "--drive-sector-order";

        // Drive Specific
        public const string PlextorSkipLeadin = "--plextor-skip-leadin";
        public const string PlextorLeadinRetries = "--plextor-leadin-retries";
        public const string AsusSkipLeadout = "--asus-skip-leadout";

        // Offset
        public const string ForceOffset = "--force-offset";
        public const string AudioSilenceThreshold = "--audio-silence-threshold";
        public const string CorrectOffsetShift = "--correct-offset-shift";
        public const string OffsetShiftRelocate = "--offset-shift-relocate";

        // Split
        public const string ForceSplit = "--force-split";
        public const string LeaveUnchanged = "--leave-unchanged";
        public const string ForceQTOC = "--force-qtoc";
        public const string SkipFill = "--skip-fill";
        public const string ISO9660Trim = "--iso9660-trim";

        // Miscellaneous
        public const string LBAStart = "--lba-start";
        public const string LBAEnd = "--lba-end";
        public const string RefineSubchannel = "--refine-subchannel";
        public const string Skip = "--skip";
        public const string DumpWriteOffset = "--dump-write-offset";
        public const string DumpReadSize = "--dump-read-size";
        public const string OverreadLeadout = "--overread-leadout";
        public const string ForceUnscrambled = "--force-unscrambled";
        public const string LegacySubs = "--legacy-subs";
        public const string DisableCDText = "--disable-cdtext";
    }
}