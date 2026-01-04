namespace FaultLens.Sdk.Transport
{
    internal sealed class TransportResult
    {
        public bool Success { get; }
        public bool IsTransient { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }

        private TransportResult(bool success, bool isTransient, string code, string message)
        {
            Success = success;
            IsTransient = isTransient;
            ErrorCode = code;
            ErrorMessage = message;
        }

        public static TransportResult Delivered() => new TransportResult(true, false, null, null);

        public static TransportResult TransientFailure(string code, string message) => new TransportResult(false, true, code, message);

        public static TransportResult PermanentFailure(string code, string message) => new TransportResult(false, false, code, message);
    }
}
