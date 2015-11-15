using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace AutoBot
{
    public class RScript
    {
        //#==================================================================== CONSTANTS
        private readonly Dictionary<RCommandType, string> _commands = new Dictionary<RCommandType, string>(); // holds valid commands

        //#==================================================================== VARIABLES
        private List<RCommand> _compiledScript = new List<RCommand>(); // compiled script
        private int _scriptIndex; // current command index
        private int _mouseSpeed;

        //#==================================================================== INITIALIZE
        public RScript()
        {
            Reset();
            _commands.Add(RCommandType.Left, @"\Aleft>(\d+),(\d+)\Z");
            _commands.Add(RCommandType.LeftDown, @"\Aldown>(\d+),(\d+)\Z");
            _commands.Add(RCommandType.LeftUp, @"\Alup>\Z");
            _commands.Add(RCommandType.Right, @"\Aright>(\d+),(\d+)\Z");
            _commands.Add(RCommandType.Move, @"\Amove>(\d+),(\d+)\Z");
            _commands.Add(RCommandType.Wait, @"\Await>(\d+)\Z");
            _commands.Add(RCommandType.WaitUntil, @"\Awaituntil>(\d+),(\d+),(\d+)\Z");
            _commands.Add(RCommandType.WaitWhile, @"\Awaitwhile>(\d+),(\d+),(\d+)\Z");
            _commands.Add(RCommandType.WaitBright, @"\Awaitbright>(\d+),(\d+),(\d+)\Z");
            _commands.Add(RCommandType.WaitDark, @"\Awaitdark>(\d+),(\d+),(\d+)\Z");
            _commands.Add(RCommandType.Speed, @"\Aspeed>(\d+)\Z");
        }

        //#==================================================================== FUNCTIONS
        public int Compile(string script)
        {
            _compiledScript.Clear(); // clear script

            script = EmbedLineNumbers(script);
            script = FormatScript(script); // format code
            Dictionary<string, string> functions = GenerateFunctions(script); // holds functions
            script = RemoveFunctionDeclarations(script);
            script = InsertFunctionCode(script, functions);
            // iterate through code
            foreach (string line in Regex.Split(script, "\r\n"))
            {
                // compile commands into script
                bool success = false; // valid line flag
                int lineNumber = int.Parse(Regex.Match(line, @"\A(\d+)-").Groups[1].Value);
                string command = Regex.Match(line, @"\A\d+-(.+)").Groups[1].Value;
                foreach (KeyValuePair<RCommandType, string> stored_command in _commands) // iterate for match in stored commands
                {
                    Match commandMatch = Regex.Match(command, stored_command.Value);
                    if (commandMatch.Success)
                    {
                        int[] parameters = new int[commandMatch.Groups.Count - 1];
                        for (int i = 1; i < commandMatch.Groups.Count; i++) parameters[i - 1] = int.Parse(commandMatch.Groups[i].Value);
                        _compiledScript.Add(new RCommand(stored_command.Key, lineNumber, parameters));
                        success = true;
                    }
                }
                // return if compile error
                if (!success)
                {
                    _compiledScript.Clear();
                    return lineNumber;
                }
            }
            _scriptIndex = 0;
            return -1;
        }
        private string EmbedLineNumbers(string script)
        {
            string[] scriptArray = Regex.Split(script, "\r\n");
            string result = string.Empty;
            for (int i = 0; i < scriptArray.Length; i++) result += (i + 1) + "-" + scriptArray[i] + "\r\n";
            return result;
        }
        private string FormatScript(string script)
        {
            string formatLine = Regex.Replace(script, @"#.*?(?=\r\n)", string.Empty); // remove comment
            formatLine = formatLine.Replace(" ", string.Empty).ToLower(); // remove space
            formatLine = Regex.Replace(formatLine, @"\r\n(\d+-\r\n)+", "\r\n").TrimEnd('\r', '\n');
            formatLine = Regex.Replace(formatLine, @"\d+-\r\n", string.Empty);
            return formatLine;
        }
        private Dictionary<string, string> GenerateFunctions(string script)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            MatchCollection matches = Regex.Matches(script, @"\d+-[A-Za-z_]+\r\n\d+-{\r\n.+?\r\n\d+-}(?=\r\n|\Z)", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                string function = match.Groups[0].Value;
                string name = Regex.Match(function, @"\A\d+-(.+)\r\n").Groups[1].Value;
                string body = Regex.Match(function, @"{\r\n(.+)\r\n\d+-}", RegexOptions.Singleline).Groups[1].Value;
                result.Add(name, body);
            }
            return result;
        }
        private string RemoveFunctionDeclarations(string script)
        {
            script = Regex.Replace(script, @"\d+-[A-Za-z_]+\r\n\d+-{\r\n.+?\r\n\d+-}(\r\n|\Z)", string.Empty, RegexOptions.Singleline);
            return script;
        }
        private string InsertFunctionCode(string script, Dictionary<string, string> functions)
        {
            foreach (KeyValuePair<string, string> function in functions)
            {
                script = Regex.Replace(script, @"(?<=\A|\r\n)\d+-" + function.Key + @">(?=\Z|\r\n)", function.Value);
            }
            return script;
        }
        public void RunNextCommand()
        {
            RCommand command = _compiledScript[_scriptIndex];
            switch (command.CommandType)
            {
                case RCommandType.Left:
                    Macro.CursorMove(new Point(command.Parameters[0], command.Parameters[1]), _mouseSpeed);
                    Macro.LeftClick();
                    break;
                case RCommandType.LeftDown:
                    Macro.CursorMove(new Point(command.Parameters[0], command.Parameters[1]), _mouseSpeed);
                    Macro.LeftDown();
                    break;
                case RCommandType.LeftUp:
                    Macro.LeftUp();
                    break;
                case RCommandType.Right:
                    Macro.CursorMove(new Point(command.Parameters[0], command.Parameters[1]), _mouseSpeed);
                    Macro.RightClick();
                    break;
                case RCommandType.Move:
                    Macro.CursorMove(new Point(command.Parameters[0], command.Parameters[1]), _mouseSpeed);
                    break;
                case RCommandType.Wait:
                    Macro.Wait(command.Parameters[0]);
                    break;
                case RCommandType.WaitUntil:
                    Macro.WaitUntilColor(new Point(command.Parameters[0], command.Parameters[1]), ColorTranslator.FromWin32(command.Parameters[2]));
                    break;
                case RCommandType.WaitWhile:
                    Macro.WaitWhileColor(new Point(command.Parameters[0], command.Parameters[1]), ColorTranslator.FromWin32(command.Parameters[2]));
                    break;
                case RCommandType.WaitBright:
                    Macro.WaitUntilBrightness(new Point(command.Parameters[0], command.Parameters[1]), command.Parameters[2]);
                    break;
                case RCommandType.WaitDark:
                    Macro.WaitWhileBrightness(new Point(command.Parameters[0], command.Parameters[1]), command.Parameters[2]);
                    break;
                case RCommandType.Speed:
                    _mouseSpeed = command.Parameters[0];
                    break;
            }
            _scriptIndex = ((_scriptIndex + 1) % _compiledScript.Count);
        }
        public void Reset()
        {
            _scriptIndex = 0;
            _mouseSpeed = 20;
        }

        //#==================================================================== PROPERTIES
        public bool IsEmpty
        {
            get { return _compiledScript.Count == 0; }
        }
        public bool IsFinished
        {
            get { return _scriptIndex == 0; }
        }
        public int CurrentLine
        {
            get { return _compiledScript[_scriptIndex].LineNumber; }
        }
    }


    public struct RCommand
    {
        //#==================================================================== VARIABLES
        public readonly RCommandType CommandType;
        public readonly int LineNumber;
        public readonly int[] Parameters;

        //#==================================================================== INITIALIZE
        public RCommand(RCommandType commandType, int lineNumber, int[] parameters)
        {
            CommandType = commandType;
            LineNumber = lineNumber;
            Parameters = parameters;
        }
    }


    public enum RCommandType
    {
        Left, LeftDown, LeftUp,
        Right,
        Move,
        Wait,
        WaitUntil, WaitWhile,
        WaitBright,  WaitDark,
        Speed
    }
}
