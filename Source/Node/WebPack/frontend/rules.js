// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

module.exports = [
    {
        test: /\.[tj]s[x]*$/i,
        exclude: /(node_modules)/,
        loader: 'ts-loader',
        options: {
            transpileOnly: true,
            projectReferences: true,
            allowTsInNodeModules: true
        }
    },
    {
        test: /\.s[ac]ss$/i,
        issuer: /\.[tj]s[x]*$/i,
        use: [
            // Creates `style` nodes from JS strings
            'style-loader',
            // Translates CSS into CommonJS
            'css-loader',
            // Compiles Sass to CSS
            'sass-loader',
        ],
        exclude: /\.module\.s[ac]ss$/i
    },
    {
        test: /\.s[ac]ss$/i,
        issuer: /\.[tj]s[x]*$/i,
        use: [
            // Creates `style` nodes from JS strings
            'style-loader',
            // Adds typescript declarations to the css modules and allows importing and using strongly typed scss modules in react components.
            'css-modules-typescript-loader',
            // Translates CSS into CommonJS
            { loader: 'css-loader', options: { modules: true, importLoaders: 1, sourceMap: true } },
            // Compiles Sass to CSS
            'sass-loader',
        ],
        include: /\.module\.s[ac]ss$/i
    },
    {
        test: /\.(png|gif|jpg|cur)$/i,
        loader: 'url-loader',
        options: { limit: 8192 }
    },
    {
        test: /\.svg/,
        use: {
            loader: 'svg-url-loader',
            options: {}
        }
    }
];
