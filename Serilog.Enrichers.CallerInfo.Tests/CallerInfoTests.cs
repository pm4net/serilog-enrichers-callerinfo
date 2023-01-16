using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Serilog.Enrichers.CallerInfo.Tests
{
    public class CallerInfoTests
    {
        [Fact]
        public void EnrichmentTest()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo.Tests", string.Empty)
                .WriteTo.InMemory()
                .CreateLogger();
            
            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("Method").WithValue("EnrichmentTest")
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }

        [Fact]
        public void LocalFunctionsAreNotIncluded()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithCallerInfo(includeFileInfo: true, "Serilog.Enrichers.CallerInfo.Tests", string.Empty)
                .WriteTo.InMemory()
                .CreateLogger();

            static void LocalFunction(string arg)
            {
                Log.Information(arg);
            }

            LocalFunction("i like turtles");

            InMemorySink.Instance.Should()
                .HaveMessage("i like turtles")
                .Appearing().Once()
                .WithProperty("Method").WithValue("LocalFunctionsAreNotIncluded")
                .And.WithProperty("Namespace").WithValue("Serilog.Enrichers.CallerInfo.Tests.CallerInfoTests");
        }
    }
}