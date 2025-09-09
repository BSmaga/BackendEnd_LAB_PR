# Biblioteka – PAB_LAB_14363

## Jak uruchomić
1. dotnet build .\Biblioteka.sln
2. API: dotnet run --project .\src\Biblioteka.Api\Biblioteka.Api.csproj --urls http://localhost:5180
   - Swagger: http://localhost:5180/swagger
3. GraphQL: dotnet run --project .\src\Biblioteka.GraphQL\Biblioteka.GraphQL.csproj --urls http://localhost:5090
   - Banana Cake Pop: http://localhost:5090/graphql
4. Testy: dotnet test .\tests\Biblioteka.Tests\Biblioteka.Tests.csproj
5. Skrypt smoke: pwsh .\tests\smoke.ps1` (Windows PowerShell 7+)

## Konta seedowane
- Admin: admin@lib.pl / Pass!23
- User:  user@lib.pl  / Pass!23

## Wymagania
- .NET 9 SDK
