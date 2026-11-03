const {chromium}=require('C:/Users/PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const page=await browser.newPage({viewport:{width:1440,height:1000}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 fs.mkdirSync('design/layout/sidebar',{recursive:true});
 await page.goto('http://localhost:5191/#/dashboard');
 await page.locator('#dashboard-range').waitFor();
 if(await page.locator('header nav,.workspace-account-menu,.v2-context').count())throw Error('Old navigation remains');
 for(const name of ['Overview','Requests','Quotations','Customers','Service catalogue','Business profile','Access & integrations']){
   const link=page.locator('#workspace-navigation').getByRole('link',{name,exact:true});
   if(!await link.isVisible()||await link.locator('svg').count()!==1)throw Error('Missing labelled icon '+name);
 }
 await page.locator('#workspace-navigation').getByRole('link',{name:'Business profile',exact:true}).click();
 await page.waitForURL('**/#/settings');
 if(await page.locator('#workspace-navigation [aria-current=page]').textContent().then(t=>t.trim())!=='Business profile')throw Error('Profile active state failed');
 await page.screenshot({path:'design/layout/sidebar/BuildWise-Configuration.png',fullPage:true});
 await page.locator('#workspace-navigation').getByRole('link',{name:'Access & integrations',exact:true}).click();
 await page.waitForURL('**/#/settings-access');await page.locator('[data-nav-section=settings-access][aria-current=page]').waitFor();
 if(await page.locator('#workspace-navigation [aria-current=page]').textContent().then(t=>t.trim())!=='Access & integrations')throw Error('Integration active state failed');
 await page.setViewportSize({width:820,height:1000});
 await page.locator('[data-workspace-menu]').click();
 if(await page.locator('[data-workspace-menu]').getAttribute('aria-expanded')!=='true')throw Error('Tablet menu failed');
 await page.keyboard.press('Escape');
 await page.setViewportSize({width:390,height:844});
 await page.locator('[data-workspace-menu]').click();
 await page.screenshot({path:'design/layout/sidebar/BuildWise-Sidebar-Mobile.png',fullPage:true,animations:'disabled'});
 await page.locator('#workspace-navigation').getByRole('link',{name:'Requests',exact:true}).click();
 await page.waitForURL('**/#/requests');
 if(await page.locator('[data-workspace-menu]').getAttribute('aria-expanded')!=='false')throw Error('Navigation did not close');
 if(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1))throw Error('Mobile overflow');
 await browser.close();if(errors.length)throw Error(errors.join('\n'));
 console.log('Sidebar checks passed: icons and names, configuration links, active states, no top navigation or profile menu, tablet/mobile navigation.');
})().catch(e=>{console.error(e);process.exit(1)});


