FROM acrgo01easnshared01.azurecr.io/platform/microsoft-dotnet-aspnet:8-standard AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

#RUN apk add --no-cache icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
USER root
RUN apk add --no-cache icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib
RUN apk update
RUN apk add bash
		
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["aia_core/aia_core.csproj", "aia_core/"]
COPY ["cms_api/cms_api.csproj", "cms_api/"]
RUN dotnet restore "cms_api/cms_api.csproj"
COPY . .
WORKDIR "/src/cms_api"
RUN dotnet build "cms_api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "cms_api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

# Create the system group 'appgroup' if it does not exist
RUN addgroup -S appgroup || true

# Check if 'appuser' exists, and create it if it does not
RUN id appuser >/dev/null 2>&1 || adduser -S -G appgroup -u 1001 appuser

# Create the app directory and set ownership
RUN chown -R appuser:appgroup /app

ENTRYPOINT ["dotnet", "cms_api.dll"]