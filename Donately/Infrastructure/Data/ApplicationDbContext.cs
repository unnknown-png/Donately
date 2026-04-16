using Microsoft.EntityFrameworkCore;

namespace Donately.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Додаси DbSet<T> для сутностей, коли почнеш створювати моделі.
}

