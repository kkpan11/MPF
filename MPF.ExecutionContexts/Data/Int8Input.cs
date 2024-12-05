using System.Globalization;

namespace MPF.ExecutionContexts.Data
{
    /// <summary>
    /// Represents an Int8 flag with an optional trailing value
    /// </summary>
    public class Int8Input : Input<sbyte?>
    {
        #region Constructors

        /// <inheritdoc/>
        public Int8Input(string name)
            : base(name) { }

        /// <inheritdoc/>
        public Int8Input(string name, bool required)
            : base(name, required) { }

        /// <inheritdoc/>
        public Int8Input(string shortName, string longName)
            : base(shortName, longName) { }

        /// <inheritdoc/>
        public Int8Input(string shortName, string longName, bool required)
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
                Value = _required ? null : sbyte.MinValue;
                return !_required;
            }

            // If the next value is valid
            if (sbyte.TryParse(parts[index + 1], out sbyte value))
            {
                index++;
                Value = value;
                return true;
            }

            // Try to process as a formatted string
            string baseVal = ExtractFactorFromValue(parts[index + 1], out long factor);
            if (sbyte.TryParse(baseVal, out value))
            {
                index++;
                Value = (sbyte)(value * factor);
                return true;
            }

            // Try to process as a hex string
            string hexValue = RemoveHexIdentifier(baseVal);
            if (sbyte.TryParse(hexValue, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
            {
                index++;
                Value = (sbyte)(value * factor);
                return true;
            }

            // Return value based on required flag
            Value = _required ? null : sbyte.MinValue;
            return !_required;
        }
    }
}