namespace Hi_Trade.Models.Common;

public class BaseResult<T> where T : class
{
    public T? Model { get; set; }
    public string? Message { get; set; }
    public ResultType ResultType { get; set; } = ResultType.Success;
}

