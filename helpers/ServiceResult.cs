using Microsoft.AspNetCore.Http;

public  class ServiceResult<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string? Error { get; set; }  
    public int StatusCode { get; set;}

    public static ServiceResult<T> Ok(T data)
    {
        if (data == null)
        throw new ArgumentNullException(nameof(data));
        return new ServiceResult<T>
        {
            Success = true,
            Data = data,
            StatusCode = StatusCodes.Status200OK
            
        };
    }

    public static ServiceResult<T> Fail(string error, int statusCode = StatusCodes.Status400BadRequest)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Error = error,
            StatusCode = statusCode
            
        };
    }
}
