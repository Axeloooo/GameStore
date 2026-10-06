import { CustomerBasket } from '../models/CustomerBasket';
import { CommandResult } from '../models/CommandResult';
import { BasketItem } from '../models/BasketItem';
import BasketClient from '../clients/BasketClient';

export class BasketState {
  private client: BasketClient;
  private userId: string | null;
  private cachedBasket: Promise<CustomerBasket> | null = null;
  private onBasketUpdated: (() => void) | null = null;

  constructor(client: BasketClient, userId: string | null) {
    this.client = client;
    this.userId = userId;
  }

  public async getBasketAsync(): Promise<CustomerBasket> {
    if (!this.cachedBasket) {
      this.cachedBasket = this.fetchBasketAsync();
    }
    return this.cachedBasket;
  }

  public async addItemAsync(newItem: BasketItem): Promise<CommandResult> {
    const basket = await this.getBasketAsync();
    basket.items.push(newItem);

    const result = await this.client.updateBasketAsync(basket);

    if (result.succeeded) {
      this.cachedBasket = null;
      this.notifyOnBasketUpdated();
    }

    return result;
  }

  public async updateQuantityAsync(id: string, quantity: number): Promise<CommandResult> {
    const basket = await this.getBasketAsync();
    const basketItem = basket.items.find(item => item.id === id);

    if (!basketItem) {
      return { succeeded: true, errors: [] };
    }

    basketItem.quantity = quantity;

    const result = await this.client.updateBasketAsync(basket);

    if (result.succeeded) {
      this.cachedBasket = null;
      this.notifyOnBasketUpdated();
    }

    return result;
  }

  public async removeItemAsync(itemId: string): Promise<CommandResult> {
    const basket = await this.getBasketAsync();
    basket.items = basket.items.filter(item => item.id !== itemId);

    const result = await this.client.updateBasketAsync(basket);

    if (result.succeeded) {
      this.cachedBasket = null;
      this.notifyOnBasketUpdated();
    }

    return result;
  }

  public setOnBasketUpdated(callback: () => void): void {
    this.onBasketUpdated = callback;
  }

  private async fetchBasketAsync(): Promise<CustomerBasket> {
    if (!this.userId) {
      return { customerId: '', items: [], totalAmount: 0 };
    }

    return await this.client.getBasketAsync(this.userId);
  }

  private notifyOnBasketUpdated(): void {
    if (this.onBasketUpdated) {
      this.onBasketUpdated();
    }
  }
}