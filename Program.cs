using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    new MySqlServerVersion(new Version(8, 0, 41)),
    MySqlOptions => MySqlOptions.EnableRetryOnFailure()));
// ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB"))));


builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        }));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
    Console.WriteLine($"🔍 Connection String: {Environment.GetEnvironmentVariable("ToDoDB")}");
    try
    {
        dbContext.Database.OpenConnection();
        Console.WriteLine("✅ הצלחנו להתחבר למסד הנתונים!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ חיבור למסד הנתונים נכשל: {ex.Message}");
    }
}


app.UseSwagger();
app.UseSwaggerUI();


app.UseCors("MyPolicy");

app.MapGet("/", async (ToDoDbContext Db) =>
{
    return await Db.Items.ToListAsync();
});

app.MapPost("/", async (String name, ToDoDbContext Db) =>
{
    var todoItem = new Item
    {
        IsComplete = false,
        Name = name
    };

    Db.Items.Add(todoItem);
    await Db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todoItem.Id}", todoItem);
});

app.MapPut("/{id}", async (int Id, bool IsComplete, ToDoDbContext Db) =>
{
    var todo = await Db.Items.FindAsync(Id);

    if (todo is null) return Results.NotFound();

    todo.IsComplete = IsComplete;
    await Db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/{id}", async (int Id, ToDoDbContext Db) =>
{
    if (await Db.Items.FindAsync(Id) is Item todo)
    {
        Db.Items.Remove(todo);
        await Db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});


app.Run();




