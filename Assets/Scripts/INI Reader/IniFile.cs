// IniFile.cs
// Implement ini file main wrapper class (most lib logic is here).
// Author: Ronen Ness.
// Date: 10/2020
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Sini
{
    /// <summary>
    /// Implement simple ini file reader.
    /// </summary>
    public class IniFile
    {
        #region Fields

        // sections.
        Dictionary<string, Dictionary<string, string>> _sections = new Dictionary<string, Dictionary<string, string>>();

        // global section.
        Dictionary<string, string> _globalSection = new Dictionary<string, string>();

        // comments
        Dictionary<string, string> _comments = new Dictionary<string, string>();

        // all keys we read. used for validations.
        HashSet<string> _keysAccessed = new HashSet<string>();

        /// <summary>
        /// Default config to use when no config instance is provided.
        /// </summary>
        public static IniConfig DefaultConfig = new IniConfig();

        // current file config.
        IniConfig _config;

        /// <summary>
        /// Ini file path.
        /// </summary>
        public string Path { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create empty ini file.
        /// Use the public static method CreateEmpty() to create empty fils.
        /// </summary>
        internal IniFile()
        {
        }

        /// <summary>
        /// Open and parse the ini file.
        /// </summary>
        /// <param name="path">Path of the ini file.</param>
        /// <param name="config">Configuration on how to parse this ini file, or null to use DefaultConfig.</param>
        public IniFile(string path, IniConfig? config = null)
        {
            // store path
            Path = path;

            // make sure path exists
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Ini file '{path}' not found!");
            }

            // read and parse file
            string[] lines = File.ReadAllLines(path);
            ParseLines(lines, config ?? DefaultConfig);
        }

        /// <summary>
        /// Create ini file instance from array of lines.
        /// </summary>
        /// <param name="fileContent">The lines of the ini file to parse, as a string array.</param>
        /// <param name="config">Configuration on how to parse this ini file, or null to use DefaultConfig.</param>
        public IniFile(string[] fileContent, IniConfig? config = null)
        {
            ParseLines(fileContent, config ?? DefaultConfig);
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parse ini file lines.
        /// </summary>
        /// <param name="lines">The lines of the ini file to parse, as a string array.</param>
        /// <param name="config">Configuration on how to parse this ini file, or null to use DefaultConfig.</param>
        protected void ParseLines(string[] lines, IniConfig config)
        {
            // store config
            _config = config;

            // turn comments to hashset
            HashSet<char> commentsCharacters = new HashSet<char>(config.CommentCharacters);

            // currently active section
            Dictionary<string, string> section = _globalSection;

            // for multiline values
            bool continueMultiline = false;
            string lastKey = null;

            // current comment lines
            List<string> currComments = new List<string>();

            // current section name
            string currSectionName = null;

            // iterate and parse lines
            int lineIndex = 0;
            foreach (string rawline in lines)
            {
                // increase line index for logs
                lineIndex++;

                // trim line
                var line = rawline.Trim();

                // is it a comment line
                var isComment = (line.Length) > 0 && commentsCharacters.Contains(line[0]);

                // skip empty and comment lines
                if (string.IsNullOrEmpty(line) || isComment)
                {
                    // if part of multiline value throw exception
                    if (continueMultiline)
                    {
                        throw new FormatException($"Invalid line {lineIndex} in ini file '{Path}': Cannot have empty line or comment after multiline value (previous line ended with '{config.MultilineContinuation}' which means value should continue to this line).");
                    }

                    // if comment, add to current comments list
                    if (isComment)
                    {
                        currComments.Add(line.Substring(1).Trim());
                    }
                    // if its not a comment, clear whatever comment we might have in cache
                    else
                    {
                        currComments.Clear();
                    }

                    // skip line
                    continue;
                }

                // if we continue previous value, append it
                if (continueMultiline)
                {
                    // get value and check if should continue to another line
                    var lineVal = rawline.Trim();
                    continueMultiline = lineVal.EndsWith(config.MultilineContinuation);

                    // remove multiline trailing character
                    if (continueMultiline)
                    {
                        lineVal = lineVal.Substring(0, lineVal.Length - config.MultilineContinuation.Length).TrimEnd();
                    }

                    // append value and continue
                    section[lastKey] = section[lastKey] + _config.NewLine + lineVal;
                    continue;
                }

                // remove in-line comments
                if (config.AllowCommentsAfterValue)
                {
                    foreach (var commentChar in commentsCharacters)
                    {
                        line = line.Split(commentChar)[0].Trim();
                    }
                }

                // open new section
                if (config.AllowSections)
                {
                    if (line.StartsWith('['))
                    {
                        // make sure section ends with ]
                        if (!line.EndsWith(']')) { throw new FormatException($"Invalid line {lineIndex} in ini file '{Path}': '{rawline}' does not have a closing bracket ] for section."); }

                        // get section name and start feeding section
                        var sectionName = line.TrimStart('[').TrimEnd(']').Trim();
                        if (!_sections.ContainsKey(sectionName))
                        {
                            _sections[sectionName] = new Dictionary<string, string>();
                        }
                        section = _sections[sectionName];
                        currSectionName = sectionName;

                        // add comment to section
                        if (currComments.Count > 0)
                        {
                            SetComment(sectionName, null, String.Join(_config.NewLine, currComments));
                            currComments.Clear();
                        }

                        // validate section key
                        if (!string.IsNullOrEmpty(config.KeyValidationRegex) && !Regex.IsMatch(sectionName, config.KeyValidationRegex))
                        {
                            throw new FormatException($"Invalid line {lineIndex} in ini file '{Path}': '{rawline}' is a section ('{sectionName}') but section name did not pass the regex validation.");
                        }

                        // continue to next line
                        continue;
                    }
                }

                // if got here its not a section - split to key/val with the separation character
                var equalIndex = line.IndexOf(config.Delimiter);
                if (equalIndex == -1)
                {
                    continue;
                    //throw new FormatException($"Invalid line {lineIndex} in ini file '{Path}': '{rawline}' does not have the sign to split between key and value ({config.Delimiter})."); 
                }

                // break into key and value
                var key = line.Substring(0, equalIndex).Trim();
                var value = line.Substring(equalIndex + 1).Trim();
                lastKey = key;

                // validate key
                if (!string.IsNullOrEmpty(config.KeyValidationRegex) && !Regex.IsMatch(key, config.KeyValidationRegex))
                {
                    throw new FormatException($"Invalid line {lineIndex} in ini file '{Path}': '{rawline}' key part '{key}' did not pass the regex validation.");
                }

                // add comment to section and key
                if (currComments.Count > 0)
                {
                    SetComment(currSectionName, key, String.Join(_config.NewLine, currComments));
                    currComments.Clear();
                }

                // check if we continue reading multiline value
                continueMultiline = !string.IsNullOrEmpty(config.MultilineContinuation) && value.EndsWith(config.MultilineContinuation);

                // remove multiline trailing character
                if (continueMultiline)
                {
                    value = value.Substring(0, value.Length - config.MultilineContinuation.Length).TrimEnd();
                }

                // add value
                section[key] = value;
            }
        }

        #endregion

        #region Getters

        /// <summary>
        /// Get all section names.
        /// </summary>
        public string[] Sections => _sections.Keys.ToArray();

        /// <summary>
        /// Get all keys under a section.
        /// </summary>
        /// <param name="section">Section name or null.</param>
        /// <returns>Array with keys under given section.</returns>
        public string[] GetKeys(string section)
        {
            if (string.IsNullOrEmpty(section))
            {
                return _globalSection.Keys.ToArray();
            }
            return _sections[section].Keys.ToArray();
        }

        /// <summary>
        /// Get all values of a section as a dictionary.
        /// </summary>
        /// <param name="section">Section name or null.</param>
        /// <returns>Section values as a dictionary.</returns>
        public Dictionary<string, string> AsDictionary(string section)
        {
            Dictionary<string, string> ret;
            if (string.IsNullOrEmpty(section))
            {
                ret = _globalSection.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            else
            {
                ret = _sections[section].ToDictionary(entry => entry.Key, entry => entry.Value);
            }

            foreach (var key in ret.Keys)
            {
                _keysAccessed.Add(section + ":" + key);
            }

            return ret;
        }

        /// <summary>
        /// Get the internal dictionary key we use for comments.
        /// </summary>
        /// <param name="section">Section or null.</param>
        /// <param name="key">Key or null.</param>
        /// <returns>Internal key we use to identify comments for this section / key.</returns>
        string GetCommentKey(string section, string key)
        {
            return (section ?? string.Empty) + ":" + (key ?? string.Empty);
        }

        /// <summary>
        /// Get the comment string we have for a given section and key, or null if there's no comment attached.
        /// </summary>
        /// <param name="section">Section to get comment for.</param>
        /// <param name="key">Key to get comment for, or null to get the section comment.</param>
        /// <returns>Comment or null if not set.</returns>
        public string GetComment(string section, string key)
        {
            var commentKey = GetCommentKey(section, key);
            _comments.TryGetValue(commentKey, out string ret);
            return ret;
        }

        /// <summary>
        /// Get string value.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value if found, or default value if not set</returns>
        public string GetStr(string section, string key, string defaultValue = null)
        {
            // add to list of read paths
            _keysAccessed.Add(section + ":" + key);

            // get section dictionary, and return default value if not found
            Dictionary<string, string> sectionDict = _globalSection;
            if (!string.IsNullOrEmpty(section))
            {
                if (!_sections.TryGetValue(section, out sectionDict))
                {
                    return defaultValue;
                }
            }

            // try to get value
            string ret;
            if (sectionDict.TryGetValue(key, out ret))
            {
                return ret;
            }
            return defaultValue;
        }

        /// <summary>
        /// Check if a section exists.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <returns>True if found, false otherwise.</returns>
        public bool ContainsSection(string section)
        {
            return _sections.ContainsKey(section);
        }

        /// <summary>
        /// Check if a key exists under a given section.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <returns>True if found, false otherwise.</returns>
        public bool ContainsKey(string section, string key)
        {
            return GetStr(section, key) != null;
        }

        /// <summary>
        /// Get a primitive value.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value if found, or default value if not set</returns>
        public T GetPrimitive<T>(string section, string key, T defaultValue)
        {
            // get value as string, and return default value if not found
            var asStr = GetStr(section, key, null);
            if (asStr == null) { return defaultValue; }

            // get converter, and raise exception if null
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter == null)
            {
                throw new ArgumentException($"No converter found for primitive type '{typeof(T).Name}'! If you want to parse an enum, please use 'GetEnum()'. For user-defined classes, please use 'GetCustomType()'.");
            }

            // parse and return
            try
            {
                return (T)converter.ConvertFromString(asStr);
            }
            // handle invalid formats
            catch (Exception)
            {
                throw new FormatException($"Invalid value in ini file! Tried to parse '[{section ?? string.Empty}].{key}' as {typeof(T).Name}, but value was invalid ('{asStr}').");
            }
        }

        /// <summary>
        /// Parse and return primitive value.
        /// </summary>
        /// <param name="T">Type to parse.</param>
        /// <param name="value">Value to parse.</param>
        /// <returns>Parsed instance.</returns>
        protected object ParsePrimitive(Type T, string value)
        {
            var converter = TypeDescriptor.GetConverter(T);
            if (converter == null)
            {
                throw new ArgumentException($"No converter found for primitive type '{T.Name}'! If you want to parse an enum, please use 'GetEnum()'. For user-defined classes, please use 'GetCustomType()'.");
            }
            return converter.ConvertFromString(value);
        }

        /// <summary>
        /// Get value as char.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as char if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public char GetChar(string section, string key, char defaultValue = (char)0)
        {
            return GetPrimitive<char>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as byte.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as byte if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public byte GetByte(string section, string key, byte defaultValue = 0)
        {
            return GetPrimitive<byte>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as short.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as short if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public short GetShort(string section, string key, short defaultValue = 0)
        {
            return GetPrimitive<short>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as ushort.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as ushort if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public ushort GetUShort(string section, string key, ushort defaultValue = 0)
        {
            return GetPrimitive<ushort>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as int.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as int if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public int GetInt(string section, string key, int defaultValue = 0)
        {
            return GetPrimitive<int>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as uint.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as uint if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public uint GetUInt(string section, string key, uint defaultValue = 0)
        {
            return GetPrimitive<uint>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as long.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as long if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public long GetLong(string section, string key, long defaultValue = 0)
        {
            return GetPrimitive<long>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as ulong.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as ulong if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public ulong GetULong(string section, string key, ulong defaultValue = 0)
        {
            return GetPrimitive<ulong>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as float.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as float if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public float GetFloat(string section, string key, float defaultValue = 0)
        {
            return GetPrimitive<float>(section, key, defaultValue);
        }

        /// <summary>
        /// Get value as double.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value as double if found, or defaultValue if not set. May raise exception on invalid format.</returns>
        public double GetDouble(string section, string key, double defaultValue = 0)
        {
            return GetPrimitive<double>(section, key, defaultValue);
        }

        /// <summary>
        /// Get boolean value.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value if found, or default value if not set Raise exception on invalid format.</returns>
        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            // get value as string, and return default value if not found
            var asStr = GetStr(section, key, null);
            if (asStr == null) { return defaultValue; }

            // normalize value
            if (_config.LowercaseBoolValues) { asStr = asStr.ToLower(); }

            // check if positive
            if (_config.BoolPositiveValues.Contains(asStr))
            {
                return true;
            }
            // check if negative
            else if (_config.BoolNegativeValues.Contains(asStr))
            {
                return false;
            }

            // invalid?
            throw new FormatException($"Invalid value in ini file! Trying to read '[{section ?? string.Empty}].{key}' as boolean, but value is '{asStr}' (not one of the valid positive or negative boolean values).");
        }

        /// <summary>
        /// Get enum value.
        /// </summary>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value if found, or default value if not set Raise exception on invalid format.</returns>
        public EnumType GetEnum<EnumType>(string section, string key, EnumType defaultValue) where EnumType : struct, IConvertible
        {
            // get value as string, and return default value if not found
            var asStr = GetStr(section, key, null);
            if (asStr == null) { return defaultValue; }

            // parse as int and throw exception if invalid
            EnumType ret;
            if (!Enum.TryParse<EnumType>(asStr, out ret))
            {
                throw new FormatException($"Invalid value in ini file! Trying to read '[{section ?? string.Empty}].{key}' as enum of type '{defaultValue.GetType().Name}', but value is '{asStr}'.");
            }

            // return value
            return ret;
        }

        /// <summary>
        /// Get a custom object type, using one of the parsers set in config
        /// </summary>
        /// <typeparam name="T">Object type to get.</typeparam>
        /// <param name="section">Section to get from, or null for global section.</param>
        /// <param name="key">Key to get value for.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>Value if found, or default value if not set Raise exception on invalid format.</returns>
        public T GetCustomType<T>(string section, string key, T defaultValue)
        {
            // first get parser and make sure we have one for this type
            Func<string, object> parser;
            if (!_config.CustomParsers.TryGetValue(typeof(T), out parser))
            {
                throw new KeyNotFoundException($"No parser found for type '{typeof(T).Name}'! To parse this object type, please provide a parser in IniConfig 'CustomParsers' dictionary.");
            }

            // get value as string
            var asStr = GetStr(section, key, null);

            // not set? return default
            if (asStr == null) { return defaultValue; }

            // parse and return
            try
            {
                return (T)parser(asStr);
            }
            // handle exceptions
            catch (Exception e)
            {
                throw new FormatException($"Invalid value in ini file! Trying to read '[{section ?? string.Empty}].{key}' as custom type '{typeof(T).Name}', but value '{asStr}' raised exception {e}.");
            }
        }

        #endregion

        #region Extras

        /// <summary>
        /// Get a list with all keys that appear in file but were not read by the user.
        /// </summary>
        /// <returns>List of unread keys.</returns>
        public IEnumerable<string> GetAllUnreadKeys()
        {
            List<string> ret = new List<string>();

            // iterate global space to find unread keys
            foreach (var key in _globalSection.Keys)
            {
                if (!_keysAccessed.Contains(":" + key))
                {
                    ret.Add(key);
                }
            }

            // iterate sections to find unread keys
            foreach (var section in _sections)
            {
                foreach (var key in section.Value.Keys)
                {
                    if (!_keysAccessed.Contains(section.Key + ":" + key))
                    {
                        ret.Add($"[{section.Key}] {key}");
                    }
                }
            }

            // return result
            return ret;
        }

        /// <summary>
        /// Get all configs as a single string, without comments.
        /// Useful for debug and logging. You can also use this to re-save the config file, but remember you'll lose empty lines and comments.
        /// </summary>
        /// <returns>All config as string.</returns>
        public string ToFullString()
        {
            // handle comments
            Action<StringBuilder, string, string> WriteComment = (StringBuilder sb, string section, string key) =>
            {
                var comment = GetComment(section, key);
                if (comment != null)
                {
                    var commentPlusSpace = _config.CommentCharacters[0] + " ";
                    sb.Append(commentPlusSpace);
                    sb.Append(comment.Replace(_config.NewLine, _config.NewLine + commentPlusSpace));
                    sb.Append('\n');
                }
            };

            // prepare value for writing
            Func<string, string> PrepareValueForOutput = (string val) =>
            {
                return val.Replace(_config.NewLine, '\\' + _config.NewLine);
            };

            // string builder to build the ini file
            StringBuilder sb = new StringBuilder();

            // handle global section
            foreach (var data in _globalSection)
            {
                WriteComment(sb, null, data.Key);
                sb.Append(data.Key);
                sb.Append(" = ");
                sb.Append(PrepareValueForOutput(data.Value));
                sb.Append("\n");
            }
            if (_globalSection.Count > 0)
            {
                sb.Append("\n");
            }

            // handle sections
            foreach (var section in _sections)
            {
                WriteComment(sb, section.Key, null);
                sb.Append("[");
                sb.Append(section.Key);
                sb.Append("]\n");
                foreach (var data in section.Value)
                {
                    WriteComment(sb, section.Key, data.Key);
                    sb.Append(data.Key);
                    sb.Append(" = ");
                    sb.Append(PrepareValueForOutput(data.Value));
                    sb.Append("\n");
                }
                sb.Append("\n");
            }

            // convert to string and return
            return sb.ToString();
        }

        #endregion

        #region Creating Files

        /// <summary>
        /// Create and return a new empty ini file.
        /// </summary>
        /// <returns>Ini file.</returns>
        public static IniFile CreateEmpty()
        {
            return new IniFile();
        }

        /// <summary>
        /// Save ini file to path.
        /// </summary>
        /// <param name="path">Path to save to.</param>
        public void SaveTo(string path)
        {
            File.WriteAllText(path, ToFullString());
        }

        /// <summary>
        /// Clear all comments.
        /// </summary>
        public void ClearComments()
        {
            _comments.Clear();
        }

        /// <summary>
        /// Set the comment string we have for a given section and key, or null to remove comments.
        /// </summary>
        /// <param name="section">Section to set comment for.</param>
        /// <param name="key">Key to set comment for, or null to set the section comment.</param>
        /// <param name="comment">Comment to set or null to remove.</param>
        public void SetComment(string section, string key, string comment)
        {
            // get comment key
            var commentKey = GetCommentKey(section, key);

            // remove comment
            if (comment == null)
            {
                if (_comments.ContainsKey(commentKey))
                {
                    _comments.Remove(commentKey);
                }
            }
            // set comment
            else
            {
                _comments[commentKey] = comment;
            }
        }

        /// <summary>
        /// Set a value, creating section if needed.
        /// </summary>
        /// <param name="section">Section to set value in, or null to use global.</param>
        /// <param name="key">Key to set.</param>
        /// <param name="value">Value to set as string.</param>
        public void SetValue(string section, string key, string value)
        {
            // get section dictionary
            Dictionary<string, string> sectionDict;
            if (!string.IsNullOrEmpty(section))
            {
                if (!_sections.TryGetValue(section, out sectionDict))
                {
                    sectionDict = _sections[section] = new Dictionary<string, string>();
                }
            }
            else
            {
                sectionDict = _globalSection;
            }

            // set value
            sectionDict[key] = value;
        }

        /// <summary>
        /// Deletes a key.
        /// If after deletion the section remains empty, will remove the entire section (unless its global).
        /// </summary>
        /// <param name="section">Section to delete from, or null to delete from global section.</param>
        /// <param name="key">Key to delete.</param>
        public void DeleteKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
            {
                _globalSection.Remove(key);
            }
            else if (_sections.TryGetValue(section, out Dictionary<string, string> sectionDict))
            {
                sectionDict.Remove(key);
                if (sectionDict.Count == 0) { _sections.Remove(section); }
            }
        }

        /// <summary>
        /// Deletes an entire section and all keys in it.
        /// If section is null or empty, will clear global section instead.
        /// </summary>
        /// <param name="section">Section to delete.</param>
        public void DeleteSection(string section)
        {
            if (string.IsNullOrEmpty(section))
            {
                _globalSection.Clear();
            }
            else
            {
                _sections.Remove(section);
            }
        }

        #endregion

        #region ToObject Methods

        /// <summary>
        /// Convert ini file to object.
        /// This method get an instance of the object and set all its public fields and properties.
        /// There's also a version of this method that will create an instance with default constructor.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to read.</typeparam>
        /// <param name="ini">Ini file to read values from.</param>
        /// <param name="flags">Parsing flags.</param>
        protected static T _ToObject<T>(IniFile ini, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs)
        {
            var ret = (T)Activator.CreateInstance(typeof(T));
            ToObject(ref ret, ini, null, flags);
            return ret;
        }

        /// <summary>
        /// Convert ini file to object.
        /// This method get an instance of the object and set all its public fields and properties.
        /// There's also a version of this method that will create an instance with default constructor.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to read.</typeparam>
        /// <param name="ini">Ini file to read values from.</param>
        /// <param name="section">Section to read from, or null to read from root.</param>
        /// <param name="flags">Parsing flags.</param>
        protected static T _ToObject<T>(IniFile ini, string section, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs)
        {
            var ret = (T)Activator.CreateInstance(typeof(T));
            ToObject(ref ret, ini, section, flags);
            return ret;
        }

        /// <summary>
        /// Convert ini file to object.
        /// This method get an instance of the object and set all its public fields and properties.
        /// There's also a version of this method that will create an instance with default constructor.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <param name="T">Object type to read.</param>
        /// <param name="ini">Ini file to read values from.</param>
        /// <param name="section">Section to read from, or null to read from root.</param>
        /// <param name="flags">Parsing flags.</param>
        /// <param name="keyPrefix">Prefix to append to all keys.</param>
        protected static object _ToObject(Type T, IniFile ini, string section, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs, string keyPrefix = "")
        {
            var ret = Activator.CreateInstance(T);
            ToObject(ref ret, ini, section, flags, keyPrefix);
            return ret;
        }

        /// <summary>
        /// Convert ini file to object.
        /// This method get an instance of the object and set all its public fields and properties.
        /// There's also a version of this method that will create an instance with default constructor.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to read.</typeparam>
        /// <param name="iniFilePath">Ini file path to read values from.</param>
        /// <param name="flags">Parsing flags.</param>
        /// <param name="section">If provided, will only read data from this section (used to read multiple objects from the same file, but lose the ability to use nesting).</param>
        /// <param name="keyPrefix">Prefix to append to all keys.</param>
        public static T ToObject<T>(string iniFilePath, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs, string section = null, string keyPrefix = "")
        {
            var ret = (T)Activator.CreateInstance(typeof(T));
            ToObject(ref ret, new IniFile(iniFilePath), section, flags, keyPrefix);
            return ret;
        }

        /// <summary>
        /// Convert ini file to object.
        /// This method get an instance of the object and set all its public fields and properties.
        /// There's also a version of this method that will create an instance with default constructor.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to read.</typeparam>
        /// <param name="instance">Object instance to read values into.</param>
        /// <param name="ini">Ini file to read values from.</param>
        /// <param name="flags">Parsing flags.</param>
        /// <param name="section">If provided, will only read data from this section (used to read multiple objects from the same file, but lose the ability to use nesting).</param>
        /// <param name="keyPrefix">Prefix to append to all keys.</param>
        public static void ToObject<T>(ref T instance, IniFile ini, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs, string section = null, string keyPrefix = "")
        {
            ToObject<T>(ref instance, ini, section, flags, keyPrefix);
        }

        /// <summary>
        /// Convert ini file to object.
        /// This method get an instance of the object and set all its public fields and properties.
        /// There's also a version of this method that will create an instance with default constructor.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to read.</typeparam>
        /// <param name="instance">Object instance to read values into.</param>
        /// <param name="ini">Ini file to read values from.</param>
        /// <param name="section">Section to read from, or null to read from root.</param>
        /// <param name="flags">Parsing flags.</param>
        /// <param name="keyPrefix">Prefix to append to all keys.</param>
        public static void ToObject<T>(ref T instance, IniFile ini, string section, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs, string keyPrefix = "")
        {
            // check if keys should be lowercase or snake case
            bool lowercase = (flags & ParseObjectFlags.LowercaseKeysAndSections) != 0;
            bool snakecase = (flags & ParseObjectFlags.SnakecaseKeysAndSections) != 0;
            if (lowercase && snakecase) { throw new ArgumentException("Can't have both 'LowercaseKeysAndSections' and 'SnakecaseKeysAndSections' flags set!"); }

            // iterate public fields and properties we can set
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            var propsAndFields = instance.GetType().GetFields(bindingFlags).Cast<MemberInfo>().Concat(instance.GetType().GetProperties(bindingFlags)).ToArray();
            foreach (MemberInfo prop in propsAndFields)
            {
                // make sure we can write to this property. if not, skip
                if (prop is PropertyInfo)
                {
                    if ((prop as PropertyInfo).GetSetMethod() == null) { continue; }
                }

                // get key from property name
                var key = prop.Name;
                var fieldType = prop is PropertyInfo ? (prop as PropertyInfo).PropertyType : (prop as FieldInfo).FieldType;

                // lowercase the key
                if (lowercase)
                {
                    key = key.ToLower();
                }
                // snakecase the key
                else if (snakecase)
                {
                    key = string.Concat(key.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
                }

                // add prefix
                var keyNoPrefix = key;
                key = keyPrefix + key;

                // value to set
                object value = null;

                // string type? no processing needed
                if (fieldType == typeof(string))
                {
                    value = ini.GetStr(section, key, null);
                }
                // enum value?
                else if (fieldType.IsEnum)
                {
                    var asStr = ini.GetStr(section, key, null);
                    if (asStr != null && !Enum.TryParse(fieldType, asStr, out value))
                    {
                        throw new FormatException($"Invalid value in ini file! Trying to read '[{section ?? string.Empty}].{key}' as enum of type '{fieldType.Name}', but value is '{asStr}'.");
                    }
                }
                // boolean type? parse with all boolean options
                else if (fieldType == typeof(bool))
                {
                    value = ini.GetBool(section, key, false);
                }
                // primitive type? get primitive value and set
                else if (fieldType.IsPrimitive)
                {
                    // attempt to parse value as primitive
                    var asStr = ini.GetStr(section, key, null);
                    if (asStr != null)
                    {
                        try
                        {
                            value = ini.ParsePrimitive(fieldType, asStr);
                        }
                        // handle invalid formats
                        catch (Exception)
                        {
                            throw new FormatException($"Invalid value in ini file! Tried to parse '[{section ?? string.Empty}].{key}' as {fieldType.Name}, but value was invalid ('{asStr}').");
                        }
                    }
                }
                // special case - field is a dictionary of strings
                else if (fieldType == typeof(Dictionary<string, string>))
                {
                    value = ini.AsDictionary(key);
                }
                // not a primitive type?
                else
                {
                    // check if its a type we registered as custom parser
                    if (ini._config.CustomParsers.TryGetValue(fieldType, out Func<string, object> parser))
                    {
                        // parse and return
                        var asStr = ini.GetStr(section, key, null);
                        if (asStr != null)
                        {
                            try
                            {
                                value = parser(asStr);
                            }
                            // handle exceptions
                            catch (Exception e)
                            {
                                throw new FormatException($"Invalid value in ini file! Trying to read '[{section ?? string.Empty}].{key}' as custom type '{fieldType.Name}', but value '{asStr}' raised exception {e}.");
                            }
                        }
                    }
                    // if we got here we have no choice but to try and convert the nested object from ini to custom type recursively.
                    else
                    {
                        // section and prefix to use for nested object
                        string nestedSection = section;
                        string nestedKeyPrefix = keyPrefix;

                        // if we're already in a section, use key prefix
                        if (section != null)
                        {
                            nestedKeyPrefix += keyNoPrefix + ".";
                        }
                        // if we're not in a section, use section for nesting
                        else
                        {
                            nestedSection = keyNoPrefix;
                        }

                        // parse value 
                        value = _ToObject(fieldType, ini, nestedSection, flags, nestedKeyPrefix);
                    }
                }

                // value doesn't exist? skip or throw exception
                if (value == null)
                {
                    if ((flags & ParseObjectFlags.AllowMissingFields) == 0) { throw new FormatException($"Missing value for field {prop.Name} (searched under section '{section}' and key '{key}' in file '{ini.Path}')."); }
                    continue;
                }

                // finally - set field value in instance
                (prop as PropertyInfo)?.SetValue(instance, value);
                (prop as FieldInfo)?.SetValue(instance, value);
            }

            // make sure there are no unread keys
            if (section == null && ((flags & ParseObjectFlags.AllowAdditionalKeys) == 0))
            {
                var extras = ini.GetAllUnreadKeys();
                if (extras.Any())
                {
                    throw new FormatException($"Found unused key '{extras.First()}' while parsing file '{ini.Path}' for object type '{typeof(T).Name}'. Note: you can disable this exception by setting the 'AllowAdditionalKeys' flag.");
                }
            }
        }

        /// <summary>
        /// Convert object to ini file.
        /// This method get an instance of the object and return an INI file composed from its public fields and properties.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to parse.</typeparam>
        /// <param name="instance">Object instance to convert to file.</param>
        /// <param name="flags">Parsing flags.</param>
        /// <returns>New INI file instance, composed from the given object.</returns>
        public static IniFile FromObject<T>(T instance, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs)
        {
            var ret = new IniFile();
            ret._config = DefaultConfig;
            FromObject(ref ret, instance, flags);
            return ret;
        }

        /// <summary>
        /// Convert object to ini file.
        /// This method get an instance of the object and an INI file, and fill the ini from the object public fields and properties.
        /// </summary>
        /// <remarks>For fields that use custom classes, be sure to register them to ini config first.</remarks>
        /// <typeparam name="T">Object type to parse.</typeparam>
        /// <param name="ini">INI file to write into.</param>
        /// <param name="instance">Object instance to convert to file.</param>
        /// <param name="flags">Parsing flags.</param>
        /// <param name="section">Section to set values into.</param>
        /// <param name="keyPrefix">Prefix to append to all keys.</param>
        public static void FromObject<T>(ref IniFile ini, T instance, ParseObjectFlags flags = ParseObjectFlags.DefaultFalgs, string section = null, string keyPrefix = "")
        {
            // check if keys should be lowercase or snake case
            bool lowercase = (flags & ParseObjectFlags.LowercaseKeysAndSections) != 0;
            bool snakecase = (flags & ParseObjectFlags.SnakecaseKeysAndSections) != 0;
            if (lowercase && snakecase) { throw new ArgumentException("Can't have both 'LowercaseKeysAndSections' and 'SnakecaseKeysAndSections' flags set!"); }

            // iterate public fields and properties we can set
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            var propsAndFields = instance.GetType().GetFields(bindingFlags).Cast<MemberInfo>().Concat(instance.GetType().GetProperties(bindingFlags)).ToArray();
            foreach (MemberInfo prop in propsAndFields)
            {
                // make sure we can read from this property. if not, skip
                if (prop is PropertyInfo)
                {
                    if ((prop as PropertyInfo).GetGetMethod() == null) { continue; }
                }

                // get key from property name
                var key = prop.Name;
                var fieldType = prop is PropertyInfo ? (prop as PropertyInfo).PropertyType : (prop as FieldInfo).FieldType;

                // lowercase the key
                if (lowercase)
                {
                    key = key.ToLower();
                }
                // snakecase the key
                else if (snakecase)
                {
                    key = string.Concat(key.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
                }

                // add prefix
                var keyNoPrefix = key;
                key = keyPrefix + key;

                // value to set
                object value = (prop is PropertyInfo) ? (prop as PropertyInfo).GetValue(instance) : (prop as FieldInfo).GetValue(instance);

                // null? remove value
                if (value == null)
                {
                    if (ini.ContainsKey(section, key))
                    {
                        ini.DeleteKey(section, key);
                    }
                }
                // string type? no processing needed
                else if ((fieldType == typeof(string)) || fieldType.IsPrimitive || fieldType.IsEnum)
                {
                    ini.SetValue(section, key, value.ToString());
                }
                // got an object? set values from the object recursively or use custom serializer.
                else
                {
                    // check if its a type we registered as custom serializer
                    if (ini._config.CustomSerializers.TryGetValue(fieldType, out Func<object, string> serializer))
                    {
                        var asStr = serializer(value);
                        ini.SetValue(section, key, asStr);
                    }
                    // no custom serializer? serialize recursively
                    else
                    {
                        // section and prefix to use for nested object
                        string nestedSection = section;
                        string nestedKeyPrefix = keyPrefix;

                        // if we're already in a section, use key prefix
                        if (section != null)
                        {
                            nestedKeyPrefix += keyNoPrefix + ".";
                        }
                        // if we're not in a section, use section for nesting
                        else
                        {
                            nestedSection = keyNoPrefix;
                        }

                        // parse value 
                        FromObject(ref ini, value, flags, nestedSection, nestedKeyPrefix);
                    }
                }
            }

        }

        /// <summary>
        /// Behavior flags we can set when converting ini file to object.
        /// </summary>
        [Flags]
        public enum ParseObjectFlags
        {
            /// <summary>
            /// No flags value.
            /// </summary>
            NoFlags = 0,

            /// <summary>
            /// Default keys to set.
            /// </summary>
            DefaultFalgs = SnakecaseKeysAndSections,

            /// <summary>
            /// If set, some fields may not exist in ini file and that's OK.
            /// If not set and a missing field is found (public field in object type that don't have value in file) - will throw exception.
            /// </summary>
            AllowMissingFields = 1 << 0,

            /// <summary>
            /// If set, will allow ini file to have additional keys that were not read into any field.
            /// If not set, will throw exception on such keys.
            /// </summary>
            AllowAdditionalKeys = 1 << 1,

            /// <summary>
            /// If set, will expect keys to be lowercase. So if for example we try to read a field named Foo, in ini file we'll look for key 'foo'.
            /// If not set, key name is expected to be exactly like field name. This flag is not competible with SnakecaseKeysAndSections.
            /// </summary>
            LowercaseKeysAndSections = 1 << 2,

            /// <summary>
            /// If set, will expect keys to be in snakecase. So if for example we try to read a field named FooBar, in the ini file we'll look for key 'foo_bar'.
            /// If not set, key name is expected to be exactly like field name. This flag is not competible with LowercaseKeysAndSections.
            /// </summary>
            SnakecaseKeysAndSections = 1 << 3,
        }

        #endregion
    }
}
