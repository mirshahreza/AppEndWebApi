using AppEndWebApiHelper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// todo : implement logger as a middleware
AppEndLogger.SetupLoggers();

// todo : implement as separated metadate / access ... middle wares
app.UseMiddleware<AppEndMiddleware>();



app.Run();
