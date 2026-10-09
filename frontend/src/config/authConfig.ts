import { UserManagerSettings } from 'oidc-client-ts';
import { getOidcConfig } from '../utils/authUtils';

// OIDC configuration that will be automatically set based on the selected identity provider
export const oidcConfig: UserManagerSettings = getOidcConfig();
