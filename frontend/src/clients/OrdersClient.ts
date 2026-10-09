import { OrdersPageDto, OrderDto } from '../models/OrdersModels';
import { getUserId } from '../utils/authUtils';
import { User } from 'oidc-client-ts';

class OrdersClient {
    private baseUrl = import.meta.env.VITE_BACKEND_API_URL;
    private accessToken: string | null;
    private user: User | null;

    constructor(accessToken: string | null = null, user: User | null = null) {
        this.accessToken = accessToken;
        this.user = user;
    }

    async getOrdersAsync(pageNumber: number, pageSize: number): Promise<OrdersPageDto> {
        const userId = this.getUserId();
        if (!userId) {
            throw new Error('Could not find user id!');
        }

        const url = new URL(`${this.baseUrl}/orders`);
        url.searchParams.append('pageNumber', pageNumber.toString());
        url.searchParams.append('pageSize', pageSize.toString());

        const response = await this.fetchWithHandling(url.toString());
        if (!response.ok) {
            const errorMessages = await this.handleFetchError(response);
            throw new Error(errorMessages.join('\n'));
        }

        const data = await response.json();
        return data || { totalPages: 0, data: [] };
    }

    async getOrderAsync(orderId: string): Promise<OrderDto> {
        const response = await this.fetchWithHandling(`${this.baseUrl}/orders/${orderId}`);

        if (!response.ok) {
            const errorMessages = await this.handleFetchError(response);
            throw new Error(errorMessages.join('\n') || 'Could not find order!');
        }

        const data = await response.json();
        if (!data) {
            throw new Error('Could not find order!');
        }

        return data;
    }

    private getUserId(): string | null {
        return getUserId(this.user);
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

export default OrdersClient;
