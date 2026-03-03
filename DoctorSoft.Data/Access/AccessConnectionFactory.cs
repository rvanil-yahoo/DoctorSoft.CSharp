using System.Data.Common;
using System.Data.Odbc;

namespace DoctorSoft.Data.Access;

public sealed class AccessConnectionFactory : IOleDbConnectionFactory
{
    private readonly string connectionString;

    public AccessConnectionFactory(string mainDbPath)
    {
        if (string.IsNullOrWhiteSpace(mainDbPath))
        {
            throw new ArgumentException("Database path is required.", nameof(mainDbPath));
        }

        connectionString = $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};Dbq={mainDbPath};";
    }

    public DbConnection Create()
    {
        return new OdbcConnection(connectionString);
    }
}
