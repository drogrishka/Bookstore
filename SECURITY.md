# Security notes

This repository is self-contained for an assessment/demo and intentionally contains development-only credentials in
`appsettings.Development.json` and `docker-compose.yml`. Never reuse them outside local evaluation.

For a real deployment:

- run with `ASPNETCORE_ENVIRONMENT=Production`;
- use `Database:Provider=SqlServer` with a least-privilege application database login;
- keep the SQL Server `sa` account and passwords in a secret store, not source control;
- provide `Auth__MachineClientSecret` through a secret store and rotate it;
- provide separate signing and encryption PFX certificates via the `Auth:Certificates` configuration section;
- terminate TLS correctly and keep OpenIddict's HTTPS requirement enabled;
- configure trusted reverse proxies/forwarded headers explicitly for the deployment topology;
- keep `Database:InitializeOnStartup=false` and apply reviewed EF Core migrations during deployment;
- replace the legacy OAuth implicit flow with Authorization Code + PKCE when the external contract permits it;
- restrict redirect URIs to exact production origins;
- add rate limiting, monitoring, centralized logging and WAF/reverse-proxy controls appropriate to the deployment;
- do not expose the seeded demo user or development OAuth clients/secrets in a production database.
