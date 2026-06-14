import puppeteer from 'puppeteer';

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  headless: true
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
await page.goto('http://localhost:8765', { waitUntil: 'networkidle0', timeout: 15000 });
await new Promise(r => setTimeout(r, 3000));

// Log all clickable elements to find the right selector
const elements = await page.evaluate(() => {
  const all = document.querySelectorAll('*');
  const results = [];
  for (const el of all) {
    const style = window.getComputedStyle(el);
    if (style.cursor === 'pointer' || el.tagName === 'BUTTON' || el.getAttribute('role') === 'button') {
      results.push({
        tag: el.tagName,
        role: el.getAttribute('role'),
        text: el.textContent.trim().slice(0, 50),
        cursor: style.cursor,
        class: el.className.toString().slice(0, 50)
      });
    }
  }
  return results;
});
console.log(JSON.stringify(elements, null, 2));
await browser.close();
