namespace AgileAi.Api.Dtos
{
    public class RegisterUserDto
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string MotDePasse { get; set; }
        public string Telephone { get; set; }
        public string Role { get; set; }
    }
}
