using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

public class NegotiateFunction : FunctionBase
{
    [FunctionName("negotiate")]
    public SignalRConnectionInfo Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        [SignalRConnectionInfo(HubName = "zoudekuai")] SignalRConnectionInfo connectionInfo)
    {
        SetCorsHeaders(req);

        return connectionInfo;
    }
}
