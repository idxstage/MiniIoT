#step 1: build 
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-stage 
RUN mkdir app 
WORKDIR /app
COPY ./Utils/Utils.csproj ./Utils/Utils.csproj
COPY ./Management/Management.csproj ./Management/Management.csproj
WORKDIR /app/Management
RUN dotnet restore 
WORKDIR /app
COPY ./Utils ./Utils
COPY ./Management ./Management
WORKDIR /app/Management
#WORKDIR /app/WebService
RUN dotnet publish -c Release -o out
# ENTRYPOINT dotnet /app/WebService/out/Microsoft.Azure.IoTSolutions.UIConfig.WebService.dll

#step 2: run 
# FROM microsoft/dotnet:2.0.3-runtime-jessie
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app 
COPY --from=build-stage /app/Management/out . 
ENTRYPOINT dotnet Management.dll