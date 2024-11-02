// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Resource for setting up Mocha with TS++ : https://www.delftstack.com/howto/typescript/mocha-typescript/

module.exports = {
    require: [
        'ts-node/register',
        'tsconfig-paths/register',
        __dirname+'/mocha-setup.js'],
    diff: true,
    "node-option": [
        "experimental-specifier-resolution=node",
        "loader=./loader.js"
    ],
    extensions: ['ts', 'tsx'],
    spec: ['**/for_*/**/*.ts'],
    ignore: ['**/node_modules/**/*', '**/*.d.ts']
}
