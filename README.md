# Serilog.Enrichers.CallerInfo

[![GitHub](https://img.shields.io/github/license/pm4net/serilog-enrichers-callerinfo?style=flat-square)](https://github.com/pm4net/serilog-enrichers-callerinfo/blob/master/LICENSE)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/pm4net/serilog-enrichers-callerinfo/tests.yml?label=tests&style=flat-square&branch=master)](https://github.com/pm4net/serilog-enrichers-callerinfo/actions/workflows/tests.yml)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Serilog.Enrichers.CallerInfo?label=NuGet&style=flat-square)](https://www.nuget.org/packages/Serilog.Enrichers.CallerInfo/)
[![Nuget](https://img.shields.io/nuget/dt/Serilog.Enrichers.CallerInfo?label=NuGet%20Downloads&style=flat-square)](https://www.nuget.org/packages/Serilog.Enrichers.CallerInfo/#versions-body-tab)

A simple Serilog enricher to add information about the calling method. Loosely based on [Serilog.Enrichers.WithCaller](https://github.com/pmetz-steelcase/Serilog.Enrichers.WithCaller), but with the properties split up for a consistent format that can be parsed automatically.

Performance may be low due the usage of reflection to obtain assembly and method information, as well as the retrieval of the current stacktrace whenever a new log event is added.

To get the actual method that emitted the log event, the stack trace is traversed and the first method that is in a matching assembly is considered the correct one. To configure which assemblies are the correct one, one can either use the configuration method with `assemblyPrefix` or the one that directly accepts a collection of fully qualified assembly names. When passing in a prefix, the assembly that calls the configuration method is recursively analyzed for referenced assemblies and whether they match the prefix. If you e.g. have a solution with projects being called `MySolution.Web`, `MySolution.Domain`, etc., you can limit the included assemblies by passing in the prefix `MySolution`.

## Usage

```csharp
// With assembly prefix
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCallerInfo(
        includeFileInfo: true, 
        assemblyPrefix: "MySolution.", 
        prefix: "myprefix")
    .WriteTo.InMemory()
    .CreateLogger();

// With fully qualified assembly names
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCallerInfo(
        includeFileInfo: true, 
        allowedAssemblies: new List<string> { "Serilog.Enrichers.CallerInfo.Tests" }, 
        prefix: "myprefix")
    .WriteTo.InMemory()
    .CreateLogger();
```

## Properties

The following properties are added:

| Property | Description | Depends on |
|----------|-------------|------------|
| `Method` | The calling method |  |
| `Namespace` | The namespace of the calling method (including class name) |  |
| `SourceFile` | The path of the source file of the calling method | `includeFileInfo` |
| `LineNumber` | The line number of the calling method | `includeFileInfo` |
| `ColumnNumber` | The column number of the calling method | `includeFileInfo` |