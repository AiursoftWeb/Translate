using Aiursoft.Translate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Translate.InMemory;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : TranslateDbContext(options)
{
    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.EnsureCreatedAsync(cancellationToken);
    }

    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
