import { useAuth } from 'react-oidc-context';
import { useMemo } from 'react';
import BasketClient from '../clients/BasketClient';
import { BasketState } from '../services/BasketState';
import { getUserId } from '../utils/authUtils';

export const useBasketState = () => {
  const auth = useAuth();
  const userId = getUserId(auth.user);
  const accessToken = auth.user?.access_token || null;

  const basketClient = useMemo(() => new BasketClient(accessToken), [accessToken]);
  const basketState = useMemo(() => new BasketState(basketClient, userId), [basketClient, userId]);

  return basketState;
};