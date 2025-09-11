# Shlongo 📦
*A C# automated migration framework for MongoDB.*

---

## 🚀 What Is Shlongo
Shlongo is a lightweight, opinionated framework to help manage **database schema/data migrations** in MongoDB using C#, which helps you version, apply, and rollback changes safely—automating repetitive tasks so your team spends less time worrying about migration chaos.

---

## ✨ Features
- Organised, versioned migrations  
- Automatic discovery & execution of migrations in order  
- Integration-friendly: works inside existing .NET / C# projects  

---

## 🏗️ Project Structure
```
/Shlongo                  ← Core migration framework
/Shlongo.Aspire           ← Extensions or aspiration module
/Shlongo.Examples.Api     ← Example usage via an API project
/Shlongo.Tests            ← Unit tests
/Shlongo.TestsInt         ← Integration tests (MongoDB, real or mocked)
/Shlongo.sln              ← Solution file
LICENSE                   ← MIT license
.gitignore
README.md
```

---

## 📋 Getting Started

### Prerequisites
- .NET 7.0+ SDK (or the version targeted by the project)  
- MongoDB instance (local or remote)  
- Basic familiarity with C# / .NET  

### Installation
```bash
git clone https://github.com/Affelios/Shlongo.git
cd Shlongo
git checkout kb
```

- Add the **Shlongo** project / DLL to your solution (`.csproj`) - a NuGet package is coming.  
- Configure your MongoDB connection (e.g. `appsettings.json`).  

---

## 🔌 Example Usage
```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddShlongo(config =>
        {
            config.MongrationAssembly = Assembly.GetExecutingAssembly();
            config.MongoClientSettings = MongoClientSettings.FromConnectionString(builder.Configuration.GetConnectionString("mongo"));
            config.MongoDatabaseName = "mongodb";
        });
    }
}
```

## 🔌 Example Migration
```csharp
public class _0001_Initial : Mongration
{
    public override async Task UpAsync(IMongrationContext context)
    {
        await context.Database.CreateCollectionAsync("users");
    }
}
```

---

## 🧪 Running Tests
```bash
cd Shlongo.Tests
dotnet test

cd ../Shlongo.TestsInt
dotnet test
```
Make sure MongoDB is available for integration tests.

---

## 💡 Why Use Shlongo
- Schema changes are tracked & reproducible  
- Eliminates “works on my machine” deployment issues  
- Example project gives a clear usage pattern  

---

## 📚 License & Contributions
Licensed under **MIT License**.  
Contributions welcome via PRs, bug reports, or feature requests!  
