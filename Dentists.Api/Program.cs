using Dentists.Api.Extensions;
using Dentists.Application.Behaviors;
using Dentists.Application.Queries;
using Dentists.Domain.Repositories;
using Dentists.Infrastructure.Persistence;
using Dentists.Infrastructure.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseName = builder.Configuration["Cosmos:DatabaseName"];

builder.Services.AddDbContext<DentistsDbContext>(options =>
    options.UseCosmos(connectionString!, databaseName!));

// Add Unit of Work and Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDentistRepository, DentistRepository>();

// Add MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetAllDentistsQuery).Assembly));

// Add FluentValidation, executed for every request through the MediatR pipeline
builder.Services.AddValidatorsFromAssembly(typeof(GetAllDentistsQuery).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Keep the built-in messages in English rather than following the server's culture.
ValidatorOptions.Global.LanguageManager.Enabled = false;

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Cosmos has no migrations: the database and its containers have to exist before the
    // first query. Provisioning outside the app is the expectation everywhere else.
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DentistsDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
