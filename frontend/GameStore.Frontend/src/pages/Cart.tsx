import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { useBasket } from '../context/BasketContext';
import { CommandResult } from '../models/CommandResult';
import { BasketItem } from '../models/BasketItem';
import { getUserId } from '../utils/authUtils';

const Cart: React.FC = () => {
    const navigate = useNavigate();
    const auth = useAuth();
    const { basket, loading, updateQuantity, removeItem } = useBasket();
    const [errorList, setErrorList] = useState<string[]>([]);

    // Use the loading state from the context directly
    const isLoading = loading;

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

    const handleCheckout = () => {
        const userId = getUserId(auth.user);
        if (userId) {
            navigate(`/checkout`);
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
                    {auth.isLoading || isLoading ? (
                        <div className="mt-2">
                            {/* Loading skeleton for cart items */}
                            <div className="card rounded-3 mb-4" aria-hidden="true">
                                <div className="card-body p-3">
                                    <div className="row d-flex justify-content-between align-items-center placeholder-glow">
                                        <div className="col-md-2">
                                            <span className="placeholder col-12" style={{ height: '80px', display: 'block' }}></span>
                                        </div>
                                        <div className="col-md-4">
                                            <span className="placeholder col-8 placeholder-lg"></span>
                                        </div>
                                        <div className="col-md-2">
                                            <span className="placeholder col-6 placeholder-lg"></span>
                                        </div>
                                        <div className="col-md-3">
                                            <span className="placeholder col-12" style={{ height: '38px', display: 'block' }}></span>
                                        </div>
                                        <div className="col-md-1">
                                            <span className="placeholder col-12"></span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div className="card rounded-3 mb-4" aria-hidden="true">
                                <div className="card-body p-3">
                                    <div className="row d-flex justify-content-between align-items-center placeholder-glow">
                                        <div className="col-md-2">
                                            <span className="placeholder col-12" style={{ height: '80px', display: 'block' }}></span>
                                        </div>
                                        <div className="col-md-4">
                                            <span className="placeholder col-8 placeholder-lg"></span>
                                        </div>
                                        <div className="col-md-2">
                                            <span className="placeholder col-6 placeholder-lg"></span>
                                        </div>
                                        <div className="col-md-3">
                                            <span className="placeholder col-12" style={{ height: '38px', display: 'block' }}></span>
                                        </div>
                                        <div className="col-md-1">
                                            <span className="placeholder col-12"></span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ) : basket ? (
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
                    ) : null}
                </div>

                <div className="col-md-3">
                    {auth.isLoading || isLoading ? (
                        <div className="card" aria-hidden="true">
                            <div className="card-body">
                                <div className="placeholder-glow">
                                    <h5 className="card-title">
                                        <span className="placeholder col-12 placeholder-lg"></span>
                                    </h5>
                                    <hr />
                                    <p className="card-text">
                                        <span className="placeholder col-6 placeholder-lg"></span>
                                        <span className="placeholder col-8 placeholder-lg"></span>
                                    </p>
                                    <a className="btn btn-primary disabled placeholder col-12" aria-disabled="true"></a>
                                </div>
                            </div>
                        </div>
                    ) : basket && basket.items.length > 0 ? (
                        <>
                            <h3 className="d-flex justify-content-between align-items-center mb-3">
                                <span className="text-muted">Summary</span>
                            </h3>
                            <hr />
                            <div className="d-flex justify-content-between">
                                <div className="h4">Total</div>
                                <div className="h4 fw-bold">${basket.totalAmount}</div>
                            </div>
                            <button
                                className="btn btn-primary btn-lg btn-block w-100"
                                onClick={handleCheckout}
                            >
                                Checkout
                            </button>
                        </>
                    ) : null}
                </div>
            </div>
        </div>
    );
};

export default Cart;