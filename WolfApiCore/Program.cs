using WolfApiCore.DbTier;
using WolfApiCore.Hubs;
using WolfApiCore.Models;
using WolfApiCore.Utilities;


var builder = WebApplication.CreateBuilder(args);


//-- TEST -----------------------------------
/*
var dbWager = new LiveDbWager();

LSport_BetSlipObj betslip = new LSport_BetSlipObj();

betslip.IdPlayer = 300563;


betslip.Events = new List<LSport_BetGame>();

betslip.Events.Add(new LSport_BetGame()
{
    FixtureId = 12628387,
    VisitorTeam = "TiPS",
    HomeTeam = "LPS",
    SportName = "SOCCER",
    LeagueName = "FINLAND - SUOMEN CUP",
    Selections = new List<LSport_EventPropDto>()
});

betslip.Events[0].Selections.Add(new LSport_EventPropDto() {
    IdL1 = "208021323512628387",
    FixtureId = 12628387,
    MarketId = 2,
    Line1 = "4.5",
    Odds1 = -175,
    Price = 1.571m,
    Name = "Under",
    BaseLine = "4.5",
    BsRiskAmount = 175,
    BsWinAmount = 100
});


dbWager.ValidateSelectionsForWagers(betslip);
*/
//------------------------------------------

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();
builder.Services.AddTransient<Base64Service>();


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
