const {chromium}=require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs');
(async()=>{
const browser=await chromium.launch({headless:true,channel:'msedge'});
const page=await browser.newPage({viewport:{width:1440,height:1080},deviceScaleFactor:1});
const errors=[];page.on('pageerror',e=>errors.push(e.message));
const routes=['dashboard','requests','new-request','request/1042','clarify/1042','scope/1042','catalogue','service/ELE-001','service-new','customers','customer/1','customer-new','quotations','quote/128','quote-edit/1042','quote-preview/1042','history/1042','settings','settings-access','brand','sign-in'];
fs.mkdirSync('design/design-two/screens',{recursive:true});
for(const route of routes){await page.goto('http://localhost:5180/design-two#/'+route);await page.locator('h1:visible').first().waitFor();await page.screenshot({path:'design/design-two/screens/'+route.replaceAll('/','-')+'.png',fullPage:true});console.log(route,await page.locator('h1').first().textContent());}
await page.goto('http://localhost:5180/design-two#/requests');await page.locator('#list-search').fill('Tendai');if(await page.locator('tbody tr').count()!==1)throw Error('Request search failed');
await page.goto('http://localhost:5180/design-two#/scope/1042');await page.locator('#scope-approved').check();await page.getByRole('button',{name:'Confirm scope',exact:true}).click();await page.waitForURL('**/design-two#/quote-edit/1042');
await page.locator('[name=rate0]').fill('100');await page.getByRole('button',{name:'Save quotation',exact:true}).click();await page.waitForURL('**/design-two#/quote-preview/1042');await page.getByRole('button',{name:'Approve quotation',exact:true}).click();await page.getByRole('button',{name:'Confirm approval',exact:true}).click();await page.getByRole('link',{name:'Create revision',exact:true}).waitFor();
await page.getByRole('link',{name:'Create revision',exact:true}).click();await page.locator('[name=rate0]').fill('125');await page.getByRole('button',{name:'Save quotation',exact:true}).click();await page.waitForURL('**/design-two#/quote-preview/1042');if(!await page.locator('.document').textContent().then(s=>s.includes('Revision 2')))throw Error('Revision failed');
await page.goto('http://localhost:5180/design-two#/history/1042');await page.getByRole('button',{name:'View retained revision'}).click();if(!await page.locator('dialog').textContent().then(s=>s.includes('Approved revision 1')))throw Error('Snapshot missing');await page.getByRole('button',{name:'Close',exact:true}).click();
await page.locator('#toast').waitFor({state:'hidden'});await page.setViewportSize({width:390,height:844});for(const route of ['dashboard','catalogue','new-request','quote-preview/1042','sign-in']){await page.goto('http://localhost:5180/design-two#/'+route);await page.locator('h1:visible').first().waitFor();await page.screenshot({path:'design/design-two/screens/mobile-'+route.replaceAll('/','-')+'.png',fullPage:true});if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1))throw Error('Mobile overflow: '+route)}
await browser.close();if(errors.length)throw Error(errors.join('\n'));console.log('All routes, search, approval, revision retention and mobile overflow checks passed.');
})().catch(e=>{console.error(e);process.exit(1)});




