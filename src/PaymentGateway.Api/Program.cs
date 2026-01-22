//using PaymentGateway.Api.Services;
using PaymentGateway.Api;
using PaymentGateway.Api.Endpoints;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Application.Common.Behaviors;
using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Application.Payments.ProcessPayment;
using PaymentGateway.Infrastructure.BankClient;
using PaymentGateway.Infrastructure.Repositories;

using Serilog;

using static System.Net.Mime.MediaTypeNames;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

//builder.Services.AddControllers(); // removed, changes to Minimal APIs

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddSingleton<PaymentsRepository>();

builder.Services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ProcessPaymentCommand>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});


// Configure Bank Client
builder.Services.Configure<BankClientOptions>(
    builder.Configuration.GetSection(BankClientOptions.SectionName));

builder.Services.AddHttpClient<IAcquiringBankClient, AcquiringBankClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BankClientOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<BankHealthCheck>("acquiring_bank");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseCors();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();


app.MapPaymentEndpoints();

app.Run();

public partial class Program { }