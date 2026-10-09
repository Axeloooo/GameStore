export interface OrdersPageDto {
    totalPages: number;
    data: OrderDto[];
}

export interface OrderDto {
    id: string;
    orderNumber: number;
    customerId: string;
    created: string; // ISO 8601 date string
    status: string;
    totalAmount: number;
    paymentCardBrand?: string;
    paymentCardLast4?: string;
    items: OrderItemDto[];
}

export interface OrderItemDto {
    productId: string;
    productName: string;
    price: number;
    quantity: number;
    imageUri: string;
    gameCodes: string[];
}
