using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace JWTAuthTemplate.Models.Identity
{
    public class UserSessionStatus
    {
        [Key]
        public int Id { get; set; }
        
        public string UserId { get; set; }
        
        public DateTime ActualAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "jsonb")]
        public string StatusParams { get; set; }

        [NotMapped]
        public Dictionary<string, object> StatusParamsDict
        {
            get => JsonSerializer.Deserialize<Dictionary<string, object>>(StatusParams);
            set => StatusParams = JsonSerializer.Serialize(value);
        }
    }
}
