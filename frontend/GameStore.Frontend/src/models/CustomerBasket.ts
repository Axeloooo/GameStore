import { BasketItem } from './BasketItem';

export interface CustomerBasket {
    customerId: string;
    items: BasketItem[];
    totalAmount: number;
}