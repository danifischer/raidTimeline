// Loads raidTimeline.Logic/template.html with some data into jsdom, the same way a browser opens the generated file.
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('jsdom');

const template = fs.readFileSync(path.join(__dirname, '..', 'raidTimeline.Logic', 'template.html'), 'utf8');

/** Fills the template like TimelineCreator does (json in the data script tag, "<" escaped so it cannot end the tag). */
function render(data) {
  const json = typeof data === 'string' ? data : JSON.stringify(data);
  return template.replace('{{data}}', () => json.replace(/</g, '\\u003c'));
}

/**
 * @param data  the object the page gets (or a raw string, e.g. broken json)
 * @param opts  hash: location hash; storage: values for localStorage; dark: system theme is dark
 */
function load(data, { hash = '', storage = {}, dark = true } = {}) {
  const errors = [];
  const dom = new JSDOM(render(data), {
    url: 'https://timeline.test/index.html' + hash,
    runScripts: 'dangerously',
    pretendToBeVisual: true,
    beforeParse(window) {
      window.matchMedia = query => ({ matches: dark && query.includes('dark'), addEventListener() {}, removeEventListener() {} });
      window.Element.prototype.scrollIntoView = function () { window.scrolled = this; };
      window.Element.prototype.scrollBy = function () {};
      for (const [key, value] of Object.entries(storage)) window.localStorage.setItem(key, value);
      window.addEventListener('error', e => errors.push(e.error || e.message));
    },
  });
  dom.window.errors = errors;
  return dom.window;
}

const click = el => el.dispatchEvent(new el.ownerDocument.defaultView.MouseEvent('click', { bubbles: true }));
const key = (el, k) => el.dispatchEvent(new el.ownerDocument.defaultView.KeyboardEvent('keydown', { key: k, bubbles: true, cancelable: true }));

let accountNumber = 0;

/** A player of a log. Only the fields the page reads, in the shape TimelineDataBuilder writes. */
function player(account = `Player${++accountNumber}.${1000 + accountNumber}`, extra = {}) {
  return {
    account, profession: 'Guardian', icon: 'guardian.png', group: 1, isNpc: false,
    damage: 100000, dps: 1000, cc: 0, resAmount: 0, resTime: 0, downed: 0, died: 0,
    mechanics: {}, buffs: [], ...extra,
  };
}

/** A pull of a boss. start/end are "yyyy-MM-ddTHH:mm:ss" like the generator writes them. */
function log(name, start, end, extra = {}) {
  return {
    name, icon: '', id: 1, killed: true, start, end, url: 'https://dps.report/abc',
    hpLeft: [], columns: [], players: [player('A.1111'), player('B.2222')], ...extra,
  };
}

module.exports = { load, click, key, player, log };
