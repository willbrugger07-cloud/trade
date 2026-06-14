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
];
const settings = { isPremium: false, onboardingComplete: true, name: 'Alex' };

// Try all possible key formats AsyncStorage might use on web
await page.evaluate((h, c, s) => {
  // Direct keys
  localStorage.setItem('habits', JSON.stringify(h));
  localStorage.setItem('completions', JSON.stringify(c));
  localStorage.setItem('user_settings', JSON.stringify(s));
  // With RN AsyncStorage prefix
  localStorage.setItem('RCTAsyncLocalStorage_V1habits', JSON.stringify(h));
  localStorage.setItem('RCTAsyncLocalStorage_V1user_settings', JSON.stringify(s));
  // Check what's actually there
  const keys = Object.keys(localStorage);
  console.log('stored keys:', JSON.stringify(keys));
}, habits, completions, settings);

await page.reload({ waitUntil: 'networkidle0' });
await new Promise(r => setTimeout(r, 4000));

// Log what's in localStorage after reload
const storageKeys = await page.evaluate(() => Object.keys(localStorage));
console.log('localStorage keys after reload:', storageKeys);

await page.screenshot({ path: '/tmp/screen-dashboard.png' });
console.log('screenshot taken');

await browser.close();
