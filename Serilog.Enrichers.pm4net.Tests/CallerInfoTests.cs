using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Serilog.Enrichers.pm4net.Tests
{
    public class CallerInfoTests
    {
        [Fact]
        public void EnrichmentTest()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithPm4Net(includeCallerInfo: true, includeFileInfo: true, "Serilog.Enrichers.pm4net.Tests")
                .WriteTo.InMemory()
                .CreateLogger();
            
            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty("pm4net_Method").WithValue("EnrichmentTest")
                .And.WithProperty("pm4net_Namespace").WithValue("Serilog.Enrichers.pm4net.Tests.CallerInfoTests");
        }

        [Fact]
        public void LocalFunctionsAreNotIncluded()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithPm4Net(includeCallerInfo: true, includeFileInfo: true, "Serilog.Enrichers.pm4net.Tests")
                .WriteTo.InMemory()
                .CreateLogger();

            void LocalFunction(string arg)
            {
                Log.Information(arg);
            }

            LocalFunction("i like turtles");

            InMemorySink.Instance.Should()
                .HaveMessage("i like turtles")
                .Appearing().Once()
                .WithProperty("pm4net_Method").WithValue("LocalFunctionsAreNotIncluded")
                .And.WithProperty("pm4net_Namespace").WithValue("Serilog.Enrichers.pm4net.Tests.CallerInfoTests");
        }
    }
}