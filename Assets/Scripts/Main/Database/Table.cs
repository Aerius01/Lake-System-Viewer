public class Table
{
    public string tableName { get; private set; }
    public bool tableStatus { get; private set; }
    public string tableMessage { get; private set; }
    public int lightColor { get; private set; }

    public Table(string name) { this.tableName = name; this.tableMessage = ""; this.tableStatus = true;}

    public void SetStatus(bool status) { this.tableStatus = status; this.lightColor = status ? 0 : 2; }
    public void SetLight(int code) { this.lightColor = code; }
    public void SetMessage(string message)
    {
        if (this.tableMessage == "") this.tableMessage = message;
        else this.AddMessage(message);
    }
    private void AddMessage(string message) { this.tableMessage += string.Format("\n\n{0}", message); }
}