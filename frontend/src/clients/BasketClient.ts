import { CustomerBasket } from '../models/CustomerBasket';
import { CommandResult } from '../models/CommandResult';

class BasketClient {
  private baseUrl = import.meta.env.VITE_BACKEND_API_URL;
  private accessToken: string | null;

  constructor(accessToken: string | null = null) {
    this.accessToken = accessToken;
  }

  async getBasketAsync(customerId: string): Promise<CustomerBasket> {
    const response = await this.fetchWithHandling(`${this.baseUrl}/baskets/${customerId}`);
    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      throw new Error(errorMessages.join('\n'));
    }
    return await response.json();
  }

  async updateBasketAsync(updatedBasket: CustomerBasket): Promise<CommandResult> {
    const dto: UpdateBasketDto = {
      items: updatedBasket.items.map(item => ({
        id: item.id,
        quantity: item.quantity
      }))
    };

    const response = await this.fetchWithHandling(`${this.baseUrl}/baskets/${updatedBasket.customerId}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
      headers: {
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const errorMessages = await this.handleFetchError(response);
      return { succeeded: false, errors: errorMessages };
    }

    return { succeeded: true, errors: [] };
  }

  private async fetchWithHandling(url: string, options?: RequestInit): Promise<Response> {
    const headers = new Headers(options?.headers || {});

    if (this.accessToken) {
      headers.append('Authorization', `Bearer ${this.accessToken}`);
    }

    const updatedOptions: RequestInit = {
      ...options,
      headers,
    };

    try {
      const response = await fetch(url, updatedOptions);
      return response;
    } catch (error) {
      if (error instanceof TypeError) {
        throw new Error('We are currently experiencing issues loading the data. Please try again later.');
      }
      throw error;
    }
  }

  private async handleFetchError(response: Response): Promise<string[]> {
    let errorMessages: string[] = ['Unknown error'];
    try {
      const errorData = await response.json();
      if (errorData.title) {
        errorMessages = [errorData.title];
        if (errorData.errors && Array.isArray(errorData.errors)) {
          errorMessages = errorMessages.concat(errorData.errors);
        }
      } else if (errorData.errors && Array.isArray(errorData.errors)) {
        errorMessages = errorData.errors;
      } else if (errorData.detail) {
        errorMessages = [errorData.detail];
      }
    } catch (e) {
      console.error('Error parsing error response:', e);
    }
    return errorMessages;
  }
}

export default BasketClient;

export interface UpdateBasketItemDto {
    id: string;
    quantity: number;
}

export interface UpdateBasketDto {
    items: UpdateBasketItemDto[];
}