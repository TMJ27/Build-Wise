const { chromium } = require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert = require('node:assert/strict');
(async () => {
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    try {
        const page = await browser.newPage({ viewport: { width: 390, height: 844 }, reducedMotion: 'reduce' });
        await page.goto('http://localhost:5191/Configuration/Profile');
        const bounds = await page.locator('#workspace-navigation').boundingBox();
        assert(bounds.x + bounds.width <= 1);
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth + 1), false);
        await page.screenshot({ path: 'design/configuration/BuildWise-Business-Profile-Mobile.png', animations: 'disabled' });
        await page.locator('[data-workspace-menu]').click();
        assert.equal(await page.locator('[data-workspace-menu]').getAttribute('aria-expanded'), 'true');
        await page.screenshot({ path: 'design/configuration/BuildWise-Business-Profile-Mobile-Navigation.png', animations: 'disabled' });
        console.log('Mobile layout and drawer verified.');
    } finally {
        await browser.close();
    }
})().catch(error => { console.error(error); process.exit(1); });
