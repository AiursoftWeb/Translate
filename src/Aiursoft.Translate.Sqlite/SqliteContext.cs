using System.Diagnostics.CodeAnalysis;
using Aiursoft.Translate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Translate.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
