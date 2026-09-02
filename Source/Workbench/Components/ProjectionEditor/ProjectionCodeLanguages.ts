// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The languages a projection can be shown as client code in.
 *
 * The names match the kernel's own, which is what crosses the wire.
 */
export type ProjectionCodeLanguage = 'CSharp' | 'TypeScript' | 'Kotlin' | 'Elixir';

/** The languages offered, in the order they are shown. */
export const ProjectionCodeLanguages: { label: string; value: ProjectionCodeLanguage }[] = [
    { label: 'C#', value: 'CSharp' },
    { label: 'TypeScript', value: 'TypeScript' },
    { label: 'Kotlin', value: 'Kotlin' },
    { label: 'Elixir', value: 'Elixir' }
];

/**
 * The languages whose client offers a model-bound projection API.
 *
 * The JVM client has none - its own documentation says so for every model-bound snippet - so Kotlin
 * shows the declarative form only rather than an empty tab.
 */
const modelBoundLanguages: ReadonlySet<ProjectionCodeLanguage> = new Set<ProjectionCodeLanguage>([
    'CSharp',
    'TypeScript',
    'Elixir'
]);

/**
 * Determines whether a language's client can express a projection on the read model itself.
 * @param language The language to check.
 * @returns True when the model-bound form can be shown.
 */
export const supportsModelBound = (language: ProjectionCodeLanguage) => modelBoundLanguages.has(language);

/** What Monaco calls each language, for syntax highlighting. */
const editorLanguages: Record<ProjectionCodeLanguage, string> = {
    CSharp: 'csharp',
    TypeScript: 'typescript',
    Kotlin: 'kotlin',
    Elixir: 'elixir'
};

/**
 * Resolves the Monaco language id for a generated language.
 * @param language The language the code was generated in.
 * @returns The Monaco language id.
 */
export const editorLanguageFor = (language: ProjectionCodeLanguage) => editorLanguages[language];
