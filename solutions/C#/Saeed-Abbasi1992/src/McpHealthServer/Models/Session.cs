using System.Collections.Concurrent;

namespace McpHealthServer.Models
{
    public class Session
    {
        public Guid SessionId { get; init; }
        public List<string> Tools { get; set; } = new() { Constants.CheckApiStatus };
        public ConcurrentQueue<CheckStatusResponseBase> Messages { get; } = new();
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
    }
}
