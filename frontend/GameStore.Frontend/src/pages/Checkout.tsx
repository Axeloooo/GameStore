import React, { useState, useMemo } from 'react';
import { useAuth } from 'react-oidc-context';
import { PaymentsClient } from '../clients/PaymentsClient';
import { CheckoutSessionDto } from '../models/PaymentModels';
import { loadStripe } from '@stripe/stripe-js';
import {
    CheckoutProvider,
    PaymentElement,
    useCheckout
} from '@stripe/react-stripe-js/checkout';
import SimpleNavMenu from '../components/SimpleNavMenu';
import StatusAlert from '../components/StatusAlert';

// Load Stripe outside of component render
const stripePromise = loadStripe(import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY || '');

// CheckoutForm component that uses useCheckout hook
const CheckoutForm: React.FC = () => {
    const [message, setMessage] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);

    const checkoutState = useCheckout();

    if (checkoutState.type === 'error') {
        return <StatusAlert variant="danger">{checkoutState.error.message}</StatusAlert>;
    }

    if (checkoutState.type === 'loading') {
        return null; // Outer loading state handles the skeleton UI
    }

    const { checkout } = checkoutState;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setMessage(null);

        const confirmResult = await checkout.confirm();

        // This point will only be reached if there is an immediate error when
        // confirming the payment. Otherwise, your customer will be redirected to
        // your `return_url`.
        if (confirmResult.type === 'error') {
            setMessage(confirmResult.error.message);
        }

        setIsLoading(false);
    };

    return (
        <form id="payment-form" onSubmit={handleSubmit}>
            <h4>Payment</h4>
            <PaymentElement id="payment-element" />

            <button
                type="submit"
                className="btn btn-primary btn-lg w-100 mt-3"
                disabled={isLoading}
            >
                {isLoading ? 'Processing...' : 'Place Order'}
            </button>

            {message && (
                <StatusAlert variant="danger" className="mt-3">
                    {message}
                </StatusAlert>
            )}
        </form>
    );
};

const Checkout: React.FC = () => {
    const { user } = useAuth();
    const [operationId] = useState(() => crypto.randomUUID());
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string>('');
    const [clientSecret, setClientSecret] = useState<string | null>(null);
    const [checkoutSession, setCheckoutSession] = useState<CheckoutSessionDto | null>(null);

    // Memoize the payments client to prevent recreation on every render
    const paymentsClient = useMemo(() =>
        new PaymentsClient(user?.access_token || null),
        [user?.access_token]
    );

    // Fetch client secret once using useEffect (prevents duplicate calls in Strict Mode)
    React.useEffect(() => {
        if (!user?.access_token) {
            setError('Missing required checkout data');
            setLoading(false);
            return;
        }

        paymentsClient.createCheckoutSession(operationId)
            .then((session: CheckoutSessionDto) => {
                setClientSecret(session.clientSecret);
                setCheckoutSession(session);
                setLoading(false);
            })
            .catch((err) => {
                setError((err as Error).message || 'Failed to initialize checkout');
                setLoading(false);
            });
    }, [user?.access_token, paymentsClient, operationId]);

    const appearance = {
        theme: 'stripe' as const,
    };

    if (!user) {
        return <div>Please log in to continue to checkout.</div>;
    }

    return (
        <div>
            <SimpleNavMenu />
            <div className="container">
                <title>Checkout</title>

                <h2 className="mt-4">Checkout</h2>

                {loading ? (
                    <div className="mt-4">
                        <div className="row">
                            <div className="col-md-8">
                                <div className="card" aria-hidden="true">
                                    <div className="card-body">
                                        <p className="card-text placeholder-glow">
                                            <button type="button" className="btn btn-secondary disabled placeholder col-6" />
                                            <button type="button" className="btn btn-secondary disabled placeholder col-5" />
                                            <span className="placeholder col-7 placeholder-lg mt-2 d-block" style={{ height: '40px' }} />
                                            <span className="placeholder col-5 placeholder-lg mt-2 d-block" style={{ height: '40px' }} />
                                            <span className="placeholder col-3 placeholder-lg mt-2 d-block" style={{ height: '40px' }} />
                                            <span className="placeholder col-6 placeholder-lg mt-2 d-block" style={{ height: '40px' }} />
                                        </p>
                                    </div>
                                </div>
                            </div>
                            <div className="col-md-4">
                                <div className="card" aria-hidden="true">
                                    <div className="card-body">
                                        <h5 className="card-title placeholder-glow">
                                            <span className="placeholder col-12 placeholder-lg"></span>
                                        </h5>
                                        <p className="card-text placeholder-glow">
                                            <span className="placeholder col-3 placeholder-lg"></span>
                                            <span className="placeholder col-8 placeholder-lg"></span>
                                            <span className="placeholder col-3 placeholder-lg"></span>
                                            <span className="placeholder col-8 placeholder-lg"></span>
                                            <span className="placeholder col-12 placeholder-lg"></span>
                                        </p>
                                        <a className="btn btn-primary disabled placeholder col-12" aria-disabled="true"></a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                ) : error ? (
                    <div className="mt-4">
                        <StatusAlert variant="danger">
                            We could not start checkout. Nothing was charged. Please try again.
                        </StatusAlert>
                    </div>
                ) : clientSecret && checkoutSession ? (
                    <CheckoutProvider
                        stripe={stripePromise}
                        options={{
                            clientSecret: clientSecret,
                            elementsOptions: { appearance },
                        }}
                    >
                        <div className="row mt-4">
                            <div className="col-md-8">
                                <CheckoutForm />
                            </div>

                            <div className="col-md-4">
                                <div id="order-summary">
                                    <h3 className="d-flex justify-content-between align-items-center mb-3">
                                        <span className="text-muted">Order Summary</span>
                                    </h3>
                                    <hr />
                                    {checkoutSession.items.map((item) => (
                                        <div key={item.productId} className="row mb-2 d-flex justify-content-between align-items-center">
                                            <div className="col-md-3">
                                                <img src={item.imageUri} className="img-fluid rounded-3" alt={item.productName} />
                                            </div>
                                            <div className="col-md-9">
                                                <div className="h5 mb-0">{item.productName}</div>
                                                <div className="d-flex justify-content-between">${item.price} x {item.quantity}</div>
                                            </div>
                                        </div>
                                    ))}
                                    <hr />
                                    <div className="d-flex justify-content-between">
                                        <div className="h4">Total</div>
                                        <div className="h4 fw-bold">
                                            ${checkoutSession.items.reduce((total, item) => total + (item.price * item.quantity), 0).toFixed(2)}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </CheckoutProvider>
                ) : null}
            </div>
        </div>
    );
};

export default Checkout;