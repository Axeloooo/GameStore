export interface CreateCheckoutSessionDto {
    operationId: string;
}

export interface CheckoutSessionItemDto {
    productId: string;
    productName: string;
    price: number;
    quantity: number;
    imageUri: string;
}

export interface CheckoutSessionDto {
    clientSecret: string;
    sessionId: string;
    items: CheckoutSessionItemDto[];
}
