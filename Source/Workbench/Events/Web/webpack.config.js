// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const path = require('path');

const webpack = require('@cratis/webpack/frontend');
module.exports = (env, argv) => {
    const isWebDevServer = (process.env.WEBPACK_DEV_SERVER || false) === 'true' ? true : false;
    const basePath = isWebDevServer ? '/' : process.env.base_path || './';
    return webpack(env, argv, basePath, config => {
        config.devServer.port = 9000;
        config.devServer.proxy = {
            '/graphql': 'http://localhost:5000',
            '/api': 'http://localhost:5000'
        }
    }, 'Cratis Events Workbench');
};
