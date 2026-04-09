using System.Diagnostics.CodeAnalysis;
using Aiursoft.Translate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.Translate.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : TranslateDbContext(options);
