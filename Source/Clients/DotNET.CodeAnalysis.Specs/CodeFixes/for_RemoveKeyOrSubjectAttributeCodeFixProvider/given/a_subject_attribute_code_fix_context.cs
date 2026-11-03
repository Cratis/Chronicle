// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_RemoveKeyOrSubjectAttributeCodeFixProvider.given;

public class a_subject_attribute_code_fix_context : a_remove_key_or_subject_attribute_code_fix_provider
{
    protected List<CodeAction> _actions;

    protected async Task RegisterFixForSubject(string usage)
    {
        var project = TestProject.CreateProject(CreateSource(usage));
        var document = project.Documents.Single();
        var root = await document.GetSyntaxRootAsync();
        var attribute = root!.DescendantNodes().OfType<AttributeSyntax>().Single(attribute => attribute.Name.ToString() == "Subject");
        var rule = new CodeAnalysis.Analyzers.KeyOrSubjectOnEventSourceIdAnalyzer().SupportedDiagnostics.Single();
        var diagnostic = Diagnostic.Create(rule, attribute.GetLocation(), "Actor", "ActorId", "Subject");
        _actions = [];
        var context = new CodeFixContext(document, diagnostic, (action, _) => _actions.Add(action), CancellationToken.None);
        await new CodeAnalysis.CodeFixes.RemoveKeyOrSubjectAttributeCodeFixProvider().RegisterCodeFixesAsync(context);
    }
}
