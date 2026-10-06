import React, { useState } from 'react';
import { useBasket } from '../context/BasketContext';
import { CommandResult } from '../models/CommandResult';
import { BasketItem } from '../models/BasketItem';

const Cart: React.FC = () => {
  const { basket, updateQuantity, removeItem } = useBasket();
  const [errorList, setErrorList] = useState<string[]>([]);

  const handleUpdateQuantity = async (itemId: string, quantity: number) => {
    setErrorList([]);
    const result: CommandResult = await updateQuantity(itemId, quantity);

    if (!result.succeeded) {
      setErrorList(result.errors);
    }
  };

  const handleRemoveItem = async (itemId: string) => {
    setErrorList([]);
    const result: CommandResult = await removeItem(itemId);

    if (!result.succeeded) {
      setErrorList(result.errors);
    }
  };

  return (
    <div>
      <h3 className="mt-4 mb-4">My Cart</h3>

      {errorList.length > 0 && errorList.map((error, index) => (
        <div key={index} className="alert alert-danger">{error}</div>
      ))}

      <div className="row">
        <div className="col-md-9">
          {!basket ? (
            <p className="mt-2"><em>Loading...</em></p>
          ) : (
            <>
              {basket.items.length === 0 ? (
                <h4>Your Cart is empty.</h4>
              ) : (
                basket.items.map((item: BasketItem) => (
                  <div key={item.id} className="card rounded-3 mb-4">
                    <div className="card-body p-3">
                      <div className="row d-flex justify-content-between align-items-center">
                        <div className="col-md-2">
                          <img src={item.imageUri} className="img-fluid rounded-3" alt={item.name} />
                        </div>
                        <div className="col-md-4">
                          <h3 className="mb-2 fw-normal">{item.name}</h3>
                        </div>
                        <div className="col-md-2">
                          <h4 className="mb-0">${item.price}</h4>
                        </div>
                        <div className="col-md-3">
                          <select
                            className="form-select"
                            value={item.quantity}
                            onChange={(e) => handleUpdateQuantity(item.id, parseInt(e.target.value, 10))}
                          >
                            <option value="1">1</option>
                            <option value="2">2</option>
                          </select>
                        </div>
                        <div className="col-md-1">
                          <button
                            type="button"
                            className="btn btn-link text-danger"
                            onClick={() => handleRemoveItem(item.id)}
                          >
                            <i className="bi bi-trash3-fill fs-3"></i>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </>
          )}
        </div>

        <div className="col-md-3">
          {basket && basket.items.length > 0 && (
            <>
              <h3 className="d-flex justify-content-between align-items-center mb-3">
                <span className="text-muted">Summary</span>
              </h3>
              <hr />
              <div className="d-flex justify-content-between">
                <div className="h4">Total</div>
                <div className="h4 fw-bold">${basket.totalAmount}</div>
              </div>
              <button className="btn btn-primary btn-lg btn-block w-100">
                Checkout
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default Cart;