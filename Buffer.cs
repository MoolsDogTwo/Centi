using System.Text;

namespace Centi_Text_Editor;

public class Buffer
{
    public string Title = "Untitled";
    private string _filePath = Environment.CurrentDirectory;
    public long FileSize;
    private List<Line> _buffer = [];
    private List<Line> _originalBuffer = [];
    public int Length => _buffer.Count;
    public State CurrentState = State.New;
    private bool IsNewFile { get; set; } = true;
    private bool _hasBeenSaved;
    
    public void DefaultInit()
    {
        Title = "Untitled";
        _filePath = Environment.CurrentDirectory;
        IsNewFile = true;
        FileSize = 1;
        _buffer = [];
        _originalBuffer = [];
        CurrentState = State.New;
        _buffer.Add(new Line(""));
    }
    
    /* Constructors */
    
    public Buffer()
    {
        DefaultInit();
    }

    public Buffer(string filePath)
    {
        Init(filePath);
    }

    /* Data structures */
    
    public enum State
    {
        New,
        Opened,
        Modified,
        Saved,
    }
    
    public class Line
    {
        private string _contents = "";
        
        public string Contents
        {
            get => _contents;
            set
            {
                _contents = value;
                RContents = "";

                StringBuilder rendered = new();

                int index = 0;
                foreach (char c in value)
                {
                    if (c == '\t')
                    {
                        rendered.Append(' ');
                        ++index;
                        while (index % Program.ShiftWidth != 0)
                        {
                            rendered.Append(' ');
                            ++index;
                        }
                    }
                    else
                    {
                        rendered.Append(c);
                        ++index;
                    }
                }
                RContents = rendered.ToString() ?? throw new InvalidOperationException();
            }
        }

        public string RContents { get; private set; } = "";

        public Line(string str)
        {
            Contents = str;
        }
    }

    /* File operations */
    
    public void Init(string filePath, bool saved = false)
    {
        if (!File.Exists(filePath))
        {
            DefaultInit();
            return;
        }
        
        _buffer.Clear();

        FileInfo fInfo = new(filePath);
        Title = fInfo.Name;
        _filePath = fInfo.FullName;
        FileSize = fInfo.Length;
        CurrentState = !saved ? State.Opened : State.Saved;
        IsNewFile = false;

        bool anyBytesRead = false;
        using StreamReader f = new(filePath);
        while (f.ReadLine() is { } line)
        {
            anyBytesRead = true;
            _buffer.Add(new Line(line));
        }

        if (!anyBytesRead) // Prevent crashing on 0 byte files.
            _buffer.Add(new Line(""));

        _originalBuffer = _buffer.ToList();
        
        f.Close();
    }

    
    public void SaveCurrentFile(bool reopenFile = true)
    {
        string fileName = Title;
    
        if (Program.Buf.IsNewFile)
        {
            while (true)
            {
                fileName = Ui.Message.Input("Write a name for new buffer", out bool c);
                if (c) return;

                if (FileActions.FileNameNotValid(fileName) || fileName.Length == 0)
                {
                    Ui.Message.Invoke("File name cannot be empty or contain invalid characters", Ui.Message.Type.Error);
                }
                else if (File.Exists(fileName))
                {
                    if (Ui.Message.Yn("File already exists. Overwrite it?"))
                        break;
                }
                else
                    break;
            }
        }
    
        StreamWriter f;
        try
        {
            f = new StreamWriter(_filePath, false);
        }
        catch (UnauthorizedAccessException)
        {
            f = new StreamWriter(Path.Combine(_filePath, fileName), false);
        }
        
        for (int i = 0; i < Length; ++i)
        {
            f.Write(i == Length - 1 ? GetLine(i) : GetLine(i) + '\n');
        }
        
        _hasBeenSaved = true;
        
        f.Close();
        
        if (reopenFile)
            Init(fileName, true);
    }

    /* Meta functions */
    
    private void CheckIndex(int index)
    {
        if (index > _buffer.Count || index < 0)
            throw new IndexOutOfRangeException("Inaccessible item.");
    }

    public string GetLine(int index)
    {
        CheckIndex(index);
        return _buffer[index].Contents;
    }

    public string GetScrLine(int index)
    {
        CheckIndex(index);
        return _buffer[index].RContents;
    }
    
    /* Editing functions */
    
    /* Editing: Insertion operations */
    
    public void InsertChar(char c, Cursor cursor)
    {
        _buffer[cursor.Y].Contents = GetLine(cursor.Y).Insert(cursor.X, c.ToString());
        cursor.Right();

        CurrentState = State.Modified;
    }
    
    public void Append(Cursor cursor)
    {
        _buffer.Insert(cursor.Y + 1, new Line(GetLine(cursor.Y)[cursor.X..]));
        _buffer[cursor.Y].Contents = GetLine(cursor.Y)[..cursor.X];
        cursor.Down();
        
        CurrentState = State.Modified;
    }
    
    public void InsertLine(string str, Cursor cursor)
    {
        Append(cursor);
        _buffer[cursor.Y].Contents = str;
        cursor.End();

        CurrentState = State.Modified;
    }
    
    /* Editing: Deletion operations */
    
    public void Delete(Cursor cursor)
    {
        if (GetLine(cursor.Y).Length > 0 && cursor.X > 0)
        {
            DeleteChar(cursor);
        }
        else if (cursor is { Y: > 0, X: 0 } && GetLine(cursor.Y).Length > 0)
        {
            JoinLine(cursor);
        }
        else if (cursor.Y > 0 && GetLine(cursor.Y).Length == 0)
            DeleteLine(cursor);
        
        // We don't need to set the buffer's current state here!
    }

    private void DeleteChar(Cursor cursor)
    {
        _buffer[cursor.Y].Contents = GetLine(cursor.Y).Remove(cursor.X - 1, 1);
        cursor.Left();
        
        CurrentState = State.Modified;
    }

    private void JoinLine(Cursor cursor)
    {
        int lineJoinPos = GetLine(cursor.Y).Length;
        string line = GetLine(cursor.Y - 1);
        _buffer[cursor.Y - 1].Contents = line + GetLine(cursor.Y);
        DeleteLine(cursor);
        cursor.X -= lineJoinPos;

        CurrentState = State.Modified;
    }

    public void DeleteLine(Cursor cursor)
    {
        if (_buffer.Count > 1 && cursor.Y > 0)
        {
            _buffer.RemoveAt(cursor.Y);
                
            cursor.Up();
            cursor.End();
        }
        else
        {
            _buffer[cursor.Y].Contents = "";
            cursor.Home();
        }
        
        CurrentState = State.Modified;
    }
}

public static class ScrBuf
{
    private static readonly StringBuilder Buffer = new();

    public static void Append(string str)
    {
        Buffer.Append(str);
    }
    
    public static void Append(char c)
    {
        Buffer.Append(c);
    }

    public static void EmptyAppend()
    {
        Buffer.Append('\n');
    }

    public static void Flush()
    {
        Console.Write(Buffer.ToString());
        Buffer.Clear();
    }
}