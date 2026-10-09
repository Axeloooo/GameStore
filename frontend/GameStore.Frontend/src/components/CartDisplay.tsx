import React from 'react';
import { useBasket } from '../context/BasketContext';

const CartDisplay: React.FC = () => {
    const { basket, refreshBasket } = useBasket();

    // Ensure badge stays in sync after external flows (e.g., checkout redirects)
    React.useEffect(() => {
        // Refresh once on mount
        refreshBasket().catch(() => { /* noop */ });

        // Also refresh when the window regains focus
        const onFocus = () => {
            refreshBasket().catch(() => { /* noop */ });
        };
        window.addEventListener('focus', onFocus);
        return () => window.removeEventListener('focus', onFocus);
    }, [refreshBasket]);

    const totalQuantity = basket?.items.reduce((total, item) => total + item.quantity, 0) || 0;

    return (
        <div className="position-relative">
            <a aria-label={`Cart, ${totalQuantity} ${totalQuantity === 1 ? 'item' : 'items'}`} href="/cart" className="d-flex align-items-center text-decoration-none text-primary">
                <i className="bi bi-bag-fill fs-3" aria-hidden="true"></i>
                <span className="cart-count position-absolute start-50 translate-middle fw-bold fs-6" style={{ top: '60%' }}>
                    {totalQuantity}
                </span>
            </a>
        </div>
    );
};

export default CartDisplay;