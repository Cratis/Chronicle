// Chronicle Projection Rules DSL - Monaco language definition (Monarch) + config
// Usage (example):
//   monaco.languages.register({ id: 'chronicle-rules-dsl' });
//   monaco.languages.setMonarchTokensProvider('chronicle-rules-dsl', chronicleRulesDslLanguage);
//   monaco.languages.setLanguageConfiguration('chronicle-rules-dsl', chronicleRulesDslConfiguration);

import type * as monaco from "monaco-editor";

export const LANGUAGE_ID = "chronicle-rules-dsl";

// Keywords are intentionally small & stable to keep the language elegant.
const KEYWORDS = [
  "projection",
  "automap",
  "every",
  "exclude",
  "children",
  "id",
  "on",
  "key",
  "parent",
  "join",
  "events",
  "remove",
  "via",
  "increment",
  "decrement",
  "count",
  "add",
  "subtract",
  "by",
];

// Built-in roots for expressions.
const BUILTINS = ["e", "ctx", "$eventSourceId"];

export const chronicleRulesDslLanguage: monaco.languages.IMonarchLanguage = {
  defaultToken: "",
  tokenPostfix: ".crdsl",

  keywords: KEYWORDS,
  builtins: BUILTINS,

  // Good enough for most projects; adjust if you decide to restrict numbers further.
  tokenizer: {
    root: [
      // whitespace
      [/[ \t\r\n]+/, ""],

      // comments
      [/#.*$/, "comment"],

      // header arrow
      [/=>/, "operator"],

      // punctuation
      [/[(),]/, "delimiter"],
      [/[{}]/, "@brackets"],

      // strings: "..." or '...'
      [/"/, { token: "string.quote", bracket: "@open", next: "@string_double" }],
      [/'/, { token: "string.quote", bracket: "@open", next: "@string_single" }],

      // template strings: `...` with ${ ... } support
      [/`/, { token: "string.quote", bracket: "@open", next: "@template" }],

      // numbers
      [/\b\d+(\.\d+)?\b/, "number"],

      // booleans/null
      [/\b(true|false|null)\b/, "constant"],

      // operators used in mapping lines
      [/=/, "operator"],

      // identifiers / keywords / type refs / builtins
      [
        /\b[A-Za-z_][\w]*\b/,
        {
          cases: {
            "@keywords": "keyword",
            "@builtins": "variable.predefined",
            "@default": "identifier",
          },
        },
      ],

      // dotted paths: highlight dots separately (e.g., e.userId, ctx.occurred)
      [/\./, "delimiter"],

      // fallback
      [/./, ""],
    ],

    string_double: [
      [/[^\\"]+/, "string"],
      [/\\./, "string.escape"],
      [/"/, { token: "string.quote", bracket: "@close", next: "@pop" }],
    ],

    string_single: [
      [/[^\\']+/, "string"],
      [/\\./, "string.escape"],
      [/'/, { token: "string.quote", bracket: "@close", next: "@pop" }],
    ],

    template: [
      // allow ${ ... } blocks inside backticks
      [/\$\{/, { token: "delimiter.bracket", next: "@templateExpr" }],
      [/[^\\`$]+/, "string"],
      [/\\./, "string.escape"],
      [/`/, { token: "string.quote", bracket: "@close", next: "@pop" }],
      [/\$/, "string"], // lone $
    ],

    templateExpr: [
      // very light expression parsing: reuse root tokenizer until closing }
      [/\}/, { token: "delimiter.bracket", next: "@pop" }],
      { include: "root" },
    ],
  },
};

export const chronicleRulesDslConfiguration: monaco.languages.LanguageConfiguration = {
  comments: {
    lineComment: "#",
  },

  brackets: [
    ["{", "}"],
    ["(", ")"],
  ],

  autoClosingPairs: [
    { open: "{", close: "}" },
    { open: "(", close: ")" },
    { open: '"', close: '"' },
    { open: "'", close: "'" },
    { open: "`", close: "`" },
  ],

  surroundingPairs: [
    { open: "{", close: "}" },
    { open: "(", close: ")" },
    { open: '"', close: '"' },
    { open: "'", close: "'" },
    { open: "`", close: "`" },
  ],

  // Indentation is structural in the DSL; Monaco can't enforce it, but it can help.
  indentationRules: {
    // increase indent after these lines (block starters)
    increaseIndentPattern: new RegExp(
      "^\\s*(projection\\b.*=>\\b.*|every\\b|children\\b.*\\bid\\b.*|on\\b.*|join\\b.*\\bon\\b.*|remove\\b\\bon\\b.*)\\s*$"
    ),
    // decrease indent is mostly handled by user; keep it permissive.
    decreaseIndentPattern: new RegExp("^\\s*$"),
  },

  folding: {
    offSide: true, // indentation-based folding (works well for this DSL)
    markers: {
      start: new RegExp("^\\s*(projection\\b|every\\b|children\\b|on\\b|join\\b|remove\\b).*\\s*$"),
      end: new RegExp("^\\s*$"),
    },
  },

  // Helpful word pattern for selection and Ctrl+D behavior.
  wordPattern:
    /(-?\d*\.\d\w*)|([^\`\~\!\@\#\%\^\&\*\(\)\=\+\[\]\{\}\\\|\;\:\'\"\,\.\/\?\s]+)/g,
};
