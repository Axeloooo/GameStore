using Microsoft.Extensions.Options;
using Stripe;

namespace GameStore.Api.Shared.Stripe;

internal class StripeEventFactory(IOptions<StripeOptions> options) : IStripeEventFactory
{
    private readonly string endpointSecret = options.Value.EndpointSecret;

    public Event Create(string jsonBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(jsonBody))
        {
            throw new ArgumentException("Request body is required.", nameof(jsonBody));
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new ArgumentException("Stripe-Signature header is required.", nameof(signatureHeader));
        }

        // Stripe.net pins one API version, but events
        // arrive in the Stripe account's default version (e.g. 2022-11-15), which
        // makes ConstructEvent throw. The signature is still verified.
        return EventUtility.ConstructEvent(
            jsonBody,
            signatureHeader,
            endpointSecret,
            throwOnApiVersionMismatch: false);
    }
}
