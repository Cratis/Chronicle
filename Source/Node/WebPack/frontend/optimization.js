// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
    minimizer: [
        new TerserPlugin({
            terserOptions: {
                sourceMap: false,

                // We want the class names and function names to be there for the IoC to work its magic
                keep_classnames: true,
                keep_fnames: true
            }
        })
    ],
    runtimeChunk: true,
    moduleIds: 'deterministic',
    splitChunks: {
        chunks: 'initial',
        // sizes are compared against source before minification
        maxSize: 200000, // splits chunks if bigger than 200k, adjust as required (maxSize added in webpack v4.15)
        cacheGroups: {
            default: false, // Disable the built-in groups default & vendors (vendors is redefined below)
            // This is the HTTP/1.1 optimised cacheGroup configuration
            vendors: {
                // picks up everything from node_modules as long as the sum of node modules is larger than minSize
                test: /[\\/]node_modules[\\/]/,
                name: 'vendors',
                priority: 19,
                enforce: true, // causes maxInitialRequests to be ignored, minSize still respected if specified in cacheGroup
                minSize: 30000 // use the default minSize
            },
            vendorsAsync: {
                // vendors async chunk, remaining asynchronously used node modules as single chunk file
                test: /[\\/]node_modules[\\/]/,
                name: 'vendors.async',
                chunks: 'async',
                priority: 9,
                reuseExistingChunk: true,
                minSize: 10000 // use smaller minSize to avoid too much potential bundle bloat due to module duplication.
            },
            commonsAsync: {
                // commons async chunk, remaining asynchronously used modules as single chunk file
                name: 'commons.async',
                minChunks: 2, // Minimum number of chunks that must share a module before splitting
                chunks: 'async',
                priority: 0,
                reuseExistingChunk: true,
                minSize: 10000 // use smaller minSize to avoid too much potential bundle bloat due to module duplication.
            }
        }
    }
};
