const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');

async function run() {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });

    try {
        const origin = 'http://localhost:5191';
        const page = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 1440, height: 1000 } });
        assert.equal((await page.goto(`${origin}/Configuration/Profile`)).status(), 200);
        assert.equal(await page.locator('#workspace-navigation a.active').innerText(), 'Business profile');
        assert.equal(await page.locator('input[name="Input.Name"]').inputValue(), 'Mavambo Projects');
        assert.equal(await page.locator('.configuration-document-preview h3').innerText(), 'Mavambo Projects');
        await page.screenshot({ path: 'design/configuration/BuildWise-Business-Profile.png' });

        const stale = await browser.newPage({ javaScriptEnabled: false });
        await stale.goto(`${origin}/Configuration/Profile`);
        await page.locator('#business-profile-form').evaluate(form => form.noValidate = true);
        await page.locator('input[name="Input.Name"]').fill('');
        await page.locator('#business-profile-form').getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await page.locator('.configuration-errors').innerText()).includes('required'));
        await page.locator('input[name="Input.Name"]').fill('Mavambo Contracting');
        await page.locator('input[name="Input.DefaultValidityDays"]').fill('0');
        await page.locator('#business-profile-form').evaluate(form => form.noValidate = true);
        await page.locator('#business-profile-form').getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await page.locator('.configuration-errors').innerText()).includes('365'));
        await page.locator('input[name="Input.DefaultValidityDays"]').fill('30');
        await page.locator('input[name="Input.Email"]').fill('accounts@mavambo.example');
        await page.locator('input[name="Input.Phone"]').fill('+263 77 123 9999');
        await page.locator('textarea[name="Input.Address"]').fill('Borrowdale, Harare');
        await page.locator('input[name="Input.RegistrationNumber"]').fill('REG-2026-002');
        await page.locator('input[name="Input.TaxNumber"]').fill('TAX-12345');
        await page.locator('textarea[name="Input.DefaultTerms"]').fill('Valid for 30 days. Confirm access before the work starts.');
        await page.locator('textarea[name="Input.DefaultExclusions"]').fill('Additional work outside the agreed scope is quoted separately.');
        await page.locator('input[type="file"]').setInputFiles({ name: 'invalid.png', mimeType: 'image/png', buffer: Buffer.from('This is not an image') });
        await page.locator('#business-profile-form').getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await page.locator('.configuration-errors').innerText()).includes('PNG or JPEG'));

        const image = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aE7kAAAAASUVORK5CYII=', 'base64');
        await page.locator('input[type="file"]').setInputFiles({ name: 'company-logo.png', mimeType: 'image/png', buffer: image });
        await page.locator('#business-profile-form').getByRole('button', { name: 'Save changes', exact: true }).click();
        assert(await page.locator('.request-success').isVisible());
        assert.equal(await page.locator('input[name="Input.Name"]').inputValue(), 'Mavambo Contracting');
        assert(await page.locator('.configuration-preview-logo img').isVisible());
        assert.equal(await page.locator('input[name="Input.DefaultValidityDays"]').inputValue(), '30');
        await page.reload();
        assert.equal(await page.locator('input[name="Input.Name"]').inputValue(), 'Mavambo Contracting');

        await stale.locator('input[name="Input.Name"]').fill('Stale company name');
        await stale.locator('#business-profile-form').getByRole('button', { name: 'Save changes', exact: true }).click();
        assert((await stale.locator('.configuration-errors').innerText()).includes('another window'));

        // Seeded quotation identity must be unchanged even if its store is first resolved after saving the profile.
        await page.goto(`${origin}/Quotations/Preview/128`);
        assert((await page.locator('.quotation-document-header').innerText()).includes('Mavambo Projects'));
        assert(!(await page.locator('.quotation-document-header').innerText()).includes('Mavambo Contracting'));
        await page.goto(`${origin}/Quotations/Edit?requestId=1036`);
        assert.equal(await page.locator('textarea[name="Input.Terms"]').inputValue(), 'Valid for 30 days. Confirm access before the work starts.');
        await page.locator('input[name="Input.Lines[0].Description"]').fill('Exterior painting labour');
        await page.locator('input[name="Input.Lines[0].Rate"]').fill('250');
        await page.getByRole('button', { name: 'Save draft', exact: true }).click();
        await page.getByRole('link', { name: 'Preview quotation', exact: true }).click();
        assert((await page.locator('.quotation-document-header').innerText()).includes('Mavambo Contracting'));
        assert((await page.locator('.quotation-document-header').innerText()).includes('accounts@mavambo.example'));
        assert(await page.locator('.quotation-company-logo').isVisible());
        const savedPreview = page.url();

        await page.goto(`${origin}/Configuration/Profile`);
        await page.locator('input[type="checkbox"][name="RemoveLogo"]').check();
        await page.locator('input[name="Input.Name"]').fill('Mavambo Projects');
        await page.locator('#business-profile-form').getByRole('button', { name: 'Save changes', exact: true }).click();
        assert.equal(await page.locator('.configuration-preview-logo img').isVisible(), false);
        await page.goto(savedPreview);
        assert((await page.locator('.quotation-document-header').innerText()).includes('Mavambo Contracting'));
        assert(await page.locator('.quotation-company-logo').isVisible());

        const live = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
        const errors = [];
        live.on('pageerror', error => errors.push(error.message));
        await live.goto(`${origin}/Configuration/Profile`);
        await live.locator('input[name="Input.Name"]').fill('Unsaved preview name');
        assert.equal(await live.locator('[data-preview-output="name"]').innerText(), 'Unsaved preview name');
        await live.getByRole('button', { name: 'Reset changes', exact: true }).click();
        await live.waitForFunction(() => document.querySelector('[data-preview-output="name"]').textContent === 'Mavambo Projects');
        await live.locator('input[type="file"]').setInputFiles({ name: 'bad.txt', mimeType: 'text/plain', buffer: Buffer.from('Not an image') });
        assert((await live.locator('#logo-client-error').innerText()).includes('PNG or JPEG'));
        await live.getByRole('button', { name: 'Reset changes', exact: true }).click();
        await live.emulateMedia({ reducedMotion: 'reduce' });
        await live.setViewportSize({ width: 390, height: 844 });
        await live.locator('#main-content').evaluate(element => element.scrollTop = 0);
        assert.equal(await live.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false);
        await live.screenshot({ path: 'design/configuration/BuildWise-Business-Profile-Mobile.png' });
        await live.locator('[data-workspace-menu]').click();
        await live.getByRole('link', { name: 'Business profile', exact: true }).click();
        assert(new URL(live.url()).pathname === '/Configuration/Profile');
        await live.setViewportSize({ width: 1440, height: 580 });
        await live.locator('#main-content').evaluate(element => element.scrollTop = 240);
        assert(await live.locator('#main-content').evaluate(element => element.scrollTop) > 0);
        assert.equal(await live.locator('#workspace-navigation').evaluate(element => element.scrollTop), 0);
        assert.equal(await live.evaluate(() => window.scrollY), 0);
        assert.deepEqual(errors, []);
        console.log('Passed: native configuration navigation, server validation, profile save/reload, logo upload/removal, concurrent edit protection, quotation defaults and retained snapshots, live preview/reset, mobile layout and independent scrolling.');
    } finally {
        await browser.close();
    }
}

run().catch(error => {
    console.error(error);
    process.exit(1);
});
