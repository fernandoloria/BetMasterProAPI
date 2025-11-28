using Dapper;
using Microsoft.Data.SqlClient;
using BetMasterApiCore.Models;
using BetMasterApiCore.Utilities;

namespace BetMasterApiCore.DbTier
{
    public class PphOptionsClass
    {

        private readonly string moverConnString;
        private readonly DbConnectionHelper _dbHelper;

        public PphOptionsClass(string moverConn)
        {
            moverConnString = moverConn;
        }

        public PphOptionsClass()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            _dbHelper = new DbConnectionHelper(config);

            moverConnString = config.GetValue<string>("SrvSettings:DbConnMover");
        }

        public List<RespPphOptionsModel> getPphOptions(ReqPphOptionsModel req)
        {
            List<RespPphOptionsModel> respList = new List<RespPphOptionsModel>();

            //string functionName = "fn_pphOptionsSelect";

            try
            {
                using (var connection = new SqlConnection(moverConnString))
                    {

                    respList = connection.Query<RespPphOptionsModel>(
                            @"  select * from fn_pphOptionsSelect (@type,@page)  as a order by a.""order"" asc",
                            new { type = req.Type, page = req.Page }
                        ).ToList();


                    }
            }
            catch (Exception ex)
            {
                // Manejar la excepción
            }

            return respList;
        }


    }


}
