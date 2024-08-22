namespace Centi_Text_Editor;

public class Cursor
{
    public int X;
    public int Y;
    public int ScrX;
    private int _previousX; 

    public Cursor(int x, int y)
    {
        if (x < 0 || x > Console.WindowWidth)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y > Console.WindowHeight)
            throw new ArgumentOutOfRangeException(nameof(y));
        
        X = x;
        ScrX = X;
        Y = y;
    }
    
    public void ClampY()
    {
        string line = Program.Buf.GetLine(Y);
        X = Math.Min(_previousX, line.Length);
    }

    public void Left()
    {
        if (X > 0)
        {
            --X;
            _previousX = X;
            return;
        }

        if (Y > 0)
        {
            Up();
            End();
        }
    }
    
    public void Right()
    {
        if (X < Program.Buf.GetLine(Y).Length)
        {
            ++X;
            _previousX = X;
            return;
        }
        
        if (Y < Program.Buf.Length - 1)
        {
            Down();
            Home();
        }
        
    }

    public void Up()
    {
        if (Y > 0)
        {
            --Y;
            ClampY();
        }
    }
    
    public void Down()
    {
        if (Y < Program.Buf.Length - 1)
        {
            ++Y;
            ClampY();
            return;
        }
        
        End();
    }

    public void Home()
    {
        X = 0;
        _previousX = X;
    }
    
    public void End()
    {
        X = Program.Buf.GetLine(Y).Length;
        _previousX = X;
    }
    
    public void Set(int x, int y)
    {
        X = x;
        Y = y;
        ScrX = Program.CalculateScrX(this);
        _previousX = x;
    }
    
    public void GotoLine()
    {
        int line;
        while (true)
        {
            string input = Ui.Message.Input("Goto line: ", out bool cancelled);
            
            if (cancelled) return;
            
            if (!int.TryParse(input, out line) || line < 1 || line > Program.Buf.Length)
                Ui.Message.Invoke("Please enter a valid line number.", Ui.Message.Type.Error);
            else
                break;
        }

        Y = line - 1;
        X = Math.Min(X, Program.Buf.GetLine(Y).Length);
        _previousX = X;
    }

    public void CursorScrSet()
    {
        Console.SetCursorPosition(ScrX - Program.WinOffsetX, Y - Program.WinOffsetY + Program.ScrContentsPadding);
    }

    public void Use()
    {
        CursorScrSet();
        
        Program.ShowCursor();
        ConsoleKeyInfo input = Console.ReadKey();
        
        /* Control key options */
        
        if (input.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            switch (input.Key)
            {
                case ConsoleKey.Q:  // Quit
                {
                    FileActions.Quit();
                    break;
                }
                case ConsoleKey.X:  // Save
                {
                    Program.Buf.SaveCurrentFile();
                    break;
                }
                case ConsoleKey.D:
                {
                    Program.Buf.DeleteLine(this);
                    break;
                }
                case ConsoleKey.O:
                {
                    Set(0, 0);
                    FileActions.Open();
                    break;
                }
                case ConsoleKey.N:
                {
                    Set(0, 0);
                    FileActions.CreateNew();
                    break;
                }
                case ConsoleKey.F:
                {
                    FileActions.Search(this);
                    break;
                }
                case ConsoleKey.L:
                {
                    GotoLine();
                    break;
                }
                case ConsoleKey.C:
                {
                    Internal.Copy(this);
                    break;
                }
                case ConsoleKey.V:
                {
                    Internal.Paste(this);
                    break;
                }
            }

            return;
        }
        
        /* Alt key actions */
        
        if (input.Modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            switch (input.Key)
            {
                case ConsoleKey.M:
                {
                    Ui.Message.Invoke("A miracle has occurred!", Ui.Message.Type.Info);
                    break;
                }
            }

            return;  // Prevent any characters being inputted to the file.
        }

        /* Regular options */
        
        switch (input.Key)
        {
            /* Cursor movement */
            
            case ConsoleKey.LeftArrow:
            {
                Left();
                break;
            }
            case ConsoleKey.RightArrow:
            {
                Right();
                break;
            }
            case ConsoleKey.UpArrow:
            {
                Up();
                break;
            }
            case ConsoleKey.DownArrow:
            {
                Down();
                break;
            }
            
            /* Operation keys */
            
            case ConsoleKey.Backspace:
            {
                Program.Buf.Delete(this);
                break;
            }
            case ConsoleKey.Enter:
            {
                Program.Buf.Append(this);
                Home();
                break;
            }
            case ConsoleKey.Home:
            {
                Home();
                break;
            }
            case ConsoleKey.End:
            {
                End();
                break;
            }
            
            /* Capture common useless keys */
            
            case ConsoleKey.Escape: case ConsoleKey.Insert: case ConsoleKey.Delete: case ConsoleKey.PageUp:
            case ConsoleKey.PageDown:
                break;
            
            /* Insert character */
            
            default:
            {
                Program.Buf.InsertChar(input.KeyChar, this);
                break;
            }
        }
    }
}