using System.Diagnostics.CodeAnalysis;
using GameStore.Data.Models;

namespace GameStore.Api.Features.Orders.CreateOrder;

public record class CreateOrderResult(
    Order? Order = null,
    [property: MemberNotNullWhen(returnValue: false, member: nameof(Order))]
    bool EmptyBasket = false
);
