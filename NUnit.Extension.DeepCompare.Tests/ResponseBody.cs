namespace DeepCompare.NUnitExtension.Tests
{
    internal class ResponseBody
    {
        public int StatusCode { get; set; }

        public bool IsSuccess { get; set; }

        public string? Message { get; set; }

        public ICollection<int>? Numbers { get; set; }

        public ICollection<string>? Strings { get; set; }

        public Method? Method { get; set; }

        public InnerMessage? InnerMessage { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? CreateDateNullable { get; set; }

        public DateTimeOffset CreateDateOffset { get; set; }

        public DateTimeOffset? CreateDateOffsetNullable { get; set; }

    }

    internal class InnerMessage
    {
        public string? Message { get; set; }
    }

    internal enum Method
    {
        None,
        GET,
        POST,
        PUT,
        DELETE
    }
}
