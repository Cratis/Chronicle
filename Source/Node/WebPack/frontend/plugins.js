// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin');
const { HtmlInterceptorPlugin } = require('./HtmlInterceptorPlugin');

module.exports = (basePath, title) => {
    return [
        new ForkTsCheckerWebpackPlugin(),

        new CleanWebpackPlugin({
            dangerouslyAllowCleanPatternsOutsideProject: true,
            dry: false,
            cleanStaleWebpackAssets: false,
            cleanOnceBeforeBuildPatterns: ['**/*.*']
        }),

        new HtmlWebpackPlugin({
            template: path.resolve(process.cwd(), 'index.ejs'),
            templateParameters: {

            },
            publicPath: basePath,
            metadata: {
                title,
                baseUrl: basePath
            }
        }),

        new HtmlInterceptorPlugin({}),
        new MiniCssExtractPlugin({
            filename: './styles.css',
        }),
    ];
};
