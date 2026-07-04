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
##
## Security hardening applied:
##   - Minimal nginx:alpine base (no shell utilities)
##   - Process runs as the built-in `nginx` user (uid 101) after start-up
##   - Security response headers injected via nginx.conf
##   - Static assets served with long-lived cache headers; index.html is never
##     cached so new deployments are picked up immediately
## ---------------------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

# Replace the default nginx config with our tuned version.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Angular's @angular/build:application outputs to dist/<project>/browser.
# The project is named "frontend" in angular.json.
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# nginx master process needs root to bind port and manage workers; it drops
# to the `nginx` user (uid 101) for worker processes automatically.
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost:8080/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
