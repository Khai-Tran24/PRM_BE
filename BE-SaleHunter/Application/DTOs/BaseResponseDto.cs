namespace BE_SaleHunter.Application.DTOs
{
    public class BaseResponseDto
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess => Code >= 200 && Code < 300;

        public static BaseResponseDto Success(string message = "Success")
        {
            return new BaseResponseDto
            {
                Code = ResponseCodes.SuccessfulOperation,
                Message = message
            };
        }

        public static BaseResponseDto Failure(string message = "Operation failed", int code = ResponseCodes.FailedRequestFailure)
        {
            return new BaseResponseDto
            {
                Code = code,
                Message = message
            };
        }
    }

    public class BaseResponseDto<T> : BaseResponseDto
    {
        public T? Data { get; set; }

        public static BaseResponseDto<T> Success(T data, string message = "Success", int code = ResponseCodes.SuccessfulOperation)
        {
            return new BaseResponseDto<T>
            {
                Code = code,
                Message = message,
                Data = data
            };
        }

        public static new BaseResponseDto<T> Failure(string message = "Operation failed", int code = ResponseCodes.FailedRequestFailure)
        {
            return new BaseResponseDto<T>
            {
                Code = code,
                Message = message,
                Data = default
            };
        }
    }

    public class RBaseResponseDto<T> : BaseResponseDto
    {
        public T? Data { get; set; }
    }

    // Response codes matching the mobile app
    public static class ResponseCodes
    {
        public const int SuccessfulOperation = 200;
        public const int SuccessfulCreation = 201;
        public const int FailedAuth = 401;
        public const int FailedNotFound = 404;
        public const int FailedRequestFailure = 400;
        public const int FailedServerError = 500;
    }
}
