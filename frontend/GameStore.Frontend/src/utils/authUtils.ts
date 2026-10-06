import { User, UserManagerSettings } from 'oidc-client-ts';

// Get the identity provider from environment variables
export const identityProvider = import.meta.env.VITE_IDENTITY_PROVIDER || 'keycloak';

// OIDC configuration based on the selected identity provider
export const getOidcConfig = (): UserManagerSettings => {
  const isKeycloak = identityProvider.toLowerCase() === 'keycloak';

  return {
    authority: isKeycloak
      ? import.meta.env.VITE_KEYCLOAK_AUTHORITY
      : import.meta.env.VITE_ENTRA_AUTHORITY,
    client_id: isKeycloak
      ? import.meta.env.VITE_KEYCLOAK_CLIENT_ID
      : import.meta.env.VITE_ENTRA_CLIENT_ID,
    redirect_uri: `${window.location.origin}/authentication/callback`,
    response_type: 'code',
    scope: isKeycloak
      ? import.meta.env.VITE_KEYCLOAK_SCOPE
      : import.meta.env.VITE_ENTRA_SCOPE,
    post_logout_redirect_uri: `${window.location.origin}/`,
  };
};

// Get the user's roles based on the identity provider
export const getUserRoles = (user: User | null | undefined): string[] => {
  if (!user) return [];

  if (identityProvider.toLowerCase() === 'keycloak') {
    // Keycloak stores roles as a string or array in the 'role' claim
    const roles = user.profile?.role;
    if (Array.isArray(roles)) {
      return roles;
    } else if (typeof roles === 'string') {
      return [roles];
    }
    return [];
  } else {
    // Entra ID stores roles in the 'roles' claim as an array
    const roles = user.profile?.roles;
    if (Array.isArray(roles)) {
      return roles;
    }
    return [];
  }
};

// Check if a user has a specific role
export const hasRole = (user: User | null | undefined, role: string): boolean => {
  const roles = getUserRoles(user);
  return roles.includes(role);
};

// Get the user ID based on the identity provider
export const getUserId = (user: User | null | undefined): string | null => {
  if (!user) return null;

  if (identityProvider.toLowerCase() === 'keycloak') {
    // For Keycloak, use the 'sub' claim as the user ID
    return user.profile?.sub?.toString() || null;
  } else {
    // For Entra ID, use the 'oid' claim as the user ID
    const oid = user.profile?.oid;
    return oid?.toString() || null;
  }
};