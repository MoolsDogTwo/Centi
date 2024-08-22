namespace Centi_Text_Editor;

public static class Ui
{
    public static char SearchKey { get; private set; }
    
    public class StatusLine
    {
        private string _leftContents = "";
        private string _rightContents = "";
        private readonly int _pos;

        public StatusLine(int pos, string leftContents, string rightContents)
        {
            _pos = pos;
            Update(leftContents, rightContents);
        }

        public void Update(string leftContents, string rightContents)
        {
            _leftContents = " " + leftContents;
            _rightContents = rightContents + " ";
        }
        
        public void Display()
        {
            DisplayLine(_pos, "\x1b[30;47m");
            ScrBuf.Append(_leftContents);
            
            Program.MoveCursor(Console.WindowWidth - _rightContents.Length, _pos);
            ScrBuf.Append(_rightContents);
            
            ScrBuf.Append("\x1b[0m");
        }
    }

    private static void DisplayLine(int pos, string colourCode)
    {
        if (!colourCode.Contains('\u001b'))
            throw new Exception("Please use ANSI escape codes only");
            
        Program.MoveCursor(0, pos);
        ScrBuf.Append("\x1b[K");
        ScrBuf.Append(colourCode);
        for (int i = 0; i < Console.WindowWidth; ++i)
        {
            ScrBuf.Append(" ");
        }
        Program.MoveCursor(0, pos);
    }

    public static class Message
    {
        public enum Type  // Function associations below:
        {
            Normal,
            Input,
            Yn,
            Error,
            Info,
            None,
        }
        
        // promptLength is useful for displaying the input box correctly. In all other cases, it should be discarded.
        private static void Display(string msg, Type type, out int promptLength)
        {
            Program.RefreshScreen(false);
            
            DisplayLine(Console.WindowHeight - 1, type != Type.Error ? "\x1b[97;104m" : "\x1b[97;101m");

            string message = type switch
            {
                Type.Normal => msg + " -- PRESS ENTER TO DISMISS",
                Type.Input => msg,
                Type.Yn => msg + " (y/n) ",
                Type.Error => "ERROR: " + msg + " -- PRESS ENTER TO DISMISS",
                Type.Info => "INFO: " + msg + " -- PRESS ENTER TO DISMISS",
                Type.None => msg,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            message = message[..Math.Min(message.Length, Console.WindowWidth)];
            
            promptLength = message.Length;
            ScrBuf.Append(message);
        }

        // Prevents potential misuse that messes up the whole screen.
        private static bool HasModifiers(ConsoleKeyInfo k)
        {
            return k.Modifiers.HasFlag(ConsoleModifiers.Control) || k.Modifiers.HasFlag(ConsoleModifiers.Alt);
        }

        public static void Invoke(string msg, Type type)
        {
            ConsoleKey input;
            do
            {
                Display(msg, type, out _);
                ScrBuf.Append("\x1b[0m");
                ScrBuf.Flush();
                
                input = Console.ReadKey().Key;
            } while (input != ConsoleKey.Enter);
        }

        public static void InvokeNoInput(string msg, Type type)
        {
            Display(msg, type, out _);
            ScrBuf.Append("\x1b[0m");
            ScrBuf.Flush();
        }

        public static string Input(string msg, out bool cancelled)
        {
            int inputIndex = 0;
            int inputBoxOffset = 0;
            const int inputLimit = byte.MaxValue;
            
            string inputtedChars = "";

            while (true)
            {
                Display(msg + $" ({inputIndex}/{inputLimit} chars) > ".PadLeft(5), Type.Input,
                    out int leftPadding);
                
                // Scroll viewport accordingly
                if (leftPadding + inputIndex > Console.WindowWidth - 1)
                    inputBoxOffset = leftPadding + inputIndex - Console.WindowWidth + 1;

                try
                {
                    ScrBuf.Append(inputtedChars.Substring(inputBoxOffset,
                        Math.Min(inputtedChars.Length - inputBoxOffset, Console.WindowWidth - 1 - leftPadding)));
                }
                catch (ArgumentOutOfRangeException)
                {
                    cancelled = true;
                    return "";
                }
                
                ScrBuf.Append("\x1b[0m");
                
                Program.ShowCursor();
                ScrBuf.Flush();

                Console.SetCursorPosition(leftPadding - inputBoxOffset + inputIndex, Console.WindowHeight - 1);
                
                ConsoleKeyInfo input = Console.ReadKey(true);
                if (HasModifiers(input)) continue;

                switch (input.Key)
                {
                    case ConsoleKey.LeftArrow:
                    {
                        if (inputIndex > 0)
                            --inputIndex;
                        break;
                    }
                    case ConsoleKey.RightArrow:
                    {
                        if (inputIndex < inputtedChars.Length)
                            ++inputIndex;
                        break;
                    }
                    case ConsoleKey.Backspace:
                    {
                        if (inputIndex > 0)
                        {
                            inputtedChars = inputtedChars.Remove(inputIndex - 1, 1);
                            --inputIndex;
                        }
                        
                        break;
                    }
                    case ConsoleKey.Enter:
                    {
                        cancelled = false;
                        return inputtedChars;
                    }
                    case ConsoleKey.Escape:
                    {
                        cancelled = true;
                        return "";
                    }
                    default:
                    {
                        if (inputIndex < inputLimit)
                        {
                            inputtedChars = inputtedChars.Insert(inputIndex, input.KeyChar.ToString());
                            ++inputIndex;
                        }
                        
                        break;
                    }
                }
            }
        }

        public static bool Yn(string msg)
        {
            char result = '\0';
            bool inputFinished = false;

            while (!inputFinished)
            {
                Display(msg , Type.Yn, out _);
                
                ScrBuf.Append(result);
                ScrBuf.Append("\x1b[0m");
                ScrBuf.Flush();

                ConsoleKeyInfo input = Console.ReadKey();
                if (HasModifiers(input)) continue;

                switch (input.Key)
                {
                    case ConsoleKey.Backspace:
                    {
                        result = '\0';
                        break;
                    }
                    case ConsoleKey.Enter:
                    {
                        if (result != '\0')
                            inputFinished = true;
                        break;
                    }
                    default:
                    {
                        result = input.KeyChar;
                        break;
                    }
                }
            }

            return result is 'y' or 'Y';
        }
    }
}