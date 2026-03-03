using System.Collections;
using System.Data;
using System.Data.Common;
using DoctorSoft.Data.Access;

namespace DoctorSoft.Tests.TestInfrastructure;

internal sealed class DelegateConnectionFactory : IOleDbConnectionFactory
{
    private readonly DelegateDbBehavior behavior;

    public DelegateConnectionFactory(Func<string, IReadOnlyList<object?>, IReadOnlyList<IReadOnlyDictionary<string, object?>>> queryHandler)
    {
        behavior = new DelegateDbBehavior { ReaderHandler = queryHandler };
    }

    public DelegateConnectionFactory(DelegateDbBehavior behavior)
    {
        this.behavior = behavior;
    }

    public DbConnection Create()
    {
        return new DelegateDbConnection(behavior);
    }
}

internal sealed class DelegateDbBehavior
{
    public Func<string, IReadOnlyList<object?>, IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReaderHandler { get; init; } =
        static (_, _) => Array.Empty<IReadOnlyDictionary<string, object?>>();

    public Func<string, IReadOnlyList<object?>, object?> ScalarHandler { get; init; } = static (_, _) => 0;

    public Func<string, IReadOnlyList<object?>, int> NonQueryHandler { get; init; } = static (_, _) => 1;

    public List<ExecutedCommand> Commands { get; } = new();

    public int CommittedTransactions { get; set; }

    public int RolledBackTransactions { get; set; }
}

internal sealed record ExecutedCommand(string Kind, string CommandText, IReadOnlyList<object?> Parameters);

internal sealed class DelegateDbConnection : DbConnection
{
    private readonly DelegateDbBehavior behavior;
    private ConnectionState state = ConnectionState.Closed;

    public DelegateDbConnection(DelegateDbBehavior behavior)
    {
        this.behavior = behavior;
    }

    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "Test";

    public override string DataSource => "Test";

    public override string ServerVersion => "1.0";

    public override ConnectionState State => state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Open()
    {
        state = ConnectionState.Open;
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        state = ConnectionState.Open;
        return Task.CompletedTask;
    }

    public override void Close()
    {
        state = ConnectionState.Closed;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return new DelegateDbTransaction(this, isolationLevel, behavior);
    }

    protected override DbCommand CreateDbCommand()
    {
        return new DelegateDbCommand(behavior) { Connection = this };
    }
}

internal sealed class DelegateDbTransaction : DbTransaction
{
    private readonly DbConnection connection;
    private readonly IsolationLevel isolationLevel;
    private readonly DelegateDbBehavior behavior;

    public DelegateDbTransaction(DbConnection connection, IsolationLevel isolationLevel, DelegateDbBehavior behavior)
    {
        this.connection = connection;
        this.isolationLevel = isolationLevel;
        this.behavior = behavior;
    }

    public override IsolationLevel IsolationLevel => isolationLevel;

    protected override DbConnection DbConnection => connection;

    public override void Commit()
    {
        behavior.CommittedTransactions++;
    }

    public override void Rollback()
    {
        behavior.RolledBackTransactions++;
    }
}

internal sealed class DelegateDbCommand : DbCommand
{
    private readonly DelegateDbBehavior behavior;
    private readonly DelegateDbParameterCollection parameters = new();

    public DelegateDbCommand(DelegateDbBehavior behavior)
    {
        this.behavior = behavior;
    }

    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection => parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        behavior.Commands.Add(new ExecutedCommand("NonQuery", CommandText, parameters.Values));
        return behavior.NonQueryHandler(CommandText, parameters.Values);
    }

    public override object? ExecuteScalar()
    {
        behavior.Commands.Add(new ExecutedCommand("Scalar", CommandText, parameters.Values));
        return behavior.ScalarHandler(CommandText, parameters.Values);
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter()
    {
        return new DelegateDbParameter();
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return BuildReader();
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ExecuteNonQuery());
    }

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ExecuteScalar());
    }

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        return Task.FromResult<DbDataReader>(BuildReader());
    }

    private DbDataReader BuildReader()
    {
        behavior.Commands.Add(new ExecutedCommand("Reader", CommandText, parameters.Values));
        var rows = behavior.ReaderHandler(CommandText, parameters.Values);
        var table = new DataTable();

        var columns = rows.SelectMany(row => row.Keys).Distinct(StringComparer.Ordinal).ToList();
        foreach (var column in columns)
        {
            table.Columns.Add(column, typeof(object));
        }

        foreach (var row in rows)
        {
            var values = columns.Select(c => row.TryGetValue(c, out var value) ? value ?? DBNull.Value : DBNull.Value).ToArray();
            table.Rows.Add(values);
        }

        return table.CreateDataReader();
    }
}

internal sealed class DelegateDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> parameters = new();

    public IReadOnlyList<object?> Values => parameters.Select(p => p.Value).ToList();

    public override int Count => parameters.Count;

    public override object SyncRoot => ((ICollection)parameters).SyncRoot;

    public override int Add(object value)
    {
        parameters.Add((DbParameter)value);
        return parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value!);
        }
    }

    public override void Clear()
    {
        parameters.Clear();
    }

    public override bool Contains(string value)
    {
        return parameters.Any(p => string.Equals(p.ParameterName, value, StringComparison.Ordinal));
    }

    public override bool Contains(object value)
    {
        return parameters.Contains((DbParameter)value);
    }

    public override void CopyTo(Array array, int index)
    {
        ((ICollection)parameters).CopyTo(array, index);
    }

    public override IEnumerator GetEnumerator()
    {
        return parameters.GetEnumerator();
    }

    public override int IndexOf(string parameterName)
    {
        return parameters.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.Ordinal));
    }

    public override int IndexOf(object value)
    {
        return parameters.IndexOf((DbParameter)value);
    }

    public override void Insert(int index, object value)
    {
        parameters.Insert(index, (DbParameter)value);
    }

    public override void Remove(object value)
    {
        parameters.Remove((DbParameter)value);
    }

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            parameters.RemoveAt(index);
        }
    }

    public override void RemoveAt(int index)
    {
        parameters.RemoveAt(index);
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        return parameters[IndexOf(parameterName)];
    }

    protected override DbParameter GetParameter(int index)
    {
        return parameters[index];
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            parameters[index] = value;
        }
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        parameters[index] = value;
    }
}

internal sealed class DelegateDbParameter : DbParameter
{
    public override DbType DbType { get; set; }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

    public override string ParameterName { get; set; } = string.Empty;

    public override string SourceColumn { get; set; } = string.Empty;

    public override object? Value { get; set; }

    public override bool SourceColumnNullMapping { get; set; }

    public override int Size { get; set; }

    public override void ResetDbType()
    {
    }
}
