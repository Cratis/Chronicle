// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFile } from 'node:fs/promises';

// This deliberately recognizes common absence phrasing, not every possible SDK claim.
const clientNames = [
    /(?:\bC#(?!\w)|\.NET\b|\b(?:Kotlin|Java|JVM|TypeScript|Elixir)\b)/i,
    /\bNode(?:\.js)?\b/,
    /\bnot shown for that client\b/i
];
const explicitAbsence = /-only\b|\bno (?:equivalent|support|API)\b|\b(?:has|have) no\b|\b(?:do|does) not\b|\b(?:don't|doesn't)\b/i;
const capabilityAbsence = /-only\b|\bonly (?:available|supported|in|for)\b|\bno (?:equivalent|support|API)\b|\bnot (?:currently |yet )?(?:supported|available|implemented|shown|exposed)\b|\b(?:don't|doesn't|do not|does not) (?:currently |yet )?(?:support|have|expose|provide|discover|offer|generate)\b|\b(?:cannot|can't|lacks?|unsupported)\b|\b(?:has|have) no\b/i;

function blankLines(text) {
    return text.replace(/[^\n]/g, ' ');
}

function closesProseFence(marker, fence) {
    if (!marker || marker[2].trim()) return false;
    return marker[1][0] === fence[0] && marker[1].length >= fence.length;
}

function withoutProseFences(lines) {
    let fence;
    return lines.map(line => {
        if (/^\s*>/.test(line)) return '';
        const marker = line.match(/^\s*(`{3,}|~{3,})(.*)$/);
        if (fence) {
            if (closesProseFence(marker, fence)) fence = undefined;
            return '';
        }
        if (!marker) return line;
        fence = marker[1];
        return '';
    });
}

function proseLines(content, isMdx) {
    const stripped = content.replace(/^---\r?\n[\s\S]*?\r?\n---(?:\r?\n|$)/, blankLines)
        .replace(/<!--[\s\S]*?-->/g, blankLines)
        .replace(/"[^"\n]*"/g, blankLines);
    const lines = withoutProseFences(stripped.split('\n'));
    return lines.map(line => {
        if (!isMdx && /^(?: {4}|\t)/.test(line)) return '';
        return line.replace(/(`+)(.*?)\1/g, '');
    });
}

function proseParagraphs(lines) {
    const paragraphs = [];
    let paragraph;
    for (const [index, line] of lines.entries()) {
        if (!line.trim()) {
            paragraph = undefined;
            continue;
        }
        if (/^\s*(?:\||[-*+]\s|#{1,6}\s|:::|<)/.test(line)) paragraph = undefined;
        if (!paragraph) {
            paragraph = { line: index + 1, text: '' };
            paragraphs.push(paragraph);
        }
        paragraph.text += `${line.trim()} `;
        if (/^\s*\|/.test(line)) paragraph = undefined;
    }
    return paragraphs;
}

function isClientCapabilityClaim(text) {
    if (!clientNames.some(pattern => pattern.test(text))) return false;
    if (!capabilityAbsence.test(text)) return false;
    // These describe packaging, diagnostics, or missing application types, not missing SDK workflows.
    if (/\bruntime-only\b|\banalyzers report unsupported\b|\b(?:has|have) no \.NET (?:event classes|type)\b/i.test(text)) return false;
    // Positive descriptions of a client's mechanism are differences, unless explicitly negated.
    return explicitAbsence.test(text) || !/\b(?:uses?|exposes?|supports?|reaches|accepts|expresses)\b/i.test(text);
}

export function findClientCapabilityClaims(content, isMdx) {
    return proseParagraphs(proseLines(content, isMdx)).flatMap(paragraph =>
        paragraph.text.trim().split(/(?<=[.!?])\s+(?!NET\b|js\b)/).filter(isClientCapabilityClaim)
            .map(text => ({ line: paragraph.line, text })));
}

function staleCapabilityBaseline(baseline, file, count) {
    if (!Object.hasOwn(baseline, file)) return false;
    return count < baseline[file] || baseline[file] === 0;
}

function compareCapabilityBaseline(claimsByFile, baseline) {
    const failures = [];
    for (const file of new Set([...Object.keys(claimsByFile), ...Object.keys(baseline)])) {
        const claims = claimsByFile[file] ?? [];
        const allowed = baseline[file] ?? 0;
        if (claims.length > allowed) {
            for (const claim of claims) failures.push(`${file}:${claim.line}: Client-capability claim '${claim.text}'; state absence in the client repo's unsupported snippet (contributing/clients). New claim not allowed.`);
        }
        if (staleCapabilityBaseline(baseline, file, claims.length)) {
            failures.push(`${file}: Baseline is stale: lower the baseline to ${claims.length} (remove entry if zero).`);
        }
    }
    return failures;
}

function validCapabilityBaseline(baseline) {
    if (baseline === null) return false;
    if (Array.isArray(baseline)) return false;
    if (typeof baseline !== 'object') return false;
    return Object.values(baseline).every(count => Number.isInteger(count) && count >= 0);
}

export async function validateCapabilityBaseline(claimsByFile, capabilityBaselinePath, errors) {
    let baseline;
    try {
        baseline = JSON.parse(await readFile(capabilityBaselinePath, 'utf8'));
    } catch (error) {
        errors.push(`Could not read client-capability baseline: ${error.message}`);
        return;
    }
    if (!validCapabilityBaseline(baseline)) {
        errors.push('Client-capability baseline must be an object of per-file nonnegative integer counts.');
        return;
    }
    errors.push(...compareCapabilityBaseline(claimsByFile, baseline));
    const count = Object.values(claimsByFile).reduce((sum, claims) => sum + claims.length, 0);
    const allowed = Object.values(baseline).reduce((sum, value) => sum + value, 0);
    console.log(`Client-capability claims: ${count}; baseline: ${allowed} across ${Object.keys(baseline).length} files.`);
}

export function selfTestClientCapabilityClaims(isSharedPage) {
    const cases = [
        ['Kotlin and Java have no equivalent.', 1],
        ['This is currently C#-only.', 1],
        ['This example is not shown for that client.', 1],
        ['Elixir does not support X.', 1],
        ["TypeScript doesn't have Y yet.", 1],
        ['This is only available in .NET.', 1],
        ['Kotlin has no API.', 1],
        ['TypeScript reaches the same result through the decorator\'s parameter.', 0],
        ['Kotlin uses `@Subject`.', 0],
        ['.NET rejects X.', 0],
        ['In .NET a class must implement only one interface.', 0],
        ['JavaScript has no equivalent.', 0],
        ['Kotlin uses a decorator; Elixir does not support X.', 1],
        ['TypeScript accepts a parameter not available as a property.', 0],
        ['> ❌ Kotlin has no equivalent.', 0],
        ['```text\nKotlin has no equivalent.\n```', 0],
        ['~~~~text\n~~~\nKotlin has no equivalent.\n~~~~', 0],
        ['````text\n```\nKotlin has no equivalent.\n````', 0],
        ['`Kotlin has no equivalent.`', 0],
        ['---\ntitle: Kotlin has no equivalent\n---\nText.', 0],
        ['<!-- Kotlin has\nno equivalent. -->', 0],
        ['TypeScript and Elixir don\'t have an\nequivalent yet.', 1],
        ['<Aside>\nElixir does not support X.\n</Aside>', 1],
        ['.NET rejects X. Kotlin has no equivalent.', 1],
        ['| Kotlin | No support |', 1],
        ['| No equivalent |\n| .NET namespace |', 0],
        ['A node cannot exist without edges.', 0],
        ['The .NET client uses runtime-only packaging.', 0],
        ['❌ "Kotlin has no equivalent."', 0],
        ['> ❌ "Kotlin has no equivalent."', 0],
        ['The .NET analyzers report unsupported signatures.', 0],
        ['A plugin has no .NET event classes.', 0]
    ];
    const failed = cases.find(([input, expected]) => findClientCapabilityClaims(input, true).length !== expected);
    const excluded = ['client-snippets/example.md', 'clients/dotnet/example.md']
        .every(file => !isSharedPage(file));
    const sample = [{ line: 1, text: 'Kotlin has no equivalent.' }];
    const baselineCases = [
        [{ page: sample }, {}, 1],
        [{ page: [...sample, ...sample] }, { page: 1 }, 2],
        [{ page: [] }, { page: 1 }, 1],
        [{}, { page: 1 }, 1],
        [{ page: sample }, { page: 1 }, 0],
        [{ page: sample, other: [] }, { other: 1 }, 2]
    ];
    const failedBaseline = baselineCases.find(([claims, baseline, expected]) => compareCapabilityBaseline(claims, baseline).length !== expected);
    const passed = [!failed, !failedBaseline, excluded].every(Boolean);
    if (!passed) {
        console.error(`Client-capability self-test failed: ${failed?.[0] ?? 'baseline comparison or excluded paths'}.`);
        process.exit(1);
    }
    console.log(`Client-capability self-test passed ${cases.length} prose cases and ${baselineCases.length} baseline cases, including excluded paths.`);
}
