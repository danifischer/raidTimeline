'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { load, click, key, player, log } = require('./page');

/* the visible text, with a space between the elements (textContent would glue the table cells together) */
const text = el => {
  const parts = [];
  (function walk(n) { for (const c of n.childNodes) { if (c.nodeType === 3) parts.push(c.textContent); else walk(c); } })(el);
  return parts.map(p => p.trim()).filter(Boolean).join(' ');
};
const days = w => [...w.document.querySelectorAll('.daytab')];
const dayPanel = (w, i) => w.document.getElementById('day-' + i);
const visibleDays = w => [...w.document.querySelectorAll('.day')].filter(d => !d.hidden).map(d => d.id);

/* ---------- data problems ---------- */

test('no logs: both pages say so and nothing throws', () => {
  const w = load({ newestFirst: false, logs: [] });
  assert.deepEqual(w.errors, []);
  assert.match(text(w.document.querySelector('#timeline')), /No logs found yet/);
  assert.match(text(w.document.querySelector('#stats')), /No logs found yet/);
  assert.equal(days(w).length, 0);
});

test('broken json: the page falls back to the empty state', () => {
  const w = load('{ this is not json');
  assert.deepEqual(w.errors, []);
  assert.match(text(w.document.querySelector('#timeline')), /No logs found yet/);
});

/* ---------- raid days ---------- */

const twoDays = newestFirst => ({
  newestFirst,
  logs: [
    log('Gorseval', '2026-03-02T20:00:00', '2026-03-02T20:05:00'),
    log('Sabetha', '2026-03-01T20:00:00', '2026-03-01T20:04:00'),
  ],
});

test('days are tabs, oldest first by default and the first one is shown', () => {
  const w = load(twoDays(false));
  assert.equal(days(w).length, 2);
  assert.match(text(dayPanel(w, 0)), /Sabetha/);
  assert.match(text(dayPanel(w, 1)), /Gorseval/);
  assert.deepEqual(visibleDays(w), ['day-0']);
  assert.equal(days(w)[0].getAttribute('aria-selected'), 'true');
});

test('newestFirst reverses the tabs', () => {
  const w = load(twoDays(true));
  assert.match(text(dayPanel(w, 0)), /Gorseval/);
});

test('clicking a tab shows its day and writes it to the url', () => {
  const w = load(twoDays(false));
  click(days(w)[1]);
  assert.deepEqual(visibleDays(w), ['day-1']);
  assert.equal(days(w)[1].getAttribute('aria-selected'), 'true');
  assert.equal(days(w)[0].getAttribute('aria-selected'), 'false');
  assert.equal(w.location.hash, '#day-2026-03-02');
});

test('arrow keys, Home and End move between the tabs (and wrap around)', () => {
  const w = load(twoDays(false));
  key(days(w)[0], 'ArrowLeft');
  assert.deepEqual(visibleDays(w), ['day-1']);
  key(days(w)[1], 'ArrowRight');
  assert.deepEqual(visibleDays(w), ['day-0']);
  key(days(w)[0], 'End');
  assert.deepEqual(visibleDays(w), ['day-1']);
  key(days(w)[1], 'Home');
  assert.deepEqual(visibleDays(w), ['day-0']);
});

test('a day in the url hash is opened at the start', () => {
  const w = load(twoDays(false), { hash: '#day-2026-03-02' });
  assert.deepEqual(visibleDays(w), ['day-1']);
});

test('an unknown day in the url hash is ignored', () => {
  const w = load(twoDays(false), { hash: '#day-1999-01-01' });
  assert.deepEqual(visibleDays(w), ['day-0']);
});

/* ---------- pulls, downtime, attempts ---------- */

test('pulls are shown in time order and the downtime between bosses is computed', () => {
  const w = load({ newestFirst: false, logs: [
    log('Sabetha', '2026-03-01T21:00:00', '2026-03-01T21:05:00'),
    log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00'),
  ] });
  const cards = [...dayPanel(w, 0).querySelectorAll('.enc h3')].map(text);
  assert.deepEqual(cards, ['Gorseval', 'Sabetha']);
  const gap = dayPanel(w, 0).querySelector('.rail-gap');
  assert.match(text(gap), /56:00 downtime/); // 20:04 -> 21:00
  assert.match(text(gap), /20:04 → 21:00/);
  assert.ok(gap.classList.contains('long'), 'more than 10 minutes is a long break');
  assert.match(text(gap), /long break/);
});

test('a short break is not flagged as a long break', () => {
  const w = load({ newestFirst: false, logs: [
    log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00'),
    log('Sabetha', '2026-03-01T20:09:00', '2026-03-01T20:14:00'),
  ] });
  const gap = dayPanel(w, 0).querySelector('.rail-gap');
  assert.ok(!gap.classList.contains('long'));
});

test('the day header counts kills, wipes and bosses', () => {
  const w = load({ newestFirst: false, logs: [
    log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', { killed: false, hpLeft: [40] }),
    log('Gorseval', '2026-03-01T20:06:00', '2026-03-01T20:11:00'),
    log('Sabetha', '2026-03-01T20:20:00', '2026-03-01T20:25:00'),
  ] });
  const head = text(dayPanel(w, 0).querySelector('.day-head'));
  assert.match(head, /2 kills/);
  assert.match(head, /1 wipes/);
  assert.match(head, /2 bosses/);
});

const attempts = () => load({ newestFirst: false, logs: [
  log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', { killed: false, hpLeft: [35.5],
    players: [player('A.1111', { dps: 1000 }), player('B.2222', { dps: 1000 })] }),
  log('Gorseval', '2026-03-01T20:06:00', '2026-03-01T20:11:00',
    { players: [player('A.1111', { dps: 1500 }), player('B.2222', { dps: 1500 })] }),
] });

test('several tries of a boss share one card, the last one is shown', () => {
  const w = attempts();
  const cards = dayPanel(w, 0).querySelectorAll('.enc');
  assert.equal(cards.length, 1);
  assert.equal(cards[0].querySelectorAll('.att').length, 2);
  assert.equal(cards[0].querySelector('.badge').textContent, 'Kill after 1 wipe');
  assert.ok(cards[0].querySelector('.attempt[data-i="1"]').classList.contains('on'));
  assert.ok(!cards[0].querySelector('.attempt[data-i="0"]').classList.contains('on'));
});

test('the second try is compared with the previous one', () => {
  const w = attempts();
  const second = text(dayPanel(w, 0).querySelector('.attempt[data-i="1"] .chips'));
  assert.match(second, /Group DPS \+50\.0%/);
  assert.match(second, /Time \+1:00/); // 5:00 instead of 4:00
  assert.match(second, /Break 2:00/); // 20:04 -> 20:06
  const first = text(dayPanel(w, 0).querySelector('.attempt[data-i="0"] .chips'));
  assert.doesNotMatch(first, /vs previous/);
});

test('a wipe shows the health the boss had left', () => {
  const w = attempts();
  const first = dayPanel(w, 0).querySelector('.attempt[data-i="0"]');
  assert.match(text(first.querySelector('.chips')), /Boss HP left 35\.5%/);
  assert.ok(first.querySelector('.hp'));
  assert.equal(dayPanel(w, 0).querySelector('.attempt[data-i="1"] .hp'), null, 'a kill has no health bar');
});

test('choosing an earlier try switches the card and the badge, the last try restores it', () => {
  const w = attempts();
  const card = dayPanel(w, 0).querySelector('.enc');
  const [first, second] = card.querySelectorAll('.att');
  click(first);
  assert.ok(card.querySelector('.attempt[data-i="0"]').classList.contains('on'));
  assert.ok(!card.querySelector('.attempt[data-i="1"]').classList.contains('on'));
  assert.equal(first.getAttribute('aria-pressed'), 'true');
  assert.equal(second.getAttribute('aria-pressed'), 'false');
  assert.equal(card.querySelector('.badge').textContent, 'Attempt #1 · Wipe');
  click(second);
  assert.equal(card.querySelector('.badge').textContent, 'Kill after 1 wipe');
});

test('clicking a pull in the day strip selects that try and scrolls to the card', () => {
  const w = attempts();
  const card = dayPanel(w, 0).querySelector('.enc');
  click(dayPanel(w, 0).querySelector('.seg-pull[data-i="0"]'));
  assert.ok(card.querySelector('.attempt[data-i="0"]').classList.contains('on'));
  assert.equal(w.scrolled, card);
});

test('the strip groups consecutive pulls of a boss under one label with the number of tries', () => {
  const w = attempts();
  const runs = dayPanel(w, 0).querySelectorAll('.run');
  assert.equal(runs.length, 1);
  assert.match(text(runs[0].querySelector('.run-label')), /Gorseval ×2/);
  assert.equal(runs[0].querySelectorAll('.seg-pull').length, 2);
});

/* ---------- tables of a pull ---------- */

test('players are sorted by dps and get their share of the damage', () => {
  const w = load({ newestFirst: false, logs: [log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', {
    players: [
      player('Low.1', { dps: 500, damage: 25000 }),
      player('High.2', { dps: 1500, damage: 75000 }),
    ] })] });
  const rows = [...dayPanel(w, 0).querySelectorAll('.view.dps tbody tr')].map(text);
  assert.equal(rows.length, 2);
  assert.match(rows[0], /High\.2.*1,500 dps.*75\.0%/);
  assert.match(rows[1], /Low\.1.*500 dps.*25\.0%/);
});

test('mechanics: ranked columns decide the order, zero shows a dash', () => {
  const columns = [{ key: 'kick', label: 'Kick', title: 'Kicked', rank: true }, { key: 'info', label: 'Info', title: 'x', rank: false }];
  const w = load({ newestFirst: false, logs: [log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', {
    columns,
    players: [
      player('Few.1', { mechanics: { kick: 1, info: 9 } }),
      player('Many.2', { mechanics: { kick: 4 } }),
    ] })] });
  const rows = [...dayPanel(w, 0).querySelectorAll('.view.mech tbody tr')];
  assert.match(text(rows[0]), /Many\.2/);
  assert.match(text(rows[1]), /Few\.1/);
  assert.equal(rows[0].querySelector('.zero').textContent, '–', 'the missing "info" of Many.2');
  assert.equal(rows[1].querySelectorAll('.hit')[1].textContent, '9');
});

test('a boss without mechanic columns says so', () => {
  const w = load({ newestFirst: false, logs: [log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00')] });
  assert.match(text(dayPanel(w, 0).querySelector('.view.mech')), /No mechanics are tracked/);
});

test('boons: one cell per player and boon plus the squad average', () => {
  const quickness = { id: 'b1', name: 'Quickness', icon: 'q.png', stacking: false };
  const might = { id: 'b2', name: 'Might', icon: 'm.png', stacking: true };
  const buffs = (q, m) => [{ ...quickness, value: q, stacks: null }, { ...might, value: m, stacks: 80 }];
  const w = load({ newestFirst: false, logs: [log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', {
    players: [player('A.1', { buffs: buffs(100, 20) }), player('B.2', { buffs: buffs(50, 10) })] })] });
  const rows = [...dayPanel(w, 0).querySelectorAll('.view.boons tbody tr')];
  assert.equal(rows.length, 3);
  assert.match(text(rows[0]), /A\.1 100% 20/);
  const avg = rows[2];
  assert.ok(avg.classList.contains('avg'));
  assert.match(text(avg), /Squad average 75% 15/);
  assert.match(dayPanel(w, 0).querySelector('.heat[title*="Might"]').title, /present 80% of the time/);
});

test('resurrects list only the players who resurrected, most first', () => {
  const w = load({ newestFirst: false, logs: [log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', {
    players: [player('None.1'), player('Some.2', { resAmount: 1, resTime: 2.5 }), player('Most.3', { resAmount: 3, resTime: 7 })] })] });
  const rows = [...dayPanel(w, 0).querySelectorAll('.view.res tbody tr')].map(text);
  assert.equal(rows.length, 2);
  assert.match(rows[0], /Most\.3 3× 7\.0s/);
});

/* ---------- views, pages, theme ---------- */

const view = w => w.document.body.dataset.view;
const viewButton = (w, v) => w.document.querySelector(`#views button[data-v="${v}"]`);

test('the view defaults to dps and a chosen view is remembered', () => {
  const w = load(twoDays(false));
  assert.equal(view(w), 'dps');
  click(viewButton(w, 'boons'));
  assert.equal(view(w), 'boons');
  assert.equal(viewButton(w, 'boons').getAttribute('aria-pressed'), 'true');
  assert.equal(viewButton(w, 'dps').getAttribute('aria-pressed'), 'false');
  assert.equal(w.localStorage.getItem('rt-view'), 'boons');
});

test('a remembered view is used when the page opens, an unknown one falls back to dps', () => {
  assert.equal(view(load(twoDays(false), { storage: { 'rt-view': 'deaths' } })), 'deaths');
  assert.equal(view(load(twoDays(false), { storage: { 'rt-view': 'nonsense' } })), 'dps');
});

test('the statistics page replaces the timeline and hides the data views', () => {
  const w = load(twoDays(false));
  const [timeline, stats] = w.document.querySelectorAll('#pages button');
  click(stats);
  assert.ok(w.document.getElementById('stats').classList.contains('on'));
  assert.ok(!w.document.getElementById('timeline').classList.contains('on'));
  assert.equal(w.document.getElementById('views').style.display, 'none');
  click(timeline);
  assert.ok(w.document.getElementById('timeline').classList.contains('on'));
  assert.equal(w.document.getElementById('views').style.display, '');
});

test('statistics: totals over all days and the kill rate per boss', () => {
  const w = load({ newestFirst: false, logs: [
    log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', { killed: false, hpLeft: [10] }),
    log('Gorseval', '2026-03-01T20:06:00', '2026-03-01T20:11:00'),
    log('Sabetha', '2026-03-02T20:00:00', '2026-03-02T20:05:00'),
  ] });
  const cards = [...w.document.querySelectorAll('#stats .card')].map(c => [text(c.querySelector('.l')), text(c.querySelector('.v'))]);
  assert.deepEqual(Object.fromEntries(cards), { 'Raid days': '2', Kills: '2', Wipes: '1', 'Time in raid': '16m' }); // 11m + 5m
  const rows = [...w.document.querySelectorAll('#stats .panel')[0].querySelectorAll('tbody tr')].map(text);
  const row = name => rows.find(r => r.includes(name));
  assert.match(row('Gorseval'), /1\/2/);
  assert.match(row('Sabetha'), /1\/1/);
  assert.match(row('Gorseval'), /9:00/); // 4:00 + 5:00 of fighting
});

test('statistics: downs and deaths count kills only', () => {
  const w = load({ newestFirst: false, logs: [
    log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', { killed: false, hpLeft: [10],
      players: [player('A.1', { died: 5, downed: 5 })] }),
    log('Gorseval', '2026-03-01T20:06:00', '2026-03-01T20:11:00', {
      players: [player('A.1', { died: 1, downed: 2 })] }),
  ] });
  const rows = [...w.document.querySelectorAll('#stats .panel')[1].querySelectorAll('tbody tr')].map(text);
  assert.deepEqual(rows, ['A.1 2 1']);
});

test('the theme follows the system and the button toggles it and remembers the choice', () => {
  const w = load(twoDays(false), { dark: true });
  assert.equal(w.document.documentElement.dataset.theme, undefined, 'no override until the button is used');
  click(w.document.getElementById('theme'));
  assert.equal(w.document.documentElement.dataset.theme, 'light');
  assert.equal(w.localStorage.getItem('rt-theme'), 'light');
  click(w.document.getElementById('theme'));
  assert.equal(w.document.documentElement.dataset.theme, 'dark');
});

test('a saved theme is applied at the start, a light system toggles to dark', () => {
  assert.equal(load(twoDays(false), { storage: { 'rt-theme': 'light' } }).document.documentElement.dataset.theme, 'light');
  const w = load(twoDays(false), { dark: false });
  click(w.document.getElementById('theme'));
  assert.equal(w.document.documentElement.dataset.theme, 'dark');
});

/* ---------- icons ---------- */

test('a boss without an icon gets a generated tile with its initials', () => {
  const w = load({ newestFirst: false, logs: [log('Vale Guardian', '2026-03-01T20:00:00', '2026-03-01T20:04:00')] });
  const icon = dayPanel(w, 0).querySelector('.enc-head svg.icon');
  assert.ok(icon);
  assert.equal(icon.querySelector('text').textContent, 'VG');
});

test('a boss icon that cannot load is replaced by the generated tile', () => {
  const w = load({ newestFirst: false, logs: [log('Vale Guardian', '2026-03-01T20:00:00', '2026-03-01T20:04:00', { icon: 'https://x.test/vg.png' })] });
  const img = dayPanel(w, 0).querySelector('.enc-head img.icon');
  assert.ok(img);
  img.dispatchEvent(new w.Event('error'));
  assert.equal(dayPanel(w, 0).querySelector('.enc-head img.icon'), null);
  assert.ok(dayPanel(w, 0).querySelector('.enc-head svg.icon'));
});

/* ---------- hostile data ---------- */

test('names from the logs are escaped and never become markup', () => {
  const evil = '<img src=x onerror="window.pwned=1">';
  const w = load({ newestFirst: false, logs: [log(evil, '2026-03-01T20:00:00', '2026-03-01T20:04:00', {
    columns: [{ key: 'm', label: evil, title: '"><b>x</b>', rank: true }],
    players: [player(evil, { profession: evil })] })] });
  assert.deepEqual(w.errors, []);
  const doc = w.document;
  assert.equal(doc.querySelectorAll('[onerror]').length, 0);
  assert.ok([...doc.querySelectorAll('b')].every(b => b.textContent !== 'x'), 'the title attribute did not break out');
  assert.match(text(dayPanel(w, 0).querySelector('.enc h3')), /^<img src=x/);
  assert.equal(w.pwned, undefined);
});

test('log links: http(s) and relative are kept, other schemes become "#"', () => {
  const href = url => load({ newestFirst: false, logs: [log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:04:00', { url })] })
    .document.querySelector('a.chip.link').getAttribute('href');
  assert.equal(href('https://dps.report/abc'), 'https://dps.report/abc');
  assert.equal(href('log.html'), 'log.html');
  assert.equal(href('javascript:alert(1)'), '#');
  assert.equal(href('JavaScript:alert(1)'), '#');
  assert.equal(href('data:text/html,<b>'), '#');
});

test('the data can contain a closing script tag without breaking the page', () => {
  const w = load({ newestFirst: false, logs: [log('</script><script>window.pwned=1</script>', '2026-03-01T20:00:00', '2026-03-01T20:04:00')] });
  assert.deepEqual(w.errors, []);
  assert.equal(w.pwned, undefined);
  assert.equal(days(w).length, 1);
});

/* ---------- boss page ---------- */

const tonight = () => ({
  newestFirst: false,
  logs: [
    log('Gorseval', '2026-03-01T20:00:00', '2026-03-01T20:05:00'),
    ...[60, 40, 15].map((hp, i) => log('Sabetha', `2026-03-02T20:${10 + i * 10}:00`, `2026-03-02T20:${14 + i * 10}:00`,
      { killed: false, hpLeft: [hp], players: [player('A.1111', { dps: 2000 + i * 1000, died: i }), player('B.2222')] })),
    log('Sabetha', '2026-03-02T20:50:00', '2026-03-02T20:53:00', { players: [player('A.1111', { dps: 6000 }), player('B.2222')] }),
  ],
});
const boss = w => w.document.querySelector('#boss');
const bossCards = w => [...boss(w).querySelectorAll('.card')].map(c => text(c));

test('boss page: opens on the boss with the most tries of the newest day and lists every try', () => {
  const w = load(tonight());
  assert.deepEqual(w.errors, []);
  assert.equal(w.document.querySelector('#boss-pick').selectedOptions[0].textContent, 'Sabetha (4)');
  assert.equal(w.document.querySelector('#boss-day').value, '2026-03-02');
  assert.equal(boss(w).querySelectorAll('.panel tbody tr').length >= 4, true);
  assert.equal(boss(w).querySelectorAll('.tb').length, 4);
  assert.equal(boss(w).querySelectorAll('.tb.kill').length, 1);
});

test('boss page: summary cards show tries, kills, fastest kill and the best wipe', () => {
  const w = load(tonight());
  const cards = bossCards(w);
  assert.match(cards[0], /4 Tries/);
  assert.match(cards[1], /1 Kills · 25% kill rate/);
  assert.match(cards[2], /3:00 Fastest kill/);
  assert.match(cards[3], /15\.0% Best wipe/);
});

test('boss page: players get their average and best dps over the tries', () => {
  const w = load(tonight());
  const row = [...boss(w).querySelectorAll('tbody tr')].find(r => text(r).startsWith('A.1111'));
  assert.match(text(row), /A\.1111 4 3,750 6,000 – – 3/);
});

test('boss page: picking another boss or all days changes the tries', () => {
  const w = load(tonight());
  w.document.querySelector('#boss-day').value = 'all';
  w.document.querySelector('#boss-day').dispatchEvent(new w.Event('change'));
  assert.equal(boss(w).querySelectorAll('.tb').length, 4);
  const pick = w.document.querySelector('#boss-pick');
  pick.value = [...pick.options].findIndex(o => o.textContent.startsWith('Gorseval'));
  pick.dispatchEvent(new w.Event('change'));
  assert.match(bossCards(w)[0], /1 Tries/);
});

test('boss page: the stats button on a boss card opens it for that day', () => {
  const w = load(tonight());
  click(w.document.querySelector('#day-0 .boss-link'));
  assert.equal(w.document.querySelector('#boss').classList.contains('on'), true);
  assert.equal(w.document.querySelector('#boss-pick').selectedOptions[0].textContent, 'Gorseval (1)');
  assert.equal(w.document.querySelector('#boss-day').value, '2026-03-01');
});

test('boss page: no logs says so', () => {
  const w = load({ newestFirst: false, logs: [] });
  assert.match(text(boss(w)), /No logs found yet/);
});

/* ---------- copy for discord ---------- */

async function copied(w, dayKey) {
  let out = null;
  Object.defineProperty(w.navigator, 'clipboard', { value: { writeText: async t => { out = t; } }, configurable: true });
  if (dayKey) { const d = w.document.querySelector('#boss-day'); d.value = dayKey; d.dispatchEvent(new w.Event('change')); }
  click(w.document.querySelector('#boss-copy'));
  await new Promise(r => setTimeout(r, 10));
  return out;
}

test('boss page: copy for discord puts the summary and every try with a link to its log on the clipboard', async () => {
  const w = load(tonight());
  const md = await copied(w);
  assert.match(md, /^## Sabetha · /);
  assert.match(md, /\*\*4\*\* tries · \*\*1\*\* kill \(25%\)/);
  assert.match(md, /Fastest kill \*\*3:00\*\*/);
  assert.match(md, /Best wipe \*\*15\.0% left\*\*/);
  assert.doesNotMatch(md, /\p{Extended_Pictographic}/u);
  assert.match(md, /```\n# {3}Start {2}Result {2}Time {2}HP left\n01 {2}20:10 {2}Wipe {4}4:00 {4}60\.0%\n/);
  assert.match(md, /\n04 {2}20:50 {2}Kill {4}3:00 {8}–\n```/);
  assert.match(md, /\*\*Logs:\*\* \[01\]\(<https:\/\/dps\.report\/abc>\) \[02\]\(<[^>]+>\) \[03\]\(<[^>]+>\) \[04\]\(<[^>]+>\)$/);
  assert.doesNotMatch(md, /Players|A\.1111/);
  assert.equal(w.document.querySelector('#boss-copy').textContent, 'Copied ✓');
});

test('boss page: a try without a web link gets no link in the discord text', async () => {
  const w = load({ newestFirst: false, logs: [log('Sabetha', '2026-03-02T20:00:00', '2026-03-02T20:03:00', { url: 'logs/a.html' })] });
  const md = await copied(w);
  assert.match(md, /Kill {4}3:00 {8}–\n```$/);
  assert.doesNotMatch(md, /Logs/);
});

test('boss page: the discord text stays under the 2000 character limit', async () => {
  const many = Array.from({ length: 60 }, (_, i) => log('Sabetha', `2026-03-02T20:${String(i).padStart(2, '0')}:00`, `2026-03-02T20:${String(i).padStart(2, '0')}:30`,
    { killed: false, hpLeft: [50], players: Array.from({ length: 10 }, (_, k) => player(`Player${k}.${1000 + k}`)) }));
  const w = load({ newestFirst: false, logs: many });
  const md = await copied(w);
  assert.equal(md.length <= 2000, true);
  assert.match(md, /^## Sabetha/);
});

test('boss page: copying falls back to the old copy command without clipboard access', async () => {
  const w = load(tonight());
  Object.defineProperty(w.navigator, 'clipboard', { value: { writeText: async () => { throw new Error('denied'); } }, configurable: true });
  w.document.execCommand = () => true;
  click(w.document.querySelector('#boss-copy'));
  await new Promise(r => setTimeout(r, 10));
  assert.equal(w.document.querySelector('#boss-copy').textContent, 'Copied ✓');
});

/* ---------- image of the health left per try ---------- */

/* jsdom has no canvas: record what is drawn */
function fakeCanvas(w) {
  const drawn = { texts: [], rects: [], fills: [], size: null };
  const ctx = new Proxy({}, { get: (t, k) => k === 'fillText' ? (s) => drawn.texts.push(s)
    : k === 'fillRect' ? (...a) => drawn.rects.push(a)
    : k === 'measureText' ? () => ({ width: 10 }) : () => { }, set: (t, k, v) => { if (k === 'fillStyle') drawn.fills.push(v); return true; } });
  w.HTMLCanvasElement.prototype.getContext = function () { drawn.size = [this.width, this.height]; return ctx; };
  w.HTMLCanvasElement.prototype.toBlob = function (cb) { cb(new w.Blob(['png'], { type: 'image/png' })); };
  return drawn;
}

test('boss page: copy image draws a bar per try and puts the png on the clipboard', async () => {
  const w = load(tonight());
  const drawn = fakeCanvas(w);
  let item = null;
  w.ClipboardItem = class { constructor(o) { this.o = o; } };
  Object.defineProperty(w.navigator, 'clipboard', { value: { write: async i => { item = i[0]; } }, configurable: true });
  click(w.document.querySelector('#boss-img'));
  await new Promise(r => setTimeout(r, 10));
  assert.deepEqual(Object.keys(item.o), ['image/png']);
  assert.ok(drawn.texts.includes('Sabetha · ' + w.document.querySelector('#boss-day').selectedOptions[0].text) || drawn.texts.some(t => t.startsWith('Sabetha · ')));
  assert.deepEqual(['60', '40', '15', '✓'].filter(t => !drawn.texts.includes(t)), []);
  assert.equal(drawn.rects.length, 1 + 4);
  assert.equal(w.document.querySelector('#boss-img').textContent, 'Copied ✓');
});

test('boss page: copy image saves the png when the clipboard cannot take images', async () => {
  const w = load(tonight());
  fakeCanvas(w);
  let saved = null;
  w.URL.createObjectURL = () => 'blob:x'; w.URL.revokeObjectURL = () => { };
  w.HTMLAnchorElement.prototype.click = function () { saved = this.download; };
  click(w.document.querySelector('#boss-img'));
  await new Promise(r => setTimeout(r, 10));
  assert.match(saved, /^Sabetha_.*\.png$/);
  assert.equal(w.document.querySelector('#boss-img').textContent, 'Saved as file ✓');
});
