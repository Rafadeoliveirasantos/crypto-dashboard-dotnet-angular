using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class TesteController : ControllerBase
{
    private readonly ILogger<TesteController> _logger;

    public TesteController(ILogger<TesteController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Teste de log estruturado!");
        return Ok("Log gerado!");
    }
}