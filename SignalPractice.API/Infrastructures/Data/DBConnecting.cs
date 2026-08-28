namespace SignalPractice.API.Infrastructures.Data;

public class DBConnecting(IConfiguration configuration)
{
    //管理連線
    public SqlConnection CreateConnecting() => new SqlConnection(configuration.GetConnectionString("DBConnecting"));
}
