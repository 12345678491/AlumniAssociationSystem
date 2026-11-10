# ===== 构建阶段 =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AlumniAssociationSystem.Web/AlumniAssociationSystem.Web.csproj ./AlumniAssociationSystem.Web/
RUN dotnet restore AlumniAssociationSystem.Web/AlumniAssociationSystem.Web.csproj
COPY . .
RUN dotnet publish AlumniAssociationSystem.Web/AlumniAssociationSystem.Web.csproj \
    -c Release -o /app/publish --no-restore

# ===== 运行阶段 =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 云平台会注入 PORT 环境变量，默认 8080
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["sh", "-c", "exec dotnet AlumniAssociationSystem.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
