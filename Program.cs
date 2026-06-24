using API_WateringDashboard;
using API_WateringDashboard.Data;
using API_WateringDashboard.Hubs;
using API_WateringDashboard.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                 "http://localhost:3000",
                 "http://192.168.1.92:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); 
    });
});

builder.Services.AddSignalR();
builder.Services.AddHostedService<MqttWorkerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();


app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.MapControllers();


app.MapHub<MqttHub>("/mqtt-hub");

app.Run();