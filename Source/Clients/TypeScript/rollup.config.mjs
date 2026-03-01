// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import typescript2 from 'rollup-plugin-typescript2';
import commonjs from 'rollup-plugin-commonjs';
import peerDepsExternal from 'rollup-plugin-peer-deps-external';
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
        typescript2({
            tsconfig: "./tsconfig.json",
            clean: true
        })
    ]
};
