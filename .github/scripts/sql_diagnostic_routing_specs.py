# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Focused assertions for the opt-in SQL diagnostic workflow route."""

import itertools
import json


def verify_sql_diagnostic_route(test, workflow, context_type, evaluate_condition):
    def evaluate(expression, bindings):
        return evaluate_condition(expression.removeprefix("${{").removesuffix("}}"), bindings)

    build = workflow("dotnet-build.yml")
    integration = workflow("integration.yml")
    dispatch = build["on"]["workflow_dispatch"]["inputs"]["sql_diagnostic"]
    reusable = integration["on"]["workflow_call"]["inputs"]["sql-diagnostic"]
    for declaration in (dispatch, reusable):
        test.assertEqual("false", declaration["default"])
        test.assertEqual("boolean", declaration["type"])
    test.assertNotIn("sql_diagnostic", build["on"]["workflow_call"]["inputs"])
    caller = build["jobs"]["integration"]["with"]["sql-diagnostic"]
    jobs = integration["jobs"]
    for event, requested in itertools.product(("pull_request", "schedule", "workflow_dispatch", "workflow_call"), (False, True)):
        context = context_type(event=event, requested=True, sql_diagnostic=requested,
                               results={job: "success" for job in ("dotnet-build", "dotnet-build-development",
                                                                 "dotnet-build-development-requested", "specs")})
        diagnostic = event == "workflow_dispatch" and requested
        test.assertEqual(diagnostic, evaluate(caller, context.bindings()))
        test.assertTrue(evaluate(build["jobs"]["integration"]["if"], context.bindings()))
        for lane in ("integration-api", "mongodb"):
            test.assertEqual(not diagnostic, evaluate(build["jobs"][lane]["if"], context.bindings()))
        bindings = {"inputs.sql-diagnostic": diagnostic,
                    "needs.discover.outputs.rolling-upgrade-enabled": "true"}
        for lane in ("client", "kernel", "clustering"):
            test.assertEqual(not diagnostic, evaluate(jobs[f"{lane}-integration-specs"]["if"], bindings))
        test.assertEqual(not diagnostic, evaluate(jobs["sql-clustering-rolling-upgrade"]["if"], bindings))
        provider = jobs["sql-clustering-integration-specs"]["strategy"]["matrix"]["provider"]
        selection = provider.removeprefix("${{ fromJson(").removesuffix(") }}")
        test.assertEqual(["SqlServer"] if diagnostic else ["PostgreSql", "SqlServer"],
                         json.loads(evaluate(selection, bindings)))
    discover = next(step for step in jobs["discover"]["steps"] if step.get("id") == "client")
    test.assertIn('echo "all-providers=${{ !inputs.sql-diagnostic && inputs.all-providers }}"', discover["run"])
