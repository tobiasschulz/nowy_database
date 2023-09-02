FROM node:20-alpine AS build
WORKDIR /app
COPY src/nowy_messagehub/ .
RUN npm install
RUN npm run build

FROM node:20-alpine AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["node", "/app/dist/index.js", "80"]

