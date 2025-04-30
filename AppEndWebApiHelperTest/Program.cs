using AppEndWebApiHelper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

AppEndLogger.SetupLoggers();
app.UseMiddleware<AppEndMiddleware>();

app.Run();
