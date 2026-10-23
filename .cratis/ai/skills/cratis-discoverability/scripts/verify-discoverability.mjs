// cratis-ai-managed: skills/cratis-discoverability/scripts/verify-discoverability.mjs
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Checks a built static site (Astro dist/ or a plain-HTML repo root) before it is published:
// every indexable page has <html lang>, one <title>, a meta description, a canonical on the
// expected host and parseable JSON-LD; the site has robots.txt, a sitemap and (optionally) a
// well-formed llms.txt. Node standard library only.
//
// Usage: node verify-discoverability.mjs --root dist --site https://blog.cratis.io [--require-llms] [--warn-only]
// Exit codes: 0 clean or warnings only, 1 errors, 2 could not run.
import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const args = process.argv.slice(2);
const option = (name, fallback) => { const i = args.indexOf(name); return i < 0 ? fallback : args[i + 1]; };
const root = option('--root', 'dist');
const site = option('--site');
const requireLlms = args.includes('--require-llms');
const warnOnly = args.includes('--warn-only');
const ignore = (option('--ignore', 'node_modules,.git,.cratis,.github,404.html,_astro,storybook') ?? '').split(',');

if (!site || !existsSync(root)) {
    console.error(`could not run: need --site and an existing --root (got ${root})`);
    process.exit(2);
}
const host = new URL(site).host;
let errors = 0;
let warnings = 0;
const report = (level, file, message) => {
    if (level === 'error' && !warnOnly) errors++; else warnings++;
    console.log(`::${level === 'error' && !warnOnly ? 'error' : 'warning'} file=${file}::${message}`);
};

function* htmlFiles(dir) {
    for (const name of readdirSync(dir)) {
        if (ignore.includes(name)) continue;
        const path = join(dir, name);
        if (statSync(path).isDirectory()) yield* htmlFiles(path);
        else if (name.endsWith('.html')) yield path;
    }
}

const attr = (tag, name) => tag.match(new RegExp(`${name}\\s*=\\s*["']([^"']*)["']`, 'i'))?.[1];
const tags = (html, pattern) => [...html.matchAll(pattern)].map(match => match[0]);
const titles = new Map();

for (const path of htmlFiles(root)) {
    const file = relative(process.cwd(), path);
    const html = readFileSync(path, 'utf8');
    const metas = tags(html, /<meta\b[^>]*>/gi);
    const robots = metas.find(tag => /name\s*=\s*["']robots["']/i.test(tag));
    if (robots && /noindex/i.test(attr(robots, 'content') ?? '')) continue;      // deliberately not indexed
    if (/http-equiv\s*=\s*["']refresh["']/i.test(html)) continue;                 // redirect stub

    if (!/<html\b[^>]*\blang\s*=/i.test(html)) report('error', file, 'missing <html lang>');
    const title = html.match(/<title[^>]*>([\s\S]*?)<\/title>/i)?.[1]?.trim();
    if (!title) report('error', file, 'missing or empty <title>');
    else {
        if (title.length > 65) report('warning', file, `title is ${title.length} characters; keep the distinguishing words in the first 60`);
        titles.set(title, [...(titles.get(title) ?? []), file]);
    }
    const description = metas.find(tag => /name\s*=\s*["']description["']/i.test(tag));
    const descriptionText = description ? attr(description, 'content') ?? '' : '';
    if (!descriptionText) report('error', file, 'missing meta description');
    else if (descriptionText.length < 60 || descriptionText.length > 160) report('warning', file, `meta description is ${descriptionText.length} characters (aim for 60-160)`);
    const canonical = tags(html, /<link\b[^>]*>/gi).find(tag => /rel\s*=\s*["']canonical["']/i.test(tag));
    const href = canonical ? attr(canonical, 'href') : undefined;
    if (!href) report('error', file, 'missing <link rel="canonical">');
    else {
        try {
            const url = new URL(href);
            if (url.host !== host) report('error', file, `canonical points at ${url.host}, expected ${host}`);
        } catch {
            report('error', file, `canonical is not an absolute URL: ${href}`);
        }
    }
    if ((html.match(/<h1\b/gi) ?? []).length !== 1) report('warning', file, 'expected exactly one <h1>');
    for (const block of html.matchAll(/<script\b[^>]*type\s*=\s*["']application\/ld\+json["'][^>]*>([\s\S]*?)<\/script>/gi)) {
        try {
            const data = JSON.parse(block[1]);
            const nodes = Array.isArray(data) ? data : data['@graph'] ?? [data];
            if (!Array.isArray(data) && !String(data['@context'] ?? '').includes('schema.org')) report('error', file, 'JSON-LD without a schema.org @context');
            for (const node of nodes) if (!node['@type']) report('warning', file, 'JSON-LD node without @type');
        } catch (error) {
            report('error', file, `JSON-LD does not parse: ${error.message}`);
        }
    }
}

for (const [title, files] of titles) if (files.length > 1) report('warning', files[0], `title "${title}" is shared by ${files.length} pages`);

const robotsPath = join(root, 'robots.txt');
if (!existsSync(robotsPath)) report('error', root, 'no robots.txt');
else if (!/^\s*sitemap\s*:/im.test(readFileSync(robotsPath, 'utf8'))) report('warning', robotsPath, 'robots.txt does not declare a Sitemap');
if (!readdirSync(root).some(name => /^sitemap.*\.xml$/.test(name))) report('error', root, 'no sitemap*.xml at the site root');

const llmsPath = join(root, 'llms.txt');
if (existsSync(llmsPath)) {
    // Same structure rules as vendor/claude-seo/scripts/agentic_check.py evaluate_llms_txt (Lighthouse llms-txt audit).
    const llms = readFileSync(llmsPath, 'utf8').replace(/^\uFEFF/, '');
    if (!/^\s*#\s+.+/m.test(llms)) report('error', llmsPath, 'llms.txt has no H1');
    if (!/\[.+\]\(.+\)/.test(llms)) report('error', llmsPath, 'llms.txt has no Markdown links');
    if (!/^\s*>\s+\S/m.test(llms)) report('warning', llmsPath, "llms.txt has no '>' summary");
} else if (requireLlms) report('error', root, 'no llms.txt');

console.log(`verify-discoverability: ${errors} error(s), ${warnings} warning(s)`);
process.exit(errors ? 1 : 0);
