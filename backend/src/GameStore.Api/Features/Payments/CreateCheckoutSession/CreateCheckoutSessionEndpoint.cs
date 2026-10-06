using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Data;
using GameStore.Api.Features.Orders.CreateOrder;
using GameStore.Api.Features.Payments.Constants;
using GameStore.Api.Shared.Authorization;
using GameStore.Api.Shared.Stripe;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GameStore.Api.Features.Payments.CreateCheckoutSession;

public static class CreateCheckoutSessionEndpoint
{
    public static void MapCreateCheckoutSession(this IEndpointRouteBuilder app)
    {
        // POST /checkout
        app.MapPost("/checkout", async (
            GameStoreContext dbContext,
            SessionService sessionService,
            ClaimsPrincipal user,
            OrderCreator orderCreator,
            CreateCheckoutSessionDto requestDto,
            IOptions<StripeOptions> stripeOptions,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Payments");

            var userIdClaimValue = user.FindFirstValue(GameStoreClaimTypes.UserId);
            if (!Guid.TryParse(userIdClaimValue, out var userId))
            {
                return Results.Forbid();
            }

            logger.LogInformation("Creating checkout session for user {UserId} and operation {OperationId}...",
                                userId,
                                requestDto.OperationId);

            var createOrderResult = await orderCreator.GetOrCreateOrderAsync(
                                    userId,
                                    requestDto.OperationId);

            if (createOrderResult.EmptyBasket)
            {
                return Results.BadRequest("No items in basket to create order");
            }

            var order = createOrderResult.Order;

            var lineItems = order.Items
                                .OrderBy(item => item.Id)
                                .Select(item => new SessionLineItemOptions
                                {
                                    PriceData = new SessionLineItemPriceDataOptions
                                    {
                                        Currency = "usd",
                                        UnitAmount = (long)(item.Price * 100),
                                        ProductData = new SessionLineItemPriceDataProductDataOptions
                                        {
                                            Name = item.ProductName,
                                            Images = [item.ImageUri]
                                        }
                                    },
                                    Quantity = item.Quantity
                                })
                                .ToList();

            var options = new SessionCreateOptions
            {
                UiMode = "custom",
                Mode = "payment",
                LineItems = lineItems,
                ReturnUrl = $"{stripeOptions.Value.CheckoutReturnUrl.TrimEnd('/')}/{order.Id}",
                CustomerEmail = user.FindFirstValue(JwtRegisteredClaimNames.Email),
                Metadata = new Dictionary<string, string>
                {
                    { MetadataKeys.OrderId, order.Id.ToString() }
                }
            };

            var requestOptions = new RequestOptions()
            {
                IdempotencyKey = $"cs-create-{order.Id}"
            };

            var session = await sessionService.CreateAsync(
                            options,
                            requestOptions);

            logger.LogInformation("Created checkout session: {SessionId}", session.Id);

            var responseDto = new CheckoutSessionDto(
                session.ClientSecret,
                order.Id,
                order.Items
                    .Select(item => new CheckoutSessionItemDto(
                        item.ProductId,
                        item.ProductName,
                        item.Price,
                        item.Quantity,
                        item.ImageUri
                    )));

            return Results.Ok(responseDto);
        })
        .WithParameterValidation();
    }
}
