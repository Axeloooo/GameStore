import { CreateCheckoutSessionDto, CheckoutSessionDto } from '../models/PaymentModels';

export class PaymentsClient {
    private baseUrl = import.meta.env.VITE_BACKEND_API_URL;
    private accessToken: string | null;

    constructor(accessToken: string | null = null) {
        this.accessToken = accessToken;
    }

    async createCheckoutSession(operationId: string): Promise<CheckoutSessionDto> {
        const dto: CreateCheckoutSessionDto = {
            operationId
        };

        const response = await this.fetchWithHandling(`${this.baseUrl}/payments/checkout`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(dto)
        });

        if (!response.ok) {
            throw new Error('Could not create checkout session!');
        }

        return await response.json();
    }

    private async fetchWithHandling(url: string, options?: RequestInit): Promise<Response> {
        const headers = new Headers(options?.headers || {});

        if (this.accessToken) {
            headers.append('Authorization', `Bearer ${this.accessToken}`);
        }

        return await fetch(url, {
            ...options,
            headers
        });
    }
}
