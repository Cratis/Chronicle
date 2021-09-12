// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

'use strict';
const mkdirp = require('mkdirp');
const fs = require('fs');
const path = require('path');

function HtmlInterceptorPlugin(options) {
    options = options || {};
    this.outputPath = options.outputPath;
}

let generatedHtml = '';

HtmlInterceptorPlugin.prototype.apply = function (compiler) {

    compiler.hooks.compilation.tap('HtmlWebpackHarddisk', function (compilation) {
        const HtmlWebpackPlugin = require('html-webpack-plugin');
        const hooks = HtmlWebpackPlugin.getHooks(compilation);

        hooks.afterEmit.tapAsync('HtmlInterceptorPlugin', function (htmlPluginData, callback) {
            generatedHtml = compilation.assets[htmlPluginData.outputName].source();
            callback(null);
        });
    });
};

const getGeneratedHtml = () => {
    return generatedHtml;
};

module.exports = {
    HtmlInterceptorPlugin,
    getGeneratedHtml
};
