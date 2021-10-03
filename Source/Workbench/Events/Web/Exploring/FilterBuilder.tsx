// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import Editor, { Monaco } from "@monaco-editor/react";

// https://mono.software/2017/04/11/custom-intellisense-with-monaco-editor/
export const FilterBuilder = () => {


    const editorDidMount = (editor: any, monaco: Monaco) => {
        monaco.languages.registerCompletionItemProvider('json', {

            provideCompletionItems: function (model, position) {
                var word = model.getWordUntilPosition(position);
                var range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };

                return {
                    suggestions: [{
                        label: 'Equals',
                        insertText: '"$eq"',
                        range
                    }]
                }
            }
        })

    };

    return (
        <Editor
            height="20vh"
            defaultLanguage="json"
            theme="vs-dark"
            onMount={editorDidMount}

            value='{}'
        />);
};
