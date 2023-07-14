using Newtonsoft.Json;
using Workleap.DomainEventPropagation.Tests.Subscription.Mocks;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class TelemetryHelperTests
{
    [Fact]
    public void GivenSerializedEventData_WhenAddingTelemetryId_ThenSerializedDataIsValid()
    {
        var testEvent = new TestDomainEvent
        {
            Number = 1,
            Text = "Hello"
        };

        var telemetryCorrelationId = Guid.NewGuid().ToString();
        var eventData = JsonConvert.SerializeObject((object?)testEvent);

        var decoratedTestEventData = TelemetryHelper.AddOperationTelemetryCorrelationIdToSerializedObject(eventData, telemetryCorrelationId);
        var deserializedTelemetryCorrelationId = TelemetryHelper.GetOperationCorrelationIdFromSerializedObject(decoratedTestEventData);

        Assert.NotNull(decoratedTestEventData);
        Assert.NotNull(deserializedTelemetryCorrelationId);
        Assert.Equal(telemetryCorrelationId, deserializedTelemetryCorrelationId);
        Assert.Contains(testEvent.Text, decoratedTestEventData);
    }

    [Fact]
    public void GivenEmptySerializedEventData_WhenAddingTelemetryId_ThenNothingIsAttempted()
    {
        var decoratedTestEventDataNull = TelemetryHelper.AddOperationTelemetryCorrelationIdToSerializedObject(null, Guid.NewGuid().ToString());
        var decoratedTestEventDataEmpty = TelemetryHelper.AddOperationTelemetryCorrelationIdToSerializedObject(string.Empty, Guid.NewGuid().ToString());

        Assert.Null(decoratedTestEventDataNull);
        Assert.Equal(string.Empty, decoratedTestEventDataEmpty);
    }

    [Fact]
    public void WhenGettingTelemetryCorrelationIdFromData_WhenDataNullorEmpty_ThenNullIsReturned()
    {
        var deserializedTelemetryCorrelationIdNull = TelemetryHelper.GetOperationCorrelationIdFromSerializedObject(null);
        var deserializedTelemetryCorrelationIdEmpty = TelemetryHelper.GetOperationCorrelationIdFromSerializedObject(string.Empty);

        Assert.Null(deserializedTelemetryCorrelationIdNull);
        Assert.Null(deserializedTelemetryCorrelationIdEmpty);
    }

    [Fact]
    public void WhenGettingTelemetryCorrelationIdFromData_WhenSerializedDataIsValid_ThenValueIsReturned()
    {
        var telemetryCorrelationId = "52e1f1888c8de44685cbf9bda04a4b9c";
        var serializedData = $"{{\"OrganizationId\":\"77cfa45f-0040-45a2-9db2-35338fa1ed09\",\"MemberId\":\"8d723e60-93ca-4dee-a542-030d5e74d49d\",\"Date\":\"2022-10-21T14:22:43.4220431Z\",\"DataVersion\":\"1\",\"telemetryCorrelationId\": \"{telemetryCorrelationId}\"}}";
        var extractedValue = TelemetryHelper.GetOperationCorrelationIdFromSerializedObject(serializedData);

        Assert.NotNull(extractedValue);
        Assert.Equal(telemetryCorrelationId, extractedValue);
    }

    [Fact]
    public void WhenGettingTelemetryCorrelationIdFromString_WhenSerializedDataIsValid_ThenValueIsReturned()
    {
        var telemetryCorrelationId = "52e1f1888c8de44685cbf9bda04a4b9c";
        var serializedData = $"{{\"OrganizationId\":\"77cfa45f-0040-45a2-9db2-35338fa1ed09\",\"MemberId\":\"8d723e60-93ca-4dee-a542-030d5e74d49d\",\"Date\":\"2022-10-21T14:22:43.4220431Z\",\"DataVersion\":\"1\",\"telemetryCorrelationId\": \"{telemetryCorrelationId}\"}}";
        var extractedValue = TelemetryHelper.GetCorrelationIdFromString(serializedData);

        Assert.NotNull(extractedValue);
        Assert.Equal(telemetryCorrelationId, extractedValue);
    }

    [Fact]
    public void WhenGettingTelemetryCorrelationIdFromString_WhenSerializedDataDoesNotCOntainTelemetryCorrelationId_ThenReturnNull()
    {
        var telemetryCorrelationId = "52e1f1888c8de44685cbf9bda04a4b9c";
        var serializedData = $"{{\"OrganizationId\":\"77cfa45f-0040-45a2-9db2-35338fa1ed09\",\"MemberId\":\"8d723e60-93ca-4dee-a542-030d5e74d49d\",\"Date\":\"2022-10-21T14:22:43.4220431Z\",\"DataVersion\":\"1\",\"telemetryCosdfsdfrrelationId\": \"{telemetryCorrelationId}\"}}";
        var extractedValue = TelemetryHelper.GetCorrelationIdFromString(serializedData);

        Assert.Null(extractedValue);
    }
}