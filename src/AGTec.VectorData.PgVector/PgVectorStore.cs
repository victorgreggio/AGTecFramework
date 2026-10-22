using System.Globalization;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace AGTec.VectorData.PgVector;

/// <summary>
/// pgvector-backed <see cref="IVectorStore"/>. Rows carry a <c>partition</c> (tenant/workspace
/// scope) and every query filters on it, preserving per-tenant isolation. Embeddings are written
/// as <c>::vector</c>-cast literals so no extra Npgsql type-mapping package is required.
/// </summary>
public sealed class PgVectorStore(NpgsqlDataSource dataSource, int dimensions) : IVectorStore, IDisposable
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialised;

    public void Dispose() => _initLock.Dispose();

    public async Task UpsertAsync(
        string partition,
        IReadOnlyList<VectorRecord> records,
        CancellationToken cancellationToken = default
    )
    {
        if (records.Count == 0)
        {
            return;
        }

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(connection);

        foreach (var record in records)
        {
            var command = new NpgsqlBatchCommand(
                """
                INSERT INTO agtec_vectors (partition, id, content, metadata, embedding, created_at)
                VALUES (@partition, @id, @content, @metadata::jsonb, @embedding::vector, now())
                ON CONFLICT (partition, id) DO UPDATE SET
                    content = EXCLUDED.content,
                    metadata = EXCLUDED.metadata,
                    embedding = EXCLUDED.embedding
                """
            );
            command.Parameters.AddWithValue("partition", partition);
            command.Parameters.AddWithValue("id", record.Id);
            command.Parameters.AddWithValue("content", record.Content);
            command.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(record.Metadata ?? Empty));
            command.Parameters.AddWithValue("embedding", Format(record.Embedding.Span));
            batch.BatchCommands.Add(command);
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorMatch>> SearchAsync(
        string partition,
        ReadOnlyMemory<float> query,
        int topK,
        IReadOnlyDictionary<string, string>? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        var sql = new StringBuilder(
            """
            SELECT id, content, metadata, 1 - (embedding <=> @query::vector) AS score
            FROM agtec_vectors
            WHERE partition = @partition
            """
        );

        await using var command = new NpgsqlCommand { Connection = connection };
        command.Parameters.AddWithValue("partition", partition);
        command.Parameters.AddWithValue("query", Format(query.Span));
        command.Parameters.AddWithValue("k", topK);

        var i = 0;
        foreach (var entry in filter ?? Empty)
        {
            sql.Append(CultureInfo.InvariantCulture, $" AND metadata->>@k{i} = @v{i}");
            command.Parameters.AddWithValue($"k{i}", entry.Key);
            command.Parameters.AddWithValue($"v{i}", entry.Value);
            i++;
        }

        sql.Append(" ORDER BY embedding <=> @query::vector LIMIT @k");
        command.CommandText = sql.ToString();

        var hits = new List<VectorMatch>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var metadata =
                JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(2))
                ?? new Dictionary<string, string>();
            hits.Add(new VectorMatch(reader.GetString(0), reader.GetString(1), reader.GetDouble(3), metadata));
        }

        return hits;
    }

    public async Task<int> CountAsync(string partition, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM agtec_vectors WHERE partition = @partition",
            connection
        );
        command.Parameters.AddWithValue("partition", partition);
        return (int)(long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_initialised)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialised)
            {
                return;
            }

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                $"""
                CREATE EXTENSION IF NOT EXISTS vector;
                CREATE TABLE IF NOT EXISTS agtec_vectors (
                    partition text NOT NULL,
                    id text NOT NULL,
                    content text NOT NULL,
                    metadata jsonb NOT NULL,
                    embedding vector({dimensions.ToString(CultureInfo.InvariantCulture)}) NOT NULL,
                    created_at timestamptz NOT NULL DEFAULT now(),
                    PRIMARY KEY (partition, id)
                );
                CREATE INDEX IF NOT EXISTS ix_agtec_vectors_partition ON agtec_vectors (partition);
                """,
                connection
            );
            await command.ExecuteNonQueryAsync(cancellationToken);
            _initialised = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    private static string Format(ReadOnlySpan<float> vector)
    {
        var builder = new StringBuilder(vector.Length * 8 + 2);
        builder.Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(vector[i].ToString("R", CultureInfo.InvariantCulture));
        }

        builder.Append(']');
        return builder.ToString();
    }
}
