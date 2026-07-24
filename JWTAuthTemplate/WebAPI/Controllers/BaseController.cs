using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace JWTAuthTemplate.WebAPI.Controllers;

public abstract class BaseController : ControllerBase
{
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
