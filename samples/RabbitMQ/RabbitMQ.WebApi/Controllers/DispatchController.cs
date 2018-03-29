using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using RabbitMQ.WebApi.Models;
using RabbitMQ.WebApi.Services;

namespace RabbitMQ.WebApi.Controllers
{
  [Route("api/dispatch")]
  public class DispatchController : Controller
  {
    private readonly IDispatchService _service;

    public DispatchController(IDispatchService service)
    {
      _service = service;
    }

    [HttpPost]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Post([FromBody]DispatchViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
      }

      await _service.SendAsync(model);

      return Ok();
    }
  }
}