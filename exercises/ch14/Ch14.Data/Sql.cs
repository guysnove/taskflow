using System.Text;
using Microsoft.Data.Sqlite;

namespace Ch14.Data;

// Tiny helpers so the demos read like SQL. Every demo uses a real SQLite file.
public static class Sql
{
    public static SqliteConnection Open(string path, bool fresh = true)
    {
        if (fresh)
        {
            SqliteConnection.ClearAllPools();
            foreach (var f in new[] { path, path + "-wal", path + "-shm" }) File.Delete(f);
        }
        var c = new SqliteConnection($"Data Source={path}");
        c.Open();
        return c;
    }

    public static int Exec(this SqliteConnection c, string sql, SqliteTransaction? tx = null)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(this SqliteConnection c, string sql, SqliteTransaction? tx = null)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        return cmd.ExecuteScalar();
    }

    // Rows as "a | b | c" lines, for printing.
    public static List<string> Rows(this SqliteConnection c, string sql, SqliteTransaction? tx = null)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        using var r = cmd.ExecuteReader();
        var rows = new List<string>();
        while (r.Read())
        {
            var line = new StringBuilder();
            for (int i = 0; i < r.FieldCount; i++) line.Append(i == 0 ? "" : " | ").Append(r.IsDBNull(i) ? "NULL" : r.GetValue(i));
            rows.Add(line.ToString());
        }
        return rows;
    }

    // Runs a statement and returns SQLite's error message, or "OK".
    public static string Try(this SqliteConnection c, string sql)
    {
        try
        {
            c.Exec(sql);
            return "OK";
        }
        catch (SqliteException ex)
        {
            return ex.Message;
        }
    }
}
