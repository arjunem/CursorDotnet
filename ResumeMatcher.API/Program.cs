using ResumeMatcher.Core.Interfaces;
using ResumeMatcher.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register our services
var useDotNet = builder.Configuration.GetValue<bool>("ResumeMatcher:UseDotNetLogic");
Console.WriteLine($"[Program] UseDotNetLogic configuration value: {useDotNet}");
builder.Services.AddScoped<IResumeSourcingService>(_ => new ResumeSourcingService(useDotNet, builder.Configuration));
builder.Services.AddScoped<IResumeParsingService>(_ => new ResumeParsingService(useDotNet));
builder.Services.AddScoped<IResumeMatchingService, ResumeMatchingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();


app.Run();


