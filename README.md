# BlazorNestedCss

## Introduction

The purpose of this repository is to provide support for nested CSS in Blazor component CSS isolation files
(see [CSS nesting](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Nesting)).

### Blazor Component Scoping

Blazor, like many other frameworks, supports CSS isolation for components by adding "scope" attributes to generated
HTML elements. It then rewrites the associated component CSS files to include those attributes.

As of SDK version 10.0.103, .NET does not fully support nested CSS in isolated CSS files. During the rewrite stage,
it may generate invalid or incorrect CSS. See [CSS nesting does not work fully in isolated CSS files #52422](https://github.com/dotnet/aspnetcore/issues/52422)
on GitHub.

### Problem and Solution

The issue has been traced to the `RewriteCSS` task in the .NET 10 SDK. Until the SDK is updated, this project provides
a temporary workaround.

It disables the built-in `RewriteCss` task (`build/BlazorNestedCss.Tasks.props`) and adds a replacement task before the
`ProcessStaticWebAssets` build step (`build/BlazorNestedCss.Tasks.targets`).

## Usage

This project provides the replacement build step through its generated NuGet package. The same result can be achieved by
adding the contents of the `.props` and `.targets` files directly to a target project's `.csproj` file.

### Steps

1. Run `./build.ps1` (use the `-deploy` switch to also deploy the package to a NuGet server).
2. Add a reference to the generated `./packages/BlazorNestedCss.Tasks.1.0.0.nuget` package from the target project that needs nested CSS support.
3. Perform a full rebuild of the target project. The following message should appear in the build output, indicating that
   the interception was successful:
```
************************************
*** Starting Blazor CSS Rewriter ***
*** Version: 10.0.403.0          ***
*** Built on: 2026-10-27 @10:56  ***
*** Added scope to    62 files.  ***
*** Revisit after SDK 10.0.403   ***
************************************
```

### Notes

The following CSS scenarios are supported and validated by the xUnit tests:
- `::deep` anywhere in a selector
- animation names
- multiple selectors separated by commas
- block comments in selectors and declarations
- line comments (`//`) converted to block comments (`/* ... */`)
- `&` in selectors
- quoted strings using either `"` or `'`
- nested `:has(...)` and similar pseudo-classes
- at-rules

### Limitations
- No support for local CSS variables (`--var-name`)

## DISCLAIMER

I am neither a CSS nor an SDK pipeline expert. This project is my attempt to make my application components work
properly with nested CSS files. Much of the code was written with help from AI and then refined until the included
xUnit tests passed. There may be unsupported scenarios or cases that have been handled incorrectly, such as local CSS
variables.



