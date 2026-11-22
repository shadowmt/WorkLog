namespace WorkLog.Application.Exceptions;

public sealed class DuplicateProjectNameException : Exception
{
    public DuplicateProjectNameException()
        : base("同じ名前のプロジェクトが既に存在します。")
    {
    }
}
