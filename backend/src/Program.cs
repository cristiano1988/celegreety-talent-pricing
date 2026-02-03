using System.Data;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Repository;
using Services;
using Npgsql;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddScoped<IDbConnection>(sp => 
    new NpgsqlConnection(builder.Configuration.GetConnectionString("Default")));

// Repositories & Services
builder.Services.AddScoped<ITalentPricingRepository, TalentPricingRepository>();
builder.Services.AddScoped<IStripeService, StripeService>();

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(Common.Behaviors.ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();