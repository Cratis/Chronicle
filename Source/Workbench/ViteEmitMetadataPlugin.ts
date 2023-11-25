// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Based on: https://github.com/arjendeblok/vite-plugin-emit-metadata

import path from 'path';
import ts from 'typescript';

const findContent = (fileContent: string, contentRegEx: RegExp) => contentRegEx.test(fileContent);

const parseTsConfig = (tsconfig: string, cwd = process.cwd()) : ts.ParsedCommandLine => {
	const fileName = ts.findConfigFile(
		cwd,
		ts.sys.fileExists,
		tsconfig
	);

	// if the value was provided, but no file, fail hard
	if (tsconfig !== undefined && !fileName)
		throw new Error(`failed to open '${fileName}'`);

	let loadedConfig: any = {};
	let baseDir = cwd;
	let configFileName;
	if (fileName) {
		const text = ts.sys.readFile(fileName);
		if (text === undefined) throw new Error(`failed to read '${fileName}'`);

		const result = ts.parseConfigFileTextToJson(fileName, text);

		if (result.error !== undefined) {
			console.error(`failed to parse '${fileName}'`);
			throw new Error(`failed to parse '${fileName}'`);
		}

		loadedConfig = result.config;
		baseDir = path.dirname(fileName);
		configFileName = fileName;
	}

	const parsedTsConfig = ts.parseJsonConfigFileContent(
		loadedConfig,
		ts.sys,
		baseDir
	);

	if (parsedTsConfig.errors[0]) console.error(parsedTsConfig.errors);

    if(loadedConfig.emitDecoratorMetadata == false) {
        console.error('vite-plugin-metadata: emitDecoratorMetadata not set', parsedTsConfig);
    }

	return parsedTsConfig;
}

export const VitePluginEmitMetadata = ({
	tsconfigPath = path.join(process.cwd(), './tsconfig.json'),
	fileRegEx = /\.ts$/,
	contentRegEx = /((?<![\(\s]\s*['"])@\w*[\w\d]\s*(?![;])[\((?=\s)])/
    } = {}) => {

	let parsedTsConfig : ts.ParsedCommandLine | null = null;

	return {
		name: 'transform-file',
		enforce: 'pre',

		transform(src: string, id: string) {
            if (!parsedTsConfig) {
				parsedTsConfig = parseTsConfig(tsconfigPath, process.cwd());
				if (parsedTsConfig.options.sourceMap) {
					parsedTsConfig.options.sourcemap = false;
				 	parsedTsConfig.options.inlineSources = true;
				 	parsedTsConfig.options.inlineSourceMap = true;
				}
            }

			if (fileRegEx.test(id)) {
				const hasDecorator = findContent(src, contentRegEx);
				if (!hasDecorator) {
					return;
				}

				const program = ts.transpileModule(src, { compilerOptions: parsedTsConfig.options });
				return {
					code: program.outputText,
					map: null
				}
			}
		},
	}
}

