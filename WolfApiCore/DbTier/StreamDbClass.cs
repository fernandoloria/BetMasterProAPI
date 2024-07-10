using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WolfApiCore.Models;

namespace WolfApiCore.DbTier
{
    public static class StreamDbClass
    {
        private static readonly string moverConnString = "Data Source=192.168.11.29;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";

        public static void PushNotification(BroadcastNotification notification)
        {   
            using (var connection = new SqlConnection(moverConnString))
            {
                var procedure = "[sp_MGL_PushStreamNotification]";
                var values = new
                {
                    event_id = notification.event_id,
                    sport_name = notification.sport_name,
                    league_name = notification.league_name,                         
                    team1 = notification.team1,
                    team2 = notification.team2, 
                    date = notification.date,
                    ts = notification.ts,   
                    broadcast = notification.broadcast
                };
                connection.Execute(procedure, values, commandType: CommandType.StoredProcedure);
            }
        }

        public static void DeleteNotification(BroadcastNotification notification)
        {
            using (var connection = new SqlConnection(moverConnString))
            {
                var procedure = "[sp_MGL_DeleteStreamNotification]";
                var values = new
                {
                    event_id = notification.event_id
                };
                connection.Execute(procedure, values, commandType: CommandType.StoredProcedure);
            }
        }

    }
}
