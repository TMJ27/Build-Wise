# Production verification — 13 November 2026

URL: https://bu-ed357d6290684197aa19d3e4419a88d1.ecs.eu-north-1.on.aws

- Release image: `release-20261113-patched`.
- Image digest: `sha256:b5225ae3123b22073a2b5ef151fc37a09108ed7c3f9500fcc08428da99f0d6f1`.
- AWS build: `buildwise-release:c6ec6cc3-f0ab-4409-a059-67226ab06087` — succeeded.
- Application task definition: `buildwise-production:4`.
- Final ECS deployment: `SUCCESSFUL`, 100% production traffic, one running task and zero pending tasks.
- Migration task definition: `buildwise-migrations:3`; task `9ab76b58cd544e52ade0ed0789297351` exited with code 0.
- All 11 database migrations applied to production RDS `BuildWise`.
- Restricted `buildwise_app` login verified: no database-owner or server-administrator membership.
- Public HTTPS `/health`: 200, database connection successful.
- HSTS header: `max-age=2592000`.
- Patched ECR basic image scan: complete, no findings. The base-image Perl package was updated to address [CVE-2026-82560](https://ubuntu.com/security/CVE-2026-82560).
- Load balancer reports the application target healthy.
- Container local timezone verified as `CAT +0200`.
- Browser verification: landing page, staff sign-in, customer sign-in and customer password recovery form render correctly. Staff sign-in reaches the production Cognito login form with the HTTPS callback; Cognito password recovery form also renders.
- Production Cognito login branding enabled for its separate client. Existing `mada` account is confirmed and enabled; first administrator setup is restricted to its email, `tavomada27@gmail.com`.
- Temporary deployment-machine SQL network rule removed; application access is through its AWS security group. The pre-existing SQL network rules were preserved.
- Local development configuration and local database were not changed by deployment.

Production has its own fresh database. Local business records were not imported. Sign in as the existing staff user and complete company, catalogue and email configuration. SES is still in sandbox, so verified recipients are required. No production customer account was created, no password reset was submitted and no email was sent during these deployment checks. Full authenticated business-flow verification requires the user's sign-in and company setup.
