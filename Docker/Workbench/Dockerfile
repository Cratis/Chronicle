# syntax=docker/dockerfile:1
FROM nginx:1.23.1-alpine

WORKDIR /app

COPY ./Source/Workbench/Web/wwwroot /usr/share/nginx/html

# Remove default nginx config and add simple config
RUN rm /etc/nginx/conf.d/default.conf && \
    echo 'server { listen 80; root /usr/share/nginx/html; index index.html; location / { try_files $uri $uri/ /index.html; } }' > /etc/nginx/conf.d/default.conf
