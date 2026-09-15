# Frontend AlmacenApp — build de dos etapas: compila con Node y sirve los
# estáticos con nginx. Contexto de build: almacenapp (ver docker-compose.yml
# en la raíz del repo).

FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .

# La SPA corre en el navegador de quien la usa (no dentro de la red de
# Docker), así que esta URL debe apuntar al puerto que el backend publica
# en el host, no al nombre del servicio de Docker Compose.
ARG VITE_API_BASE_URL=http://localhost:5097/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
