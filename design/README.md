# BuildWise screen designs

The existing ASP.NET Razor Pages app now contains a responsive clickable screen prototype. Start it with `dotnet run --launch-profile http` and open the printed local URL. The current review preview uses http://localhost:5180.

Open `/#/brand` for the logo, colour system, typography and a directory of all 20 primary screen designs. The overview dashboard is the default screen.

Included screens: overview; requests; new request; request details; clarification; scope review; service catalogue; service editor; new service; customers; customer profile; new customer; quotations; quotation details; quotation editor; quotation preview and approval; revision history; business settings; integrations and access; sign in. A workflow guide is also included.

Logo assets are editable SVG files in `wwwroot/brand`. PNG screen captures are in `design/screens`, with selected mobile layouts and a screen contact sheet alongside this file.

The interface uses realistic records without demo labels. Records and edits are saved in this browser's local storage. Authentication, AI, server storage and email delivery are not implemented. The print action uses the browser print dialog, which supports Save as PDF. No external email is sent. All monetary values are illustrative provider-entered allowances. Currency and tax rules need agreement before backend development.

Design choices: provider-only request capture, one provider workspace, owner approval, USD examples and no tax applied. Customer accounts, online acceptance, payments, scheduling and execution tracking remain outside the first-release screen scope.

Validation: .NET build, JavaScript syntax, rendering all primary routes, request search, scope confirmation, quotation approval, retained revisions and narrow-screen overflow. `verify.cjs` captures screenshots using the bundled Playwright and installed Edge. It operates in an isolated browser context.
