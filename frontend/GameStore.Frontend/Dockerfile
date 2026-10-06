FROM node:22 as build

WORKDIR /app

COPY package.json package.json
COPY package-lock.json package-lock.json

RUN npm install

COPY . .

# Generate .env.production from build args
ARG VITE_BACKEND_API_URL
ARG VITE_IDENTITY_PROVIDER
ARG VITE_ENTRA_CLIENT_ID
ARG VITE_ENTRA_AUTHORITY
ARG VITE_ENTRA_SCOPE
RUN echo "VITE_BACKEND_API_URL=$VITE_BACKEND_API_URL\nVITE_IDENTITY_PROVIDER=$VITE_IDENTITY_PROVIDER\nVITE_ENTRA_CLIENT_ID=$VITE_ENTRA_CLIENT_ID\nVITE_ENTRA_AUTHORITY=$VITE_ENTRA_AUTHORITY\nVITE_ENTRA_SCOPE=$VITE_ENTRA_SCOPE" > /app/.env.production

RUN npm run build

FROM nginx:alpine

COPY --from=build /app/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

# Expose the default nginx port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
