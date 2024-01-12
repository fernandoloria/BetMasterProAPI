using WolfApiCore.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();


string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:4200", "http://localhost:8101", "http://localhost:5500", "https://pph-design.netlify.app",
                                              "http://www.contoso.com", "https://live.bridgehost.net",
                                              "https://vegasliveadmin.bridgehost.net", "https://vegaslive.bet", "https://streaming.vegaslive.bet", "https://propb.bridgehost.net", "https://scores.bridgehost.net")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                      });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors(MyAllowSpecificOrigins);

app.MapControllers();
//signalR config

app.MapHub<Messages>("cnn").AllowAnonymous();
//app.MapHub<MessagesPb>("cnnpb").AllowAnonymous();
//**************

app.Run();
