const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');

async function run() {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });

    try {
        const origin = 'http://localhost:5191';
        const page = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 1440, height: 1100 } });
        assert.equal((await page.goto(`${origin}/Configuration/Access`)).status(), 200);
        assert.equal(await page.locator('#workspace-navigation a.active').innerText(), 'Access & integrations');
        assert.equal(await page.locator('.access-team-table tbody tr').count(), 4);
        assert.equal(await page.locator('.access-integration-card').count(), 3);
        assert.deepEqual(await page.locator('.access-owner-stats strong').allTextContents(), ['03', '02']);
        await page.screenshot({ path: 'design/configuration/BuildWise-Access.png' });
        await page.locator('#main-content').evaluate(element => element.scrollTop = 600);
        await page.screenshot({ path: 'design/configuration/BuildWise-Integrations.png' });
        await page.getByText('Understand team roles', { exact: true }).click();
        assert(await page.getByRole('heading', { name: 'Estimator', exact: true }).isVisible());

        await page.getByRole('link', { name: 'Add team member', exact: true }).click();
        await page.screenshot({ path: 'design/configuration/BuildWise-Add-Team-Member.png' });
        await page.locator('form.access-form').evaluate(form => form.noValidate = true);
        await page.getByRole('button', { name: 'Add team member', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('required'));
        await page.locator('input[name="Input.Name"]').fill('Chipo Dube');
        await page.locator('input[name="Input.Email"]').fill('RUTH@mavambo.example');
        await page.locator('select[name="Input.Role"]').selectOption('Viewer');
        await page.getByRole('button', { name: 'Add team member', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('already exists'));
        await page.locator('input[name="Input.Email"]').fill('chipo@mavambo.example');
        await page.getByRole('button', { name: 'Add team member', exact: true }).click();
        assert.equal(await page.locator('.access-team-table tbody tr').count(), 5);
        assert((await page.locator('.request-success').innerText()).includes('No invitation email'));
        await page.getByRole('link', { name: 'Edit Chipo Dube', exact: true }).click();
        const memberUrl = page.url();
        const staleMember = await browser.newPage({ javaScriptEnabled: false });
        await staleMember.goto(memberUrl);
        await page.locator('select[name="Input.Role"]').selectOption('Administrator');
        await page.getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await page.locator('.access-team-table tbody tr').filter({ hasText: 'Chipo Dube' }).innerText()).includes('Can approve'));
        await staleMember.locator('input[name="Input.Name"]').fill('Outdated name');
        await staleMember.getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await staleMember.locator('.request-validation').innerText()).includes('another window'));
        await page.goto(memberUrl);
        await page.locator('input[type="checkbox"][name="Input.IsActive"]').uncheck();
        await page.getByRole('button', { name: 'Save changes', exact: true }).click();
        const inactiveRow = page.locator('.access-team-table tbody tr').filter({ hasText: 'Chipo Dube' });
        assert((await inactiveRow.innerText()).includes('Inactive'));
        assert((await inactiveRow.innerText()).includes('No approval access'));
        assert.equal((await page.request.get(`${origin}/Configuration/Access/Member/b17d0000-0000-4000-9000-000000000001`)).status(), 403);
        assert.equal((await page.request.get(`${origin}/Configuration/Access/Member/00000000-0000-0000-0000-000000000000`)).status(), 404);

        await page.getByRole('link', { name: 'Configure AI', exact: true }).click();
        await page.screenshot({ path: 'design/configuration/BuildWise-AI-Preferences.png' });
        await page.locator('form.access-form').evaluate(form => form.noValidate = true);
        await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('model or deployment'));
        await page.locator('select[name="Input.Provider"]').selectOption('Azure OpenAI');
        await page.locator('input[name="Input.Model"]').fill('quotation-assistant');
        await page.locator('input[type="checkbox"][name="Input.EnableWhenConnected"]').check();
        await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
        const aiCard = page.locator('.access-integration-card').filter({ has: page.getByRole('heading', { name: 'AI assistance', exact: true }) });
        assert((await aiCard.innerText()).includes('Preferences saved'));
        assert.equal(await aiCard.locator('.badge').innerText(), 'Not connected');
        await page.getByRole('link', { name: 'Configure AI', exact: true }).click();
        assert.equal(await page.locator('input[name="Input.Model"]').inputValue(), 'quotation-assistant');
        const staleIntegration = await browser.newPage({ javaScriptEnabled: false });
        await staleIntegration.goto(page.url());
        await page.locator('input[name="Input.Model"]').fill('revised-deployment');
        await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
        await staleIntegration.getByRole('button', { name: 'Save preferences', exact: true }).click();
        assert((await staleIntegration.locator('.request-validation').innerText()).includes('another window'));

        await page.getByRole('link', { name: 'Configure email', exact: true }).click();
        await page.screenshot({ path: 'design/configuration/BuildWise-Email-Preferences.png' });
        assert.equal(await page.locator('input[name="Input.SenderName"]').inputValue(), 'Mavambo Projects');
        await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('hostname'));
        await page.locator('input[name="Input.Host"]').fill('smtp.example.com');
        await page.getByRole('button', { name: 'Save preferences', exact: true }).click();
        const emailCard = page.locator('.access-integration-card').filter({ has: page.getByRole('heading', { name: 'Email delivery', exact: true }) });
        assert.equal(await emailCard.locator('.badge').innerText(), 'Not connected');
        assert((await emailCard.innerText()).includes('Preferences saved'));
        assert.equal((await page.request.get(`${origin}/Configuration/Access/Integration/unknown`)).status(), 404);

        const mobile = await browser.newPage({ viewport: { width: 390, height: 844 }, reducedMotion: 'reduce' });
        const errors = [];
        const externalRequests = [];
        mobile.on('pageerror', error => errors.push(error.message));
        mobile.on('request', request => {
            if (!request.url().startsWith(origin)) externalRequests.push(request.url());
        });
        for (const [route, name] of [
            ['/Configuration/Access', 'Access'],
            ['/Configuration/Access/Member', 'Member'],
            ['/Configuration/Access/Integration/ai', 'AI'],
            ['/Configuration/Access/Integration/email', 'Email']
        ]) {
            await mobile.goto(`${origin}${route}`);
            assert.equal(await mobile.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, route);
            await mobile.screenshot({ path: `design/configuration/BuildWise-${name}-Mobile.png`, animations: 'disabled' });
        }
        await mobile.locator('select[name="Input.Provider"]').selectOption('Microsoft 365');
        assert.equal(await mobile.locator('#smtp-settings').isVisible(), false);
        await mobile.locator('select[name="Input.Provider"]').selectOption('SMTP');
        assert(await mobile.locator('#smtp-settings').isVisible());
        await mobile.locator('[data-workspace-menu]').click();
        await mobile.getByRole('link', { name: 'Access & integrations', exact: true }).click();
        assert(new URL(mobile.url()).pathname === '/Configuration/Access');
        await page.setViewportSize({ width: 1440, height: 580 });
        await page.goto(`${origin}/Configuration/Access`);
        await page.locator('#main-content').evaluate(element => element.scrollTop = 250);
        assert(await page.locator('#main-content').evaluate(element => element.scrollTop) > 0);
        assert.equal(await page.locator('#workspace-navigation').evaluate(element => element.scrollTop), 0);
        assert.equal(await page.evaluate(() => window.scrollY), 0);
        assert.deepEqual(errors, []);
        assert(externalRequests.every(url => url.startsWith('https://fonts.googleapis.com/') || url.startsWith('https://fonts.gstatic.com/')));
        console.log('Passed: native access navigation, member validation/add/edit/deactivation, owner guard, concurrent save checks, AI/email preferences without verified-connection claims, mobile layout and independent scrolling.');
    } finally {
        await browser.close();
    }
}

run().catch(error => {
    console.error(error);
    process.exit(1);
});
