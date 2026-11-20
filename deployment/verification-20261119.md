# Landing banner release verification — 19 November 2026

- Live URL: https://bu-ed357d6290684197aa19d3e4419a88d1.ecs.eu-north-1.on.aws/
- Public `/health` returned HTTP 200 and `Healthy`.
- Reloaded the public landing page and verified the banner fully loaded at its original 1734 × 907 dimensions.
- No horizontal overflow in the verified desktop viewport.
- Browser screenshot: `obj/buildwise-live-banner.jpg`.

Release submitted on 18 November:

- Image: `release-20261118-landing-banner-patched`.
- Digest: `sha256:c02f3eb83e1ba9c5629155dafead08bb18c33db4ca063428c9d8a0e37b2f2cc9`.
- CodeBuild `buildwise-release:602e83f5-c4ad-42d0-8ed2-cdaec2507c37` succeeded.
- ECR image scan completed with no findings after updating the Ubuntu runtime packages.
- Migration task `356a986ca12e42c6aa1ac7982c4d90cb` (`buildwise-migrations:5`) exited with code 0.
- Application definition: `buildwise-production:6`.
- ECS deployment `yz1PKjmI0BI7lQMJAZWQW` was observed at 100% production traffic on 18 November.

The AWS administrator session expired before the follow-up management API check on 19 November, so its final ECS lifecycle status was not reread. The deployed banner and public database health were independently verified live. No new release or infrastructure changes were needed for these follow-up checks.
