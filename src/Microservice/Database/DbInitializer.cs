using Microsoft.EntityFrameworkCore;

namespace AGTec.Microservice.Database;

public static class DbInitializer
{
    public static void Initialize(DbContext dbContext)
    {
        dbContext.Database.Migrate();
    }
}