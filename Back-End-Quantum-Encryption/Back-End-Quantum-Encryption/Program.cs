using System.Text.Json.Serialization;
using Back_End_Quantum_Encryption.Hubs;
using Back_End_Quantum_Encryption.Quantum;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    // Send enums as "Rectilinear"/"Diagonal" instead of 0/1, easier for the front-end.
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<Bb84Simulator>();

// SignalR: real-time connection with the front-end (WebSockets under the hood).
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

// Allow the Vite dev server to call this API. SignalR also needs AllowCredentials.
var frontendOrigins = builder.Configuration.GetSection("Cors:FrontendOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // There is no page at "/", so send the browser straight to Swagger.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseCors();
app.MapControllers();
app.MapHub<Bb84Hub>("/hubs/bb84");

app.Run();

// Makes Program visible to the integration tests.
public partial class Program;
