const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const path = require('path');
const assert = require('assert/strict');

(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        const page = await browser.newPage({ viewport: { width: 1440, height: 1050 }, reducedMotion: 'reduce' });
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto('http://localhost:5191/');
        await page.locator('.workspace-config-dropdown > summary').click();
        await page.locator('[data-nav-section="settings"]').click();
        await page.waitForURL('**/Configuration/Profile/Details');
        assert.equal(await page.locator('h1').innerText(), 'Company profile');
        assert.equal(await page.locator('[data-nav-section="settings"]').getAttribute('aria-current'), 'page');
        const detailsUrl = page.url();
        await page.evaluate(() => document.fonts.ready);
        await page.screenshot({ path: path.join(__dirname, 'BuildWise-Profile-Details.png') });

        const edit = page.getByRole('link', { name: 'Edit profile', exact: true });
        if (await edit.count()) {
            await edit.click();
            await page.waitForURL('**/Configuration/Profile/Edit/**');
            const companyId = await page.locator('[name="Input.CompanyId"]').inputValue();
            assert.notEqual(companyId, '00000000-0000-0000-0000-000000000000');
            assert.ok((await page.locator('[name="Input.Name"]').inputValue()).length);
            await page.getByRole('link', { name: 'Cancel', exact: true }).click();
            await page.waitForURL('**/Configuration/Profile/Details/**');
        } else {
            await page.getByRole('link', { name: 'Add company profile', exact: true }).click();
            await page.waitForURL('**/Configuration/Profile/Edit');
            assert.equal(await page.locator('[name="Input.CompanyId"]').inputValue(), '00000000-0000-0000-0000-000000000000');
            await page.getByRole('link', { name: 'Cancel', exact: true }).click();
            await page.waitForURL('**/Configuration/Profile/Details');
        }

        await page.setViewportSize({ width: 390, height: 844 });
        await page.goto(detailsUrl);
        await page.evaluate(() => document.fonts.ready);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth));
        await page.screenshot({ path: path.join(__dirname, 'BuildWise-Profile-Details-Mobile.png') });
        const missing = await page.request.get('http://localhost:5191/Configuration/Profile/Details/11111111-1111-1111-1111-111111111111');
        assert.equal(missing.status(), 404);
        assert.deepEqual(errors, []);
        console.log('Passed: sidebar opens details, database-backed edit/add links and Cancel work, unknown ID returns 404, mobile layout fits. No valid save submitted.');
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
