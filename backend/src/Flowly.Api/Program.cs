var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var cs = builder.Configuration["ConnectionStrings:DefaultConnection"]
         ?? builder.Configuration["DATABASE_CONNECTION_STRING"];

// builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
