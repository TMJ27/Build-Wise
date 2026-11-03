const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');

async function run() {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });

    try {
        const origin = 'http://localhost:5191';
        const page = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 1440, height: 1000 } });
        assert.equal((await page.goto(`${origin}/Services`)).status(), 200);
        assert.equal(await page.locator('.catalogue-card').count(), 6);
        assert.equal(await page.locator('#workspace-navigation a.active').innerText(), 'Service catalogue');
        await page.screenshot({ path: 'design/catalogue/BuildWise-Catalogue.png' });
        await page.getByRole('link', { name: 'Archived 1', exact: true }).click();
        assert.equal(await page.locator('.catalogue-card').count(), 1);
        await page.goto(`${origin}/Services?Category=Electrical`);
        assert.equal(await page.locator('.catalogue-card').count(), 2);
        await page.getByRole('searchbox', { name: 'Search services', exact: true }).fill('ELE-001');
        await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
        assert.equal(await page.locator('.catalogue-card').count(), 1);
        await page.getByRole('link', { name: 'Electrical installation', exact: true }).click();
        const electricalUrl = page.url();
        const electricalId = electricalUrl.split('/').pop();
        assert.equal(await page.locator('.catalogue-numbered-list').first().locator('li').count(), 3);
        assert(await page.getByRole('link', { name: 'Electrical site assessment ELE-002', exact: false }).isVisible());
        await page.screenshot({ path: 'design/catalogue/BuildWise-Service-Details.png' });
        await page.getByRole('link', { name: 'Start request', exact: true }).click();
        assert.equal(await page.locator('input[name="Input.Title"]').inputValue(), 'Electrical installation');
        assert.equal(await page.locator('select[name="Input.Category"]').inputValue(), 'Electrical');
        await page.goto(`${origin}/Services?Search=nomatch`);
        assert(await page.getByRole('heading', { name: 'No matching services' }).isVisible());
        await page.goto(`${origin}/Services?Status=invalid&Category=invalid`);
        assert.equal(await page.locator('.catalogue-card').count(), 6);

        await page.getByRole('link', { name: 'Add service', exact: true }).click();
        await page.screenshot({ path: 'design/catalogue/BuildWise-Add-Service.png' });
        await page.locator('form.catalogue-editor').evaluate(form => form.noValidate = true);
        await page.getByRole('button', { name: 'Add service', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('required'));
        await page.locator('input[name="Input.Code"]').fill('ELE-001');
        await page.locator('input[name="Input.Name"]').fill('Water tank maintenance');
        await page.locator('select[name="Input.Category"]').selectOption('Plumbing');
        await page.locator('select[name="Input.Unit"]').selectOption('Project');
        await page.locator('textarea[name="Input.Description"]').fill('Inspection and routine maintenance of an accessible water storage tank.');
        await page.locator('textarea[name="Input.Inclusions"]').fill('Accessible tank inspection\nAgreed maintenance tasks');
        await page.locator('textarea[name="Input.Exclusions"]').fill('Replacement tank supply');
        await page.locator('textarea[name="Input.Questions"]').fill('What is the tank capacity?\nIs safe access available?');
        await page.locator('textarea[name="Input.Stages"]').fill('Inspection\nMaintenance\nFinal checks');
        await page.locator('input[type="checkbox"][name="Input.RequiresSiteAssessment"]').check();
        await page.getByRole('button', { name: 'Add service', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('already exists'));
        await page.locator('input[name="Input.Code"]').fill('PLM-002');
        await page.getByRole('button', { name: 'Add service', exact: true }).click();
        assert(await page.getByRole('heading', { name: 'Water tank maintenance', exact: true }).isVisible());
        const newServiceUrl = page.url();
        const newServiceId = newServiceUrl.split('/').pop();
        assert.match(newServiceId, /^[0-9a-f-]{36}$/);
        assert(await page.locator('.request-success').isVisible());
        await page.getByRole('link', { name: 'Edit service', exact: true }).click();
        await page.locator('textarea[name="Input.Questions"]').fill('What is the tank capacity?\nIs safe access available?\nWhen was it last serviced?');
        await page.getByRole('button', { name: 'Save changes', exact: true }).click();
        assert.equal(await page.locator('.catalogue-numbered-list').first().locator('li').count(), 3);
        await page.getByRole('button', { name: 'Archive service', exact: true }).click();
        assert.equal(await page.getByRole('link', { name: 'Start request', exact: true }).count(), 0);
        await page.goto(`${origin}/Services`);
        assert.equal(await page.locator('.catalogue-card').count(), 6);
        await page.goto(newServiceUrl);
        await page.getByRole('button', { name: 'Activate service', exact: true }).click();
        assert(await page.getByRole('link', { name: 'Start request', exact: true }).isVisible());
        await page.goto(`${origin}/`);
        assert((await page.locator('.v2-metric').last().innerText()).includes('7 active catalogue services'));

        await page.goto(`${origin}/Services/Details/b17d0000-0000-4000-8000-000000000005`);
        await page.getByRole('link', { name: 'Edit service', exact: true }).click();
        await page.locator(`input[name="Input.DependencyIds"][value="${electricalId}"]`).check();
        await page.getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('circular'));
        assert.equal((await page.request.get(`${origin}/Services/Details/00000000-0000-0000-0000-000000000000`)).status(), 404);

        const mobile = await browser.newPage({ viewport: { width: 390, height: 844 } });
        const errors = [];
        mobile.on('pageerror', error => errors.push(error.message));
        for (const [route, name] of [
            ['/Services', 'Catalogue'],
            [`/Services/Details/${electricalId}`, 'Details'],
            [`/Services/Edit/${newServiceId}`, 'Editor']
        ]) {
            await mobile.goto(`${origin}${route}`);
            assert.equal(await mobile.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, route);
            await mobile.screenshot({ path: `design/catalogue/BuildWise-Service-${name}-Mobile.png` });
        }
        await mobile.locator('[data-workspace-menu]').click();
        await mobile.getByRole('link', { name: 'Service catalogue', exact: true }).click();
        assert(new URL(mobile.url()).pathname === '/Services');
        await page.setViewportSize({ width: 1440, height: 580 });
        await page.goto(`${origin}/Services`);
        await page.locator('#main-content').evaluate(element => element.scrollTop = 230);
        assert(await page.locator('#main-content').evaluate(element => element.scrollTop) > 0);
        assert.equal(await page.locator('#workspace-navigation').evaluate(element => element.scrollTop), 0);
        assert.equal(await page.evaluate(() => window.scrollY), 0);
        assert.deepEqual(errors, []);
        console.log('Passed: native catalogue navigation/filtering, service details, request prefill, add/edit/validation, archive/activate, circular dependency protection, live dashboard counts, responsive layout and independent scrolling.');
    } finally {
        await browser.close();
    }
}

run().catch(error => {
    console.error(error);
    process.exit(1);
});
