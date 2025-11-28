using Azure.Core;
using Azure;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Net;
using BetMasterApiCore.Models;
using BetMasterApiCore.Stream;
using BetMasterApiCore.Utilities;

namespace BetMasterApiCore.DbTier
{
    public static class StreamDbClass
    {
        private static readonly string moverConnString;
        private static readonly DbConnectionHelper _dbHelper;

        // 🔹 Constructor estático: se ejecuta una sola vez al cargar la clase
        static StreamDbClass()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            _dbHelper = new DbConnectionHelper(config);

            // Obtiene la cadena desde appsettings.json
            moverConnString = config.GetValue<string>("SrvSettings:DbConnMover");
        }
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

        public static List<StreamLinksDTO> getStreamLinks(int fixtureId) 
        {
            using (var connection = new SqlConnection(moverConnString))
            {
                var procedure = "exec sp_MGL_GetStreamLink @fixtureId ";
                var values = new { fixtureId = fixtureId };
                
                return connection.Query<StreamLinksDTO>(procedure, values).ToList();
            }
        }

        public static ResponseStreamAccess GetStreamAccessRules(int idPlayer)
        {
            var response = new ResponseStreamAccess();

            try
            {
                string sql = "exec sp_MGL_StreamAccess  @idplayer ";
                var values = new { idplayer = idPlayer };

                using var connection = new SqlConnection(moverConnString);

                var responseSQL = connection.Query<ResponseStreamAccess>(sql, values).First();

                response.Access = responseSQL.Access;
                response.Message = responseSQL.Message;

            }
            catch (Exception ex)
            {
                response.Message = "An internal error has occurred";
                response.Access = false;
                return response;
            }

            return response;
        }
    }
}
