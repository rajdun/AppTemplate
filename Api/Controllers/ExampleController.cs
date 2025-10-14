using Application.Common.Mediator;
using Application.Cqrs.Example.Query;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ExampleController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public ExampleController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    public async Task<IActionResult> Example([FromQuery] string param)
    {
        var query = new ExampleQuery(param);
        var result = await _mediator.SendAsync<ExampleQuery, string>(query);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}