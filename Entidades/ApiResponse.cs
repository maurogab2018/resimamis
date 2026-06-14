namespace ResimamisBackend.Entidades
{
    public class ApiResponse
    {
        public object? data { get; set; }
        public string? message { get; set; }
        public List<string>? errors { get; set; }
    }
}
