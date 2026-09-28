// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fromMarkdown } from 'mdast-util-from-markdown';
import { gfm } from 'micromark-extension-gfm';
import { gfmFromMarkdown } from 'mdast-util-gfm';
import { mdxjs } from 'micromark-extension-mdxjs';
import { mdxFromMarkdown } from 'mdast-util-mdx';
import { visit } from 'unist-util-visit';

function parsePage(content, isMdx) {
    // The site handles YAML frontmatter and converts DocFX xrefs before MDX parsing.
    // Retain newlines so code node positions still refer to the original page.
    const siteContent = content
        .replace(/^---[ \t]*\r?\n[\s\S]*?\r?\n---[ \t]*(?=\r?\n|$)/, match => match.replace(/[^\r\n]/g, ' '))
        .replace(/<xref:([^>\n]+)>/g, (_, inner) => `\`${inner.split('?')[0]}\``);
    return fromMarkdown(siteContent, {
        extensions: [gfm(), ...(isMdx ? [mdxjs()] : [])],
        mdastExtensions: [gfmFromMarkdown(), ...(isMdx ? [mdxFromMarkdown()] : [])]
    });
}

export function createClientFenceDetector(clientFenceLanguages) {
    return (content, isMdx = false) => {
        const violations = [];
        visit(parsePage(content, isMdx), 'code', node => {
            if (node.lang && clientFenceLanguages.has(node.lang.toLowerCase())) {
                violations.push(node.position.start.line);
            }
        });
        return violations;
    };
}

export function createChronicleSnippetReferenceDetector() {
    return (content, isMdx = false) => {
        const references = new Set();
        visit(parsePage(content, isMdx), 'mdxJsxFlowElement', node => {
            if (node.name !== 'ChronicleClientTabs') return;
            const attribute = name => node.attributes.find(candidate => candidate.type === 'mdxJsxAttribute' && candidate.name === name);
            const snippet = attribute('snippet');
            const variants = attribute('variants');
            // A variants list that leaves out csharp renders no C# tab, the same as the site's expansion.
            // An empty value renders every client too, as the site treats it like an omitted attribute.
            const includesCSharp = typeof variants?.value !== 'string'
                || variants.value === ''
                || variants.value.split(',').map(key => key.trim()).includes('csharp');
            if (typeof snippet?.value === 'string' && includesCSharp) references.add(snippet.value);
        });
        return references;
    };
}
