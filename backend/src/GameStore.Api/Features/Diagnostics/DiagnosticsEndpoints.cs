using GameStore.Api.Features.Diagnostics.GetNodeInfo;

namespace GameStore.Api.Features.Diagnostics;

public static class DiagnosticsEndpoints
{
    public static void MapDiagnostics(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/diagnostics");

        group.MapGetNodeInfo();
    }
}
