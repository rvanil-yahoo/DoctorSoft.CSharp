using System.Data.Common;

namespace DoctorSoft.Data.Access;

public interface IOleDbConnectionFactory
{
    DbConnection Create();
}
