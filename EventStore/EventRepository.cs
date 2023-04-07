using System.Reflection;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace EventStore;

public class EventRepository: IEventRepository
{
    private bool _databaseCreated = false;
    private readonly string _connectionString;
    private readonly Dictionary<string, Type> _typeFor;

    public EventRepository(string connectionString, Assembly assemblyContainingEvents)
    {
        _connectionString = connectionString;

        _typeFor = assemblyContainingEvents
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEvent)))
            .ToDictionary(keySelector: t => t.Name);
    }

    private SqliteConnection OpenConnectionAsync()
    {
        var connection = new SqliteConnection(_connectionString); 
        connection.Open();
        return connection;
    }
    
    private void CreateDatabaseIfNotExists(SqliteConnection connection)
    {
        if (_databaseCreated) return;
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS event (
                aggregate_id INT, 
                version INT,
                occured_on INT,
                type TEXT,
                payload TEXT)
            ";
        command.ExecuteNonQuery();
        _databaseCreated = true;
    }

    public void Append(int aggregateId, int version, IEvent @event)
    { 
        using var connection = OpenConnectionAsync();
        CreateDatabaseIfNotExists(connection);
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO event (aggregate_id, version, occured_on, type, payload)
            VALUES ($aggregateId, $version, $occuredOn, $type, $payload)
            ";
        command.Parameters.AddWithValue("$aggregateId", aggregateId);
        command.Parameters.AddWithValue("$version", version);
        command.Parameters.AddWithValue("$occuredOn", DateTime.Now.Ticks);
        command.Parameters.AddWithValue("$type", @event.GetType().Name);
        command.Parameters.AddWithValue("$payload", JsonConvert.SerializeObject(@event));
        command.ExecuteNonQueryAsync();
    }

    public IList<IEvent> Load(int aggregateId)
    {
        using var connection = OpenConnectionAsync();
        CreateDatabaseIfNotExists(connection);
        var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT type, payload 
            FROM event
            WHERE aggregate_id = '{aggregateId}'
            ORDER BY version
            ";
        using var reader = command.ExecuteReader();
        var result = new List<IEvent>();
        while (reader.Read())
        {
            var type = _typeFor[reader.GetString(0)];
            var payload = reader.GetString(1);
            result.Add((IEvent)JsonConvert.DeserializeObject(payload, type));
        }

        return result;
    }
}