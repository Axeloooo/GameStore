import React from 'react';
import { useBasket } from '../context/BasketContext';

const CartDisplay: React.FC = () => {
  const { basket } = useBasket();

  const totalQuantity = basket?.items.reduce((total, item) => total + item.quantity, 0) || 0;

  return (
    <div className="position-relative">
      <a aria-label="cart" href="/cart" className="d-flex align-items-center text-decoration-none text-white">
        <i className="bi bi-bag-fill fs-3"></i>
        <span className="position-absolute start-50 translate-middle text-dark fw-bold fs-6" style={{ top: '60%' }}>
          {totalQuantity}
        </span>
      </a>
    </div>
  );
};

export default CartDisplay;