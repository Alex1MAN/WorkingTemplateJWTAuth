using JWTAuthTemplate.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JWTAuthTemplate.WebAPI.Controllers
{
    [ApiController]
    [Route("matrices")]
    public class MatrixController : BaseController
    {
        private readonly IMatrixService _matrixService;

        public MatrixController(IMatrixService matrixService)
        {
            _matrixService = matrixService;
        }

        [HttpPost("multiply")]
        public IActionResult MultiplyMatrices([FromBody] MatrixMultiplicationDTO matrices)
        {
            var result = _matrixService.Multiply(matrices.MatrixA, matrices.MatrixB);
            return Ok(result);
        }

        [HttpPost("add")]
        public IActionResult AddMatrices([FromBody] MatrixAdditionDTO matrices)
        {
            var result = _matrixService.Add(matrices.MatrixA, matrices.MatrixB);
            return Ok(result);
        }

        [HttpPost("calculate-determinant")]
        public IActionResult CalculateDeterminant([FromBody] SquareMatrixDTO matrix)
        {
            var determinant = _matrixService.CalculateDeterminant(matrix.Data);
            return Ok(determinant);
        }
    }
}
