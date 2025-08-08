using JWTAuthTemplate.DTO.Identity;
using JWTAuthTemplate.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.Controllers
{
    [ApiController]
    [Route("TestMatrix")]
    public class TestMatrixController : ControllerBase
    {
        private readonly TestMatrixService _matrixService;

        public TestMatrixController(TestMatrixService matrixService)
        {
            _matrixService = matrixService;
        }

        [HttpPost("Transpose")]
        public IActionResult TransposeMatrix([FromBody] TestMatrixDTO matrixDTO)
        {
            var result = _matrixService.Transpose(matrixDTO.Data);
            return Ok(new { TransposedMatrix = result });
        }
    }
}
