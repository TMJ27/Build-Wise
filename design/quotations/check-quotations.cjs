const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');

async function run() {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });

    try {
        const page = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 1440, height: 1000 } });
        const origin = 'http://localhost:5191';

        assert.equal((await page.goto(`${origin}/Quotations`)).status(), 200);
        assert.equal(await page.locator('.quotation-table tbody tr').count(), 7);
        assert.equal(await page.locator('#workspace-navigation a.active').innerText(), 'Quotations');
        assert.equal(await page.locator('a[href*="design-two"],a[href*="#/"],script[src*="design-two"]').count(), 0);
        await page.screenshot({ path: 'design/quotations/BuildWise-Quotations.png' });

        await page.getByRole('link', { name: 'Drafts 2', exact: true }).click();
        assert.equal(await page.locator('.quotation-table tbody tr').count(), 2);
        await page.goto(`${origin}/Quotations?Search=Pinewood`);
        assert.equal(await page.locator('.quotation-table tbody tr').count(), 2);
        await page.goto(`${origin}/Quotations?Search=nomatch`);
        assert(await page.getByRole('heading', { name: 'No matching quotations' }).isVisible());
        await page.goto(`${origin}/Quotations?Status=invalid&Sort=invalid&PageNumber=-1`);
        assert.equal(await page.locator('.quotation-table tbody tr').count(), 7);

        await page.getByRole('link', { name: 'Create quotation', exact: true }).click();
        await page.locator('select[name="Input.RequestId"]').selectOption('1036');
        await page.locator('input[name="Input.Lines[0].Description"]').fill('Exterior wall preparation');
        await page.locator('input[name="Input.Lines[0].Quantity"]').fill('2');
        await page.locator('input[name="Input.Lines[0].Rate"]').fill('125.50');
        await page.getByRole('button', { name: 'Add item', exact: true }).click();
        assert.equal(await page.locator('input[name="Input.Lines[0].Description"]').inputValue(), 'Exterior wall preparation');
        await page.locator('input[name="Input.Lines[1].Description"]').fill('Weatherproof paint');
        await page.locator('input[name="Input.Lines[1].Quantity"]').fill('3');
        await page.locator('input[name="Input.Lines[1].Rate"]').fill('80');
        await page.screenshot({ path: 'design/quotations/BuildWise-Quotation-Editor.png' });
        await page.getByRole('button', { name: 'Save draft', exact: true }).click();
        assert(page.url().includes('/Quotations/Details/129'));
        assert((await page.locator('.quotation-total').innerText()).includes('$491.00'));
        await page.screenshot({ path: 'design/quotations/BuildWise-Quotation-Details.png' });

        await page.getByRole('link', { name: 'Edit draft', exact: true }).click();
        await page.getByRole('button', { name: 'Remove item 2', exact: true }).click();
        await page.getByRole('button', { name: 'Save draft', exact: true }).click();
        assert((await page.locator('.quotation-total').innerText()).includes('$251.00'));
        assert((await page.locator('.requests-heading').innerText()).includes('REVISION 2'));

        await page.locator('.quotation-approval').evaluate(form => form.noValidate = true);
        await page.getByRole('button', { name: 'Approve quotation', exact: true }).click();
        assert((await page.locator('.request-validation').innerText()).includes('Confirm'));
        await page.locator('input[name="confirm"]').check();
        await page.getByRole('button', { name: 'Approve quotation', exact: true }).click();
        assert.equal(await page.getByRole('link', { name: 'Edit draft', exact: true }).count(), 0);
        assert(await page.locator('.request-success').isVisible());
        await page.goto(`${origin}/Quotations/Edit/129`);
        assert(page.url().endsWith('/Quotations/Details/129'));
        await page.getByRole('link', { name: 'Preview quotation', exact: true }).click();
        assert((await page.locator('.quotation-document').innerText()).includes('$251.00'));
        await page.screenshot({ path: 'design/quotations/BuildWise-Quotation-Preview.png' });
        await page.emulateMedia({ media: 'print' });
        assert.equal(await page.locator('.v2-header').isVisible(), false);
        assert.equal(await page.locator('.quotation-preview-toolbar').isVisible(), false);
        assert(await page.locator('.quotation-document').isVisible());
        await page.emulateMedia({ media: 'screen' });

        await page.goto(`${origin}/`);
        assert.equal(await page.locator('.v2-metric-value b').nth(2).innerText(), '06');
        await page.goto(`${origin}/Requests/Details/1036`);
        assert((await page.locator('.requests-heading').innerText()).includes('Quotation approved'));
        assert(await page.getByRole('link', { name: 'View quotation BW-2026-0129' }).isVisible());
        assert.equal((await page.request.get(`${origin}/Quotations/Details/999999`)).status(), 404);

        const mobile = await browser.newPage({ viewport: { width: 390, height: 844 } });
        const errors = [];
        mobile.on('pageerror', error => errors.push(error.message));

        for (const [route, screenshot] of [
            ['/Quotations', 'Register'],
            ['/Quotations/Details/127', 'Details'],
            ['/Quotations/Edit/127', 'Editor'],
            ['/Quotations/Preview/128', 'Preview']
        ]) {
            await mobile.goto(`${origin}${route}`);
            assert.equal(await mobile.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false, route);
            await mobile.screenshot({ path: `design/quotations/BuildWise-Quotation-${screenshot}-Mobile.png` });
        }

        await mobile.locator('[data-workspace-menu]').click();
        await mobile.getByRole('link', { name: 'Quotations', exact: true }).click();
        assert(new URL(mobile.url()).pathname === '/Quotations');

        assert.deepEqual(errors, []);
        console.log('Passed: native register/filtering, create/edit/add/remove items, calculated totals, approval confirmation, approved edit lock, preview/print styling, shared dashboard/request state, mobile layout and navigation.');
    } finally {
        await browser.close();
    }
}

run().catch(error => {
    console.error(error);
    process.exit(1);
});

