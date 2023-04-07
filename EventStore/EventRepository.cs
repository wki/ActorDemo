using Microsoft.Data.Sqlite;

namespace EventStore;

public class EventRepository: IEventRepository
{
    private readonly string _connectionString;

    public EventRepository(string connectionString)
    {
        _connectionString = connectionString;
        CreateDatabaseIfNotExists();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString); 
        connection.Open();
        return connection;
    }
    
    private void CreateDatabaseIfNotExists()
    {
        using var connection = OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText =
            @"CREATE TABLE IF NOT EXISTS event (
                aggregate_id TEXT, 
                occured_on INT, 
                type TEXT, 
                version INT, 
                payload TEXT)";
        command.ExecuteNonQuery();
    }

    public Task AppendAsync(Guid aggregateId, IEvent @event)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<IEvent>> LoadAsync(Guid aggregateId)
    {
        await using var connection = OpenConnection();
        var command = connection.CreateCommand();
        command.CommandText = $@"select type, payload from event where aggregate_id = '{aggregateId}' order by version";
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<IEvent>();
        while (reader.Read())
        {
            var type = reader.GetString(0);
            var payload = reader.GetString(1);

            result.Add(new TextEvent {Info = payload});
        }

        return result;
    }
}