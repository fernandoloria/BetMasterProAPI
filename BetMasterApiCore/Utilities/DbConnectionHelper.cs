using Microsoft.Data.SqlClient;

namespace BetMasterApiCore.Utilities
{
    public class DbConnectionHelper
    {
        private readonly IConfiguration _configuration;

        public DbConnectionHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection GetDgsConnection()
        {
            string conn = _configuration.GetValue<string>("SrvSettings:DbConnDGS");
            return new SqlConnection(conn);
        }

        public SqlConnection GetMoverConnection()
        {
            string conn = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            return new SqlConnection(conn);
        }
    }
}
