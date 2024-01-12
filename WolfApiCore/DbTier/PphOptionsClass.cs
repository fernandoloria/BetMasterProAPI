using Dapper;
using Microsoft.Data.SqlClient;
using WolfApiCore.Models;

namespace WolfApiCore.DbTier
{
    public class PphOptionsClass
    {

        private readonly string moverConnString = "Data Source=192.168.11.36;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";


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
