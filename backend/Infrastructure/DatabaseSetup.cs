using MongoDB.Bson;
using MongoDB.Driver;

namespace ContentManager.Api.Infrastructure;

public static class DatabaseSetup
{
    public static async Task InitializeAsync(IMongoDatabase database)
    {
        var indexes = new Dictionary<string, CreateIndexModel<BsonDocument>[]>
        {
            ["products"] = [new(Builders<BsonDocument>.IndexKeys.Ascending("slug"),
                new CreateIndexOptions { Name = "unique_slug", Unique = true })],
            ["slugReservations"] = [new(Builders<BsonDocument>.IndexKeys.Ascending("slug"),
                new CreateIndexOptions { Name = "unique_slug", Unique = true })],
            ["admins"] = [new(Builders<BsonDocument>.IndexKeys.Ascending("login"),
                new CreateIndexOptions { Name = "unique_login", Unique = true })],
            ["sessions"] =
            [
                new(Builders<BsonDocument>.IndexKeys.Ascending("tokenHash"),
                    new CreateIndexOptions { Name = "unique_token_hash", Unique = true }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
                    new CreateIndexOptions { Name = "expires_cleanup", ExpireAfter = TimeSpan.Zero })
            ],
            ["orders"] =
            [
                new(Builders<BsonDocument>.IndexKeys.Ascending("requestKey"),
                    new CreateIndexOptions { Name = "unique_request_key", Unique = true }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("payment.accountId").Ascending("payment.referenceHash"),
                    new CreateIndexOptions<BsonDocument>
                    {
                        Name = "unique_payment_reference",
                        Unique = true,
                        PartialFilterExpression = new BsonDocument("payment.referenceHash",
                            new BsonDocument("$type", "string"))
                    })
            ]
        };
        foreach (var (collection, models) in indexes)
            await database.GetCollection<BsonDocument>(collection).Indexes.CreateManyAsync(models);
    }
}
