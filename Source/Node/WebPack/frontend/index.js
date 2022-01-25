// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const output = require('./output');
const optimization = require('./optimization');
const resolve = require('./resolve');
const rules = require('./rules');
const plugins = require('./plugins');
const devServer = require('./devServer');

module.exports = (env, argv, basePath, callback, port, title) => {
    const production = argv.mode === 'production';
    basePath = basePath || '/';
    title = title;

    const config = {
        entry: './index.tsx',
        target: 'web',
        output: output(env, argv, basePath),
        optimization,
        resolve,
        module: {
            rules
        },
        plugins: plugins(basePath, title),
        devtool: production ? false : 'inline-source-map',
        devServer: devServer(basePath, port)
    };

    if (callback) {
        callback(config);
    }

    return config;
};
