import { UserManagerSettings } from 'oidc-client-ts';

// OIDC configuration
export const oidcConfig: UserManagerSettings = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID,
  redirect_uri: `${window.location.origin}/authentication/callback`,
  response_type: 'code',
  scope: import.meta.env.VITE_OIDC_SCOPE,
  post_logout_redirect_uri: `${window.location.origin}/`,
};
