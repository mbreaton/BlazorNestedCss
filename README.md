# BlazorNestedCss

## Introduction

The purpose of this repository is to provide support for Blazor component isolated CSS files when using nested CSS
(see [CSS nesting](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Nesting)).

### Blazor Component Scoping

Blazor (among other frameworks) supports components and component CSS isolation support by adding "scope" attributes to generated 
HTML elements and then rewriting the associated component CSS files to add those attributes to achieve the CSS isolation.

As of SDK v10.0.103, dotnet does not support this when using nested CSS and, during the rewrite stage, will not generate 
proper CSS (see [CSS nesting does not work fully in isolated CSS files #52422](https://github.com/dotnet/aspnetcore/issues/52422)) 
on the dotnet github site.

### Problem Area/Solution

The deficiency has been narrowed down to the RewriteCSS Task Module in the .Net 10 SDK and needs to be addressed.
In the meantime, this solution attempts to provide a temporary fix until such time the SDK is updated.

It accomplishes the this by disabling the built-in RewriteCss task (`build/BlazorNestedCss.Tasks.props`) and adding a new task
before the `ProcessStaticWebAssets` build step (`build/BlazorNestedCss.Tasks.targets`).

## Usage

This project provides the replacement build step via its generated nuget package, although the
same result can be accomplished by updating any `.csproj` file directly with the content of the above `.props` and `.targets` content.

### Steps

1. "build" and "pack" `BlazorNestedCss.Tasks`
1. add a reference to the generated `./packages/BlazorNestedCss.Tasks.1.0.0.nuget` package from the target project needing nested CSS support
1. perform a full rebuild of the target project, the following should appear in the build output, indicating the
interception was successful:
```
************************************
*** Starting Blazor CSS Rewriter ***
*** Version: 1.0.0.0             ***
*** Built on: 2026-02-28 @08:57  ***
*** Added scope to    54 files.  ***
*** Revisit after SDK 10.0.103   ***
************************************
```

### Notes

The following CSS usage is supported and validated in xUnit test files:
- support for `::deep` anywhere in selector
- support for names in `animations`
- support for multiple selectors (those separated by `,`)
- support for block comments in selectors/declarations
- support for line comments (`//`) by converting to block comments (`/* .. */`)
- support for `&` in selectors
- support for quoted strings (both `"` and `'`)
- support for nested `:has(...)` and similar pseudo classes
- support for `@` at blocks

### Limitations
- no support for local CSS variables (`--var-name`)

## DISCLAIMER

I am not a CSS nor SDK pipeline expert. This is simply **my** attempt to get **my** project components to behave 
properly alongside nested CSS files. Much of the code was written with some help from AI and tweaked to get the 
included xUnit tests to pass. I may have missed some things, while some others may have been too complex for
me to properly handle (e.g., local css variables to name just one)



