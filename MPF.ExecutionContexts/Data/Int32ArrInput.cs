using System;
using System.Globalization;
using System.Text;

namespace MPF.ExecutionContexts.Data
{
    /// <summary>
    /// Represents an Int32 flag with an optional trailing value
    /// </summary>
    public class Int32ArrInput : Input<int?[]>
    {
        #region Properties

        /// <summary>
        /// Internal array size
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Indicates a minimum value (inclusive) for the flag
        /// </summary>
        public int? MinValue { get; set; } = null;

        /// <summary>
        /// Indicates a maximum value (inclusive) for the flag
        /// </summary>
        public int? MaxValue { get; set; } = null;

        #endregion

        #region Constructors

        /// <inheritdoc/>
        public Int32ArrInput(string name)
            : base(name) { }

        /// <inheritdoc/>
        public Int32ArrInput(string name, bool required)
            : base(name, required) { }

        /// <inheritdoc/>
        public Int32ArrInput(string shortName, string longName)
            : base(shortName, longName) { }

        /// <inheritdoc/>
        public Int32ArrInput(string shortName, string longName, bool required)
            : base(shortName, longName, required) { }

        /// <inheritdoc/>
        public Int32ArrInput(string[] names)
            : base(names) { }

        /// <inheritdoc/>
        public Int32ArrInput(string[] names, bool required)
            : base(names, required) { }

        #endregion

        /// <inheritdoc/>
        public override string Format(bool useEquals)
        {
            // Do not output if there is no value
            if (Value == null)
                return string.Empty;

            // Build the output format
            var builder = new StringBuilder();

            // Flag name
            builder.Append(Name);

            // Only output separator and value if needed
            if (_required || (!_required && Value != null))
            {
                // Separator
                if (useEquals)
                    builder.Append("=");
                else
                    builder.Append(" ");

                // Value
                int?[] nonNull = Array.FindAll(Value, i => i != null);
                string[] stringValues = Array.ConvertAll(nonNull, i => i.ToString() ?? string.Empty);
                builder.Append(string.Join(" ", stringValues));
            }

            return builder.ToString();
        }

        /// <inheritdoc/>
        public override bool Process(string[] parts, ref int index)
        {
            // Check the parts array
            if (index < 0 || index >= parts.Length)
                return false;

            // Check for space-separated
            string part = parts[index];
            if (part == Name || (_altNames.Length > 0 && Array.FindIndex(_altNames, n => n == part) > -1))
            {
                Value = new int?[Size];
                for (int i = 0; i < Size; i++)
                {
                    // Ensure the value exists
                    if (index + 1 >= parts.Length)
                    {
                        Value[i] = _required ? null : int.MinValue;
                        return !_required;
                    }

                    // If the next value is valid
                    if (ParseValue(parts[index + 1], out int? value) && value != null)
                    {
                        index++;
                        Value[i] = value;
                        continue;
                    }

                    // Return value based on required flag
                    Value[i] = _required ? null : int.MinValue;
                    return !_required;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse a value from a string
        /// </summary>
        private static bool ParseValue(string str, out int? output)
        {
            // If the next value is valid
            if (int.TryParse(str, out int value))
            {
                output = value;
                return true;
            }

            // Try to process as a formatted string
            string baseVal = ExtractFactorFromValue(str, out long factor);
            if (int.TryParse(baseVal, out value))
            {
                output = (int)(value * factor);
                return true;
            }

            // Try to process as a hex string
            string hexValue = RemoveHexIdentifier(baseVal);
            if (int.TryParse(hexValue, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
            {
                output = (int)(value * factor);
                return true;
            }

            // The value could not be parsed
            output = null;
            return false;
        }
    }
}