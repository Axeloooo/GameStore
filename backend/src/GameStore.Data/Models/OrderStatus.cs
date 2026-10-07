namespace GameStore.Data.Models;

public enum OrderStatus
{
    Unknown = 0, // Default
    Pending = 1, // Order created, pending payment
    Processing = 2, // Payment confirmed, fulfillment started
    Completed = 3 // Fulfillment completed
}
