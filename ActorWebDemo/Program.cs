using ActorWebDemo.Service;
using EventStore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventRepository>(provider => 
    new EventRepository(
        connectionString: "Data Source = ./repository.db",
        assemblyContainingEvents: typeof(EventRepository).Assembly)
);
builder.Services.AddSingleton<Backend>();
builder.Services.AddHostedService<Backend>(provider => 
    provider.GetService<Backend>()
);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();