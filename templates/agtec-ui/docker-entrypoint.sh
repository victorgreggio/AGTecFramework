#!/bin/sh
# 12-factor runtime config: substitute only our placeholders into the nginx config at container start
# (one image for all environments), then run nginx in the foreground.
set -eu

: "${WEB_UI_PORT:=8080}"
: "${API_UPSTREAM:=http://api:8080}"

export WEB_UI_PORT API_UPSTREAM

# Substitute ONLY our vars so nginx's own $host/$uri/... are left intact.
envsubst '${WEB_UI_PORT} ${API_UPSTREAM}' \
    < /etc/nginx/templates/default.conf.template \
    > /etc/nginx/conf.d/default.conf

exec nginx -g 'daemon off;'
