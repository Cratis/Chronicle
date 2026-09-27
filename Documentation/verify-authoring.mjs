// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const documentationRoot = path.dirname(fileURLToPath(import.meta.url));
const validAsideVariants = new Set(['note', 'tip', 'caution', 'danger']);
const orphanDirectoryExclusions = new Set(['_includes', 'client-snippets']);
// Keep these names and aliases in step with Documentation/web/variant-docs.yml's Chronicle ratchetLanguages.
const clientFenceLanguages = new Set(['csharp', 'cs', 'c#', 'java', 'kotlin', 'kt', 'kts', 'elixir', 'ex', 'exs', 'typescript', 'ts', 'tsx']);
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

function isSharedPage(file) {
    const firstDirectory = path.relative(documentationRoot, file).split(path.sep)[0];
    return firstDirectory !== 'client-snippets' && firstDirectory !== 'clients';
}

function directClientFenceLines(content, isMdx = false) {
    const violations = [];
    const containers = [];
    let fence;
    let paragraph = false;
    for (const [index, line] of content.split('\n').entries()) {
        if (!line.trim()) {
            paragraph = false;
            continue;
        }

        let remainder = line;
        for (let position = 0; position < containers.length; position++) {
            const container = containers[position];
            const prefix = container.type === 'quote'
                ? remainder.match(/^ {0,3}> ?/)
                : remainder.match(new RegExp(`^ {${container.indent}}`));
            if (!prefix) {
                // A paragraph can continue a list item without repeating its indentation.
                // Keep the list context so a fenced block after a blank line remains in it.
                if (!fence && paragraph && !/^ {0,3}(?:>|[-+*] +|\d{1,9}[.)] +|#{1,6}(?:\s|$)|`{3,}|~{3,})/.test(line)) {
                    remainder = undefined;
                    break;
                }
                containers.length = position;
                fence = undefined;
                break;
            }
            remainder = remainder.slice(prefix[0].length);
        }
        if (remainder === undefined) continue;

        if (!fence) {
            while (true) {
                const quote = remainder.match(/^ {0,3}> ?/);
                if (quote) {
                    containers.push({ type: 'quote' });
                    remainder = remainder.slice(quote[0].length);
                    continue;
                }

                const list = remainder.match(/^( {0,3})([-+*]|\d{1,9}[.)])( +)/);
                if (!list) break;
                // A list item's content starts after 1–4 spaces; more than 4
                // leaves the excess as indentation (possibly an indented code block).
                const padding = list[3].length <= 4 ? list[3].length : 1;
                containers.push({ type: 'list', indent: list[1].length + list[2].length + padding });
                remainder = remainder.slice(list[1].length + list[2].length + padding);
            }
        }

        // MDX disables indented code blocks: even a fence indented four spaces
        // inside an MDX component or list is parsed as a fenced code block.
        const match = remainder.match(isMdx
            ? /^ *(`{3,}|~{3,})[ \t]*([^\s`~]*)(.*)$/
            : /^ {0,3}(`{3,}|~{3,})[ \t]*([^\s`~]*)(.*)$/);
        if (!match) {
            paragraph = !fence && remainder.trim().length > 0;
            continue;
        }

        paragraph = false;
        const marker = match[1];
        if (!fence) {
            fence = { character: marker[0], length: marker.length };
            if (clientFenceLanguages.has(match[2].toLowerCase())) violations.push(index + 1);
        } else if (marker[0] === fence.character && marker.length >= fence.length &&
            match[2] === '' && /^[ \t]*$/.test(match[3])) {
            fence = undefined;
        }
    }
    return violations;
}

function selfTestClientFences() {
    const planted = [...clientFenceLanguages].map(language => `\`\`\`${language}\nexample\n\`\`\``).join('\n\n');
    const found = directClientFenceLines(planted).length;
    const nested = directClientFenceLines('````md\n```csharp\nexample\n````\n').length;
    const infoStringIsNotACloser = directClientFenceLines('```md\n```tsx\n``` \n```java\nexample\n```');
    const tildeInfoStringIsNotACloser = directClientFenceLines('~~~md\n~~~c#\n~~~ \n~~~kt\nexample\n~~~');
    const containerCases = [
        ['blockquote', '> ```csharp\n> example\n> ```', '1'],
        ['nested blockquote', '> > ~~~java\n> > example\n> > ~~~', '1'],
        ['bullet list', '- ```kotlin\n  example\n  ```', '1'],
        ['ordered list', '1. ```elixir\n   example\n   ```', '1'],
        ['list continuation', '- item\n\n  ```tsx\n  example\n  ```', '3'],
        ['nested list', '- item\n  1. ```cs\n     example\n     ```', '2'],
        ['quote and list', '> - ```typescript\n>   example\n>   ```', '1'],
        ['nested fence in quote', '> ````md\n> ```csharp\n> ````', ''],
        ['info-string closer in quote', '> ```md\n> ```tsx\n> ``` \n> ```java\n> example\n> ```', '4'],
        ['info-string closer in list', '- ~~~md\n  ~~~kt\n  ~~~ \n  ~~~c#\n  example\n  ~~~', '4'],
        ['indented code block', '    ```csharp\n    example\n    ```', ''],
        ['indented list content', '-     ```csharp\n      example', ''],
        ['sibling list item', '- ```text\n- ```java\n  example\n  ```', '2'],
        ['lazy nested-list continuation', '- outer\n  - inner\nlazy continuation\n\n    ```csharp\n    example\n    ```', '5'],
        ['lazy continuation then sibling', '- outer\n  - inner\nlazy continuation\n\n- sibling\n    ```csharp\n    example\n    ```', '6'],
        ['indented fence in MDX', '    ```csharp\n    example\n    ```', '1', true],
        ['indented fence inside MDX Aside', '<Aside>\n    ```ts\n    example\n    ```\n</Aside>', '2', true],
        ['indented fence inside MDX TabItem', '<TabItem>\n    ~~~java\n    example\n    ~~~\n</TabItem>', '2', true],
        ['indented fence inside Markdown Aside', '<Aside>\n    ```ts\n    example\n    ```\n</Aside>', '', false],
        ['indented fence inside Markdown TabItem', '<TabItem>\n    ~~~java\n    example\n    ~~~\n</TabItem>', '', false],
        ['lazy continuation with MDX indentation', '- outer\n  - inner\nlazy continuation\n\n        ```csharp\n        example\n        ```', '5', true]
    ];
    const failedCase = containerCases.find(([, input, expected, isMdx]) => directClientFenceLines(input, isMdx).join(',') !== expected);
    const excluded = ['client-snippets/example.md', 'clients/dotnet/example.md']
        .every(file => !isSharedPage(path.join(documentationRoot, file)));
    if (found !== clientFenceLanguages.size || nested !== 0 || failedCase ||
        infoStringIsNotACloser.join(',') !== '4' || tildeInfoStringIsNotACloser.join(',') !== '4' || !excluded) {
        console.error(`Client fence self-test failed: detected ${found} of ${clientFenceLanguages.size} planted fences; nested: ${nested}; info-string closer: ${infoStringIsNotACloser}; tilde closer: ${tildeInfoStringIsNotACloser}; container case: ${failedCase?.[0] ?? 'none'}; excluded paths: ${excluded}.`);
        process.exit(1);
    }
    console.log(`Client fence self-test detected ${found} planted fences across ${clientFenceLanguages.size} languages and passed ${containerCases.length} container cases.`);
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

if (process.argv.length > 3 || (process.argv[2] && process.argv[2] !== '--self-test')) {
    console.error('Usage: node Documentation/verify-authoring.mjs [--self-test]');
    process.exit(2);
}
if (process.argv[2] === '--self-test') {
    selfTestClientFences();
    process.exit(0);
}

const markdownFiles = await filesBelow(documentationRoot, name => /\.mdx?$/i.test(name));
const sharedPages = markdownFiles.filter(isSharedPage);
if (sharedPages.length === 0) errors.push('No shared Chronicle pages found; client fence audit cannot run.');
const tocFiles = await filesBelow(documentationRoot, name => /^toc\.ya?ml$/i.test(name));
for (const file of markdownFiles) {
    const content = await readFile(file, 'utf8');
    validateContent(file, content);
    if (!isSharedPage(file)) continue;
    for (const line of directClientFenceLines(content, path.extname(file).toLowerCase() === '.mdx')) {
        errors.push(`${relative(file)}:${line}: Direct client-language fence in a shared page; use <ChronicleClientTabs> in .mdx.`);
    }
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
console.log(`Documentation authoring validation passed for ${markdownFiles.length} files (${markdownCount} .md, ${mdxCount} .mdx) and ${tocFiles.length} toc files; ${sharedPages.length} shared pages checked for client-language fences.`);
