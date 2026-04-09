using Aiursoft.DbTools;
using Aiursoft.DbTools.InMemory;
using Aiursoft.Translate.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.Translate.InMemory;

public class InMemorySupportedDb : SupportedDatabaseType<TranslateDbContext>
{
    public override string DbType => "InMemory";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurInMemoryDb<InMemoryContext>();
    }

    public override TranslateDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InMemoryContext>();
    }
}
