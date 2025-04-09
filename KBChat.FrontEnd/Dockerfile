FROM node:22-slim AS build

ARG VITE_PUBLIC_APP_ID
ARG VITE_PUBLIC_AUTHORITY_URL
ARG VITE_BACKEND_SCOPE
ARG VITE_SAMPLEPROMPTS
ARG VITE_API_PATH

WORKDIR /app

COPY package.json package.json
COPY package-lock.json package-lock.json
RUN npm install

COPY . .

RUN npm run build

FROM nginx:alpine

COPY --from=build /app/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

# Expose the default nginx port
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
