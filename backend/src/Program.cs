using System.Data;
using Features.TalentPricings.Interfaces;
using Features.TalentPricings.Repository;
using Services;
using Npgsql;
using FluentValidation;
using Stripe;
using Microsoft.Extensions.Options;
using Common.Configuration;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Options
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(StripeSettings.SectionName));

// Dapper Mapping
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddScoped<IDbConnection>(sp => 
    new NpgsqlConnection(builder.Configuration.GetConnectionString("Default")));

// Repositories & Services
builder.Services.AddScoped<ITalentPricingRepository, TalentPricingRepository>();
builder.Services.AddScoped<IStripeClient>(sp => {
    var settings = sp.GetRequiredService<IOptions<StripeSettings>>().Value;
    return new StripeClient(settings.SecretKey);
});
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
app.UseCors("AllowAll");
app.UseMiddleware<Common.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();