# syntax=docker/dockerfile:1.7

## ---------------------------------------------------------------------------
## Build stage: install pinned deps and produce the production bundle.
## ---------------------------------------------------------------------------
FROM node:22.14-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

## ---------------------------------------------------------------------------
## Runtime stage: serve the static bundle with nginx.
## ---------------------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
