// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import typescript2 from 'rollup-plugin-typescript2';
import commonjs from 'rollup-plugin-commonjs';
import peerDepsExternal from 'rollup-plugin-peer-deps-external';
import * as typescriptLegacy from 'typescript-legacy';
import { readFileSync } from 'fs';

const pkg = JSON.parse(readFileSync('./package.json', 'utf-8'));

export default {
    input: "index.ts",

    output: [
        {
            dir: "dist/cjs",
            format: "cjs",
            exports: "named",
            sourcemap: true,
            preserveModules: true,
            preserveModulesRoot: "."
        },
        {
            dir: "dist/esm",
            format: "es",
            exports: "named",
            sourcemap: true,
            preserveModules: true,
            preserveModulesRoot: "."
        }
    ],
    external: [
        ...Object.keys(pkg.dependencies || {}),
        ...Object.keys(pkg.peerDependencies || {}),
    ],
    plugins: [
        peerDepsExternal(),
        commonjs({
            include: /node_modules/,
            esmExternals: true,
        }),
        // rollup-plugin-typescript2 needs the classic TypeScript Program/LanguageService API for
        // declaration bundling, which TS7's native compiler package no longer exposes. Source is
        // still authored against and type-checked by TS7 (see the "build" script's `tsc -b` step);
        // this pinned legacy instance is only used internally by the plugin to emit dist/.
        typescript2({
            tsconfig: "./tsconfig.json",
            clean: true,
            typescript: typescriptLegacy
        })
    ]
};
