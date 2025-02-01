
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IssueRepro.Data;

public class DatabaseContextFactory : IDesignTimeDbContextFactory<IssueReproDb>
{
    public IssueReproDb CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IssueReproDb>();
        optionsBuilder.UseNpgsql("Data Source=blog.db");

        return new IssueReproDb(optionsBuilder.Options);
    }
}

