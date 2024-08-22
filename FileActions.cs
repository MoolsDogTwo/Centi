using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Centi_Text_Editor;

public static class FileActions
{
    public static void LoadFiles(string[] files)
    {
        foreach (string file in files)
        {
            Program.BufferStack.Add(File.Exists(file) ? new Buffer(file) : new Buffer());
        }
    }
    
    public static void AlertIfModified()
    {
        if (Program.Buf.CurrentState is Buffer.State.Modified or Buffer.State.New)
        {
            if (Ui.Message.Yn("File has not yet been saved. Would you like to save?"))
                Program.Buf.SaveCurrentFile(false);
        }
    }

    public static void Open()
    {
        string filePath = Ui.Message.Input("Enter filename to open", out bool c);
    
        if (c) return;
    
        if (!File.Exists(filePath))
        {
            Ui.Message.Invoke("Specified file does not exist. Operation cancelled.", Ui.Message.Type.Error);
            return;
        }
    
        AlertIfModified();
        Program.Buf.Init(filePath);
    }

    public static void CreateNew()
    {
        AlertIfModified();
        Program.Buf.DefaultInit();
    }


    public static bool FileNameNotValid(string filename)
    {
        bool notValid = true;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            notValid = filename.Contains('/');
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            notValid = Regex.IsMatch(filename, "[<>:\"/\\|?*]");
        return notValid;
    }

    private struct MatchIndex
    {
        public int Line;
        public int Index;
    }
        
    public static void Search(Cursor cursor)
    {
        int originalX = cursor.X;
        int originalY = cursor.Y;
        
        List<MatchIndex> resultIndexes = [];
        
        /* Query & Search */
        
        string query = Ui.Message.Input("Search: ", out bool cancelled);
        if (cancelled) return;
        
        for (int i = 0; i < Program.Buf.Length; ++i)
        {
            foreach (Match m in Regex.Matches(Program.Buf.GetLine(i), @$"(?i){Regex.Escape(query)}"))
            {
                MatchIndex match = new() { Index = m.Index, Line = i };
                resultIndexes.Add(match);
            }
        }
            
        /* Select */
        
        int index = 0;
        
        while (resultIndexes.Count > 0)
        {
            cursor.Set(resultIndexes[index].Index, resultIndexes[index].Line);
            
            Ui.Message.InvokeNoInput($"Results: ({index + 1}/{resultIndexes.Count})", Ui.Message.Type.None);

            ScrBuf.Flush();
            
            cursor.CursorScrSet();
            
            Program.ShowCursor();
            ConsoleKey input = Console.ReadKey(true).Key;

            switch (input)
            {
                case ConsoleKey.N:
                {
                    if (index < resultIndexes.Count - 1)
                        ++index;
                    else
                        index = 0;
                    break;
                }
                case ConsoleKey.B:
                    if (index > 0)
                        --index;
                    else
                        index = resultIndexes.Count - 1;
                    break;
                case ConsoleKey.Enter:
                {
                    return;
                }
                case ConsoleKey.Escape:
                {
                    cursor.Set(originalX, originalY);
                    return;
                }
            }
        }
        Ui.Message.Invoke("No matches were found.", Ui.Message.Type.Error);
    }

    public static void Quit()
    {
        AlertIfModified();
		Console.Clear();
        Environment.Exit(0);
    }
}