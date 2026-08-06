// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// TypeScript 7 ships a native (Go) compiler whose npm package no longer exposes
// the programmatic compiler API (ts.createSourceFile / ts.SyntaxKind / …), so this
// script parses with @swc/core instead, matching the approach @cratis/arc.vite's
// EmitMetadataPlugin takes for the same reason.

import { promises as fs } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseSync } from '@swc/core';

const generatedModules = [
    'clients',
    'events',
    'events_constraints',
    'eventsequences',
    'eventstores',
    'host',
    'identities',
    'jobs',
    'namespaces',
    'observation',
    'observation_reactors',
    'observation_reducers',
    'observation_webhooks',
    'projections',
    'readmodels',
    'recommendations',
    'security',
    'seeding',
    'protobuf-net/bcl'
];

const currentDirectory = path.dirname(fileURLToPath(import.meta.url));
const generatedDirectory = path.join(currentDirectory, 'generated');
const generatedIndexPath = path.join(generatedDirectory, 'index.ts');

/**
 * Gets the exported symbols for a generated TypeScript module.
 * @param {string} filePath The absolute path to the generated TypeScript file.
 * @returns {Promise<Map<string, 'type' | 'value'>>} A map of exported symbol names and their export kind.
 */
const getExportedSymbols = async (filePath) =>
{
    const sourceText = await fs.readFile(filePath, 'utf8');
    const module = parseSync(sourceText, { syntax: 'typescript', tsx: false });
    const exportedSymbols = new Map();

    /**
     * Tracks an exported symbol and prefers value exports when the same symbol also has a type export.
     * @param {string} name The exported symbol name.
     * @param {'type' | 'value'} kind The kind of export.
     */
    const addExport = (name, kind) =>
    {
        const currentKind = exportedSymbols.get(name);
        exportedSymbols.set(name, currentKind === 'value' || kind === 'value' ? 'value' : 'type');
    };

    for (const statement of module.body)
    {
        if (statement.type === 'ExportNamedDeclaration')
        {
            for (const specifier of statement.specifiers)
            {
                if (specifier.type !== 'ExportSpecifier')
                {
                    continue;
                }

                const name = (specifier.exported ?? specifier.orig).value;
                addExport(name, statement.typeOnly || specifier.isTypeOnly ? 'type' : 'value');
            }
            continue;
        }

        if (statement.type !== 'ExportDeclaration')
        {
            continue;
        }

        const declaration = statement.declaration;

        switch (declaration.type)
        {
            case 'TsInterfaceDeclaration':
            case 'TsTypeAliasDeclaration':
                addExport(declaration.id.value, 'type');
                break;

            case 'TsEnumDeclaration':
            case 'ClassDeclaration':
            case 'FunctionDeclaration':
                addExport((declaration.id ?? declaration.identifier).value, 'value');
                break;

            case 'VariableDeclaration':
                for (const variableDeclaration of declaration.declarations)
                {
                    if (variableDeclaration.id.type === 'Identifier')
                    {
                        addExport(variableDeclaration.id.value, 'value');
                    }
                }
                break;
        }
    }

    return exportedSymbols;
};

/**
 * Gets the generated exports for a module after removing already claimed duplicate symbols.
 * @param {string} modulePath The generated module path relative to the generated directory.
 * @param {Set<string>} claimedSymbols The symbols that are already exported by earlier modules.
 * @returns {Promise<{ valueExports: string[]; typeExports: string[]; }>} The module exports that should be kept.
 */
const getModuleExports = async (modulePath, claimedSymbols) =>
{
    const filePath = path.join(generatedDirectory, `${modulePath}.ts`);
    const exportedSymbols = await getExportedSymbols(filePath);
    const valueExports = [];
    const typeExports = [];

    for (const [name, kind] of [...exportedSymbols.entries()].sort(([left], [right]) => left.localeCompare(right)))
    {
        if (claimedSymbols.has(name))
        {
            continue;
        }

        claimedSymbols.add(name);

        if (kind === 'value')
        {
            valueExports.push(name);
        }
        else
        {
            typeExports.push(name);
        }
    }

    return { valueExports, typeExports };
};

const claimedSymbols = new Set();
const lines = [
    '// Copyright (c) Cratis. All rights reserved.',
    '// Licensed under the MIT license. See LICENSE file in the project root for full license information.',
    '',
    '// This file is generated by generate-generated-index.mjs. Do not edit manually.',
    ''
];

for (const modulePath of generatedModules)
{
    const { valueExports, typeExports } = await getModuleExports(modulePath, claimedSymbols);

    if (valueExports.length)
    {
        lines.push(`export { ${valueExports.join(', ')} } from './${modulePath}';`);
    }

    if (typeExports.length)
    {
        lines.push(`export type { ${typeExports.join(', ')} } from './${modulePath}';`);
    }

    if (valueExports.length || typeExports.length)
    {
        lines.push('');
    }
}

await fs.writeFile(generatedIndexPath, `${lines.join('\n').trimEnd()}\n`);

console.log(`Generated ${generatedIndexPath}`);
