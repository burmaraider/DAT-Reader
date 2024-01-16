// IniConfig.cs
// Configuration struct to use with ini files (how to parse them and custom objects parser).
// Author: Ronen Ness.
// Date: 10/2020
using System;
using System.Collections.Generic;

namespace Sini
{
    /// <summary>
    /// Configuration for parsing ini files.
    /// </summary>
    public struct IniConfig
    {
        /// <summary>
        /// Characters used to begin comments.
        /// </summary>
        public char[] CommentCharacters;

        /// <summary>
        /// If true, allow comments to appear after the value segment.
        /// If false, will only allow whole-line comments.
        /// </summary>
        public bool AllowCommentsAfterValue;

        /// <summary>
        /// If true, will allow adding sections with square brackets.
        /// </summary>
        public bool AllowSections;

        /// <summary>
        /// Character used to separate between the key and the value.
        /// </summary>
        public char Delimiter;

        /// <summary>
        /// Regex used to validate keys and section names.
        /// Set to null to skip validations.
        /// </summary>
        public string KeyValidationRegex;

        /// <summary>
        /// Words to parse as 'true' when trying to parse boolean values.
        /// </summary>
        public HashSet<string> BoolPositiveValues;

        /// <summary>
        /// Words to parse as 'false' when trying to parse boolean values.
        /// </summary>
        public HashSet<string> BoolNegativeValues;

        /// <summary>
        /// If true, will always lowercase boolean values before parsing them.
        /// </summary>
        public bool LowercaseBoolValues;

        /// <summary>
        /// If not null or empty, will allow to break string values into multiple lines by adding this sequence at the end of the line to break.
        /// For example, if the value is \, every value ending with \ will continue to the following line. 
        /// </summary>
        public string MultilineContinuation;

        /// <summary>
        /// Custom parsing methods for fetching custom types.
        /// </summary>
        public Dictionary<Type, Func<string, object>> CustomParsers;

        /// <summary>
        /// Custom serializing methods for serializing custom types.
        /// </summary>
        public Dictionary<Type, Func<object, string>> CustomSerializers;

        /// <summary>
        /// Characters to use for new lines when writing the ini file.
        /// </summary>
        public string NewLine;

        /// <summary>
        /// Create the ini configuration with default params.
        /// </summary>
        public IniConfig(string temp ="")
        {
            CommentCharacters = new char[] { '#', ';', '*', '/' };
            AllowCommentsAfterValue = true;
            AllowSections = true;
            Delimiter = '=';
            KeyValidationRegex = @"^[a-zA-Z_\.0-9]+$";
            BoolPositiveValues = new HashSet<string>(new string[] { "1", "true", "yes", "on" });
            BoolNegativeValues = new HashSet<string>(new string[] { "0", "false", "no", "off" });
            LowercaseBoolValues = true;
            NewLine = "\n";
            MultilineContinuation = "\\";
            CustomParsers = new Dictionary<Type, Func<string, object>>();
            CustomSerializers = new Dictionary<Type, Func<object, string>>();
        }
    }
    
}
