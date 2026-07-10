using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace JWTAuthTemplate.WebAPI.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        // Для операций без возврата данных (POST/DELETE/PUT без тела)
        protected async Task<IActionResult> ExecuteSafeAsync(Func<Task> action)
        {
            try
            {
                await action();
                return Ok();
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(statusCode: 500, detail: ex.Message);
            }
        }


        // Для операций с возвратом IActionResult (GET/POST с возвратом)
        protected async Task<IActionResult> ExecuteSafeAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (ValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(statusCode: 500, detail: ex.Message);
            }
        }
    }
}
