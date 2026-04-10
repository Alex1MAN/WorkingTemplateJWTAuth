using Microsoft.AspNetCore.Identity;

namespace JWTAuthTemplate.Models.Identity
{
    public class ApplicationRole: IdentityRole<string>
    {
        public ApplicationRole()
        {
            // Устанавливаем идентификатор при создании объекта
            base.Id = Guid.NewGuid().ToString();
        }


        public List<ApplicationUserRole> Users { get; set; } = new();
    }
}
