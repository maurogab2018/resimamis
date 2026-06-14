namespace ResimamisBackend.Entidades
{
    public class ApiResponse
    {
        public bool success { get; set; }
        public object? data { get; set; }
        public string? message { get; set; }
        public List<string> errors { get; set; } = new();
    }
}
