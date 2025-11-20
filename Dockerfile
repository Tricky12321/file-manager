FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_24.x | bash \
    && apt-get install nodejs -yq \
	&& apt-get install -y libgdiplus
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_24.x | bash \
    && apt-get install nodejs -yq \
	&& apt-get install -y libgdiplus
RUN npm install --global yarn
WORKDIR /src
COPY ["FileScanner/FileScanner.csproj", "FileScanner/"]
RUN dotnet restore "FileScanner/FileScanner.csproj"
COPY . .
WORKDIR /src/FileScanner
RUN dotnet build "FileScanner.csproj" -c Release -o /app/build
RUN dotnet publish "FileScanner.csproj" -c Release -o /app/publish
FROM build AS publish

COPY --from=build /app/publish .
FROM base AS final
ENV LANG C.UTF-8
ENV LC_ALL C.UTF-8
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["dotnet", "FileScanner.dll"]
