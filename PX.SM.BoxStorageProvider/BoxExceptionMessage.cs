using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.SM.BoxStorageProvider
{
    public class BoxExceptionMessage
    {
        public int Status { get; set; }

        public string Type { get; set; }

        public string Code { get; set; }

        [JsonProperty("help_url")]
        public string HelpUrl { get; set; }

        public string Message { get; set; }

        [JsonProperty("request_id")]
        public string RequestID { get; set; }

        public virtual string GetErrorMessage()
        {
            var builder = new StringBuilder($@"
                {Messages.BoxServiceError}
                Type: {Type}
                Status : {Status}
                Code : {Code}
                Message : {Message}
                RequestID : {RequestID}
                HelpUrl : {HelpUrl}"
            );

            return builder.ToString();
        }
    }

    public class BoxExceptionMessageWithContext : BoxExceptionMessage
    {
        [JsonProperty("context_info")]
        public BoxExceptionContextInfo ContextInfo { get; set; }

        public override string GetErrorMessage()
        {
            var builder = new StringBuilder(base.GetErrorMessage());
            foreach (var error in ContextInfo.Errors)
            {
                builder.Append($"Reason : {error.Reason} Name : {error.Name} Message : {error.Message}");
            }

            return builder.ToString();
        }
    }

    public class BoxExceptionContextInfo
    {
        public IEnumerable<BoxExceptionErrors> Errors { get; set; }
    }

    
    public class BoxExceptionErrors
    {
        public string Reason { get; set; }

        public string Name { get; set; }

        public string Message { get; set; }
    }
}
