const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');

async function run() {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });

    try {
        const origin = 'http://localhost:5191';
        const page = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 1440, height: 1000 } });
        assert.equal((await page.goto(`${origin}/Customers`)).status(), 200);
        assert.equal(await page.locator('.customer-table tbody tr').count(), 8);
        assert.equal(await page.locator('#workspace-navigation a.active').innerText(), 'Customers');
        assert.equal(await page.locator('a[href*="design-two"],a[href*="#/"]').count(), 0);
        await page.screenshot({ path: 'design/customers/BuildWise-Customers.png' });
        await page.getByRole('link', { name: 'Next', exact: true }).click();
        assert.equal(await page.locator('.customer-table tbody tr').count(), 2);
        await page.goto(`${origin}/Customers?Type=Business`);
        assert.equal(await page.locator('.customer-table tbody tr').count(), 3);
        await page.getByRole('searchbox', { name: 'Search customers', exact: true }).fill('Pinewood');
        await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
        assert.equal(await page.locator('.customer-table tbody tr').count(), 1);
        await page.getByRole('link', { name: 'Pinewood Guesthouse Business', exact: true }).click();
        assert.equal(await page.locator('.customer-history-table tbody tr').count(), 2);
        assert((await page.locator('.customer-work-summary').innerText()).includes('$2,240.00'));
        await page.screenshot({ path: 'design/customers/BuildWise-Customer-Profile.png' });
        await page.getByRole('link', { name: 'Quotations 2', exact: true }).click();
        assert.equal(await page.locator('.customer-history-table tbody tr').count(), 2);
        await page.getByRole('link', { name: 'BW-2026-0125 Revision 2', exact: true }).click();
        assert(page.url().endsWith('/Quotations/Details/125'));
        await page.goto(`${origin}/Customers?Search=nomatch`);
        assert(await page.getByRole('heading', { name: 'No matching customers' }).isVisible());
        await page.goto(`${origin}/Customers?Type=bad&Sort=bad&PageNumber=-1`);
        assert.equal(await page.locator('.customer-table tbody tr').count(), 8);
        await page.getByRole('link', { name: 'Add customer', exact: true }).click();
        await page.screenshot({ path: 'design/customers/BuildWise-Add-Customer.png' });
        await page.locator('form').evaluate(form => form.noValidate = true);
        await page.getByRole('button', { name: 'Add customer', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('required'));
        await page.locator('input[name="Input.Name"]').fill('Chipo Dube');
        await page.locator('input[name="Input.Email"]').fill('chipo@example.com');
        await page.locator('input[name="Input.Phone"]').fill('+263 77 234 5678');
        await page.locator('input[name="Input.Address"]').fill('Avondale, Harare');
        await page.locator('textarea[name="Input.Notes"]').fill('Prefers morning appointments.');
        await page.getByRole('button', { name: 'Add customer', exact: true }).click();
        assert(await page.getByRole('heading', { name: 'Chipo Dube', exact: true }).isVisible());
        assert(await page.getByRole('heading', { name: 'No requests yet', exact: true }).isVisible());
        await page.locator('.customer-actions').getByRole('link', { name: 'New request', exact: true }).click();
        assert.equal(await page.locator('input[name="Input.Customer"]').inputValue(), 'Chipo Dube');
        assert.equal(await page.locator('input[name="Input.Email"]').inputValue(), 'chipo@example.com');
        await page.locator('input[name="Input.Title"]').fill('Bathroom tap replacement');
        await page.locator('select[name="Input.Category"]').selectOption('Plumbing');
        await page.locator('textarea[name="Input.Description"]').fill('Replace the leaking bathroom tap and check the existing supply pipe.');
        await page.getByRole('button', { name: 'Create request', exact: true }).click();
        await page.goto(`${origin}/Customers/Details/11`);
        assert.equal(await page.locator('.customer-history-table tbody tr').count(), 1);
        await page.getByRole('link', { name: 'Edit customer', exact: true }).click();
        await page.locator('input[name="Input.Name"]').fill('Tendai Moyo');
        await page.getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('already exists'));
        await page.locator('input[name="Input.Name"]').fill('Chipo M. Dube');
        await page.getByRole('button', { name: 'Save changes', exact: true }).click();
        assert(await page.getByRole('heading', { name: 'Chipo M. Dube', exact: true }).isVisible());
        await page.getByRole('link', { name: 'Bathroom tap replacement REQ-1043', exact: false }).first().click();
        assert((await page.locator('.request-detail-grid').innerText()).includes('Chipo M. Dube'));
        await page.goto(`${origin}/`);
        assert.equal(await page.locator('.v2-metric-value b').nth(3).innerText(), '11');
        assert.equal((await page.request.get(`${origin}/Customers/Details/999999`)).status(), 404);

        const mobile = await browser.newPage({ viewport: { width: 390, height: 844 } });
        const errors = [];
        mobile.on('pageerror', error => errors.push(error.message));
        for (const [route, name] of [
            ['/Customers', 'Directory'],
            ['/Customers/Details/9', 'Profile'],
            ['/Customers/Edit/9', 'Edit']
        ]) {
            await mobile.goto(`${origin}${route}`);
            assert.equal(await mobile.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, route);
            await mobile.screenshot({ path: `design/customers/BuildWise-Customer-${name}-Mobile.png` });
        }
        await mobile.locator('[data-workspace-menu]').click();
        await mobile.getByRole('link', { name: 'Customers', exact: true }).click();
        assert(new URL(mobile.url()).pathname === '/Customers');
        await page.setViewportSize({ width: 1440, height: 580 });
        await page.goto(`${origin}/Customers`);
        await page.locator('#main-content').evaluate(element => element.scrollTop = 200);
        assert(await page.locator('#main-content').evaluate(element => element.scrollTop) > 0);
        assert.equal(await page.locator('#workspace-navigation').evaluate(element => element.scrollTop), 0);
        assert.equal(await page.evaluate(() => window.scrollY), 0);
        assert.deepEqual(errors, []);
        console.log('Passed: directory filters/search/pagination, customer creation/validation, linked requests/quotations, duplicate protection, rename continuity, dashboard totals, mobile layout and independent scrolling.');
    } finally {
        await browser.close();
    }
}

run().catch(error => {
    console.error(error);
    process.exit(1);
});
