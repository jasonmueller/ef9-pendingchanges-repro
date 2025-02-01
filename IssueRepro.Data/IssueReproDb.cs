
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IssueRepro.Data;

public class IssueReproDb(DbContextOptions<IssueReproDb> options) : IdentityDbContext(options)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
        base.OnModelCreating(builder);
	}
}

