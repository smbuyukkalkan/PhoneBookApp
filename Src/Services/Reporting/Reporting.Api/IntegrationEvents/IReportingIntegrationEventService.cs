using EventBus.Events;

namespace Reporting.Api.IntegrationEvents
{
    public interface IReportingIntegrationEventService
    {

        void PublishThroughEventBus(IntegrationEvent evt);
    }
}
