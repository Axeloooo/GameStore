using Stripe;

namespace GameStore.Api.Shared.Stripe;

public interface IStripeEventFactory
{
    Event Create(string jsonBody, string signatureHeader);
}
