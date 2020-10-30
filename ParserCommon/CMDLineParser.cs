/* Lightweight C# Command line parser
 *
 * Author  : Christian Bolterauer
 * Date    : 8-Aug-2009
 * Version : 1.0
 * Changes : 
 * https://www.codeproject.com/Articles/39120/Lightweight-C-Command-Line-Parser
 */

using System;
using System.Collections;

/// <summary>
/// Command Line Parser.
/// </summary>
/// <remarks>
///     supports:
///     - 'unlimited' number of alias names
///     - Options starting with '-' or '/'
///     - String, Integer and Double parameter options
///     - option and parameter attached in one argument (e.g. -P=123 ) or as args pair (e.g. -P 123)
///     - handling differnt number decimal seperators
///     - provides usage message of available (registered) options
///</remarks>
///
namespace CMDLine
{
    /// <summary>
    /// Command Line Parser for creating and parsing command line options
    /// </summary>
    /// <remarks> Throws: MissingOptionException, DuplicateOptionException and if set InvalidOptionsException.
    /// </remarks>
    /// <seealso cref="Parse"/>
    /// <example>
    ///
    ///     //using CMDLine
    ///
    ///     //create parser
    ///     CMDLineParser parser = new CMDLineParser();
    ///
    ///     //add default help "-help",..
    ///     parser.AddHelpOption();
    ///
    ///     //add Option to parse
    ///     CMDLineParser.Option DebugOption = parser.AddBoolSwitch("-Debug", "Print Debug information");
    ///
    ///     //add Alias option name
    ///     DebugOption.AddAlias("/Debug");
    ///
   ///
    ///     try
    ///     {
    ///         //parse
    ///         parser.Parse(args);
    ///     }
    ///     catch (CMDLineParser.CMDLineParserException e)
    ///     {
    ///         Console.WriteLine("Error: " + e.Message);
    ///         parser.HelpMessage();
    ///     }
    ///     parser.Debug();
    ///
    ///</example>
    public class CMDLineParser
    {
        private string[] _cmdlineArgs = null;
        private System.Collections.ArrayList SwitchesStore = null;
        private ArrayList _matchedSwitches = null;
        private ArrayList _unmatchedArgs = null;
        private ArrayList _invalidArgs = null;

        private CMDLineParser.Option _help = null;

        /// <summary>
        ///collect not matched (invalid) command line options as invalid args
        /// </summary>
        public bool collectInvalidOptions = true;
        /// <summary>
        ///throw an exception if not matched (invalid) command line options were detected
        /// </summary>
        public bool throwInvalidOptionsException = false;

        public bool isConsoleApplication = true;

        /// <summary>
        /// create a Command Line Parser for creating and parsing command line options
        /// </summary>
        public CMDLineParser()
        { }
        /// <summary>
        /// Add a default help switch "-help","-h","-?","/help"
        /// </summary>
        public Option AddHelpOption()
        {
            this._help = this.AddBoolSwitch("-help", "Command line help");
            this._help.AddAlias("-h");
            this._help.AddAlias("-?");
            this._help.AddAlias("--help");
            return this._help;
        }
        /// <summary>
        /// Parses the command line and sets the values of each registered switch
        /// or parameter option.
        /// </summary>
        /// <param name="args">The arguments array sent to Main(string[] args)</param>
        /// <returns>'true' if all parsed options are valid otherwise 'false'</returns>
        /// <exception cref="MissingOptionException"></exception>
        /// <exception cref="DuplicateOptionException"></exception>
        /// <exception cref="InvalidOptionsException"></exception>
        public bool Parse(string[] args)
        {
            this.Clear();
            this._cmdlineArgs = args;
            this.ParseOptions();
            if (this._invalidArgs.Count > 0)
            {
                if (this.throwInvalidOptionsException)
                {
                    var iopts = "";
                    foreach (string arg in this._invalidArgs)
                    {
                        iopts += "'" + arg + "';";
                    }
                    throw new InvalidOptionsException("Invalid command line argument(s): " + iopts);
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// Reset Parser and values of registed options.
        /// </summary>
        public void Clear()
        {
            this._matchedSwitches = null;
            this._unmatchedArgs = null;
            this._invalidArgs = null;

            if (this.SwitchesStore != null)
            {
                foreach (Option s in this.SwitchesStore)
                {
                    s.Clear();
                }
            }
        }
        /// <summary>
        /// Add (a custom) Option (Optional)
        /// </summary>
        /// <remarks>
        /// To add instances (or subclasses) of 'CMDLineParser.Option'
        /// that implement:
        /// <code>'public override object parseValue(string parameter)'</code>
        /// </remarks>
        /// <param name="opt">subclass from 'CMDLineParser.Option'</param>
        /// <seealso cref="AddBoolSwitch"/>
        /// <seealso cref="AddStringParameter"/>
        public void AddOption(Option opt)
        {
            this.CheckCmdLineOption(opt.Name);
            (this.SwitchesStore ??= new System.Collections.ArrayList()).Add(opt);
        }
        /// <summary>
        /// Add a basic command line switch.
        /// (exist = 'true' otherwise 'false').
        /// </summary>
        public Option AddBoolSwitch(string name, string description)
        {
            var opt = new Option(name, description, typeof(bool), false, false);
            this.AddOption(opt);
            return opt;
        }
        /// <summary>
        /// Add a string parameter command line option.
        /// </summary>
        public Option AddStringParameter(string name, string description, bool required)
        {
            var opt = new Option(name, description, typeof(string), true, required);
            this.AddOption(opt);
            return opt;
        }
        /// <summary>
        /// Check if name is a valid Option name
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="CMDLineParseException"></exception>
        private void CheckCmdLineOption(string name)
        {
            if (!isASwitch(name))
            {
                throw new CMDLineParserException("Invalid Option:'" + name + "'::" + IS_NOT_A_SWITCH_MSG);
            }
        }
        //
        protected const string IS_NOT_A_SWITCH_MSG = "The Switch name does not start with an switch identifier '-' or '/'  or contains space!";
        protected static bool isASwitch(string arg) => arg.StartsWith("-") && !arg.Contains(" ");

        private void ParseOptions()
        {
            this._matchedSwitches = new ArrayList();
            this._unmatchedArgs = new ArrayList();
            this._invalidArgs = new ArrayList();

            if (this._cmdlineArgs != null && this.SwitchesStore != null)
            {
                for (var idx = 0; idx < this._cmdlineArgs.Length; idx++)
                {
                    var arg = this._cmdlineArgs[idx];
                    var found = false;
                    foreach (Option s in this.SwitchesStore)
                    {
                        if (this.compare(s, arg))
                        {
                            s.isMatched = found = true;
                            this._matchedSwitches.Add(s);
                            idx = this.processMatchedSwitch(s, this._cmdlineArgs, idx);
                        }
                    }
                    if (!found)
                    {
                        this.processUnmatchedArg(arg);
                    }
                }
                this.checkReqired();
            }
        }

        private void checkReqired()
        {
            foreach (Option s in this.SwitchesStore)
            {
                if (s.isRequired && (!s.isMatched))
                {
                    throw new MissingRequiredOptionException("Missing Required Option:'" + s.Name + "'");
                }
            }
        }

        private bool compare(Option s, string arg)
        {
            if (!s.needsValue)
            {
                foreach (var optname in s.Names)
                {
                    if (optname.Equals(arg))
                    {
                        s.Name = optname; //set name in case we match an alias name
                        return true;
                    }
                }
                return false;
            }
            else
            {
                foreach (var optname in s.Names)
                {
                    if (arg.StartsWith(optname))
                    {
                        this.checkDuplicateAndSetName(s, optname);
                        return true;
                    }
                }
                return false;
            }
        }

        private void checkDuplicateAndSetName(Option s, string optname)
        {
            if (s.isMatched && s.needsValue)
            {
                throw new DuplicateOptionException("Duplicate: The Option:'" + optname + "' allready exists on the comand line as  +'" + s.Name + "'");
            }
            else
            {
                s.Name = optname; //set name in case we match an alias name
            }
        }

        private int retrieveParameter(ref string parameter, string optname, string[] cmdlineArgs, int pos)
        {
            if (cmdlineArgs[pos].Length == optname.Length) // arg must be in next cmdlineArg
            {
                if (cmdlineArgs.Length > pos + 1)
                {
                    pos++; //change command line index to next cmdline Arg.
                    parameter = cmdlineArgs[pos];
                }
            }
            else
            {
                parameter = cmdlineArgs[pos][optname.Length..];
            }
            return pos;
        }

        protected int processMatchedSwitch(Option s, string[] cmdlineArgs, int pos)
        {
            //if help switch is matched give help .. only works for console apps
            if (s.Equals(this._help) && this.isConsoleApplication)
            {
                Console.Write(this.HelpMessage());
            }
            //process bool switch
            if (s.Type == typeof(bool) && !s.needsValue)
            {
                s.Value = true;
                return pos;
            }

            if (s.needsValue)
            {
                //retrieve parameter value and adjust pos
                var parameter = "";
                pos = this.retrieveParameter(ref parameter, s.Name, cmdlineArgs, pos);
                //parse option using 'IParsableOptionParameter.parseValue(parameter)'
                //and set parameter value
                try
                {
                    if (s.Type != null)
                    {
                        ((IParsableOptionParameter)s).Value = ((IParsableOptionParameter)s).parseValue(parameter);
                        return pos;
                    }
                }
                catch (Exception ex)
                {
                    throw new ParameterConversionException(ex.Message);
                }
            }
            //unsupported type ..
            throw new CMDLineParserException("Unsupported Parameter Type:" + s.Type);
        }

        protected void processUnmatchedArg(string arg)
        {
            if (this.collectInvalidOptions && isASwitch(arg)) //assuming an invalid comand line option
            {
                this._invalidArgs.Add(arg); //collect first, throw Exception later if set..
            }
            else
            {
                this._unmatchedArgs.Add(arg);
            }
        }
        /// <summary>
        /// String array of remaining arguments not identified as command line options
        /// </summary>
        public string[] RemainingArgs() => this._unmatchedArgs == null ? null : (string[])this._unmatchedArgs.ToArray(typeof(string));
        /// <summary>
        /// String array of matched command line options
        /// </summary>
        public string[] matchedOptions()
        {
            if (this._matchedSwitches == null)
            {
                return null;
            }

            var names = new ArrayList();
            for (var s = 0; s < this._matchedSwitches.Count; s++)
            {
                names.Add(((Option)this._matchedSwitches[s]).Name);
            }

            return (string[])names.ToArray(typeof(string));
        }
        /// <summary>
        /// String array of not identified command line options
        /// </summary>
        public string[] invalidArgs() => this._invalidArgs == null ? null : (string[])this._invalidArgs.ToArray(typeof(string));
        /// <summary>
        /// Create usage: A formated help message with a list of registered command line options.
        /// </summary>
        public string HelpMessage()
        {
            const string indent = "  ";
            var ind = indent.Length;
            const int spc = 3;
            var len = 0;
            foreach (Option s in this.SwitchesStore)
            {
                foreach (var name in s.Names)
                {
                    var nlen = name.Length;
                    if (s.needsValue)
                    {
                        nlen += " [..]".Length;
                    }

                    len = Math.Max(len, nlen);
                }
            }
            var help = "\nCommand line options are:\n\n";
            var req = false;
            foreach (Option s in this.SwitchesStore)
            {
                var line = indent + s.Names[0];
                if (s.needsValue)
                {
                    line += " [..]";
                }

                while (line.Length < len + spc + ind)
                {
                    line += " ";
                }

                if (s.isRequired)
                {
                    line += "(*) ";
                    req = true;
                }
                line += s.Description;

                help += line + "\n";
                if (s.Aliases?.Length > 0)
                {
                    foreach (var name in s.Aliases)
                    {
                        line = indent + name;
                        if (s.needsValue)
                        {
                            line += " [..]";
                        }

                        help += line + "\n";
                    }
                }
                help += "\n";
            }
            if (req)
            {
                help += "(*) Required.\n";
            }

            return help;
        }
        /// <summary>
        /// Print debug information of this CMDLineParser to the system console.
        /// </summary>
        public void Debug()
        {
            Console.WriteLine();
            Console.WriteLine("\n------------- DEBUG CMDLineParser -------------\n");
            if (this.SwitchesStore != null)
            {
                Console.WriteLine("There are {0} registered switches:", this.SwitchesStore.Count);
                foreach (Option s in this.SwitchesStore)
                {
                    Console.WriteLine("Command : {0} : [{1}]", s.Names[0], s.Description);
                    Console.Write("Type    : {0} ", s.Type);
                    Console.WriteLine();

                    if (s.Aliases != null)
                    {
                        Console.Write("Aliases : [{0}] : ", s.Aliases.Length);
                        foreach (var alias in s.Aliases)
                        {
                            Console.Write(" {0}", alias);
                        }

                        Console.WriteLine();
                    }
                    Console.WriteLine("Required: {0}", s.isRequired);

                    Console.WriteLine("Value is: {0} \n",
                        s.Value ?? "(Unknown)");
                }
            }
            else
            {
                Console.WriteLine("There are no registered switches.");
            }

            if (this._matchedSwitches != null)
            {
                if (this._matchedSwitches.Count > 0)
                {
                    Console.WriteLine("\nThe following switches were found:");
                    foreach (Option s in this._matchedSwitches)
                    {
                        Console.WriteLine("  {0} Value:{1}",
                            s.Name ?? "(Unknown)",
                            s.Value ?? "(Unknown)");
                    }
                }
                else
                {
                    Console.WriteLine("\nNo Command Line Options detected.");
                }
            }
            Console.Write(this.InvalidArgsMessage());
            Console.WriteLine("\n----------- DEBUG CMDLineParser END -----------\n");
        }

        private string InvalidArgsMessage()
        {
            const string indent = "  ";
            var msg = "";
            if (this._invalidArgs != null)
            {
                msg += "\nThe following args contain invalid (unknown) options:";
                if (this._invalidArgs.Count > 0)
                {
                    foreach (string s in this._invalidArgs)
                    {
                        msg += "\n" + indent + s;
                    }
                }
                else
                {
                    msg += "\n" + indent + "- Non -";
                }
            }
            return msg + "\n";
        }
        /// <summary>
        /// Interface supporting parsing and setting of string parameter Values to objects
        /// </summary>
        private interface IParsableOptionParameter
        {
            /// <summary>
            /// parse string parameter to convert to an object
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns>an object</returns>
            object parseValue(string parameter);
            /// <summary>
            /// Get or Set the value
            /// </summary>
            object Value { get; set; }
        }
        /// <summary>
        /// A comand line Option: A switch or a string parameter option.
        /// </summary>
        /// <remarks> Use AddBoolSwitch(..) or  AddStringParameter(..) (Factory)
        /// Methods to create and store a new parsable 'CMDLineParser.Option'.
        /// </remarks>
        public class Option : IParsableOptionParameter
        {
            private System.Collections.ArrayList _Names = null;
            private readonly bool _needsVal = false;

            private Option() { }

            public Option(string name, string description, System.Type type, bool hasval, bool required)
            {
                this.Type = type;
                this._needsVal = hasval;
                this.isRequired = required;
                this.Initialize(name, description);
            }

            private void Initialize(string name, string description)
            {
                this.Name = name;
                this.Description = description;
                this._Names = new System.Collections.ArrayList
                {
                    name
                };
            }

            public void AddAlias(string alias)
            {
                if (!CMDLineParser.isASwitch(alias))
                {
                    throw new CMDLineParserException("Invalid Option:'" + alias + "'::" + IS_NOT_A_SWITCH_MSG);
                }

                (this._Names ??= new System.Collections.ArrayList()).Add(alias);
            }

            public void Clear()
            {
                this.isMatched = false;
                this.Value = null;
            }

            //getters and setters
            public string Name { get; set; } = "";

            public string Description { get; set; } = "";
            /// <summary>
            /// Object Type of Option Value (e.g. typeof(int))
            /// </summary>
            public System.Type Type { get; }

            public bool needsValue => this._needsVal;

            public bool isRequired { get; set; } = false;
            /// <summary>
            /// set to 'true' if Option has been detected on the command line
            /// </summary>
            public bool isMatched { get; set; } = false;

            public string[] Names => (this._Names != null) ? (string[])this._Names.ToArray(typeof(string)) : null;

            public string[] Aliases
            {
                get
                {
                    if (this._Names == null)
                    {
                        return null;
                    }

                    var list = new ArrayList(this._Names);
                    list.RemoveAt(0); //remove 'name' (first element) from the list to leave aliases only
                    return (string[])list.ToArray(typeof(string));
                }
            }

            public object Value { get; set; } = null;

            #region IParsableOptionParameter Member
            /// <summary>
            /// Default implementation of parseValue:
            /// Subclasses should override this method to provide a method for converting
            /// the parsed string parameter to its Object type
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns>converted value</returns>
            /// <see cref="NumberOption.parseValue"/>
            public virtual object parseValue(string parameter)
            {
                //set string parameter
                if (this.Type == typeof(string) && this.needsValue)
                {
                    return parameter;//string needs no parsing (conversion) to string...
                }
                else
                {
                    //throw Exception when parseValue has not been implemented by a subclass 
                    throw new Exception("Option is missing an method to convert the value.");
                }
            }
            #endregion
        }

        /// <summary>
        /// Command line parsing Exception.
        /// </summary>
        public class CMDLineParserException : Exception
        {
            public CMDLineParserException(string message)
                : base(message)
            { }
        }
        /// <summary>
        /// Thrown when required option was not detected
        /// </summary>
        public class MissingRequiredOptionException : CMDLineParserException
        {
            public MissingRequiredOptionException(string message)
                : base(message)
            { }
        }
        /// <summary>
        /// Thrown when invalid (not registered) options have been detected
        /// </summary>
        public class InvalidOptionsException : CMDLineParserException
        {
            public InvalidOptionsException(string message)
                : base(message)
            { }
        }
        /// <summary>
        /// Thrown when duplicate option was detected
        /// </summary>
        public class DuplicateOptionException : CMDLineParserException
        {
            public DuplicateOptionException(string message)
                : base(message)
            { }
        }
        /// <summary>
        /// Thrown when parameter value conversion to specified type failed
        /// </summary>
        public class ParameterConversionException : CMDLineParserException
        {
            public ParameterConversionException(string message)
                : base(message)
            { }
        }
    }
}
