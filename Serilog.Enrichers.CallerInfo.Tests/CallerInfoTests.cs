using System.Runtime.CompilerServices;
using AwesomeAssertions;
using Serilog.Events;
using Serilog.Sinks.InMemory;

namespace Serilog.Enrichers.CallerInfo.Tests
{
    public class CallerInfoTests
    {
        private const string TestAssembly = "Serilog.Enrichers.CallerInfo.Tests";
        private const string TestClass = "Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests";

        [Fact]
        public void CanManuallySpecifyAssemblies()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Method").Should().Be(nameof(CanManuallySpecifyAssemblies));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void CanAutodetectAssemblies()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo")
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Method").Should().Be(nameof(CanAutodetectAssemblies));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void CanAutodetectFromManualStartingAssembly()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo", startingAssembly: TestAssembly)
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Method").Should().Be(nameof(CanAutodetectFromManualStartingAssembly));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void CanExcludeAssemblies()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo", excludedPrefixes: new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");

            var logEvent = SingleEvent("Test log message");
            logEvent.Properties.Should().NotContainKey("Method");
            logEvent.Properties.Should().NotContainKey("Namespace");
        }

        [Fact]
        public void CanRestrictFilePathDepth()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo", filePathDepth: 2)
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Error(new Exception("Error occurred!"), "Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "SourceFile").Should().Be(Path.Combine(TestAssembly, "CallerInfoTests.cs"));
        }

        [Fact]
        public void FilePathDepthOfOneOnlyGivesFileName()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly }, filePathDepth: 1)
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "SourceFile").Should().Be("CallerInfoTests.cs");
        }

        [Fact]
        public void FullFilePathIsUsedByDefault()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "SourceFile").Should().Be(CurrentFile());
        }

        [Fact]
        public void LineAndColumnNumberPointToTheLoggingCall()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message"); var line = CurrentLine();

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "LineNumber").Should().Be(line);
            PropertyOf(logEvent, "ColumnNumber").Should().Be(13);
        }

        [Fact]
        public void FileInfoCanBeLeftOut()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: false, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Method").Should().Be(nameof(FileInfoCanBeLeftOut));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
            logEvent.Properties.Should().NotContainKey("SourceFile");
            logEvent.Properties.Should().NotContainKey("LineNumber");
            logEvent.Properties.Should().NotContainKey("ColumnNumber");
        }

        [Fact]
        public void PrefixIsPrependedToAllProperties()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly }, prefix: "caller_")
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Serilog.Enrichers.CallerInfo_Prefix").Should().Be("caller_");
            PropertyOf(logEvent, "caller_Method").Should().Be(nameof(PrefixIsPrependedToAllProperties));
            PropertyOf(logEvent, "caller_Namespace").Should().Be(TestClass);
            logEvent.Properties.Should().ContainKeys("caller_SourceFile", "caller_LineNumber", "caller_ColumnNumber");
            logEvent.Properties.Should().NotContainKey("Method");
        }

        [Fact]
        public void ExistingPropertiesAreNotOverwritten()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message {Method}", "custom");

            var logEvent = SingleEvent("Test log message {Method}");
            PropertyOf(logEvent, "Method").Should().Be("custom");
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void LocalFunctionsAreNotIncluded()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo")
                .WriteTo.InMemory()
                .CreateLogger();

            static void LocalFunction(string arg)
            {
                Log.Information(arg);
            }

            LocalFunction("i like turtles");

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(LocalFunctionsAreNotIncluded));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void PrivateFunctionShouldBeAvailable()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo")
                .WriteTo.InMemory()
                .CreateLogger();

            PrivateLocalFunction("i like turtles");

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(PrivateLocalFunction));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void LambdasAreNotIncluded()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            var message = "i like turtles";
            Action lambda = () => Log.Information(message);
            lambda();

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(LambdasAreNotIncluded));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public async Task AsyncMethodIsResolvedBeforeAwait()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            Log.Information("Test log message");
            await Task.Yield();

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Method").Should().Be(nameof(AsyncMethodIsResolvedBeforeAwait));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public async Task AsyncMethodIsResolvedAfterAwait()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            await Task.Delay(1, TestContext.Current.CancellationToken);
            Log.Information("Test log message");

            var logEvent = SingleEvent("Test log message");
            PropertyOf(logEvent, "Method").Should().Be(nameof(AsyncMethodIsResolvedAfterAwait));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public async Task CalledAsyncMethodIsResolved()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            await PrivateAsyncFunction("i like turtles");

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(PrivateAsyncFunction));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void IteratorMethodIsResolved()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            PrivateIterator("i like turtles").ToList();

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(PrivateIterator));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void GenericMethodIsResolved()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            PrivateGenericFunction("i like turtles");

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(PrivateGenericFunction));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass);
        }

        [Fact]
        public void NestedGenericTypeIsReportedWithItsFullName()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, new List<string> { TestAssembly })
                .WriteTo.InMemory()
                .CreateLogger();

            new NestedGeneric<int>().InstanceMethod("i like turtles");

            var logEvent = SingleEvent("i like turtles");
            PropertyOf(logEvent, "Method").Should().Be(nameof(NestedGeneric<int>.InstanceMethod));
            PropertyOf(logEvent, "Namespace").Should().Be(TestClass + "+NestedGeneric`1");
        }

        private static void PrivateLocalFunction(string arg)
        {
            Log.Information(arg);
        }

        private static async Task PrivateAsyncFunction(string arg)
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
            Log.Information(arg);
        }

        private static IEnumerable<int> PrivateIterator(string arg)
        {
            Log.Information(arg);
            yield return 1;
        }

        private static void PrivateGenericFunction<T>(T arg) where T : notnull
        {
            Log.Information(arg.ToString()!);
        }

        private class NestedGeneric<T>
        {
            public void InstanceMethod(string arg)
            {
                Log.Information(arg);
            }
        }

        private static LogEvent SingleEvent(string messageTemplate)
        {
            return InMemorySink.Instance.LogEvents.Should().ContainSingle(e => e.MessageTemplate.Text == messageTemplate).Subject;
        }

        private static object? PropertyOf(LogEvent logEvent, string name)
        {
            logEvent.Properties.Should().ContainKey(name);
            return logEvent.Properties[name].Should().BeOfType<ScalarValue>().Subject.Value;
        }

        private static int CurrentLine([CallerLineNumber] int line = 0) => line;

        private static string CurrentFile([CallerFilePath] string file = "") => file;
    }
}
