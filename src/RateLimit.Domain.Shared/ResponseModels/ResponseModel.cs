namespace RateLimit.ResponseModels;
public class ResponseModel<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public string[]? Errors { get; set; }

    public ResponseModel()
    { }

    public ResponseModel(bool success, T data)
    {
        Success = success;
        Data = data;
    }

    public ResponseModel(bool success, T data, string error)
    {
        Success = success;
        Data = data;
        Errors = [error];
    }

    public ResponseModel(bool success, T data, string[] errors)
    {
        Success = success;
        Data = data;
        Errors = errors;
    }
}

public class ResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public ResponseModel()
    { }

    public ResponseModel(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}