import puppeteer from 'puppeteer';

const browser = await puppeteer.launch({
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  headless: true
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });

await page.goto('http://localhost:8765', { waitUntil: 'domcontentloaded', timeout: 15000 });

const habits = [
  { id: '1', name: 'Morning Run', emoji: '🏃', category: 'fitness', frequency: 'daily', reminderTime: '07:00', createdAt: new Date().toISOString() },
  { id: '2', name: 'Meditate', emoji: '🧘', category: 'mindfulness', frequency: 'daily', reminderTime: '08:00', createdAt: new Date().toISOString() },
  { id: '3', name: 'Read 30 min', emoji: '📚', category: 'productivity', frequency: 'daily', reminderTime: '21:00', createdAt: new Date().toISOString() },
];
const today = new Date().toISOString().split('T')[0];
const completions = [
  { id: 'c1', habitId: '1', completedAt: new Date().toISOString(), date: today },
  { id: 'c2', habitId: '2', completedAt: new Date().toISOString(), date: today },
];
const settings = { isPremium: false, onboardingComplete: true, name: 'Alex' };

await page.evaluate((h, c, s) => {
  localStorage.setItem('habits', JSON.stringify(h));
  localStorage.setItem('completions', JSON.stringify(c));
  localStorage.setItem('user_settings', JSON.stringify(s));
}, habits, completions, settings);

await page.reload({ waitUntil: 'networkidle0' });
await new Promise(r => setTimeout(r, 3000));
await page.screenshot({ path: '/tmp/screen-dashboard2.png' });
console.log('dashboard done');

// Click Coach tab
const coachCoords = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  let node;
  while ((node = walker.nextNode())) {
    if (node.textContent.trim() === 'Coach' && node.children.length <= 2) {
      const rect = node.getBoundingClientRect();
      if (rect.width > 0 && rect.y > 700) return { x: rect.x + rect.width/2, y: rect.y + rect.height/2 };
    }
  }
  return null;
});
console.log('coach tab:', coachCoords);
if (coachCoords) {
  await page.mouse.click(coachCoords.x, coachCoords.y);
  await new Promise(r => setTimeout(r, 2000));
}
await page.screenshot({ path: '/tmp/screen-coach.png' });
console.log('coach done');

// Click Settings tab
const settingsCoords = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  let node;
  while ((node = walker.nextNode())) {
    if (node.textContent.trim() === 'Settings' && node.children.length <= 2) {
      const rect = node.getBoundingClientRect();
      if (rect.width > 0 && rect.y > 700) return { x: rect.x + rect.width/2, y: rect.y + rect.height/2 };
    }
  }
  return null;
});
console.log('settings tab:', settingsCoords);
if (settingsCoords) {
  await page.mouse.click(settingsCoords.x, settingsCoords.y);
  await new Promise(r => setTimeout(r, 2000));
}
await page.screenshot({ path: '/tmp/screen-settings.png' });
console.log('settings done');

// Go back to dashboard and click + button to add habit
const dashCoords = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  let node;
  while ((node = walker.nextNode())) {
    if (node.textContent.trim() === 'Dashboard' && node.children.length <= 2) {
      const rect = node.getBoundingClientRect();
      if (rect.width > 0 && rect.y > 700) return { x: rect.x + rect.width/2, y: rect.y + rect.height/2 };
    }
  }
  return null;
});
if (dashCoords) {
  await page.mouse.click(dashCoords.x, dashCoords.y);
  await new Promise(r => setTimeout(r, 1000));
}
// Click FAB (+)
const fabCoords = await page.evaluate(() => {
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT);
  let node;
  while ((node = walker.nextNode())) {
    if (node.textContent.trim() === '+') {
      const rect = node.getBoundingClientRect();
      if (rect.width > 0) return { x: rect.x + rect.width/2, y: rect.y + rect.height/2 };
    }
  }
  return null;
});
console.log('fab:', fabCoords);
if (fabCoords) {
  await page.mouse.click(fabCoords.x, fabCoords.y);
  await new Promise(r => setTimeout(r, 2000));
}
await page.screenshot({ path: '/tmp/screen-add-habit.png' });
console.log('add habit done');

await browser.close();
