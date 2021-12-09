using System.Data;

public class StringTable
{
    public DataTable table;
    public int nullCounter = 0;
    public bool parsingComplete = false;

    public DataTable parseTable(string csvText)
    {
        parsingComplete = false;

        // determine number of columns by splitting on the commas before the first linebreak
        int positionOfNewLine = csvText.IndexOf("\n");
        if (positionOfNewLine >= 0)
        {
            string partBefore = csvText.Substring(0, positionOfNewLine);
            int totalColumns = partBefore.Trim().Split(',').Length;

            table = new DataTable();
            for (int i = 0; i < totalColumns; i++)
            {
                table.Columns.Add(string.Format("{0}", i), typeof(string));
            }
        }

        // add rows individually
        foreach (string line in csvText.Split("\n"[0]))
        {
            // clean up the edges of the row and then split it
            string[] row = line.Replace("\"", "").Trim().Split(',');
            bool emptyRow = true;

            foreach(string entry in row)
            {
                if (string.IsNullOrEmpty(entry) || string.IsNullOrWhiteSpace(entry))
                {
                    nullCounter++;
                }
                else
                {
                    emptyRow = false;
                }
            }

            // only add the row if it's populated
            if (!emptyRow)
            {
                // TODO: wrap this in a try-catch after some quality testing
                // what if the number of entries is too big or too small for the number of columns in the table?
                // if the row add fails, move to the next foreach
                table.Rows.Add(row);
            }
        }

        parsingComplete = true;
        return table;
    }
}