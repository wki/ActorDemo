using EventStore;
using Microsoft.AspNetCore.Mvc;

namespace ActorWebDemo.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventRepository _eventRepository;

    public EventsController(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }
    
    [HttpGet, Route("{aggregateId}")]
    public async Task<IList<IEvent>> Events([FromRoute] Guid aggregateId)
    {
        var result = await _eventRepository.LoadAsync(aggregateId);

        return result;
    }
}