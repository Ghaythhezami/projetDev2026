using System;

namespace AgileAi.Api.Dtos
{
    public class UserResponseDto
    {
        public Guid UserId { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string Role { get; set; }
    }
}
