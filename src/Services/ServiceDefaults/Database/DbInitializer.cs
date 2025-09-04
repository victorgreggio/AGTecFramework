using Microsoft.EntityFrameworkCore;

namespace AGTec.Services.ServiceDefaults.Database;

public static class DbInitializer
{
    public static void Initialize(DbContext dbContext)
    {
        dbContext.Database.Migrate();
    }
}
