# Lootlark React Front-End (GameStore)

This README explains how to configure and run the Lootlark front end (the GameStore React app) locally with either Keycloak (the default) or Entra ID as the identity provider. The whole local setup, including the backend, is described in [docs/local-development.md](../docs/local-development.md).

## 1. Install Node.js
Download and install Node.js from the official website: https://nodejs.org/en/download

This project uses **v22.x**, the version CI uses (see [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)).

## 2. Start the back end
The front end needs the GameStore API and Keycloak. From the repository root, start them with the Aspire AppHost:

```bash
dotnet run --project backend/src/GameStore.AppHost --launch-profile http
```

The API listens on http://localhost:5082. In Development it accepts requests from any origin, so no CORS change is needed.

## 3. Configure the Identity Provider

### Option A: Keycloak (default)
The AppHost imports the `gamestore` realm from `backend/localinfra/gamestore-realm.json`. It already contains the `gamestore-frontend-react` client (public, standard flow, redirect URI http://localhost:5173/authentication/callback) and the `gamestore_api.all` scope. You only need to create a user; see [Create a Keycloak user](../docs/local-development.md#create-a-keycloak-user).

### Option B: Entra ID
1. **Register an application in the Microsoft Entra admin center**:
   * Navigate to Microsoft Entra ID > App registrations > New registration
   * Enter a name for your application (e.g., "Game Store React Frontend")
   * Select "Single-page application (SPA)" as the application type
   * Add the redirect URI: http://localhost:5173/authentication/callback
   * Register the application

2. **Configure API permissions**:
   * Go to "API permissions"
   * Add permissions for your back-end API (e.g., "gamestore_api.all" scope)
   * Grant admin consent for these permissions if you have admin rights

3. **Note your application (client) ID and tenant details**:
   * Client ID will be displayed on the overview page
   * Authority URL will be in the format: https://[tenant-name].ciamlogin.com/[tenant-id]/v2.0

The back end must be configured for the same tenant; see [Using Microsoft Entra ID instead of Keycloak](../docs/local-development.md#using-microsoft-entra-id-instead-of-keycloak).

## 4. Configure the React front-end
Copy the example settings to `.env.local` (git-ignored) in this folder:

```bash
cp .env.example .env.local
```

[`.env.example`](.env.example) lists every setting with local defaults for Keycloak:

* `VITE_BACKEND_API_URL`: the API, http://localhost:5082
* `VITE_IDENTITY_PROVIDER`: `keycloak` or `entra`
* `VITE_KEYCLOAK_CLIENT_ID`, `VITE_KEYCLOAK_AUTHORITY`, `VITE_KEYCLOAK_SCOPE`: the imported realm
* `VITE_ENTRA_CLIENT_ID`, `VITE_ENTRA_AUTHORITY`, `VITE_ENTRA_SCOPE`: only for Entra
* `VITE_STRIPE_PUBLISHABLE_KEY`: your Stripe test publishable key (`pk_test_...`); set this one

VITE_ values are baked into the browser bundle: never put a secret in them (only public client ids, authority URLs, the API URL and the Stripe publishable key pk_test_...).

## 5. Install the dependencies
Open a terminal in this folder (`frontend/`) and run:

```bash
npm ci
```

## 6. Run the React front-end
Ensure the back end and the identity provider are running, then start the application:

```bash
npm run dev
```

This will start the React front-end on http://localhost:5173 (override the port with the `VITE_PORT` environment variable, but keep 5173 so the Keycloak redirect URI and the checkout return URL keep working).

Other scripts:

```bash
npm run lint      # ESLint
npm run build     # type check and production build into dist/
npm run preview   # serve the production build locally
```

## 7. Using the application
- Browse the game catalog without logging in
- Log in using your identity provider credentials to:
  - Add games to your cart
  - Make purchases (Stripe test card 4242 4242 4242 4242, any future expiry, any CVC)
  - Edit or add games (if you have the `Admin` role)

## Troubleshooting
- If authentication fails, verify your `.env.local` configuration matches your identity provider settings
- Check the browser console for errors
- Ensure the back end is running and configured to validate tokens from your identity provider
