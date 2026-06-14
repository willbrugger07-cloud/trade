import puppeteer from 'puppeteer';

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  headless: true
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });

// Pre-seed AsyncStorage (it uses localStorage under the hood on web)
await page.goto('http://localhost:8765', { waitUntil: 'domcontentloaded', timeout: 15000 });

const habits = [
  { id: '1', name: 'Morning Run', emoji: '🏃', category: 'fitness', frequency: 'daily', reminderTime: '07:00', createdAt: new Date().toISOString() },
  { id: '2', name: 'Meditate', emoji: '🧘', category: 'mindfulness', frequency: 'daily', reminderTime: '08:00', createdAt: new Date().toISOString() },
  { id: '3', name: 'Read 30 min', emoji: '📚', category: 'productivity', frequency: 'daily', reminderTime: '21:00', createdAt: new Date().toISOString() },
];
const today = new Date().toISOString().split('T')[0];
const completions = [
  { id: 'c1', habitId: '1', completedAt: new Date().toISOString(), date: today },
];
const settings = { isPremium: false, onboardingComplete: true, name: 'Alex' };

await page.evaluate((h, c, s) => {
  localStorage.setItem('@habitcoach:habits', JSON.stringify(h));
  localStorage.setItem('@habitcoach:completions', JSON.stringify(c));
  localStorage.setItem('@habitcoach:user_settings', JSON.stringify(s));
}, habits, completions, settings);

await page.reload({ waitUntil: 'networkidle0' });
await new Promise(r => setTimeout(r, 4000));
await page.screenshot({ path: '/tmp/screen-dashboard.png' });
console.log('dashboard screenshot done');

// Find and click AI Chat tab
const tabInfo = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  const results = [];
  let node;
  while ((node = walker.nextNode())) {
    const t = node.textContent.trim();
    if (t === 'AI Coach' || t === 'AI Chat' || t === 'Chat') {
      const rect = node.getBoundingClientRect();
      if (rect.width > 0) results.push({ text: t, x: rect.x + rect.width/2, y: rect.y + rect.height/2 });
    }
  }
  return results;
});
console.log('tabs found:', JSON.stringify(tabInfo));

if (tabInfo.length > 0) {
  await page.mouse.click(tabInfo[0].x, tabInfo[0].y);
  await new Promise(r => setTimeout(r, 1500));
}
await page.screenshot({ path: '/tmp/screen-aichat.png' });
console.log('ai chat screenshot done');

// Go to Settings tab
const settingsTab = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  const results = [];
  let node;
  while ((node = walker.nextNode())) {
    const t = node.textContent.trim();
    if (t === 'Settings') {
      const rect = node.getBoundingClientRect();
      if (rect.width > 0 && rect.width < 200) results.push({ text: t, x: rect.x + rect.width/2, y: rect.y + rect.height/2 });
    }
  }
  return results;
});
console.log('settings tabs:', JSON.stringify(settingsTab));
if (settingsTab.length > 0) {
  await page.mouse.click(settingsTab[0].x, settingsTab[0].y);
  await new Promise(r => setTimeout(r, 1500));
}
await page.screenshot({ path: '/tmp/screen-settings.png' });
console.log('settings screenshot done');

await browser.close();
