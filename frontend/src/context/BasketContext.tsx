import React, { createContext, useContext, useState, useEffect, useCallback, useMemo } from 'react';
import { CustomerBasket } from '../models/CustomerBasket';
import { CommandResult } from '../models/CommandResult';
import { BasketItem } from '../models/BasketItem';
import BasketClient from '../clients/BasketClient';
import { useAuth } from 'react-oidc-context';
import { BasketState } from '../services/BasketState';

interface BasketContextProps {
  basket: CustomerBasket | null;
  loading: boolean;
  error: string | null;
  addItem: (newItem: BasketItem) => Promise<CommandResult>;
  updateQuantity: (id: string, quantity: number) => Promise<CommandResult>;
  removeItem: (itemId: string) => Promise<CommandResult>;
}

const BasketContext = createContext<BasketContextProps | undefined>(undefined);

interface BasketProviderProps {
  children: React.ReactNode;
}

export const BasketProvider: React.FC<BasketProviderProps> = ({ children }) => {
  const auth = useAuth();
  const userId = auth.user?.profile?.sub || null;
  const accessToken = auth.user?.access_token || null;

  const basketClient = useMemo(() => new BasketClient(accessToken), [accessToken]);
  const basketState = useMemo(() => new BasketState(basketClient, userId), [basketClient, userId]);

  const [basket, setBasket] = useState<CustomerBasket | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchBasket = useCallback(async () => {
    if (!userId) {
      setBasket({ customerId: '', items: [], totalAmount: 0 });
      setLoading(false);
      return;
    }

    try {
      const fetchedBasket = await basketState.getBasketAsync();
      setBasket(fetchedBasket);
    } catch (err) {
      setError('Failed to fetch basket');
    } finally {
      setLoading(false);
    }
  }, [userId, basketState]);

  useEffect(() => {
    fetchBasket();
    basketState.setOnBasketUpdated(fetchBasket);
  }, [fetchBasket, basketState]);

  const addItem = async (newItem: BasketItem): Promise<CommandResult> => {
    const result = await basketState.addItemAsync(newItem);
    if (result.succeeded) {
      fetchBasket();
    }
    return result;
  };

  const updateQuantity = async (id: string, quantity: number): Promise<CommandResult> => {
    const result = await basketState.updateQuantityAsync(id, quantity);
    if (result.succeeded) {
      fetchBasket();
    }
    return result;
  };

  const removeItem = async (itemId: string): Promise<CommandResult> => {
    const result = await basketState.removeItemAsync(itemId);
    if (result.succeeded) {
      fetchBasket();
    }
    return result;
  };

  return (
    <BasketContext.Provider value={{ basket, loading, error, addItem, updateQuantity, removeItem }}>
      {children}
    </BasketContext.Provider>
  );
};

export const useBasket = (): BasketContextProps => {
  const context = useContext(BasketContext);
  if (!context) {
    throw new Error('useBasket must be used within a BasketProvider');
  }
  return context;
};