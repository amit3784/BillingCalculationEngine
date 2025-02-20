using Carter;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5175").AllowAnyMethod().AllowAnyHeader();
                          policy.WithOrigins("http://localhost:5174").AllowAnyMethod().AllowAnyHeader();
                          policy.WithOrigins("http://localhost:5173").AllowAnyMethod().AllowAnyHeader();
                          policy.WithOrigins("http://localhost:5176").AllowAnyMethod().AllowAnyHeader();
                          policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                      });
});
builder.Services.AddAntiforgery();
builder.Services.AddCarter();
var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAntiforgery();
app.UseHttpsRedirection();
app.MapControllers();
app.MapCarter();

app.Run();
