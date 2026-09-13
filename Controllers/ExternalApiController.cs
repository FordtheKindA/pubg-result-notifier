using Microsoft.AspNetCore.Mvc;
using ServicePractice.Services;
using ServicePractice.DTOs;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;


namespace ServicePractice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalApiController : ControllerBase
{
    private readonly ExternalApiService _externalApiService;

    public ExternalApiController(ExternalApiService externalApiService)
    {
        _externalApiService = externalApiService;
    }

[HttpGet]
public async Task<IActionResult> GetDataAsync()
{
    string Url = "https://jsonplaceholder.typicode.com/todos/1";
    var todo = await _externalApiService.GetDataAsync(Url);
        if (todo == null)
        {
            return StatusCode(502);
        }
    
    return Ok(new
{
    todo.Id,
    todo.Title,
    todo.Completed
});
}
}
