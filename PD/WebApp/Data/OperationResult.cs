namespace WebApp.Data;
public class OperationResult
{
    public bool Status { get; }
    public string Message { get; }
    public object? Result { get; }

    public OperationResult(bool status, string message = "", object? result = null)
    {
        Status = status;
        Message = message;
        Result = result;
    }
}
