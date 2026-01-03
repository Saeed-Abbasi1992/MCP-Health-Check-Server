using System.Text.Json;

namespace McpHealthServer.Models
{
    public class JsonRpcRequest
    {
        public string Jsonrpc { get; set; } = "2.0";
        public int Id { get; set; }
        public string Method { get; set; }
        public JsonElement Params { get; set; }
    }
}
