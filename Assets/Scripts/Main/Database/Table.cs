using System.Collections.Generic;

public class Table
{
    public string tableName { get; private set; }
    public string tableMessage { get; private set; }
    public int lightColor { get; private set; } // 0 - green, 1 - yellow, 2 - red
    public List<string> presentColumns { get; private set; }

    public bool status { get { if (this.lightColor == 2) return false; else return true; } }
    public bool imported = false;

    public Table(string name)
    { 
        this.tableName = name;
        this.tableMessage = ""; 
        this.lightColor = 0;
        this.presentColumns = new List<string>();
    }

    public void SetLight(int code) { this.lightColor = code; }
    public void SetMessage(string message)
    {
        if (this.tableMessage == "") this.tableMessage = message;
        else this.AddMessage(message);
    }
    private void AddMessage(string message) { this.tableMessage += string.Format("\n\n{0}", message); }

    public void Imported(bool status) { this.imported = status; }
}