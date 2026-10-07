using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.Api.Shared.Stripe;

public static class StripeExtensions
{
    public static IHostApplicationBuilder AddStripe(
        this IHostApplicationBuilder builder)
    {
        var stripeSection = builder.Configuration.GetSection("Stripe");

        builder.Services.AddOptions<StripeOptions>()
                        .Bind(stripeSection)
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StripeOptions>>()
                            .Value;
            return new StripeClient(options.SecretKey);
        });

        builder.Services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<StripeClient>();
            return new SessionService(client);
        });

        builder.Services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<StripeClient>();
            return new PaymentIntentService(client);
        });

        builder.Services.AddSingleton<IStripeEventFactory, StripeEventFactory>();

        return builder;
    }
}
