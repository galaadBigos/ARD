using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealDeal.Auth;
using RealDeal.Auth.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<AuthDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}



//				OOOOOOOOOOH FAUT LIRE CECI
// Cette méthode permet de rendre cette API seulement ouverte vers l'api Gateway, vous pouvez commenter ce code si nécessaire. Pareil dans l'api Bet



app.Use(async (context, next) =>
{
	var secret = "SecretAuthApi";
	if (!context.Request.Headers.TryGetValue("X-Api-Secret", out var providedSecret) || providedSecret != secret)
	{
		context.Response.StatusCode = StatusCodes.Status403Forbidden;
		await context.Response.WriteAsync("Forbidden");
		return;
	}
	await next();
});

using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
	context.Database.EnsureCreated();
}

app.MapPost("/user", async ([FromBody] User user, [FromServices] AuthDbContext context) =>
{
	context.Users.Add(user);
	await context.SaveChangesAsync();
	return Results.Created($"/create/{user.Id}", user);
});

app.MapGet("/user/list", ([FromServices] AuthDbContext context) =>
{
	var result = context.Users;
	return Results.Ok(result);
});

app.MapGet("/user/{id:guid}", ([FromRoute] Guid id, [FromServices] AuthDbContext context) =>
{
	var result = context.Users.Where(u => u.Id == id).FirstOrDefault();
	return result is not null ? Results.Ok(result) : Results.NotFound();
});

app.Run();
