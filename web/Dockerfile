# --- Étape 1 : build Angular avec Node ---
FROM node:24-alpine AS build
WORKDIR /app

# package.json et lockfile d'abord : npm ci reste en cache tant qu'ils ne changent pas.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# --- Étape 2 : fichiers statiques servis par nginx ---
FROM nginx:1.30-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/quiz-web/browser /usr/share/nginx/html
EXPOSE 80
