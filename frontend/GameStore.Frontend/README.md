# Lootlark React Front-End (GameStore)

This README provides instructions to configure and run the Game Store React front-end with either Keycloak or Entra ID as your identity provider.

## 1. Install Node.js
Download and install Node.js from the official website: https://nodejs.org/en/download

The recommended LTS version for this project is **v22.x** or later.

## 2. Configure the Identity Provider
You can configure the application to use either Keycloak or Entra ID.

### Option A: Keycloak Configuration
1. **Create a new client in Keycloak**:
   * Navigate to your Keycloak admin console
   * Create a new client with the following settings:
     * **Client ID**: gamestore-frontend-react
     * **Client Type**: SPA (Single Page Application)
     * **Valid Redirect URIs**: http://localhost:5173/authentication/callback
     * **Client authentication**: Off
     * **Authentication flow**: Standard flow (enable), Direct access grants (disable), Implicit flow (disable)

2. **Configure client scopes**:
   * Add the scope **gamestore_api.all** to the client configuration (this scope should be defined in your API)

3. **Note your realm URL**:
   * This will be needed for the environment configuration (typically something like http://localhost:8080/realms/gamestore)

### Option B: Entra ID Configuration
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

## 3. Configure the Game Store back-end API
Update your API to allow CORS requests from the React front-end:

1. Open **Program.cs** in your back-end project
2. Add the following CORS configuration:

```csharp
// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            var allowedOrigin = "http://localhost:5173";
            policy.WithOrigins(allowedOrigin)
                  .WithHeaders(HeaderNames.Authorization, HeaderNames.ContentType)
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Make sure to add CORS middleware BEFORE UseAuthorization
app.UseCors();
app.UseAuthorization();
```

## 4. Configure the React front-end
Create or update the **.env** file at the root of this project with the following settings:

```
VITE_BACKEND_API_URL=http://localhost:5082

# Identity Provider Selection (keycloak or entra)
VITE_IDENTITY_PROVIDER=keycloak  # Change to "entra" if using Entra ID

# Keycloak Configuration 
VITE_KEYCLOAK_AUTHORITY=http://localhost:8080/realms/gamestore
VITE_KEYCLOAK_CLIENT_ID=gamestore-frontend-react
VITE_KEYCLOAK_SCOPE=openid gamestore_api.all

# Entra ID Configuration
VITE_ENTRA_AUTHORITY=https://your-tenant.ciamlogin.com/your-tenant-id/v2.0
VITE_ENTRA_CLIENT_ID=your-client-id
VITE_ENTRA_SCOPE=api://your-api-id/gamestore_api.all openid profile email offline_access
```

Replace the placeholder values with your actual configuration details.

## 5. Install the dependencies
Open a terminal at the root directory of the project and run:

```bash
npm install
```

## 6. Run the React front-end
Ensure your Game Store back-end API and identity provider (Keycloak or Entra ID) are running, then start the application:

```bash
npm run dev
```

This will start the React front-end on http://localhost:5173.

## 7. Using the application
- Browse the game catalog without logging in
- Log in using your identity provider credentials to:
  - Add games to your cart
  - Make purchases
  - Edit or add games (if you have admin privileges)

## Troubleshooting
- If authentication fails, verify your .env configuration matches your identity provider settings
- Check browser console for any CORS-related errors
- Ensure your backend API is properly configured to validate tokens from your identity provider