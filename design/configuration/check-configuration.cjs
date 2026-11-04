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
        const menu = page.locator('.workspace-config-dropdown');
        assert.equal(await menu.evaluate(element => element.open), false);
        await menu.locator('summary').click();
        assert.deepEqual(await menu.locator('a span').allTextContents(), ['Profile', 'Users', 'User groups', 'Currency']);
        await page.locator('[data-nav-section="settings-currency"]').click();
        await page.waitForURL('**/Configuration/Currency');
        assert.equal(await menu.evaluate(element => element.open), true);
        assert.equal(await page.locator('h1').innerText(), 'Currency');
        assert.equal(await page.locator('[name="Input.CreatorId"]').count(), 1);

        const token = await page.locator('[name="__RequestVerificationToken"]').inputValue();
        const invalid = await page.request.post('http://localhost:5191/Configuration/Currency', {
            form: { __RequestVerificationToken: token, 'Input.Name': '', 'Input.Code': 'XX', 'Input.Symbol': '' }
        });
        assert.equal(invalid.status(), 200);
        assert.match(await invalid.text(), /validation-summary-errors/);
        const badCreator = await page.request.post('http://localhost:5191/Configuration/Currency', {
            form: {
                __RequestVerificationToken: token, 'Input.Name': 'Validation only',
                'Input.Code': 'ZZZ', 'Input.Symbol': 'Z',
                'Input.CreatorId': '11111111-1111-1111-1111-111111111111'
            }
        });
        assert.equal(badCreator.status(), 200);
        assert.match(await badCreator.text(), /Select an existing user/);
        await page.evaluate(() => document.fonts.ready);
        await page.screenshot({ path: path.join(__dirname, 'BuildWise-Currency.png') });

        await page.locator('[data-nav-section="settings-users"]').click();
        await page.waitForURL('**/Configuration/Users');
        assert.equal(await page.locator('h1').innerText(), 'Users');
        assert.equal(await page.locator('[data-nav-section="settings-users"]').getAttribute('aria-current'), 'page');
        await page.locator('[data-nav-section="settings-groups"]').click();
        await page.waitForURL('**/Configuration/UserGroups');
        assert.equal(await page.locator('h1').innerText(), 'User groups');

        await page.locator('[data-nav-section="settings"]').click();
        const edit = page.getByRole('link', { name: 'Edit profile', exact: true });
        await (await edit.count() ? edit : page.getByRole('link', { name: 'Add company profile', exact: true })).click();
        assert.equal(await page.locator('select[name="Input.DefaultCurrency"]').count(), 1);
        assert.equal(await page.getByRole('link', { name: 'Manage currencies' }).getAttribute('href'), '/Configuration/Currency');
        const profileToken = await page.locator('[name="__RequestVerificationToken"]').inputValue();
        const companyId = await page.locator('[name="Input.CompanyId"]').inputValue();
        const invalidProfile = await page.request.post(page.url(), {
            form: { __RequestVerificationToken: profileToken, 'Input.CompanyId': companyId, 'Input.DefaultCurrency': 'INVALID' }
        });
        assert.equal(invalidProfile.status(), 200);
        assert.match(await invalidProfile.text(), /Select a currency from the list/);

        await page.setViewportSize({ width: 390, height: 844 });
        for (const route of ['Currency', 'Users', 'UserGroups', 'Profile/Details']) {
            await page.goto('http://localhost:5191/Configuration/' + route);
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), route);
        }
        await page.goto('http://localhost:5191/Configuration/Currency');
        await page.screenshot({ path: path.join(__dirname, 'BuildWise-Currency-Mobile.png') });
        await page.getByRole('button', { name: 'Open workspace navigation' }).click();
        await page.locator('[data-nav-section="settings-groups"]').click();
        await page.waitForURL('**/Configuration/UserGroups');
        assert.deepEqual(errors, []);
        console.log('Passed: Configuration dropdown, all four links, SQL registers, creator validation, base-currency selection/validation and mobile navigation/layout. No valid save submitted.');
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exitCode = 1; });
