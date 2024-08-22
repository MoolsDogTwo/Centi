/* Program written by Aleksandar Milovic (MoolsDogTwo)
 * Special thanks to this source https://viewsourcecode.org/snaptoken/kilo for extra help for many features!
 * (Main function is at the bottom of the file)
*/

namespace Centi_Text_Editor;

class Program
{
    /* Variables */
    
    private const string Version = "1.0";
    public static List<Buffer> BufferStack = [];
    private static int _currentBuffer = 0;
    public const int ScrContentsPadding = 1;
    public static int WinOffsetX;
    public static int WinOffsetY;
    public const int ShiftWidth = 4;
    public static Buffer Buf => BufferStack[_currentBuffer];
    
    /* Program wide variables */
    
    private static readonly Cursor ProgramCursor = new(0, 0);
    private static readonly Ui.StatusLine ProgramStatusLine = new(Console.WindowHeight, "", "");
    private static readonly Ui.StatusLine ProgramTabLine = new(0, "", "");

    /* Screen functions */
    
    public static void HideCursor()
    {
        Console.CursorVisible = false;
    }
    
    public static void ShowCursor()
    {
        Console.CursorVisible = true;
    }
    
    public static void MoveCursor(int x, int y)
    {
        ScrBuf.Append($"\x1b[{y + 1};{x + 1}H");
    }

    // Logic taken from: https://viewsourcecode.org/snaptoken/kilo/04.aTextViewer.html
    // FIXME: Scrolling to earlier lines with less or more tabs doesn't work properly.
    public static int CalculateScrX(Cursor cursor)
    {
        int scrX = 0;

        for (int i = 0; i < cursor.X; ++i)
        {
            if (Buf.GetLine(cursor.Y)[i] == '\t')
                scrX += ShiftWidth - scrX % ShiftWidth;
            else
                ++scrX;
        }

        return scrX;
    }

    // Logic taken from: https://viewsourcecode.org/snaptoken/kilo/04.aTextViewer.html
    public static void ScrollScr(Cursor cursor)
    {
        cursor.ScrX = CalculateScrX(cursor);
        
        // Vertical scrolling
        if (cursor.Y < WinOffsetY)
            WinOffsetY = cursor.Y;
        if (cursor.Y >= WinOffsetY + Console.WindowHeight - 2)
            WinOffsetY = cursor.Y - Console.WindowHeight + 3;
        
        // Horizontal scrolling
        if (cursor.ScrX < WinOffsetX)
            WinOffsetX = cursor.ScrX;
        if (cursor.ScrX >= WinOffsetX + Console.WindowWidth)
            WinOffsetX = cursor.ScrX - Console.WindowWidth + 1;
    }
    
    // TODO: Fix the logic for this thing.
    private static void DisplayBuffer()
    {
        Console.SetCursorPosition(0, ScrContentsPadding);
        for (int row = 0; row < Console.WindowHeight - 1 - ScrContentsPadding; ++row)
        {
            ScrBuf.Append("\x1b[K");
            if (row + WinOffsetY >= Buf.Length)
            {
                ScrBuf.EmptyAppend();
                continue;
            }

            string line = Buf.GetScrLine(row + WinOffsetY);
            int lineLength = line.Length - WinOffsetX;
           
            if (lineLength > 0)
                ScrBuf.Append(line.Substring(WinOffsetX, Math.Min(lineLength, Console.WindowWidth)) + "\r\n");
            else
                ScrBuf.EmptyAppend();
        }
    }

    /* Misc functions */
    
    private static string GetBufferState()
    {
        return Buf.CurrentState switch
        {
            Buffer.State.New => "(new)",
            Buffer.State.Opened => "",
            Buffer.State.Modified => "(modified)",
            Buffer.State.Saved => "(saved to disk)",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static void RefreshScreen(bool flush = true)
    {
        HideCursor();
        Console.SetCursorPosition(0, 0);
        
        ScrollScr(ProgramCursor);
        
        DisplayBuffer();

        float winProgress = (float) (ProgramCursor.Y + 1) / Buf.Length * 100;
        
        ProgramStatusLine.Update($"{Buf.Title} {GetBufferState()}", $"FileSize: {Buf.FileSize} "
                                                                    + (Buf.FileSize != 1 ? "Bytes" : "Byte"));
        ProgramStatusLine.Display();
        ProgramTabLine.Update($"Centi Text Editor {Version}",
            $"col: {ProgramCursor.X + 1} / row: {ProgramCursor.Y + 1} / {(int)winProgress}%");
        ProgramTabLine.Display();

        if (flush)
        {
            ScrBuf.Flush();
        }
    }

    /* Main */
    
    // ReSharper disable once ArrangeTypeMemberModifiers
    static void Main(string[] args)
    {
        Console.TreatControlCAsInput = true;
        
        if (args.Length > 0)
            FileActions.LoadFiles(args);
        else
            BufferStack.Add(new Buffer());

        while (true)
        {
            RefreshScreen();
            ProgramCursor.Use();
        }
    }
}