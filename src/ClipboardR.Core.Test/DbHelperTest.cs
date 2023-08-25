using System.Windows.Media.Imaging;
using Xunit.Abstractions;
using System.Drawing;
using ClipboardR.Core;

namespace ClipboardR.Core.Test;

public class DbHelperTest
{
    private static string _defaultIconPath = "Images/clipboard.png";

    private readonly ClipboardData TestRecord = new()
    {
        Text = "",
        Type = CbMonitor.ContentTypes.Image,
        Data = (object)"Test Text",
        SenderApp = "Source.exe",
        DisplayTitle = "Text Test",
        IconPath = _defaultIconPath,
        Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
        PreviewImagePath = _defaultIconPath,
        Score = 241,
        InitScore = 1,
        Time = DateTime.Now,
        Pined = false,
        CreateTime = DateTime.Now,
    };

    private readonly ITestOutputHelper _testOutputHelper;

    public DbHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestCreateDb()
    {
        var helper = new DbHelper("Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared");
        helper.CreateDb();
        var command = helper.Connection.CreateCommand();
        command.CommandText = "SELECT name from sqlite_master WHERE name='record'";
        Assert.True(command.ExecuteScalar() is string name && name.ToString() == "record");
    }

    [Fact]
    public void TestInsertRecord()
    {
        // test text
        var exampleTextRecord = TestRecord;
        var helper = new DbHelper("Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared");
        helper.CreateDb();
        helper.AddOneRecord(exampleTextRecord);
        var c = helper.GetAllRecord().First();
        helper.Close();
        Assert.True(c.Equals(exampleTextRecord));
    }

    [Theory]
    [InlineData(0, "2023-06-01", 3)]
    [InlineData(1, "2023-06-01", 24)]
    [InlineData(2, "2023-06-01", 72)]
    public void TestDeleteRecordBefore(int type, string creatTime, uint keepTime)
    {
        var exampleTextRecord = TestRecord;
        exampleTextRecord.CreateTime = DateTime.Parse(creatTime);
        var helper = new DbHelper("Data Source=InMemoryTestDb;Mode=Memory;Cache=Shared");
        helper.CreateDb();
        var spans = Enumerable.Range(0, 5000).Skip(12).Select(i => TimeSpan.FromHours(i));
        var rng = new Random();
        foreach (var s in spans)
        {
            var tmpRecord = exampleTextRecord;
            tmpRecord.Type = (CbMonitor.ContentTypes)rng.Next(0, 4);
            helper.AddOneRecord(tmpRecord);
        }

        helper.DeleteRecordByKeepTime(type, keepTime);

        var recordsAfterDelete = helper.GetAllRecord();
        foreach (var record in recordsAfterDelete)
        {
            Assert.True(record.CreateTime + TimeSpan.FromDays(keepTime / 24) >= DateTime.Now);
        }
        helper.Close();
    }
}