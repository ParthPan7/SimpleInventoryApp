# SimpleInventoryApp

A lightweight, enterprise-ready WPF inventory management application
built with .NET 8, MVVM, EF Core, SQL Server in Docker, and full xUnit
test coverage.\
This README provides setup instructions, Docker configuration, EF Core
guidance, test project setup, folder structure, and a clear refactor
roadmap.

## 🚀 Prerequisites

-   Windows 10/11
-   .NET 8 SDK
-   Visual Studio 2022
-   Docker Desktop for Windows (WSL2 recommended)

## 🐳 Docker Setup

### Install Docker Desktop

Verify:

    docker version
    docker info

## 🗄️ Run SQL Server in Docker

    docker pull mcr.microsoft.com/mssql/server:2022-latest
    docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_password123" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

Connection string:

    Server=localhost,1433;Database=SimpleInventoryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;

## 📦 EF Core Configuration

### Register DbContext

``` csharp
services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

### Migrations

    dotnet ef migrations add InitialCreate -p SimpleInventoryApp
    dotnet ef database update -p SimpleInventoryApp

## 🧪 Test Project Setup

Add packages:

    dotnet add SimpleInventoryApp.Tests package xunit
    dotnet add SimpleInventoryApp.Tests package Microsoft.EntityFrameworkCore.InMemory

Sample test:

``` csharp
[Fact]
public void ProductName_IsRequired()
{
    var product = new Product { ProductCategory = "Electronics", ProductQuantity = 1 };
    var ctx = new ValidationContext(product);
    var results = new List<ValidationResult>();
    var valid = Validator.TryValidateObject(product, ctx, results, true);
    Assert.False(valid);
}
```

## 📁 Folder Structure Refactoring Plans

    SimpleInventoryApp/
      Core/
      Data/
      UI/
      Tests/

## 🔧 Refactor Roadmap

### Phase 1 --- Validation & Tests

### Phase 2 --- Repository Pattern

### Phase 3 --- Namespace Cleanup

### Phase 4 --- Multi‑Project Split

## ⚠️ Common Pitfalls

-   Version mismatches
-   Incorrect DbContext constructor
-   Missing `TrustServerCertificate=True`

## 💡 Enhancements

-   Serilog
-   CI/CD
-   Docker Compose
