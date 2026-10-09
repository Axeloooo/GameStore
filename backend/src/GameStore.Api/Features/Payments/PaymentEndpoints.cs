using GameStore.Api.Features.Payments.CreateCheckoutSession;
using GameStore.Api.Features.Payments.StripeWebhook;

namespace GameStore.Api.Features.Payments;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPayments(this WebApplication app)
    {
        var group = app.MapGroup("/payments");

        group.MapCreateCheckoutSession();
        group.MapStripeWebhook();

        return group;
    }
}
