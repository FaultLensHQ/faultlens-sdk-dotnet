using System.Text.Json.Serialization;

namespace FaultLens.Sdk
{
    public sealed class DeliveryResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; }

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; }

        private DeliveryResult(bool success, string errorCode = null, string errorMessage = null)
        {
            Success = success;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public static DeliveryResult Delivered()
            => new DeliveryResult(true, null, null);

        public static DeliveryResult Failed(string errorCode, string errorMessage)
            => new DeliveryResult(false, errorCode, errorMessage);
    }
}

