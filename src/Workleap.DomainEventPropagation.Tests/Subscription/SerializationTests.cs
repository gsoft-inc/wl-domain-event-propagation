using Newtonsoft.Json;

namespace Workleap.DomainEventPropagation.Tests.Subscription;

public class SerializationTests
{
    [Fact]
    public void CanSerializeDomainEvent()
    {
        var bananaEvent = new BananaEvent()
        {
            Id = Guid.NewGuid(),
            Number = 101011,
            Word = "Bandana"
        };

        var serializedEvent = SerializeEvent(bananaEvent);

        Assert.Equal(bananaEvent.GetType(), ((IDomainEvent)bananaEvent).GetType());
        Assert.Contains(bananaEvent.Word, serializedEvent);
        Assert.Contains(bananaEvent.Number.ToString(), serializedEvent);
        Assert.Contains(bananaEvent.Id.ToString(), serializedEvent);
    }

    [Fact]
    public void CanResolveDomainEventType()
    {
        var bananaEvent = new BananaEvent()
        {
            Id = Guid.NewGuid(),
            Number = 101011,
            Word = "Bandana"
        };

        Assert.Equal(bananaEvent.GetType(), ((IDomainEvent)bananaEvent).GetType());
    }

    private static string SerializeEvent(IDomainEvent domainEvent)
    {
        return JsonConvert.SerializeObject(domainEvent);
    }

    private class BananaEvent : IDomainEvent
    {
        public Guid Id { get; set; }

        public int Number { get; set; }

        public string Word { get; set; }

        public string DataVersion => "1";
    }
}