namespace Centi_Text_Editor;

public static class Internal
{
    /* Copy and Paste functionality */
    private static string? _cutBuffer;

    public static void Copy(Cursor cursor)
    {
        _cutBuffer = Program.Buf.GetLine(cursor.Y);
    }
    
    public static void Paste(Cursor cursor)
    {
        if (_cutBuffer == null)
        {
            Ui.Message.Invoke("Clipboard is empty", Ui.Message.Type.Info);
            return;
        }
        
        Program.Buf.InsertLine(_cutBuffer, cursor);
    }
}