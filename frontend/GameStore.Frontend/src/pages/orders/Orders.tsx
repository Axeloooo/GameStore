import React, { useEffect, useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import OrdersClient from '../../clients/OrdersClient';
import { OrdersPageDto } from '../../models/OrdersModels';
import { PaginationInfo } from '../../models/PaginationInfo';
import Pagination from '../../components/Pagination';

const Orders: React.FC = () => {
    const [searchParams] = useSearchParams();
    const { user } = useAuth();
    const [ordersPage, setOrdersPage] = useState<OrdersPageDto | null>(null);
    const [paginationInfo, setPaginationInfo] = useState<PaginationInfo | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const PAGE_SIZE = 5;
    const currentPage = parseInt(searchParams.get('page') || '1', 10);

    useEffect(() => {
        const fetchOrders = async () => {
            setLoading(true);
            setError(null);

            try {
                const ordersClient = new OrdersClient(user?.access_token || null, user || null);
                const ordersData = await ordersClient.getOrdersAsync(currentPage, PAGE_SIZE);
                setOrdersPage(ordersData);
                setPaginationInfo(new PaginationInfo(currentPage, ordersData.totalPages));
            } catch (error) {
                console.error('Failed to fetch orders:', error);
                setError(error instanceof Error ? error.message : 'Failed to load orders');
            }
            setLoading(false);
        };

        fetchOrders();
    }, [currentPage, user]);

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

    const getOrderUrl = (orderId: string): string => {
        return `/order-details/${orderId}`;
    };

    if (loading) {
        return (
            <div className="container">
                <title>Your Orders</title>
                <h3 className="mt-4 mb-4">Your Orders</h3>
                <p><em>Loading...</em></p>
            </div>
        );
    }

    if (error) {
        return (
            <div className="container">
                <title>Your Orders</title>
                <h3 className="mt-4 mb-4">Your Orders</h3>
                <div className="alert alert-danger">
                    <strong>Error:</strong> {error}
                </div>
            </div>
        );
    }

    return (
        <div className="container">
            <title>Your Orders</title>
            <h3 className="mt-4 mb-4">Your Orders</h3>

            {ordersPage && paginationInfo ? (
                <>
                    {ordersPage.data.length === 0 ? (
                        <p>You have no orders.</p>
                    ) : (
                        <>
                            {ordersPage.data.map((order) => (
                                <div key={order.id}>
                                    <div className="row border rounded p-3" style={{ backgroundColor: '#f0f2f2' }}>
                                        <div className="col-md-3">
                                            <p className="mb-0">ORDER PLACED</p>
                                            <p className="mb-0">{formatOrderDate(order.created)}</p>
                                        </div>
                                        <div className="col-md-3">
                                            <p className="mb-0">TOTAL</p>
                                            <p className="mb-0">${order.totalAmount}</p>
                                        </div>
                                        <div className="col-md-3">
                                            <p className="mb-0">STATUS</p>
                                            <p className="mb-0">{order.status}</p>
                                        </div>
                                        <div className="col-md-3">
                                            <p className="mb-0">ORDER # {formatOrderNumber(order.orderNumber)}</p>
                                            <Link className="mb-0" to={getOrderUrl(order.id)}>
                                                View order details
                                            </Link>
                                        </div>
                                    </div>

                                    <div className="row border rounded p-3 mb-4">
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
                                                            {order.status === "Completed" ? (
                                                                <p className="mb-1">
                                                                    <Link to={getOrderUrl(order.id)}>Game Codes Ready</Link>
                                                                </p>
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
                            ))}

                            <div className="row mt-2">
                                <div className="col">
                                    <Pagination
                                        paginationInfo={paginationInfo}
                                        onPageChange={() => { }}
                                    />
                                </div>
                            </div>
                        </>
                    )}
                </>
            ) : null}
        </div>
    );
};

export default Orders;
