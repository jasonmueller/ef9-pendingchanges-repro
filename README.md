

Removing these lines from IssueRepro.Web will cause migrations to succeed.
```
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<IssueReproDb>();
```

## Step by step build up

Below are the be basic steps to build up the solution in this repo. 
Some of the file edits might not be called out below, but those that
aren't are basically just the Program.cs files for the AppHost and
the Web project, so it's not too difficult to see what was added there
(basically just enough to get it to run and connect everything up).

### Create the solution and projects (basic Aspire build)

Create Web and Data projects and the Aspire AppHost and ServiceDefaults
and then add necessary references between them.

```
APP_NAME="IssueRepro"
mkdir $APP_NAME && cd $APP_NAME && git init

# Create appliction projects
dotnet new sln -n $APP_NAME
dotnet new webapp -n $APP_NAME.Web
dotnet sln add $APP_NAME.Web

dotnet new classlib -n $APP_NAME.Data
dotnet sln add $APP_NAME.Data

dotnet add $APP_NAME.Web reference $APP_NAME.Data

# Add Aspire
dotnet new aspire-apphost -o $APP_NAME.AppHost
dotnet sln add $APP_NAME.AppHost
dotnet add $APP_NAME.AppHost reference $APP_NAME.Web

dotnet new aspire-servicedefaults -o $APP_NAME.ServiceDefaults
dotnet sln add $APP_NAME.ServiceDefaults
dotnet add $APP_NAME.Web reference $APP_NAME.ServiceDefaults
```

Add the Web project to the AppHost and wire it up with service defaults.

AppHost/Program.cs

```
builder.AddProject<Projects.IssueRepro_Web>();
```

Web/Program.cs

```
builder.AddServiceDefaults();
```

### Plug in Identity using PostgreSQL

Add the necessary PostgreSQL packages

```
dotnet add $APP_NAME.AppHost package Aspire.Hosting.Azure.PostgreSQL
dotnet add $APP_NAME.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add $APP_NAME.Web package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
```

Add identity and our context

```
dotnet add $APP_NAME.Data package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

```
echo "
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace $APP_NAME.Data;

public class ${APP_NAME}Db(DbContextOptions<${APP_NAME}Db> options) : IdentityDbContext(options)
{
	protected override void OnModelCreating(ModelBuilder builder)
	{
        base.OnModelCreating(builder);
	}
}
" > $APP_NAME.Data/${APP_NAME}Db.cs
```

Add PostgreSQL to our stack

AppHost/Program.cs

```
var db = builder.AddPostgres("postgres").WithDataVolume("issuedata").AddDatabase("issues");

// Change the following line to add the db references
builder.AddProject<Projects.IssueRepro_Web>("web").WithReference(db).WaitFor(db);
```

Web/Program.cs

```
builder.AddNpgsqlDbContext<IssueReproDb>("issues");
```

### Ready migrations

```
dotnet add $APP_NAME.Data package Microsoft.EntityFrameworkCore.Design
```

Create our DbContext factory

```
echo "
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace $APP_NAME.Data;

public class DatabaseContextFactory : IDesignTimeDbContextFactory<${APP_NAME}Db>
{
    public ${APP_NAME}Db CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Database>();
        optionsBuilder.UseNpgsql(\"Data Source=blog.db\");

        return new ${APP_NAME}Db(optionsBuilder.Options);
    }
}
" > $APP_NAME.Data/DatabaseContextFactory.cs
```

Install the tooling

```
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

Create our migration

```
dotnet ef migrations add CreateIdentitySchema --context "${APP_NAME}Db" --project $APP_NAME.Data
```

### Apply migrations on web startup

I wouldn't normally do this, but it's the fastest path to forcing migrations.

Web/Program.cs

```
// Before app.Run...
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await SetupDatabaseAsync(scope.ServiceProvider.GetRequiredService<IssueReproDb>(), CancellationToken.None);
}

// At end of file...

static async Task SetupDatabaseAsync(IssueReproDb dbContext, CancellationToken cancellationToken)
{
    await EnsureDatabaseAsync(dbContext, cancellationToken);
    await RunMigrationAsync(dbContext, cancellationToken);
}

static async Task EnsureDatabaseAsync(IssueReproDb dbContext, CancellationToken cancellationToken)
{
    var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();
    var strategy = dbContext.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
        // Create the database if it does not exist.
        if (!await dbCreator.ExistsAsync(cancellationToken))
        {
            await dbCreator.CreateAsync(cancellationToken);
        }
    });

    return;
}

static async Task RunMigrationAsync(IssueReproDb dbContext, CancellationToken cancellationToken)
{
    var strategy = dbContext.Database.CreateExecutionStrategy();
    
    await strategy.ExecuteAsync(async () =>
        await dbContext.Database.MigrateAsync(cancellationToken));
}
```

### Start it up

```
dotnet run --project IssueRepro.AppHost
```

Migrations should run successfully.  However, we need to plug the ASP.NET Identity stuff in
so we can use it and get the UI.  So, we add the following lines...

Web/Program.cs

```
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<IssueReproDb>();
```

Boom! Pending changes error will fire every time we run the app and migrations will not apply (even if you
delete the data volume and start fresh).