using GameStore.Api.Features.Orders.ConfirmOrderPayment;
using GameStore.Api.Features.Payments.Constants;
using GameStore.Api.Shared.Stripe;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.Api.Features.Payments.StripeWebhook;

public static class StripeWebhookEndpoint
{
    public static void MapStripeWebhook(this IEndpointRouteBuilder app)
    {
        // POST /payments/stripe-webhook
        app.MapPost("stripe-webhook", async (
            HttpContext context,
            PaymentIntentService paymentIntentService,
            ConfirmOrderPaymentOperation confirmOrderPayment,
            IStripeEventFactory stripeEventFactory,
            IOptions<StripeOptions> options,
            ILoggerFactory loggerFactory
        ) =>
        {
            var logger = loggerFactory.CreateLogger("Payments");

            var jsonBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var signature = context.Request.Headers["Stripe-Signature"].ToString();

            try
            {
                var stripeEvent = stripeEventFactory.Create(jsonBody, signature);

                logger.LogInformation("Received Stripe event: {EventType}", stripeEvent.Type);

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    if (stripeEvent.Data.Object is not Session session)
                    {
                        logger.LogError(
                            "Unexpected object type {ObjType} for event {Id}",
                            stripeEvent.Data.Object.Object,
                            stripeEvent.Id);
                        return Results.BadRequest();
                    }

                    if (!session.Metadata.TryGetValue(MetadataKeys.OrderId, out var orderIdString)
                                        || !Guid.TryParse(orderIdString, out var orderId))
                    {
                        logger.LogError(
                            "Missing or invalid OrderId metadata on session {Id}",
                            session.Id);
                        return Results.BadRequest();
                    }

                    logger.LogInformation(
                        "Payment succeeded for checkout session {SessionId} and order {OrderId}",
                        session.Id,
                        orderId);

                    var paymentIntentOptions = new PaymentIntentGetOptions
                    {
                        Expand = ["payment_method"]
                    };

                    var paymentIntent = await paymentIntentService.GetAsync(
                        session.PaymentIntentId,
                        paymentIntentOptions
                    );

                    var cardBrand = paymentIntent.PaymentMethod.Card.Brand;
                    var cardLast4 = paymentIntent.PaymentMethod.Card.Last4;

                    if (session.AmountTotal is null)
                    {
                        logger.LogError("Session {SessionId} completed without AmountTotal for order {OrderId}",
                                        session.Id,
                                        orderId);
                        return Results.BadRequest();
                    }

                    var amountCharged = session.AmountTotal.Value / 100m;

                    var paymentConfirmed = await confirmOrderPayment.ExecuteAsync(
                        orderId,
                        paymentIntent.Id,
                        cardBrand,
                        cardLast4,
                        amountCharged
                    );

                    if (!paymentConfirmed)
                    {
                        logger.LogWarning("Failed to confirm payment information on order {OrderId}",
                                        orderId);
                        return Results.BadRequest(); // 400 -> Event handled, found issues
                    }

                    logger.LogInformation("Successfully processed payment for order {OrderId}",
                                            orderId);
                }

                return Results.Ok(); // 200 -> Event handled, all good!
            }
            catch(StripeException ex)
            {
                logger.LogWarning(ex, "Stripe webhook signature verification failed");
                return Results.BadRequest("Invalid Stripe signature");
            }
        })
        .AllowAnonymous();
    }
}
