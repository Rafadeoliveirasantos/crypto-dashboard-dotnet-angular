var builder = WebApplication.CreateBuilder(args);

// Adicione a pol�tica de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // endere�o do Angular em dev
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Outros servi�os...
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Use a pol�tica de CORS
app.UseCors("AllowAngular");

// Outros middlewares...
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();