// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import Editor, { Monaco } from "@monaco-editor/react";

// https://mono.software/2017/04/11/custom-intellisense-with-monaco-editor/
// https://gist.github.com/mwrouse/05d8c11cd3872c19c684bd1904a2202e
// https://blog.checklyhq.com/customizing-monaco/
// https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.itextmodel.html
export const FilterBuilder = () => {


    const editorDidMount = (editor: any, monaco: Monaco) => {

        monaco.languages.registerCompletionItemProvider('json', {

            provideCompletionItems: function (model, position) {
                const suggestions:any[] = [];
                var word = model.getWordUntilPosition(position);
                var range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };
                const enclosingBrackets = model.findEnclosingBrackets(position);
                debugger;
                if (enclosingBrackets != null) {
                    suggestions.push({
                        label: 'Equals',
                        insertText: '"$eq": {}',
                        range
                    });
                    suggestions.push({
                        label: 'And',
                        insertText: '"$and": {}',
                        range
                    });
                    suggestions.push({
                        label: 'Or',
                        insertText: '"$or": {}',
                        range
                    });
                }

                return {
                    suggestions
                }
            }
        })
    };

    const content = '{}';

    return (
        <Editor
            height="20vh"
            defaultLanguage="json"
            theme="vs-dark"
            onMount={editorDidMount}
            value={content}
        />);
};
