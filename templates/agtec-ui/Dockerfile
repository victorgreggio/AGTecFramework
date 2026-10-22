# syntax=docker/dockerfile:1.7
# Independently-deployable SPA: build the front-end bundle, serve it from nginx (which also reverse-proxies
# the API so the browser talks to a single origin — see nginx.conf.template). If the build installs packages
# from a private registry, pass auth at build time as a BuildKit secret so it is never baked into the image:
#   DOCKER_BUILDKIT=1 docker build --secret id=npmrc,src=$HOME/.npmrc -t my-web .

FROM node:22-bookworm-slim AS build
WORKDIR /app
COPY package.json package-lock.json .npmrc ./
RUN --mount=type=secret,id=npmrc,target=/root/.npmrc npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS runtime
RUN apk add --no-cache gettext
# Point this at your build's output directory (the folder containing index.html).
COPY --from=build /app/dist/<app-name>/browser /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY docker-entrypoint.sh /docker-entrypoint-spa.sh
RUN chmod +x /docker-entrypoint-spa.sh
ENV WEB_UI_PORT=8080 \
    API_UPSTREAM=http://api:8080
EXPOSE 8080
ENTRYPOINT ["/docker-entrypoint-spa.sh"]
