namespace MPF.ExecutionContexts.Data
{
    /// <summary>
    /// Represents a boolean flag with an optional trailing value
    /// </summary>
    public class BooleanInput : Input<bool?>
    {
        #region Constructors

        /// <inheritdoc/>
        public BooleanInput(string name)
            : base(name) { }

        /// <inheritdoc/>
        public BooleanInput(string name, bool required)
            : base(name, required) { }

        /// <inheritdoc/>
        public BooleanInput(string shortName, string longName)
            : base(shortName, longName) { }

        /// <inheritdoc/>
        public BooleanInput(string shortName, string longName, bool required)
            : base(shortName, longName, required) { }

        #endregion

        /// <inheritdoc/>
        public override bool Process(string[] parts, ref int index)
        {
            // Check the parts array
            if (parts.Length == 0)
                return false;

            // Check the index
            if (index < 0 || index >= parts.Length)
                return false;

            // Check the name
            if (parts[index] != Name && (_longName != null && parts[index] != _longName))
                return false;

            // Ensure the value exists
            if (!DoesExist(parts, index + 1))
            {
                Value = _required ? null : true;
                return !_required;
            }

            // If the next value is valid
            if (!bool.TryParse(parts[index + 1], out bool value))
            {
                Value = _required ? null : true;
                return !_required;
            }

            index++;
            return value;
        }
    }
}