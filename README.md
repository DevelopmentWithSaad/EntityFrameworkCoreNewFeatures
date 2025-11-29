# 📘 EntityFrameworkCoreNewFeatures

A **.NET 8 Minimal API** project demonstrating the latest **Entity
Framework Core (EF Core)** features with practical implementation, clean
architecture, and production-level patterns.

This project is built in **Visual Studio 2022**, using **code-first
approach**, temporal tables, JSON columns, interceptors, soft delete,
stored procedures, Swagger documentation, and more.

------------------------------------------------------------------------

## 🚀 **Overview**

This repository showcases modern EF Core 8 capabilities inside a
real-world REST API using **Minimal APIs**.

It contains:

-   Temporal Tables for `Product` & `Category`
-   JSON Columns (`ProductSpecifications`)
-   Custom EF Interceptors
-   Bulk Update & Bulk Delete
-   Global Query Filters
-   Split Queries
-   Stored Procedures (Insert/Update/Delete)
-   Soft Delete (`IsDeleted`)
-   JSON Circular Reference handling
-   Clean folder structure\
-   Swagger/OpenAPI with minimal API

------------------------------------------------------------------------

## 🛠️ **Technologies Used**

  Technology          Version
  ------------------- ---------
  .NET                8
  EF Core             8
  SQL Server          2019+
  Visual Studio       2022
  C#                  12
  Swagger / OpenAPI   Enabled

------------------------------------------------------------------------

# 📂 Project Structure

    EntityFrameworkCoreNewFeatures/
    │
    ├── Data/
    │   └── AppDbContext.cs
    │
    ├── DTO/
    │   └── ProductDto.cs
    │
    ├── Interceptors/
    │   └── CustomSaveChangesInterceptor.cs
    │
    ├── Migrations/
    │   └── (Auto-generated EF Core Migrations)
    │
    ├── Models/
    │   ├── Category.cs
    │   ├── Product.cs
    │   └── ProductSpecifications.cs
    │
    ├── Program.cs
    └── README.md

------------------------------------------------------------------------

# ✨ **Entity Framework Core 8 Features Included**

## 1️⃣ AsNoTracking()

Improves read performance.

## 2️⃣ Bulk Update

Efficient batch operations.

## 3️⃣ Bulk Delete

Deletes without loading entities.

## 4️⃣ JSON Column

Maps ProductSpecifications into SQL Server JSON.

## 5️⃣ Split Queries

Optimizes loading of related data.

## 6️⃣ Temporal Tables

Track history of Products & Categories.

## 7️⃣ Stored Procedures

Used for insert, update, delete of Product.

## 8️⃣ Global Query Filter

Soft-delete using `IsDeleted`.

## 9️⃣ Interceptors

Custom SaveChanges interceptor.

------------------------------------------------------------------------

# 🔄 JSON Circular Reference Handling

Configured with:

    ReferenceHandler.IgnoreCycles

------------------------------------------------------------------------

# ▶️ How to Run the Project

1.  Clone repository\
2.  Update connection string\
3.  Run migrations\
4.  Start API\
5.  Open Swagger at `/swagger`

------------------------------------------------------------------------

# 🤝 Contributing

Pull requests are welcome.

# 📜 License

MIT License
