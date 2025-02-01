

Removing these lines from IssueRepro.Web will cause migrations to succeed.
```
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<IssueReproDb>();
```
