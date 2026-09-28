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
        if (entry.isDirectory() && entry.name !== 'node_modules') {
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

async function loadPageDetectors() {
    try {
        // Resolve parser dependencies from the verification toolchain outside the published docs root.
        const { createClientFenceDetector, createChronicleSnippetReferenceDetector } = await import('../.github/scripts/docs-verification/client-fence-detector.mjs');
        return {
            directClientFenceLines: createClientFenceDetector(clientFenceLanguages),
            snippetReferences: createChronicleSnippetReferenceDetector()
        };
    } catch {
        console.error('Documentation verifier dependencies are missing. Run: npm ci --prefix .github/scripts/docs-verification');
        process.exit(2);
    }
}

function selfTestClientFences(directClientFenceLines) {
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
        ['nested-list outdent', '1. outer\n   1. inner\n    ```csharp\n    example\n    ```', '3'],
        ['nested blockquote outdent', '> > inner\n> ```csharp\n> example\n> ```', '2'],
        ['tab list marker', '-\t```csharp\n\texample\n\t```', '1'],
        ['YAML frontmatter', '---\ntitle: "ConceptAs<T>"\n---\n```cs\nexample\n```', '4', true],
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
        ['tab-indented fence inside MDX Aside', '<Aside>\n\t```csharp\n\texample\n\t```\n</Aside>', '2', true],
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

function unusedClientSnippets(snippetFiles, referencedIds) {
    return snippetFiles.filter(file => {
        const id = path.relative(path.join(documentationRoot, 'client-snippets'), file).split(path.sep).join('/').replace(/\.mdx?$/i, '');
        return !id.startsWith('legacy/') && !referencedIds.has(id);
    });
}

function selfTestSnippetReferences(snippetReferences) {
    const referenced = snippetReferences('<ChronicleClientTabs snippet="example/referenced" variants="csharp" />\n```mdx\n<ChronicleClientTabs snippet="example/fenced" />\n```\n`<ChronicleClientTabs snippet="example/inline" />`', true);
    // References on client-specific pages count too, even though those pages skip the shared fence audit.
    for (const id of snippetReferences('<ChronicleClientTabs snippet="example/clients" />', true)) referenced.add(id);
    // A tab restricted to other clients renders no C# tab, so it does not use the C# snippet.
    for (const id of snippetReferences('<ChronicleClientTabs snippet="example/other-clients" variants="kotlin,java" />\n\n<ChronicleClientTabs snippet="example/mixed" variants="kotlin, csharp" />\n\n<ChronicleClientTabs snippet="example/empty-variants" variants="" />', true)) referenced.add(id);
    const snippetFiles = ['example/referenced.md', 'example/clients.md', 'example/mixed.md', 'example/empty-variants.md', 'example/other-clients.md', 'example/unused.mdx', 'example/fenced.md', 'example/inline.md', 'legacy/old.md']
        .map(file => path.join(documentationRoot, 'client-snippets', file));
    const unused = unusedClientSnippets(snippetFiles, referenced).map(file => path.basename(file));
    if (referenced.size !== 4 || !referenced.has('example/referenced') || !referenced.has('example/clients') || !referenced.has('example/mixed') || !referenced.has('example/empty-variants') || unused.join(',') !== 'other-clients.md,unused.mdx,fenced.md,inline.md') {
        console.error(`Client snippet self-test failed: references ${[...referenced]}; unused ${unused}.`);
        process.exit(1);
    }
    console.log(`Client snippet self-test detected ${unused.length} planted unused snippets and excluded four referenced and one legacy snippet.`);
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
const { directClientFenceLines, snippetReferences } = await loadPageDetectors();
if (process.argv[2] === '--self-test') {
    selfTestClientFences(directClientFenceLines);
    selfTestSnippetReferences(snippetReferences);
    process.exit(0);
}

const markdownFiles = await filesBelow(documentationRoot, name => /\.mdx?$/i.test(name));
const sharedPages = markdownFiles.filter(isSharedPage);
const snippetFiles = markdownFiles.filter(file => path.relative(documentationRoot, file).split(path.sep)[0] === 'client-snippets');
if (sharedPages.length === 0) errors.push('No shared Chronicle pages found; client fence audit cannot run.');
if (snippetFiles.every(file => path.relative(path.join(documentationRoot, 'client-snippets'), file).split(path.sep)[0] === 'legacy')) {
    errors.push('No non-legacy Chronicle client snippets found; unused snippet audit cannot run.');
}
const referencedIds = new Set();
const tocFiles = await filesBelow(documentationRoot, name => /^toc\.ya?ml$/i.test(name));
for (const file of markdownFiles) {
    const content = await readFile(file, 'utf8');
    validateContent(file, content);
    if (path.relative(documentationRoot, file).split(path.sep)[0] === 'client-snippets') continue;
    const isMdx = path.extname(file).toLowerCase() === '.mdx';
    try {
        for (const id of snippetReferences(content, isMdx)) referencedIds.add(id);
    } catch (error) {
        errors.push(`${relative(file)}:${error.line ?? 1}: Could not parse page for client snippet references: ${error.reason ?? error.message}`);
    }
    if (!isSharedPage(file)) continue;
    try {
        for (const line of directClientFenceLines(content, isMdx)) {
            errors.push(`${relative(file)}:${line}: Direct client-language fence in a shared page; use <ChronicleClientTabs> in .mdx.`);
        }
    } catch (error) {
        errors.push(`${relative(file)}:${error.line ?? 1}: Could not parse shared page for client-language fences: ${error.reason ?? error.message}`);
    }
}
for (const file of unusedClientSnippets(snippetFiles, referencedIds)) {
    errors.push(`${relative(file)}: Client snippet is not referenced by any ChronicleClientTabs snippet attribute on a page.`);
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
console.log(`Documentation authoring validation passed for ${markdownFiles.length} files (${markdownCount} .md, ${mdxCount} .mdx) and ${tocFiles.length} toc files; ${sharedPages.length} shared pages checked for client-language fences; ${snippetFiles.length} client snippets checked for references (${referencedIds.size} IDs referenced, legacy/ excluded).`);
