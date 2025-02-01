using IssueRepro.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<IssueReproDb>("issues");
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<IssueReproDb>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await SetupDatabaseAsync(scope.ServiceProvider.GetRequiredService<IssueReproDb>(), CancellationToken.None);
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

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
