// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fileTypes = /\.(js|json|css|html|png|jpg|jpeg|gif)$/;

module.exports = (basePath, port) => {
    const actualPort = process.env.port || port || 9000;

    return {
        historyApiFallback: { index: basePath },
        host: '0.0.0.0',
        port: actualPort,
        static: {
            publicPath: basePath,
            directory: process.cwd(),
        },
        liveReload: false,

        proxy: {
            '/api': 'http://localhost:5000',
            '/graphql': 'http://localhost:5000',
        },
        onAfterSetupMiddleware: (devServer) => {
            devServer.app.get('*', (req, res, next) => {
                const match = req.originalUrl.match(fileTypes);
                if (match && match.length > 0) {
                    next();
                    return;
                }
                const html = require('./HtmlInterceptorPlugin').getGeneratedHtml();
                res.send(html);
            });
        }
    };
};
