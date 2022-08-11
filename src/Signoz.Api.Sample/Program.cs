using A55.Signoz;

var builder = WebApplication.CreateBuilder(args);

builder.UseSignoz();

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var app = builder.Build();

app.MapGet("/hello", () => "World!");
app.MapGet("/bye", () => { throw new InvalidProgramException("expected error"); });

app.UseSwagger().UseSwaggerUI();

app.Run();
