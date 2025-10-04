using Microsoft.AspNetCore.Mvc;
using CryptoDashboard.Application.Services;
using CryptoDashboard.Dto;

namespace CryptoDashboard.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public ActionResult<UserDto> Get(Guid id)
        {
            var user = _service.GetUserById(id);
            return Ok(user);
        }
    }
}