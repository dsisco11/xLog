using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

public static class ConsoleDataTable
{
    public static string Stringify(DataTable Data, int MaxColumnSize = 0)
    {
        if (Data == null)
        {
            throw new ArgumentNullException(nameof(Data));
        }

        int[] ColumnSize = Data.Columns.ToList().Select(col => col.ColumnName.Length+2).ToArray();

        for (int i = 0; i < Data.Rows.Count; i++)
        {
            DataRow row = Data.Rows[i];
            for(int x=0; x<Data.Columns.Count; x++)
            {
                ColumnSize[x] = Math.Max(ColumnSize[x], row.ItemArray[x].ToString().Length+2);
            }
        }

        if (MaxColumnSize > 0)
        {
            for (int x = 0; x < Data.Columns.Count; x++)
            {
                ColumnSize[x] = Math.Max(ColumnSize[x], MaxColumnSize);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("--" + Data.TableName + "--");
        //sb.AppendLine(string.Join(" | ", Data.Columns.ToList()));
        // Write the column names
        string[] colNames = new string[Data.Columns.Count];
        for (int x = 0; x < Data.Columns.Count; x++)
        {
            string val = Data.Columns[x].ToString();
            int olen = Math.Min(val.Length, ColumnSize[x]);
            val = val.Substring(0, olen);
            // Pad the column value if its smaller then the column space
            int pad;
            if ((pad = ColumnSize[x] - val.Length) > 0)
                val += new string(' ', pad);

            colNames[x] = val;
        }
        sb.AppendLine(string.Join(" | ", colNames));
        // Now append a separator to make the table header with the column names more distinct
        string[] separators = new string[Data.Columns.Count];
        for (int x = 0; x < Data.Columns.Count; x++)
        {
            separators[x] = new string('-', ColumnSize[x]);
        }
        sb.AppendLine(string.Join("-|-", separators));

        // Write the data values
        for (int y=0; y<Data.Rows.Count; y++)
        {
            DataRow row = Data.Rows[y];
            string[] colValues = new string[Data.Columns.Count];

            for (int x = 0; x < Data.Columns.Count; x++)
            {
                string val = row.ItemArray[x].ToString();
                int olen = Math.Min(val.Length, ColumnSize[x]);
                val = val.Substring(0, olen);
                // Pad the column value if its smaller then the column space
                int pad;
                if ((pad = ColumnSize[x] - val.Length) > 0)
                    val += new string(' ', pad);

                colValues[x] = val;
            }

            sb.AppendLine(string.Join(" | ", colValues));
        }

        return sb.ToString();
    }


    public static void AddRange(this DataColumnCollection collection, params string[] columns)
    {
        foreach (var column in columns)
        {
            collection.Add(column);
        }
    }

    public static List<DataTable> ToList(this DataTableCollection collection)
    {
        var list = new List<DataTable>();
        foreach (var table in collection)
        {
            list.Add((DataTable)table);
        }
        return list;
    }

    public static List<DataColumn> ToList(this DataColumnCollection collection)
    {
        var list = new List<DataColumn>();
        foreach (var column in collection)
        {
            list.Add((DataColumn)column);
        }
        return list;
    }
}