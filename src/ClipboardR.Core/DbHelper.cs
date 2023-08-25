using System.Data;
using Microsoft.Data.Sqlite;

namespace ClipboardR.Core;

public class DbHelper
{
    public SqliteConnection Connection { get; private set; }
    public bool KeepConnection { get; private set; }

    public DbHelper(SqliteConnection connection)
    {
        this.Connection = connection;
    }

    public DbHelper(string dbPath, bool keepConnection = true)
    {
        this.Connection = new SqliteConnection(dbPath);
        this.KeepConnection = keepConnection;
    }

    public bool CreateDb()
    {
        Connection.Open();
        var command = Connection.CreateCommand();
        command.CommandText = "SELECT name from sqlite_master WHERE name='record'";
        if (command.ExecuteScalar() is "record")
            return true;
        command.CommandText =
            """
                CREATE TABLE assets (
                 id	INTEGER NOT NULL UNIQUE,
                 data_b64	TEXT,
                 md5 TEXT UNIQUE ,
                 PRIMARY KEY("id" AUTOINCREMENT)
                );
            
                CREATE TABLE "record" (
                 "id"	INTEGER NOT NULL UNIQUE,
                 "hash_id"	TEXT UNIQUE,
                 "data"	TEXT,
                 "text"	TEXT,
                 "display_title"	TEXT,
                 "senderapp"	TEXT,
                 "icon_path"	TEXT,
                 "icon_id"	INTEGER,
                 "preview_image_path"	TEXT,
                 "content_type"	INTEGER,
                 "score"	INTEGER,
                 "init_score"	INTEGER,
                 "time"	TEXT,
                 "create_time"	TEXT,
                 "pined"	INTEGER,
                 PRIMARY KEY("id" AUTOINCREMENT),
                 FOREIGN KEY("icon_id") REFERENCES "assets"("md5")
                );
            """;
        command.ExecuteNonQuery();
        CloseIfNotKeep();
        return true;
    }

    public void AddOneRecord(ClipboardData data)
    {
        Connection.Open();
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        var command = Connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO assets(data_b64, md5) VALUES ($data_b64, $md5)";
        command.Parameters.AddWithValue("$data_b64", iconB64);
        command.Parameters.AddWithValue("$md5", iconMd5);
        command.ExecuteNonQuery();
        
        string? insertData = "";
        switch (data.Type)
        {
            case CbMonitor.ContentTypes.Text:
                insertData = data.Data as string;
                break;
            case CbMonitor.ContentTypes.Image:
                insertData = iconB64;
                break;
            case CbMonitor.ContentTypes.Files:
                insertData = data.Data is not string[] t ? "" : string.Join('\n', t);
                break;
            default:
                // don't process others
                return;
        }

        command.CommandText =
            """
            INSERT OR IGNORE INTO record(hash_id, data, text, display_title,
                               senderapp, icon_path, icon_id,
                               preview_image_path, content_type,
                               score, init_score, time,
                               create_time, pined)
            VALUES ($hash_id, $data, $text, $display_title,
                   $senderapp, $icon_path, $icon,
                   $preview_image_path, $content_type,
                   $score, $init_score, $time,
                   $create_time, $pined);
            """;
        command.Parameters.AddWithValue("$hash_id", data.GetHashCode());
        command.Parameters.AddWithValue("$data", insertData);
        command.Parameters.AddWithValue("$text", data.Text);
        command.Parameters.AddWithValue("$display_title", data.DisplayTitle);
        command.Parameters.AddWithValue("$senderapp", data.SenderApp);
        command.Parameters.AddWithValue("$icon_path", data.IconPath);
        command.Parameters.AddWithValue("$icon", iconMd5);
        command.Parameters.AddWithValue("$preview_image_path", data.PreviewImagePath);
        command.Parameters.AddWithValue("$content_type", data.Type);
        command.Parameters.AddWithValue("$score", data.Score);
        command.Parameters.AddWithValue("$init_score", data.InitScore);
        command.Parameters.AddWithValue("$time", data.Time);
        command.Parameters.AddWithValue("$create_time", data.CreateTime);
        command.Parameters.AddWithValue("$pined", data.Pined);

        command.ExecuteNonQuery();
        CloseIfNotKeep();
    }

    public void DeleteOneRecord(ClipboardData clipboardData)
    {
        Connection.Open();
        var command = Connection.CreateCommand();
        command.CommandText = "DELETE from record WHERE hash_id == $hash_id";
        command.Parameters.AddWithValue("$hash_id", clipboardData.GetHashCode());
        command.ExecuteNonQuery();
        CloseIfNotKeep();
    }

    public void PinOneRecord(ClipboardData clipboardData)
    {
        Connection.Open();
        var command = Connection.CreateCommand();
        command.CommandText = "UPDATE record SET pined=$pined WHERE hash_id == $hash_id";
        command.Parameters.AddWithValue("$pined", clipboardData.Pined);
        command.Parameters.AddWithValue("$hash_id", clipboardData.GetHashCode());
        command.ExecuteNonQuery();
        CloseIfNotKeep();
    }

    public LinkedList<ClipboardData> GetAllRecord()
    {
        Connection.Open();
        var command = Connection.CreateCommand();
        command.CommandText =
            "SELECT r.data, r.text, r.display_title, r.senderapp, r.icon_path, " +
            "a.data_b64, r.preview_image_path, r.content_type,score, r.init_score, " +
            "r.time, r.create_time, r.pined " +
            "FROM record r, assets a WHERE r.icon_id=a.md5";
        LinkedList<ClipboardData> allRecord = new();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var type = (CbMonitor.ContentTypes)Convert.ToInt32(reader.GetValue(7));
            
            var clipboardData = new ClipboardData
            {
                Data = reader.GetString(0),
                Text = reader.GetString(1),
                DisplayTitle = reader.GetString(2),
                SenderApp = reader.GetString(3),
                IconPath = reader.GetString(4),
                Icon = reader.GetString(5).ToBitmapImage(),
                PreviewImagePath = reader.GetString(6),
                Type = type,
                Score = reader.GetInt32(8),
                InitScore = reader.GetInt32(9),
                Time = reader.GetDateTime(10),
                CreateTime = reader.GetDateTime(11),
                Pined = reader.GetBoolean(12),
            };
            switch (type)
            {
                case CbMonitor.ContentTypes.Text:
                    clipboardData.Data = (clipboardData.Data as string)!;
                    break;
                case CbMonitor.ContentTypes.Image:
                    clipboardData.Data = (clipboardData.Data as string)!.ToImage();
                    break;
                case CbMonitor.ContentTypes.Files:
                    var t = (clipboardData.Data as string)!;
                    clipboardData.Data = t.Split('\n');
                    break;
                default:
                    break;
            }
            allRecord.AddLast(clipboardData);
        }

        CloseIfNotKeep();

        return allRecord;
    }

    public void DeleteRecordByKeepTime(int contentType, uint keepTime)
    {
        Connection.Open();
        var command = Connection.CreateCommand();
        command.CommandText = "DELETE FROM record WHERE julianday(create_time) + $keepTime / 24 < julianday('now')";
        command.Parameters.AddWithValue("$keepTime", keepTime);
        command.ExecuteNonQuery();
        CloseIfNotKeep();
    }

    public void Close()
    {
        if ((Connection.State & ConnectionState.Open) != 0)
            Connection.Close();
    }

    private void CloseIfNotKeep()
    {
        if (!KeepConnection)
            Connection.Close();
    }
}