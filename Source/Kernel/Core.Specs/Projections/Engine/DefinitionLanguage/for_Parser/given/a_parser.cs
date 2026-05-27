// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.AST;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Parser.given;

public class a_parser : Specification
{
    protected Document _document;
    protected ParsingErrors _errors;

    protected void Parse(string declaration)
    {
        Document parsedDocument = null;
        ParsingErrors capturedErrors = null;

        var tokenizer = new Tokenizer(declaration);
        var tokenizeResult = tokenizer.Tokenize();

        tokenizeResult.Match(
            tokens =>
            {
                var parser = new Parser(tokens);
                var parseResult = parser.Parse();
                return parseResult.Match(
                    document =>
                    {
                        parsedDocument = document;
                        return 0;
                    },
                    errors =>
                    {
                        capturedErrors = errors;
                        return 0;
                    });
            },
            errors =>
            {
                capturedErrors = errors;
                return 0;
            });

        _document = parsedDocument;
        _errors = capturedErrors ?? new ParsingErrors([]);
    }
}
