using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Shared.Stripe;

public class StripeOptions
{
    [Required]
    public required string SecretKey { get; set; }

    [Required]
    public required string CheckoutReturnUrl { get; set; }

    [Required]
    public required string EndpointSecret { get; set; }
}
