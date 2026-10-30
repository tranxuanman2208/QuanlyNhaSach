namespace QuanlyNhaSach.Models
{
    public class OperationResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        protected OperationResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static OperationResult Success(string message = "") => new(true, message);
        public static OperationResult Failure(string message) => new(false, message);
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; }

        private OperationResult(bool isSuccess, string message, T? data)
            : base(isSuccess, message)
        {
            Data = data;
        }

        public static OperationResult<T> Success(T data, string message = "") => new(true, message, data);
        public new static OperationResult<T> Failure(string message) => new(false, message, default);
    }
}
