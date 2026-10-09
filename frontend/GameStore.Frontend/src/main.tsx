import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import '@fontsource-variable/baloo-2';
import '@fontsource-variable/nunito-sans';
import './styles/lootlark-tokens.css';
import './index.css';
import App from './App';
import Home from './pages/Home';
import Catalog from './pages/catalog/Catalog';
import EditGame from './pages/catalog/EditGame';
import { AuthProvider } from 'react-oidc-context';
import { oidcConfig } from './config/authConfig';
import AuthenticationCallback from './pages/AuthenticationCallback';
import PrivateRoute from './components/PrivateRoute';
import AccessDenied from './pages/AccessDenied';
import Cart from './pages/Cart';
import Game from './pages/Game';
import Checkout from './pages/Checkout';
import OrderCreated from './pages/orders/OrderCreated';
import OrderDetails from './pages/orders/OrderDetails';
import Orders from './pages/orders/Orders';
import NotFound from './pages/NotFound';
import { BasketProvider } from './context/BasketContext';

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <Router>
            <AuthProvider {...oidcConfig}>
                <BasketProvider>
                    <Routes>
                        <Route path="/" element={<App />}>
                            <Route index element={<Home />} />
                            <Route path="catalog" element={
                                <PrivateRoute requiredRole="Admin">
                                    <Catalog />
                                </PrivateRoute>} />
                            <Route path="catalog/editgame" element={
                                <PrivateRoute requiredRole="Admin">
                                    <EditGame />
                                </PrivateRoute>} />
                            <Route path="catalog/editgame/:id" element={
                                <PrivateRoute requiredRole="Admin">
                                    <EditGame />
                                </PrivateRoute>} />
                            <Route path="cart" element={
                                <PrivateRoute>
                                    <Cart />
                                </PrivateRoute>} />
                            <Route path="orders" element={
                                <PrivateRoute>
                                    <Orders />
                                </PrivateRoute>} />
                            <Route path="order-details/:orderId" element={
                                <PrivateRoute>
                                    <OrderDetails />
                                </PrivateRoute>} />
                            <Route path="game/:id" element={<Game />} />
                            <Route path="/accessDenied" element={<AccessDenied />} />
                            <Route path="*" element={<NotFound />} />
                        </Route>
                        <Route path="checkout" element={
                            <PrivateRoute>
                                <Checkout />
                            </PrivateRoute>} />
                        <Route path="order-created/:orderId" element={
                            <PrivateRoute>
                                <OrderCreated />
                            </PrivateRoute>} />
                        <Route path="/authentication/callback" element={<AuthenticationCallback />} />
                    </Routes>
                </BasketProvider>
            </AuthProvider>
        </Router>
    </StrictMode>,
);