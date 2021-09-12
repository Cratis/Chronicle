// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const webpack = require('@cratis/webpack/frontend');
module.exports = (env, argv) => {
    return webpack(env, argv, '/', config => {
        config.devServer.proxy = {
            '/graphql': 'http://localhost:5000',
            '/graphql/ui': 'http://localhost:5000',
            '/api': 'http://localhost:5000',
            '/metrics': 'http://localhost:5000'
        };
        config.devServer.before = (app, server, compiler) => { };
    }, 9000, 'Cratis Management UI');
};
