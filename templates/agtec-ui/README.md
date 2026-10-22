# agtec-ui — SPA + nginx reverse-proxy template

A drop-in way to ship a single-page app (Angular, React, …) alongside a AGTec API so that:

- the **API is never published** — only the UI container is exposed, and nginx reverse-proxies `/_api/*`
  to the API over the internal container network;
- the **browser talks to one origin**, so there is no CORS to configure;
- the API host stays a **pure API** (it serves no static files).

## Files

| File | Purpose |
| --- | --- |
| `Dockerfile` | Two-stage build: compile the bundle with Node, serve it from `nginx:alpine`. Private-registry auth is a BuildKit secret, never baked into the image. |
| `nginx.conf.template` | The server block. `/_api/` is proxied to `${API_UPSTREAM}`; everything else is the SPA with `index.html` fallback. |
| `docker-entrypoint.sh` | Substitutes `${WEB_UI_PORT}` / `${API_UPSTREAM}` at container start (one image, every environment) and runs nginx. |

## Why the lazy DNS resolution matters

nginx resolves upstream hostnames **once at config-load time** by default, so if the API container isn't
resolvable the instant nginx starts, it hard-fails with `host not found in upstream`. The template avoids
this with Docker's embedded DNS and a **variable** `proxy_pass`:

```nginx
resolver 127.0.0.11 valid=10s ipv6=off;   # Docker's embedded DNS
set        $api_upstream "${API_UPSTREAM}";
proxy_pass $api_upstream;                  # a variable forces per-request resolution
```

This makes the UI resilient to start-up ordering — it comes up whether or not the API is ready yet.

## Usage

1. Copy this folder next to your SPA's `package.json`.
2. In the `Dockerfile`, set the `COPY --from=build /app/dist/<app-name>/browser` path to your build output
   (the directory that contains `index.html`).
3. Wire it in compose, publishing only the UI:

   ```yaml
   ui:
     build:
       context: ./ui
       secrets: [npmrc]          # only if you install from a private registry
     environment:
       WEB_UI_PORT: "8080"       # nginx's port INSIDE the container
       API_UPSTREAM: "http://api:8080"
     ports:
       - "${UI_PORT:-8088}:8080" # the only published port; the API stays internal
     depends_on: [api]
   ```

4. The API service needs no published port — `expose: ["8080"]` is enough.
