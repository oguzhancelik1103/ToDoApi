using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ItemRepository>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Map-CRUD iþlemleri ve Grouping

var items = app.MapGroup("/todoItems");

items.MapGet("/", ([FromServices] ItemRepository items) => 
{
    return items.GetAll();
});

items.MapGet("/{id}", ([FromServices] ItemRepository items, int id) =>
{
    return items.GetById(id);
});

items.MapPost("/", ([FromServices] ItemRepository items, Item item) =>
{
    if (items.GetById(item.id) == null)
    {
        items.Add(item);
        return Results.Created($"/{item.id}", item);
    }
    return Results.BadRequest();
});

items.MapPut("/{id}", ([FromServices] ItemRepository items, int id, Item item) =>
{
    if (items.GetById(id) == null)
    {
        return Results.BadRequest();
    }

    items.Update(item);
    return Results.NoContent();
});

items.MapDelete("/{id}", ([FromServices] ItemRepository items, int id) =>
{
    if (items.GetById(id) == null)
    {
        return Results.BadRequest();
    }

    items.Delete(id);
    return Results.NoContent();
});



app.UseHttpsRedirection();

app.Run();


record Item(int id, string title, bool complete);

class ItemRepository
{
    private readonly ApiDbContext _context;

    public ItemRepository(ApiDbContext context)
    {
        _context = context;
    }

    //CRUD iþlemleri
    public List<Item> GetAll() => _context.Items.ToList();

    public Item? GetById(int id) => _context.Items.FirstOrDefault(x => x.id == id) == null ? null 
        : _context.Items.First(x => x.id == id);

    public void Add(Item obj)
    {
        _context.Items.Add(obj);
        _context.SaveChanges();
    }
    public void Update(Item obj)
    {
        var objExist = GetById(obj.id);

        if (objExist != null)
           _context.Entry(objExist).CurrentValues.SetValues(obj);

        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var objExist = GetById(id);

        if(objExist != null)
            _context.Remove(objExist);
        
        _context.SaveChanges();
    }
}

class ApiDbContext : DbContext
{
    public virtual DbSet<Item> Items { get; set; }
    
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
        
    }
}