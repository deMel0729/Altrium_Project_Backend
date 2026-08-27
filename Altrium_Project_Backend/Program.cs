using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Repositories;
using Altrium_Project_Backend.Repositories.Interfaces;


var builder = WebApplication.CreateBuilder(args);



// Adding the Controllers 
builder.Services.AddControllers();
// added CORS policy to allow requests from the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// regitering the database connection factory
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
// registering repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<IDealRepository, DealRepository>();
builder.Services.AddScoped<IEngagementRepository, EngagementRepository>();
builder.Services.AddScoped<IFollowUpRepository, FollowUpRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();



var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseCors("dev");

app.UseAuthorization();

app.MapControllers();

app.Run();
