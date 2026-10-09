import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { useBasket } from '../../context/BasketContext';
import OrdersClient from '../../clients/OrdersClient';
import { OrderDto } from '../../models/OrdersModels';
import SimpleNavMenu from '../../components/SimpleNavMenu';
import StatusAlert from '../../components/StatusAlert';

const OrderCreated: React.FC = () => {
    const { orderId } = useParams<{ orderId: string }>();
    const { user } = useAuth();
    const { refreshBasket } = useBasket();
    const [order, setOrder] = useState<OrderDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const fetchOrder = async () => {
            if (!orderId) {
                setError('Order ID not provided');
                setLoading(false);
                return;
            }

            try {
                const ordersClient = new OrdersClient(user?.access_token || null, user || null);
                const orderData = await ordersClient.getOrderAsync(orderId);
                setOrder(orderData);

                // Refresh the basket since the order was successfully placed
                // The backend should have emptied the cart at this point
                try {
                    await refreshBasket();
                } catch (basketError) {
                    console.warn('Failed to refresh basket after order creation:', basketError);
                    // Don't fail the entire page if basket refresh fails
                }
            } catch (error) {
                console.error('Failed to fetch order:', error);
                setError(error instanceof Error ? error.message : 'Failed to load order');
            }
            setLoading(false);
        };

        fetchOrder();
    }, [orderId, user, refreshBasket]);

    const formatOrderNumber = (orderNumber: number): string => {
        return orderNumber.toString().padStart(7, '0');
    };

    return (
        <div>
            <SimpleNavMenu />
            <div className="container">
                <title>Order created</title>

                <h3 className="mt-3">Your order was created</h3>

                {loading ? (
                    <p><em>Loading...</em></p>
                ) : error ? (
                    <StatusAlert variant="danger">{error}</StatusAlert>
                ) : order ? (
                    <>
                        <p>Your order number is: <strong>{formatOrderNumber(order.orderNumber)}</strong></p>

                        <Link to={`/order-details/${order.id}`}>View Order Details</Link>

                        <h4 className="mt-4">Game Codes</h4>
                        <p>Your game codes will appear on the order details page shortly.</p>
                    </>
                ) : null}
            </div>
        </div>
    );
};

export default OrderCreated;
