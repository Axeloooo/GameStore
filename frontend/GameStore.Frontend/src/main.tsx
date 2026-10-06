import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
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
              <Route path="game/:id" element={<Game />} />
              <Route path="/accessDenied" element={<AccessDenied />} />
            </Route>
            <Route path="/authentication/callback" element={<AuthenticationCallback />} />
          </Routes>
        </BasketProvider>
      </AuthProvider>
    </Router>
  </StrictMode>,
);