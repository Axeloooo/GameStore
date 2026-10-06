namespace GameStore.Api.Features.Diagnostics.GetNodeInfo;

public static class GetNodeInfoEndpoint
{
    public static void MapGetNodeInfo(this IEndpointRouteBuilder app)
    {
        app.MapGet("/nodeInfo", () =>
        {
            return new { NodeName = Environment.MachineName };
        });
    }
}
