using CatalogService.Api.Models;
using Microsoft.EntityFrameworkCore;
using ServiceDefaults;
using Shared.Contracts.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Database initialization with retry and seeding
for (int i = 0; i < 5; i++)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        
        await context.Database.EnsureCreatedAsync();
        
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Id = "cat-1", Name = "Electronics", Description = "Electronic devices" },
                new Category { Id = "cat-2", Name = "Accessories", Description = "Computer accessories" }
            );
            await context.SaveChangesAsync();
        }
        
        if (!await context.Products.AnyAsync())
        {
            context.Products.AddRange(
                new Product { Id = "product-1", Name = "Laptop", Description = "High-end gaming laptop", Price = 1500.00m, ImageUri = "https://placehold.co/600x400?text=Laptop", CategoryId = "cat-1" },
                new Product { Id = "product-2", Name = "Mouse", Description = "Wireless ergonomic mouse", Price = 50.00m, ImageUri = "https://placehold.co/600x400?text=Mouse", CategoryId = "cat-2" },
                new Product { Id = "product-3", Name = "Keyboard", Description = "Mechanical RGB keyboard", Price = 120.00m, ImageUri = "https://placehold.co/600x400?text=Keyboard", CategoryId = "cat-2" }
            );
            await context.SaveChangesAsync();
        }
        break; // Success
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Catalog database not ready yet, retrying... (Attempt {Attempt})", i + 1);
        await Task.Delay(5000 * (i + 1)); 
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapGet("/products", async (CatalogDbContext db) =>
    await db.Products.Include(p => p.Category).Select(p => new ProductDto 
    { 
        Id = p.Id, 
        Name = p.Name, 
        Description = p.Description, 
        Price = p.Price,
        ImageUri = p.ImageUri,
        CategoryId = p.CategoryId,
        CategoryName = p.Category != null ? p.Category.Name : ""
    }).ToListAsync());

app.MapGet("/products/{id}", async (string id, CatalogDbContext db) =>
    await db.Products.Include(p => p.Category).Where(p => p.Id == id)
        .Select(p => new ProductDto 
        { 
            Id = p.Id, 
            Name = p.Name, 
            Description = p.Description, 
            Price = p.Price,
            ImageUri = p.ImageUri,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : ""
        })
        .FirstOrDefaultAsync() is ProductDto p ? Results.Ok(p) : Results.NotFound());

app.MapGet("/products/search", async (string q, CatalogDbContext db) =>
    await db.Products.Include(p => p.Category)
        .Where(p => p.Name.Contains(q) || p.Description.Contains(q))
        .Select(p => new ProductDto 
        { 
            Id = p.Id, 
            Name = p.Name, 
            Description = p.Description, 
            Price = p.Price,
            ImageUri = p.ImageUri,
            CategoryId = p.CategoryId,
            CategoryName = p.Category != null ? p.Category.Name : ""
        }).ToListAsync());

app.MapPost("/products", async (CreateProductRequest request, CatalogDbContext db) =>
{
    var product = new Product
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Description = request.Description,
        Price = request.Price,
        ImageUri = request.ImageUri,
        CategoryId = request.CategoryId
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/products/{product.Id}", new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        ImageUri = product.ImageUri,
        CategoryId = product.CategoryId
    });
});

app.MapPut("/products/{id}", async (string id, UpdateProductRequest request, CatalogDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = request.Name;
    product.Description = request.Description;
    product.Price = request.Price;
    product.ImageUri = request.ImageUri;
    product.CategoryId = request.CategoryId;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/products/{id}", async (string id, CatalogDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Category Endpoints
app.MapGet("/categories", async (CatalogDbContext db) =>
    await db.Categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description }).ToListAsync());

app.MapPost("/categories", async (CreateCategoryRequest request, CatalogDbContext db) =>
{
    var category = new Category
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Description = request.Description
    };

    db.Categories.Add(category);
    await db.SaveChangesAsync();

    return Results.Created($"/categories/{category.Id}", new CategoryDto
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description
    });
});

app.Run();
