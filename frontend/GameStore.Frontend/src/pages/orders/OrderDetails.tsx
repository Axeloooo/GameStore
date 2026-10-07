import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import OrdersClient from '../../clients/OrdersClient';
import { OrderDto } from '../../models/OrdersModels';

const OrderDetails: React.FC = () => {
    const { orderId } = useParams<{ orderId: string }>();
    const { user } = useAuth();
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
            } catch (error) {
                console.error('Failed to fetch order:', error);
                setError(error instanceof Error ? error.message : 'Failed to load order');
            }
            setLoading(false);
        };

        fetchOrder();
    }, [orderId, user]);

    const formatOrderNumber = (orderNumber: number): string => {
        return orderNumber.toString().padStart(7, '0');
    };

    const formatOrderDate = (dateString: string): string => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    if (loading) {
        return (
            <div>
                <title>Order Details</title>
                <h3 className="mt-4 mb-4">Order Details</h3>
                <p><em>Loading...</em></p>
            </div>
        );
    }

    if (error) {
        return (
            <div>
                <title>Order Details</title>
                <h3 className="mt-4 mb-4">Order Details</h3>
                <div className="alert alert-danger">
                    <strong>Error:</strong> {error}
                </div>
            </div>
        );
    }

    if (!order) {
        return (
            <div>
                <title>Order Details</title>
                <h3 className="mt-4 mb-4">Order Details</h3>
                <div className="alert alert-warning">
                    Order not found.
                </div>
            </div>
        );
    }

    return (
        <div>
            <title>Order Details</title>
            <h3 className="mt-4 mb-4">Order Details</h3>

            <div className="row border rounded p-3">
                <div className="col-md-4">
                    <h5>Order Summary</h5>
                    <p className="mb-1"><strong>Order Number:</strong> {formatOrderNumber(order.orderNumber)}</p>
                    <p className="mb-1"><strong>Order Date:</strong> {formatOrderDate(order.created)}</p>
                    <p className="mb-0"><strong>Order Status:</strong> {order.status}</p>
                </div>
                <div className="col-md-4">
                    <h5>Payment Method</h5>
                    <p>{order.paymentCardBrand} ending in {order.paymentCardLast4}</p>
                </div>
                <div className="col-md-4">
                    <h5>Total Amount</h5>
                    <p><strong>${order.totalAmount}</strong></p>
                </div>
            </div>

            <div className="row border rounded p-3 mt-3">
                <div className="col">
                    <ul className="list-unstyled">
                        {order.items.map((item, index) => (
                            <div key={index} className="row mb-3">
                                <div className="col-md-1 mt-2">
                                    <img
                                        src={item.imageUri}
                                        alt="Product Image"
                                        style={{ width: '100px', height: 'auto' }}
                                        className="img-fluid"
                                    />
                                </div>
                                <div className="col-md-11 mt-2">
                                    <h5>
                                        <Link to={`/game/${item.productId}`}>
                                            {item.productName}
                                        </Link>
                                    </h5>
                                    <p className="mb-1"><strong>Quantity:</strong> {item.quantity}</p>
                                    <p className="mb-1"><strong>Price:</strong> ${item.price} each</p>
                                    {item.gameCodes && item.gameCodes.length > 0 ? (
                                        item.gameCodes.map((gameCode, codeIndex) => (
                                            <p key={codeIndex} className="mb-1">
                                                Game Code: <strong>{gameCode}</strong>
                                            </p>
                                        ))
                                    ) : (
                                        <p className="mb-1">Game codes will be available soon.</p>
                                    )}
                                </div>
                            </div>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
};

export default OrderDetails;
