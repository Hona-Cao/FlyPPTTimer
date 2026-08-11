// Real Chromium smoke test for the FlyPPTTimer remote web page.
//
// Drives headless Chromium through the stable playwright Node API (no @playwright/cli
// session flag, which proved environment-fragile across CI runners). Validates:
//   1. 390x844 mobile layout has no horizontal overflow
//   2. timer.start command executes and paints the command response
//   3. presentation tab activates with the fixture presentation name
//   4. continuous reverse swipe returns to the timer page
//   5. zh/en language switching works
//
// This file is copied next to an installed `playwright` package at runtime so that the
// bare `playwright` import resolves from the install prefix.

import http from 'http';
import fs from 'fs';
import path from 'path';
import { chromium } from 'playwright';

function parseArgs(argv) {
  const out = { web: null, port: 0 };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--web') out.web = argv[++i];
    else if (argv[i] === '--port') out.port = Number(argv[++i]) || 0;
  }
  return out;
}

function fail(msg) {
  console.error('Remote browser validation failed: ' + msg);
  process.exit(1);
}

const args = parseArgs(process.argv.slice(2));
if (!args.web) fail('--web <directory> is required');
const webRoot = path.resolve(args.web);
if (!fs.existsSync(path.join(webRoot, 'index.html'))) fail('index.html not found in ' + webRoot);

const contentTypes = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'application/javascript; charset=utf-8' };
const server = http.createServer((req, res) => {
  let p = decodeURIComponent((req.url || '/').split('?')[0]);
  if (p === '/') p = '/index.html';
  const fp = path.join(webRoot, p);
  fs.readFile(fp, (err, buf) => {
    if (err) { res.statusCode = 404; res.end('not found'); return; }
    res.setHeader('content-type', contentTypes[path.extname(fp)] || 'application/octet-stream');
    res.end(buf);
  });
});

await new Promise(r => server.listen(args.port, '127.0.0.1', r));
const port = server.address().port;
const base = `http://127.0.0.1:${port}`;

// Fixture state/command payloads the remote page polls and posts against.
const state = { ok: true, message: '', timerState: { mode: '倒计时', state: '停止', running: false, durationMs: 480000, elapsedMs: 0, remainingMs: 480000, displayText: '08:00', isOvertime: false, continueOvertime: true, windowVisible: true, muted: false, timeUpBlackoutActive: false, ruleCount: 1 }, presentationState: { powerPointInstalled: true, powerPointRunning: true, hasPresentation: true, isSlideShowRunning: true, presentationName: 'browser-fixture.pptx', presentationPath: 'C:/fixture/browser-fixture.pptx', currentSlide: 2, totalSlides: 10, screenMode: '正常', presentations: [{ id: 'fixture-id', name: 'browser-fixture.pptx', directory: 'C:/fixture', isActive: true, isOpen: true, isManaged: true }] }, version: '4.0.0', connectedClients: 1, revision: 7 };
const command = { ok: true, message: '命令已执行', timerState: { mode: '倒计时', state: '运行中', running: true, durationMs: 480000, elapsedMs: 1000, remainingMs: 479000, displayText: '07:59', isOvertime: false, windowVisible: true, muted: false, timeUpBlackoutActive: false, ruleCount: 1 }, presentationState: { powerPointInstalled: true, powerPointRunning: true, hasPresentation: true, isSlideShowRunning: true, presentationName: 'browser-fixture.pptx', currentSlide: 3, totalSlides: 10, screenMode: '正常', presentations: [] }, version: '4.0.0', revision: 8 };

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
await page.route('**/state*', r => r.fulfill({ contentType: 'application/json', body: JSON.stringify(state) }));
await page.route('**/command*', r => r.fulfill({ contentType: 'application/json', body: JSON.stringify(command) }));

await page.goto(`${base}/index.html?token=browser-test`, { waitUntil: 'load' });
await page.waitForTimeout(600);

// 1+2+3: command execution + presentation tab + mobile layout (no horizontal overflow).
await page.locator('[data-command="timer.start"]').click();
await page.waitForTimeout(200);
await page.locator('[data-page="pptPage"]').click();
await page.waitForTimeout(400);
const info = await page.evaluate(() => ({
  messageLength: document.getElementById('message').textContent.length,
  presentationActive: document.querySelector('[data-page="pptPage"]').classList.contains('active'),
  presentation: document.getElementById('pptName').textContent,
  width: innerWidth,
  clientWidth: document.documentElement.clientWidth,
  scrollWidth: document.documentElement.scrollWidth
}));
if (info.messageLength !== 5) fail(`message length ${info.messageLength} != 5`);
if (!info.presentationActive) fail('presentation tab not active after click');
if (info.presentation !== 'browser-fixture.pptx') fail(`presentation name "${info.presentation}" != browser-fixture.pptx`);
if (info.scrollWidth !== 390) fail(`horizontal overflow: scrollWidth ${info.scrollWidth} != 390`);
if (info.clientWidth !== 390) fail(`clientWidth ${info.clientWidth} != 390`);

// 4: continuous reverse swipe returns to the timer page with no horizontal overflow.
const gesture = await page.evaluate(async () => {
  const v = document.getElementById('pagesViewport');
  const swipe = (from, to) => {
    const make = x => new Touch({ identifier: 1, target: v, clientX: x, clientY: 300, pageX: x, pageY: 300, screenX: x, screenY: 300 });
    const a = make(from), b = make(to);
    v.dispatchEvent(new TouchEvent('touchstart', { touches: [a], targetTouches: [a], changedTouches: [a], bubbles: true, cancelable: true }));
    v.dispatchEvent(new TouchEvent('touchmove', { touches: [b], targetTouches: [b], changedTouches: [b], bubbles: true, cancelable: true }));
    v.dispatchEvent(new TouchEvent('touchend', { touches: [], targetTouches: [], changedTouches: [b], bubbles: true, cancelable: true }));
  };
  swipe(60, 330); await new Promise(r => setTimeout(r, 40)); swipe(320, 60); await new Promise(r => setTimeout(r, 40)); swipe(60, 330); await new Promise(r => setTimeout(r, 380));
  return { timerActive: document.querySelector('[data-page="timerPage"]').classList.contains('active'), track: getComputedStyle(document.getElementById('pagesTrack')).transform, bodyScrollWidth: document.body.scrollWidth };
});
if (!gesture.timerActive) fail('reverse swipe did not return to timer page');
if (gesture.track !== 'matrix(1, 0, 0, 1, 0, 0)') fail(`track transform "${gesture.track}" != identity`);
if (gesture.bodyScrollWidth !== 390) fail(`gesture horizontal overflow: bodyScrollWidth ${gesture.bodyScrollWidth} != 390`);

// 5: language switching to en-US.
await page.addInitScript(() => Object.defineProperty(Navigator.prototype, 'language', { get: () => 'en-US' }));
await page.reload({ waitUntil: 'load' });
await page.waitForTimeout(500);
const eng = await page.evaluate(() => ({
  lang: document.documentElement.lang,
  title: document.title,
  heading: document.querySelector('h1').textContent,
  status: document.getElementById('connection').textContent,
  scrollWidth: document.documentElement.scrollWidth
}));
if (eng.lang !== 'en') fail(`lang "${eng.lang}" != en`);
if (!eng.title.includes('FlyPPTTimer Remote')) fail(`title "${eng.title}" missing FlyPPTTimer Remote`);
if (eng.heading !== 'Presentation Remote') fail(`heading "${eng.heading}" != Presentation Remote`);
if (eng.status !== 'Connected') fail(`status "${eng.status}" != Connected`);
if (eng.scrollWidth !== 390) fail(`en horizontal overflow: scrollWidth ${eng.scrollWidth} != 390`);

await browser.close();
server.close();
console.log('Real Chromium remote page passed: 390x844 layout, command, presentation tab, continuous reverse swipe, and zh/en behavior.');
