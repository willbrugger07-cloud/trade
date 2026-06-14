import puppeteer from 'puppeteer';

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  headless: true
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
await page.goto('http://localhost:8765', { waitUntil: 'networkidle0', timeout: 15000 });
await new Promise(r => setTimeout(r, 3000));

// Get all elements with text "Next"
const info = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  const results = [];
  let node;
  while ((node = walker.nextNode())) {
    if (node.textContent.trim() === 'Next') {
      const rect = node.getBoundingClientRect();
      results.push({
        tag: node.tagName,
        role: node.getAttribute('role'),
        text: node.textContent.trim(),
        x: rect.x, y: rect.y, w: rect.width, h: rect.height,
        onclick: !!node.onclick,
        hasChildren: node.children.length
      });
    }
  }
  return results;
});
console.log(JSON.stringify(info, null, 2));

// Try clicking by coordinates at the center of the Next button area (bottom of screen)
// Next button is at bottom: ~y=800 center of width 390
await page.mouse.click(195, 808);
await new Promise(r => setTimeout(r, 1500));
await page.screenshot({ path: '/tmp/screen-after-click.png' });
console.log('screenshot taken');
await browser.close();
