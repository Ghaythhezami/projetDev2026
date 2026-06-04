using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgileAi.Api.Services;
using AgileAi.Data.Context;
using AgileAi.Domain.Dto;
using AgileAi.Domain.Models;

namespace AgileAi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private const int RefreshTokenExpiryDays = 5;
        private readonly string _jwtSecret;
        private readonly ICurrentUserService _currentUser;

        public UserController(AppDbContext context, IConfiguration configuration, ICurrentUserService currentUser)
        {
            _authContext = context;
            _jwtSecret = configuration["Jwt:Secret"];
            _currentUser = currentUser;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] LoginRequestDto login)
        {
            if (login == null)
                return BadRequest();

            var user = await _authContext.Users.FirstOrDefaultAsync(x => x.Email == login.Email);

            if (user == null)
                return NotFound(new { Message = "User not found!" });

            var passwordHasher = new PasswordHasher<User>();
            if (passwordHasher.VerifyHashedPassword(user, user.MotDePasse, login.MotDePasse) != PasswordVerificationResult.Success)
                return BadRequest(new { Message = "Password is Incorrect" });

            user.Token = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
            await _authContext.SaveChangesAsync();

            return Ok(new TokenApiDto
            {
                AccessToken = user.Token,
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> AddUserWithRole([FromBody] RegisterUserDto request)
        {
            if (request == null || string.IsNullOrEmpty(request.Role))
                return BadRequest();

            var roleName = request.Role;

            if (await CheckEmailExistAsync(request.Email))
                return BadRequest(new { Message = "Email Already Exist" });

            var passMessage = CheckPasswordStrength(request.MotDePasse);
            if (!string.IsNullOrEmpty(passMessage))
                return BadRequest(new { Message = passMessage });

            var validRoles = new List<string> { "admin", "agent de controle", "analyste" };
            if (!validRoles.Contains(roleName.ToLower()))
                return BadRequest(new { Message = "Invalid role name" });

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Nom = request.Nom,
                Prenom = request.Prenom,
                Email = request.Email,
                Telephone = request.Telephone,
                Role = request.Role
            };

            var passwordHasher = new PasswordHasher<User>();
            user.MotDePasse = passwordHasher.HashPassword(user, request.MotDePasse);

            _authContext.Users.Add(user);
            await _authContext.SaveChangesAsync();

            return Ok(new
            {
                Status = 200,
                Message = "User Added!",
                User = ToUserResponse(user)
            });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = await _authContext.Users
                .Select(user => new UserResponseDto
                {
                    UserId = user.UserId,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email,
                    Telephone = user.Telephone,
                    Role = user.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenApiDto tokenApiDto)
        {
            if (tokenApiDto == null)
                return BadRequest("Invalid Client Request");

            var principal = GetPrincipleFromExpiredToken(tokenApiDto.AccessToken);
            var userIdValue = principal.FindFirstValue("UserId");

            if (!Guid.TryParse(userIdValue, out var userId))
                return BadRequest("Invalid Token");

            var user = await _authContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.RefreshToken != tokenApiDto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return BadRequest("Invalid Request");

            var newAccessToken = CreateJwt(user);
            var newRefreshToken = CreateRefreshToken();

            user.Token = newAccessToken;
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
            await _authContext.SaveChangesAsync();

            return Ok(new TokenApiDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpGet("details")]
        public async Task<ActionResult<UserResponseDto>> GetUserDetailsByEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("L'email de l'User est requis.");

            if (!_currentUser.IsAdmin && !string.Equals(_currentUser.Email, email, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound("User non trouve.");

            return Ok(ToUserResponse(user));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAll()
        {
            var users = await _authContext.Users
                .Select(user => new UserResponseDto
                {
                    UserId = user.UserId,
                    Nom = user.Nom,
                    Prenom = user.Prenom,
                    Email = user.Email,
                    Telephone = user.Telephone,
                    Role = user.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        private string CreateJwt(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, user.Nom),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = credentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private async Task<bool> CheckEmailExistAsync(string email)
            => await _authContext.Users.AnyAsync(x => x.Email == email);

        private static string CheckPasswordStrength(string pass)
        {
            StringBuilder sb = new StringBuilder();
            if (pass.Length < 9)
                sb.Append("Minimum password length should be 8\n");
            if (!(Regex.IsMatch(pass, "[a-z]") && Regex.IsMatch(pass, "[A-Z]") && Regex.IsMatch(pass, "[0-9]")))
                sb.Append("Password should be AlphaNumeric\n");
            if (!Regex.IsMatch(pass, "[<,>,@,!,#,$,%,^,&,*,(,),_,+,\\[,\\],{,},?,:,;,|,',\\,.,/,~,`,-,=]"))
                sb.Append("Password should contain special charcter\n");
            return sb.ToString();
        }

        private string CreateRefreshToken()
        {
            var tokenBytes = new byte[64];
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                rngCryptoServiceProvider.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        private ClaimsPrincipal GetPrincipleFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid Token");

            return principal;
        }

        private static UserResponseDto ToUserResponse(User user)
        {
            return new UserResponseDto
            {
                UserId = user.UserId,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Telephone = user.Telephone,
                Role = user.Role
            };
        }
    }
}
