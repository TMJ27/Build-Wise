const {chromium}=require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs');
(async()=>{
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const page=await browser.newPage({viewport:{width:1440,height:1080}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 fs.mkdirSync('design/layout/approved',{recursive:true});
 for(const path of ['/#/new-request','/design-two#/new-request','/#/dashboard','/#/catalogue','/Privacy']){
   const response=await page.goto('http://localhost:5191'+path);
   if(response && response.status()!==200)throw Error('Response '+response.status());
   await page.locator('.v2-header').waitFor();
   if(await page.locator('.v2-header').count()!==1||await page.locator('.v2-rail').count()!==1)throw Error('Duplicate chrome');
   if(await page.locator('.v2-header').evaluate(e=>getComputedStyle(e).backgroundColor)!=='rgb(255, 255, 255)')throw Error('Wrong header theme');
   if(await page.locator('.v2-rail').evaluate(e=>getComputedStyle(e).backgroundColor)!=='rgb(20, 35, 59)')throw Error('Wrong rail theme');
   if(path.includes('new-request')){
     await page.locator('#request-form').waitFor();
     if(await page.locator('#workspace-navigation [aria-current=page]').textContent()!=='Requests')throw Error('Active nav mismatch');
     const colour=await page.locator('#request-form .btn.primary').evaluate(e=>getComputedStyle(e).backgroundColor);
     if(colour!=='rgb(170, 98, 72)')throw Error('Wrong accent '+colour);
     await page.screenshot({path:'design/layout/approved/'+(path.startsWith('/design-two')?'alias':'default')+'-new-request.png',fullPage:true});
   }
 }
 await page.setViewportSize({width:390,height:844});
 await page.goto('http://localhost:5191/#/new-request');
 await page.locator('#request-form').waitFor();
 await page.locator('[data-workspace-menu]').click();
 if(await page.locator('[data-workspace-menu]').getAttribute('aria-expanded')!=='true')throw Error('Mobile menu failure');
 await page.locator('#workspace-navigation').getByRole('link',{name:'Customers',exact:true}).click();
 await page.waitForURL('**/#/customers');
 if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1))throw Error('Mobile overflow');
 await browser.close();if(errors.length)throw Error(errors.join('\n'));
 console.log('Approved Blueprint verified on default route and alias: navy rail, white header, muted orange, one shell, active navigation, mobile menu and native pages.');
})().catch(e=>{console.error(e);process.exit(1)});

