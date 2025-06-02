# Game Store React Front-End

Please follow these steps to configure and run this React front-end in your dev box:

## 1. Install Node.js
Download install Node.js from the official website: https://nodejs.org/en/download

The latest LTS version that this front-end was tested with is **v22.12.0**. Using a different version may not work.


## 2. Configure Keycloak
You need to create a new public client in Keycloak to let this React front-end access the protected Game Store back-end API endpoints and authenticate users.

Here for the main settings you must use for this client:
* **Name**: gamestore-frontend-react
* **Valid Redirect URIs**: http://localhost:5173/authentication/callback
* **Client authentication**: Off
* **Authentication flow**: Standard flow (all others Off)

Also, you must add **gamestore_api.all** as an optional client scope for this new client.

## 3. Configure the Game Store back-end API
The back-end API you have worked on across this course is mostly ready to work with this front-end. However, you need to update it with a CORS policy to allow requests from the React front-end.

To do that, open **Program.cs** in the back-end project and add the CORS policy:

```csharp
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
```

Also enable the CORS middleware:

```csharp
app.UseCors();
app.UseAuthorization();
```

Just make sure to make the call to **UseCors** before **UseAuthorization**, or it won't work.

More about CORS configuration in ASP.NET Core can be found [here](https://learn.microsoft.com/aspnet/core/security/cors)


## 4. Configure the React front-end
Open and update the **.env** file at the root of this repo with the following settings:
* **VITE_BACKEND_API_URL**: The URL of your Game Store back-end API
* **VITE_OIDC_AUTHORITY**: The URL of your Keycloak realm (e.g. http://localhost:8080/realms/gamestore)
* **VITE_OIDC_CLIENT_ID**: The client ID of the public client you just created in Keycloak (e.g. gamestore-frontend-react)

## 5. Install the dependencies
Open a terminal and navigate to the root of this repo, then run the following command:

```bash
npm install
```

## 6. Run the React front-end
Make sure both your Game Store back-end API and Keycloak are running, then open a terminal and run the following command:

```bash
npm run dev
```

This will start the React front-end on http://localhost:5173. 

Open this URL in your browser and have fun!