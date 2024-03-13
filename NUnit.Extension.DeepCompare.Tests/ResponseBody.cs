namespace NUnit.Extension.DeepCompare.Tests
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
