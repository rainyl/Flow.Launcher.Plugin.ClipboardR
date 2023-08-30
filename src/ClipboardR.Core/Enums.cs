namespace ClipboardR.Core;

public enum CbContentType
{
    Text = 0,
    Image = 1,
    Files = 2,
    Other = 3,
}

public enum CbOrders
{
    Score = 0,
    CreateTime = 1,
    SourceApplication = 2,
    Type = 3,
}

public enum CbResultType
{
    Record,
    Clear,
}