// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const path = require('path');

const webpack = require('@cratis/webpack/frontend');
module.exports = (env, argv) => {
    const isWebDevServer = (process.env.WEBPACK_DEV_SERVER || false) === 'true' ? true : false;
    const basePath = isWebDevServer ? '/' : './';
    return webpack(env, argv, basePath, config => {
        config.devServer.port = 9000;
    }, 'Cratis Portal');
};
