using JWTAuthTemplate.Context;
using JWTAuthTemplate.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JWTAuthTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Optional: Requires authentication
    public class UserProfilesMinioController : ControllerBase
    {
        /*
        private readonly ApplicationDbContext _context;

        public UserProfilesMinioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserProfiles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserProfileMinio>>> GetUserProfiles()
        {
            return await _context.UserProfilesMinio.ToListAsync();
        }

        // GET: api/UserProfiles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserProfileMinio>> GetUserProfile(int id)
        {
            var userProfile = await _context.UserProfilesMinio.FindAsync(id);

            if (userProfile == null)
            {
                return NotFound();
            }

            return userProfile;
        }

        // POST: api/UserProfiles
        [HttpPost]
        public async Task<ActionResult<UserProfileMinio>> CreateUserProfile(UserProfileMinio userProfile)
        {
            _context.UserProfilesMinio.Add(userProfile);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUserProfile", new { id = userProfile.Id }, userProfile);
        }

        // PUT: api/UserProfiles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserProfile(int id, UserProfileMinio userProfile)
        {
            if (id != userProfile.Id)
            {
                return BadRequest();
            }

            _context.Entry(userProfile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserProfileExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/UserProfiles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserProfile(int id)
        {
            var userProfile = await _context.UserProfilesMinio.FindAsync(id);
            if (userProfile == null)
            {
                return NotFound();
            }

            _context.UserProfilesMinio.Remove(userProfile);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserProfileExists(int id)
        {
            return _context.UserProfilesMinio.Any(e => e.Id == id);
        }
        */
    }

}
