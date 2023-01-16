# Serilog.Enrichers.CallerInfo

[![GitHub](https://img.shields.io/github/license/pm4net/serilog-enrichers-callerinfo?style=flat-square)](https://github.com/pm4net/serilog-enrichers-callerinfo/blob/master/LICENSE)
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/pm4net/serilog-enrichers-callerinfo/tests.yml?label=tests&style=flat-square&branch=master)](https://github.com/pm4net/serilog-enrichers-callerinfo/actions/workflows/tests.yml)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/Serilog.Enrichers.CallerInfo?label=NuGet&style=flat-square)](https://www.nuget.org/packages/Serilog.Enrichers.CallerInfo/)
[![Nuget](https://img.shields.io/nuget/dt/Serilog.Enrichers.CallerInfo?label=NuGet%20Downloads&style=flat-square)](https://www.nuget.org/packages/Serilog.Enrichers.CallerInfo/#versions-body-tab)

A simple Serilog enricher to add information about the calling method. Loosely based on [Serilog.Enrichers.WithCaller](https://github.com/pmetz-steelcase/Serilog.Enrichers.WithCaller), but with the properties split up for a consistent format that can be parsed automatically.

Performance may be low due the usage of reflection to obtain assembly and method information, as well as the retrieval of the current stacktrace whenever a new log event is added.

## Usage

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCallerInfo(
        includeFileInfo: true, 
        assemblyPrefix: "MySolution", 
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