namespace McpHealthServer
{
    public class Constants
    {
        public const string CheckApiStatus = "check_api_status";
        public const string Config_SessionCleanUp_Key = "SessionCleanup";

        //ساختار JSON معتییاست اما یک Object Request صحیح نیست)مثال فاقد متد(
        public const string Error_InvalidRequest = "Invalid Request";
        public const int ErrorCode_InvlidRequest = -32600;

        public const string Error_InvalidMethod = "Method not found";
        public const int ErrorCode_InvalidMethod = -32601;


        //درخواست توسط کلاینت یا سرور لغو شده است
        public const string Error_RequestCancelled = "Request Cancelled";
        public const int ErrorCode_RequestCancelled = -32800;

        //سرور نتوانست JSON ارسالی را پردازش کند
        public const string Error_ParseError = "Parse Error";
        public const int ErrorCode_ParseError = -32700;

        public const string InternalServerError = "Internal Error";
        public const int InternalServerError_Code = -32603;

        public const string Error_InvalidParams = "Invalid Params";
        public const int ErrorCode_InvalidParams = -32602;

        public const int ErrorCode_CustomeServerError = -32000;
    }
}
