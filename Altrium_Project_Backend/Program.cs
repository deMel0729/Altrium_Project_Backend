using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Repositories;
using Altrium_Project_Backend.Repositories.Interfaces;


var builder = WebApplication.CreateBuilder(args);
// Adding the Controllers 
builder.Services.AddControllers();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// regitering the database connection factory
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
// registering repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();



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
