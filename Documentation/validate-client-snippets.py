#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

import re
import shutil
import subprocess
import sys
import textwrap
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SNIPPET_ROOT = REPO_ROOT / "Documentation" / "client-snippets"
GENERATED_DIR = REPO_ROOT / "Documentation" / ".client-snippet-validation"
GENERATED_PROJECT = GENERATED_DIR / "ClientSnippetValidation.csproj"
GENERATED_SOURCE = GENERATED_DIR / "Snippets.cs"
FENCE_RE = re.compile(r"```([^\s`]+)[^\n]*\n(.*?)\n```", re.DOTALL)
USING_DIRECTIVE_RE = re.compile(r"^using\s+(?:static\s+)?[A-Za-z_][A-Za-z0-9_.]*(?:\s*=\s*[A-Za-z_][A-Za-z0-9_.]*)?\s*;$")
VALIDATION_EXCLUDED_PREFIXES = ("legacy/",)

BODY_SNIPPETS = {
    "get-started/client-flow": "",
    "projections/projection-declaration-language/adhoc-querying/inferred-vs-explicit": """
        IProjections projections = default!;
    """,
    "projections/projection-declaration-language/adhoc-querying/type-mismatch": """
        IProjections projections = default!;
    """,
    "projections/projection-declaration-language/adhoc-querying/error-handling": """
        IProjections projections = default!;
    """,
    "projections/projection-declaration-language/adhoc-querying/custom-sequence": """
        IProjections projections = default!;
    """,
    "events/appending/schema-validation": """
        IEventLog eventLog = default!;
        EventSourceId eventSourceId = new("order-123");
        var customerId = "customer-42";
        var total = 42m;
    """,
    "events/appending/occurred": """
        IEventLog eventLog = default!;
        EventSourceId eventSourceId = new("order-123");
        var customerId = "customer-42";
        var total = 42m;
    """,
    "read-models/getting-single-instance/basic": """
        IEventStore eventStore = default!;
        ReadModelKey accountId = new("account-42");
    """,
    "read-models/getting-collection-instances/basic": """
        IEventStore eventStore = default!;
    """,
    "read-models/getting-collection-instances/filtering": """
        IEventStore eventStore = default!;
        var threshold = 1_000m;
    """,
    "read-models/getting-collection-instances/event-count": """
        IEventStore eventStore = default!;
    """,
    "read-models/getting-snapshots/basic": """
        IEventStore eventStore = default!;
        ReadModelKey orderId = new("order-123");
    """,
    "read-models/getting-snapshots/analyze": """
        IEventStore eventStore = default!;
        ReadModelKey orderId = new("order-123");
    """,
    "read-models/watching-read-models/basic": """
        IEventStore eventStore = default!;
    """,
    "read-models/watching-read-models/filtering": """
        IEventStore eventStore = default!;
        var threshold = 1_000m;
    """,
    "sinks/index/register-sql-storage": """
        ISiloBuilder siloBuilder = default!;
        KernelStorageSql::Cratis.Chronicle.Configuration.ChronicleOptions options = new();
    """,
    "hosting/configuration/storage/in-memory": """
        ISiloBuilder siloBuilder = default!;
    """,
    # OrderSummary is declared globally by concepts/designing-read-models/constructor-may-not-run.
    "testing/read-models/scenario/strict-event-subscription": "",
    "testing/read-models/scenario/substitutions": "",
    "testing/read-models/scenario/strict-fidelity": "",
}

# Declaration-style snippets that need a namespace wrapper with aliases for Kernel-only
# types whose bare name collides with a client type already imported globally (e.g.
# EventStoreName, IEventTypes). The wrapper is generator-only — invisible in the rendered
# .md snippet — so the alias plumbing never leaks into the public docs; the snippet body
# itself stays exactly what a real Kernel contributor would write in their own file.
DECLARATION_NAMESPACE_ALIASES = {
    "Cratis.Chronicle.Patches": [
        "using EventStoreName = KernelStorageSql::Cratis.Chronicle.Concepts.EventStoreName;",
        "using IEventTypes = KernelStorageSql::Cratis.Chronicle.EventTypes.IEventTypes;",
    ],
}

DECLARATION_NAMESPACES = {
    "contributing/kernel/patches/index/basic-structure": "Cratis.Chronicle.Patches",
    "contributing/kernel/patches/index/dependencies": "Cratis.Chronicle.Patches",
    "contributing/kernel/patches/index/semantic-logging": "Cratis.Chronicle.Patches",
    "contributing/kernel/patches/index/implement-down": "Cratis.Chronicle.Patches",
    "contributing/kernel/patches/index/idempotency": "Cratis.Chronicle.Patches",
    "contributing/kernel/patches/index/spec": "Cratis.Chronicle.Patches",
    "contributing/kernel/patches/index/rename-reactors-example": "Cratis.Chronicle.Patches",
}


def wrap_in_namespace(relative_path: str, body: str) -> str:
    namespace = DECLARATION_NAMESPACES.get(relative_path)
    if namespace is None:
        return body

    aliases = "\n".join(DECLARATION_NAMESPACE_ALIASES.get(namespace, []))
    indented_body = textwrap.indent(body, "    ")
    indented_aliases = textwrap.indent(aliases, "    ")
    return f"namespace {namespace}\n{{\n{indented_aliases}\n\n{indented_body}\n}}"


def snippet_files() -> list[Path]:
    files = [
        path
        for path in sorted([*SNIPPET_ROOT.rglob("*.md"), *SNIPPET_ROOT.rglob("*.mdx")])
        if not snippet_key(path).startswith(VALIDATION_EXCLUDED_PREFIXES)
    ]
    snippets = {}
    for path in files:
        key = snippet_key(path)
        if key in snippets:
            raise ValueError(f"Duplicate client snippet {key}: {snippets[key]} and {path.relative_to(REPO_ROOT)}")
        snippets[key] = path.relative_to(REPO_ROOT)
    return files


def snippet_key(path: Path) -> str:
    return path.relative_to(SNIPPET_ROOT).with_suffix("").as_posix()


def extract_snippet(path: Path) -> str:
    raw = path.read_text(encoding="utf-8")
    matches = FENCE_RE.findall(raw)
    if len(matches) != 1:
        raise ValueError(f"{path.relative_to(REPO_ROOT)} must contain exactly one fenced C# snippet")

    language, code = matches[0]
    if language != "csharp":
        raise ValueError(f"{path.relative_to(REPO_ROOT)} must use a csharp code fence, got {language!r}")

    return code.strip()


def split_usings(code: str) -> tuple[list[str], str]:
    usings: list[str] = []
    body: list[str] = []
    for line in code.splitlines():
        if USING_DIRECTIVE_RE.match(line):
            usings.append(line)
        else:
            body.append(line)
    return usings, "\n".join(body).strip()


def method_name(relative_path: str) -> str:
    return "Snippet_" + re.sub(r"[^A-Za-z0-9_]", "_", relative_path)


def method(relative_path: str, prelude: str, body: str) -> str:
    lines = [line for line in [textwrap.dedent(prelude).strip(), body] if line]
    method_body = textwrap.indent("\n\n".join(lines), "        ")
    return f"    public static async Task {method_name(relative_path)}()\n    {{\n{method_body}\n    }}"


def generate_source() -> str:
    files = snippet_files()
    if not files:
        raise ValueError(f"No client snippets found in {SNIPPET_ROOT}")

    usings = {
        "using Cratis.Chronicle;",
        "using Cratis.Chronicle.Events;",
        "using Cratis.Chronicle.EventSequences;",
        "using Cratis.Chronicle.Projections.ModelBound;",
        "using Cratis.Chronicle.ReadModels;",
        "using Cratis.Chronicle.Reactors;",
        "using System.Reactive.Linq;",
        "using KernelStorageSql::Orleans.Hosting;",
        "using KernelStorageSql::Cratis.Chronicle.Setup;",
        "using KernelStorageSql::Cratis.Chronicle.Patching;",
        "using KernelStorageSql::Cratis.Chronicle.Storage;",
        "using KernelStorageSql::Cratis.Chronicle.Concepts.System;",
        "using Microsoft.Extensions.Logging;",
        "using Microsoft.Extensions.DependencyInjection;",
        "using Cratis.Chronicle.XUnit.Integration;",
    }
    declarations: list[str] = [
        """
        public readonly record struct OrderId(string Value)
        {
            public static implicit operator EventSourceId(OrderId id) => new(id.Value);
        }
        """.strip(),
        """
        public record AccountInfo(string Name, decimal Balance);
        """.strip(),
        """
        public record Account(string Id, string Name, decimal Balance, DateTimeOffset CreatedDate);
        """.strip(),
        """
        public enum OrderStatus
        {
            New,
            Confirmed,
            Shipped,
            Completed
        }
        """.strip(),
        """
        public record Order(string Id, OrderStatus Status, decimal TotalAmount);
        """.strip(),
    ]
    methods: list[str] = []

    for path in files:
        relative_path = snippet_key(path)
        snippet_usings, body = split_usings(extract_snippet(path))
        usings.update(snippet_usings)

        if relative_path in BODY_SNIPPETS:
            methods.append(method(relative_path, BODY_SNIPPETS[relative_path], body))
        else:
            declarations.append(wrap_in_namespace(relative_path, body))

    return "\n\n".join([
        "// This file is generated by Documentation/validate-client-snippets.py.",
        "#pragma warning disable CA1812, CA1852, IDE0051",
        "extern alias KernelStorageSql;",
        *sorted(usings),
        "",
        *declarations,
        "",
        "public static class DocumentationSnippetBodies",
        "{",
        "\n\n".join(methods),
        "}",
        "",
    ])


def generate_project() -> str:
    return """
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
        <RunAnalyzers>false</RunAnalyzers>
        <RunAnalyzersDuringBuild>false</RunAnalyzersDuringBuild>
        <EnableNETAnalyzers>false</EnableNETAnalyzers>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="../../Source/Infrastructure/Infrastructure.csproj" />
        <ProjectReference Include="../../Source/Clients/DotNET/DotNET.csproj" />
        <ProjectReference Include="../../Source/Clients/Aspire/Aspire.csproj" />
        <ProjectReference Include="../../Source/Clients/AspNetCore/AspNetCore.csproj" />
        <ProjectReference Include="../../Source/Clients/Testing/Testing.csproj" />
        <ProjectReference Include="../../Source/Clients/XUnit.Integration/XUnit.Integration.csproj" />
        <ProjectReference Include="../../Source/Kernel/Contracts/Contracts.csproj" />
        <ProjectReference Include="../../Source/Kernel/Core/Core.csproj">
            <Aliases>KernelStorageSql</Aliases>
        </ProjectReference>
        <ProjectReference Include="../../Source/Kernel/Storage/Storage.csproj">
            <Aliases>KernelStorageSql</Aliases>
        </ProjectReference>
        <ProjectReference Include="../../Source/Kernel/Concepts/Concepts.csproj">
            <Aliases>KernelStorageSql</Aliases>
        </ProjectReference>
        <ProjectReference Include="../../Source/Kernel/Storage.Sql/Storage.Sql.csproj">
            <Aliases>KernelStorageSql</Aliases>
        </ProjectReference>
        <ProjectReference Include="../../Source/Kernel/Storage.InMemory/Storage.InMemory.csproj">
            <Aliases>KernelStorageSql</Aliases>
        </ProjectReference>
        <PackageReference Include="MongoDB.Driver" />
        <PackageReference Include="Cratis.Specifications" />
        <PackageReference Include="Cratis.Specifications.XUnit" />
        <PackageReference Include="xunit" />
        <PackageReference Include="NSubstitute" />
    </ItemGroup>
</Project>
""".strip() + "\n"


def main() -> int:
    shutil.rmtree(GENERATED_DIR, ignore_errors=True)
    GENERATED_DIR.mkdir(parents=True, exist_ok=True)
    GENERATED_PROJECT.write_text(generate_project(), encoding="utf-8")
    GENERATED_SOURCE.write_text(generate_source(), encoding="utf-8")

    try:
        subprocess.run(
            [
                "dotnet",
                "build",
                str(GENERATED_PROJECT),
                "--configuration",
                "Release",
                "-p:DisableProxyGenerator=true",
                "-p:DisableDockerBuild=true",
            ],
            cwd=REPO_ROOT,
            check=True,
        )
    finally:
        shutil.rmtree(GENERATED_DIR, ignore_errors=True)

    print("C# Chronicle client snippets compiled successfully.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"Client snippet validation failed: {error}", file=sys.stderr)
        raise SystemExit(1)
