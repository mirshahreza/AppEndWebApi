using AppEndWebApiHelper;
using Serilog;


Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.WriteTo.File("log.txt",
		rollingInterval: RollingInterval.Day,
		rollOnFileSizeLimit: true)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<AppEndMiddleware>();

app.Run();
