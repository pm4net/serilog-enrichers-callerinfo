using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Serilog.Enrichers.pm4net.Tests
{
    public class CallerInfoTests
    {
        private const string Template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        
        [Fact]
        public void EnrichmentTest()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithPm4Net(includeCallerInfo: true, includeFileInfo: true, "Serilog.Enrichers.pm4net.Tests")
                .WriteTo.InMemory(outputTemplate: Template)
                .CreateLogger();
            
            Log.Error(new Exception("Error occurred!"), "Test log message");
            InMemorySink.Instance.Should()
                .HaveMessage("Test log message")
                .Appearing().Once()
                .WithProperty($"pm4net_Method").WithValue("EnrichmentTest")
                .And.WithProperty("pm4net_Namespace").WithValue("Serilog.Enrichers.pm4net.Tests.CallerInfoTests");
        }
    }
}