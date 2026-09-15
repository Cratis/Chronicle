// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const documentationRoot = path.dirname(fileURLToPath(import.meta.url));
const validAsideVariants = new Set(['note', 'tip', 'caution', 'danger']);
const orphanDirectoryExclusions = new Set(['_includes', 'client-snippets']);
const errors = [];

async function filesBelow(directory, predicate) {
    const files = [];
    for (const entry of await readdir(directory, { withFileTypes: true })) {
        const entryPath = path.join(directory, entry.name);
        if (entry.isDirectory()) {
            files.push(...await filesBelow(entryPath, predicate));
        } else if (predicate(entry.name)) {
            files.push(entryPath);
        }
    }

    return files;
}

function relative(file) {
    return path.relative(path.dirname(documentationRoot), file).split(path.sep).join('/');
}

function withoutInlineCode(line) {
    return line.replace(/(`+)(.*?)\1/g, '');
}

function validateContent(file, content) {
    const isMarkdown = path.extname(file).toLowerCase() === '.md';
    let fence;

    for (const [index, line] of content.split('\n').entries()) {
        const fenceMatch = line.match(/^\s*(`{3,}|~{3,})/);
        if (fenceMatch) {
            const marker = fenceMatch[1];
            if (!fence) {
                fence = { character: marker[0], length: marker.length };
            } else if (marker[0] === fence.character && marker.length >= fence.length) {
                fence = undefined;
            }
            continue;
        }

        if (fence) continue;

        const asideMatch = line.match(/^\s*:::(\w[\w-]*)/);
        if (asideMatch && !validAsideVariants.has(asideMatch[1])) {
            errors.push(`${relative(file)}:${index + 1}: Unknown Starlight aside variant '${asideMatch[1]}'. Use note, tip, caution, or danger.`);
        }

        if (isMarkdown && /^\s*import\s+(?:.+\s+from\s+)?['"]/.test(line)) {
            errors.push(`${relative(file)}:${index + 1}: Imports require .mdx; in .md they render as visible prose.`);
        }

        if (isMarkdown) {
            const componentMatch = line.match(/^\s*<\/?([A-Z][A-Za-z0-9.]*)\b/);
            if (componentMatch) {
                errors.push(`${relative(file)}:${index + 1}: <${componentMatch[1]}> requires .mdx; in .md it renders as an inert element.`);
            }
        }

        const authoringLine = withoutInlineCode(line);
        if (authoringLine.includes('<ChronicleClientTabs')) {
            if (isMarkdown) {
                errors.push(`${relative(file)}:${index + 1}: <ChronicleClientTabs> requires a .mdx source file.`);
            }

            if (!/^\s*<ChronicleClientTabs\s+[^<>]*\/>\s*$/.test(authoringLine)) {
                errors.push(`${relative(file)}:${index + 1}: <ChronicleClientTabs> must be a self-closing component on its own line.`);
            }
        }
    }
}

async function validateLandingCollisions(files) {
    for (const file of files) {
        const extension = path.extname(file);
        const possibleDirectory = file.slice(0, -extension.length);
        let directoryStats;
        try {
            directoryStats = await stat(possibleDirectory);
        } catch {
            continue;
        }

        if (!directoryStats.isDirectory()) continue;

        const entries = await readdir(possibleDirectory);
        if (entries.some(entry => /^index\.mdx?$/i.test(entry))) {
            errors.push(`${relative(file)}: Conflicts with ${relative(possibleDirectory)}/index.md[x]. The site demotes the directory index to /overview/ and can orphan it; keep one landing page for the route.`);
        }
    }
}

function unquote(value) {
    const trimmed = value.trim();
    if ((trimmed.startsWith('"') && trimmed.endsWith('"')) ||
        (trimmed.startsWith("'") && trimmed.endsWith("'"))) {
        return trimmed.slice(1, -1);
    }
    return trimmed;
}

function tocEntries(content) {
    const lines = content.split('\n');
    const entries = [];

    for (let index = 0; index < lines.length; index++) {
        const itemMatch = lines[index].match(/^(\s*)-\s+name\s*:/);
        if (!itemMatch) continue;

        const indentation = itemMatch[1].length;
        const entry = { line: index + 1 };
        for (let propertyIndex = index + 1; propertyIndex < lines.length; propertyIndex++) {
            const line = lines[propertyIndex];
            const nextItem = line.match(/^(\s*)-\s+/);
            if (nextItem && nextItem[1].length <= indentation) break;

            const propertyMatch = line.match(/^(\s*)(href|items)\s*:\s*(.*)$/);
            if (!propertyMatch || propertyMatch[1].length !== indentation + 2) continue;

            if (propertyMatch[2] === 'href') {
                entry.href = unquote(propertyMatch[3]);
                entry.hrefLine = propertyIndex + 1;
            } else {
                entry.itemsLine = propertyIndex + 1;
            }
        }
        entries.push(entry);
    }

    return entries;
}

function isExternalHref(href) {
    return /^(?:https?:)?\/\//i.test(href) || href.startsWith('/');
}

async function validateTocs(tocFiles) {
    const references = new Map();

    for (const tocFile of tocFiles) {
        const entries = tocEntries(await readFile(tocFile, 'utf8'));
        for (const entry of entries) {
            if (entry.href && entry.itemsLine) {
                errors.push(`${relative(tocFile)}:${entry.line}: A toc entry cannot combine href and items; use a group or a leaf.`);
            }
            if (!entry.href) continue;

            const cleanHref = entry.href.split('#')[0].split('?')[0];
            if (cleanHref.split('/').includes('..')) {
                errors.push(`${relative(tocFile)}:${entry.hrefLine}: Toc href '${entry.href}' escapes its section with '..'; link to the section landing instead.`);
                continue;
            }
            if (isExternalHref(cleanHref)) continue;

            const target = path.resolve(path.dirname(tocFile), cleanHref);
            const relativeTarget = path.relative(documentationRoot, target);
            if (relativeTarget.startsWith(`..${path.sep}`) || path.isAbsolute(relativeTarget)) {
                errors.push(`${relative(tocFile)}:${entry.hrefLine}: Toc href '${entry.href}' escapes the Documentation root.`);
                continue;
            }

            let targetStats;
            try {
                targetStats = await stat(target);
            } catch {
                errors.push(`${relative(tocFile)}:${entry.hrefLine}: Toc href '${entry.href}' does not resolve to a file.`);
                continue;
            }
            if (!targetStats.isFile()) {
                errors.push(`${relative(tocFile)}:${entry.hrefLine}: Toc href '${entry.href}' does not resolve to a file.`);
                continue;
            }

            const uses = references.get(target) ?? [];
            uses.push(`${relative(tocFile)}:${entry.hrefLine}`);
            references.set(target, uses);
        }
    }

    for (const [target, uses] of references) {
        if (uses.length > 1) {
            errors.push(`${uses.join(', ')}: Duplicate toc href for ${relative(target)}.`);
        }
    }

    return references;
}

function isOrphanExcluded(file) {
    const parts = path.relative(documentationRoot, file).split(path.sep);
    return parts.some(part => orphanDirectoryExclusions.has(part));
}

function validateOrphans(files, references) {
    for (const file of files) {
        if (isOrphanExcluded(file)) continue;
        if (!references.has(file)) {
            errors.push(`${relative(file)}: Documentation page is not referenced by any toc.yml.`);
        }
    }
}

const markdownFiles = await filesBelow(documentationRoot, name => /\.mdx?$/i.test(name));
const tocFiles = await filesBelow(documentationRoot, name => /^toc\.ya?ml$/i.test(name));
for (const file of markdownFiles) {
    validateContent(file, await readFile(file, 'utf8'));
}
await validateLandingCollisions(markdownFiles);
const references = await validateTocs(tocFiles);
validateOrphans(markdownFiles, references);

if (errors.length > 0) {
    console.error('Documentation authoring validation failed:');
    for (const error of errors) console.error(`  - ${error}`);
    process.exit(1);
}

const markdownCount = markdownFiles.filter(file => path.extname(file).toLowerCase() === '.md').length;
const mdxCount = markdownFiles.length - markdownCount;
console.log(`Documentation authoring validation passed for ${markdownFiles.length} files (${markdownCount} .md, ${mdxCount} .mdx) and ${tocFiles.length} toc files.`);
