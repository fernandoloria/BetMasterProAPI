using WolfApiCore.DbTier;
using WolfApiCore.Hubs;
using WolfApiCore.Models;
using WolfApiCore.Utilities;


var builder = WebApplication.CreateBuilder(args);


//-- TEST -----------------------------------
/*
var testEnabled = true;

if(testEnabled)
{
    var dbWager = new LiveDbWager();

    LSport_BetSlipObj betslip = new LSport_BetSlipObj();

    betslip.IdPlayer = 300563;
    betslip.IsMobile = true;


    betslip.Events = new List<LSport_BetGame>();

    betslip.Events.Add(new LSport_BetGame()
    {
        FixtureId = 10518376,        
        HomeTeam = "Harris English",
        VisitorTeam = "Chris Kirk",
        SportName = "Golf",
        LeagueName = "US Masters 2024",

        Selections = new List<LSport_EventPropDto>()
    });

    //163145791010518376	10518376	274	Outright Winner	NULL	Scottie Scheffler	1	1.0	5	53	2024-04-11 23:51:17.403	140	2024-04-11 19:51:17.063	NULL	NULL

    betslip.Events[0].Selections.Add(new LSport_EventPropDto() 
    {
        IdL1 = "163145791010518376",
        FixtureId = 10518376,
        MarketId = 274,
        MarketName = "Outright Winner",
        Line1 = null,
        Odds1 = 140,
        Price = 2.4m,
        Name = "Scottie Scheffler",
        BaseLine = null,
        BsRiskAmount = 10,
        BsWinAmount = 14
    });


    dbWager.ValidateSelectionsForWagers(betslip);
}
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
