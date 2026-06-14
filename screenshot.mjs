import puppeteer from 'puppeteer';

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  headless: true
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
await page.goto('http://localhost:8765', { waitUntil: 'networkidle0', timeout: 15000 });
await new Promise(r => setTimeout(r, 3000));

// Onboarding step 1: enter name
await page.type('input[placeholder="Your name"]', 'Alex');
await new Promise(r => setTimeout(r, 500));
await page.screenshot({ path: '/tmp/screen-onboarding-name.png' });

// Click Next
async function clickButton(label) {
  const btns = await page.$$('div[role="button"], button');
  for (const btn of btns) {
    const txt = await page.evaluate(el => el.textContent, btn);
    if (txt && txt.includes(label)) { await btn.click(); return true; }
  }
  return false;
}

await clickButton('Next');
await new Promise(r => setTimeout(r, 1500));
await page.screenshot({ path: '/tmp/screen-onboarding-habits.png' });

// Pick a habit on step 2
const habitBtns = await page.$$('div[role="button"]');
if (habitBtns.length > 1) {
  await habitBtns[0].click();
  await new Promise(r => setTimeout(r, 300));
}
await clickButton('Next');
await new Promise(r => setTimeout(r, 1500));
await page.screenshot({ path: '/tmp/screen-onboarding-step3.png' });

// Finish
await clickButton('Next');
await clickButton('Start');
await clickButton('Get Started');
await clickButton('Done');
await new Promise(r => setTimeout(r, 2500));
await page.screenshot({ path: '/tmp/screen-dashboard.png' });

// Try to click AI Chat tab
const tabs = await page.$$('div[role="button"], button');
for (const tab of tabs) {
  const txt = await page.evaluate(el => el.textContent, tab);
  if (txt && txt.includes('AI')) { await tab.click(); break; }
}
await new Promise(r => setTimeout(r, 1500));
await page.screenshot({ path: '/tmp/screen-aichat.png' });

// Click Settings tab
const tabs2 = await page.$$('div[role="button"], button');
for (const tab of tabs2) {
  const txt = await page.evaluate(el => el.textContent, tab);
  if (txt && (txt.includes('Settings') || txt.includes('Setting'))) { await tab.click(); break; }
}
await new Promise(r => setTimeout(r, 1500));
await page.screenshot({ path: '/tmp/screen-settings.png' });

console.log('done');
await browser.close();
