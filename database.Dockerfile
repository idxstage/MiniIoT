#step 1: build 
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-stage 
RUN mkdir app 
WORKDIR /app
COPY ./Utils/Utils.csproj ./Utils/Utils.csproj
COPY ./Database/Database.csproj ./Database/Database.csproj
WORKDIR /app/Database
RUN dotnet restore 
WORKDIR /app
COPY ./Utils ./Utils
COPY ./Database ./Database
WORKDIR /app/Database
#WORKDIR /app/WebService
RUN dotnet publish -c Release -o out
# ENTRYPOINT dotnet /app/WebService/out/Microsoft.Azure.IoTSolutions.UIConfig.WebService.dll

#step 2: run 
# FROM microsoft/dotnet:2.0.3-runtime-jessie
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app 
COPY --from=build-stage /app/Database/out . 
ENTRYPOINT dotnet Database.dll