using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;

        public AccountController(IConfiguration configuration, ApplicationDbContext context)
        {
            this.configuration = configuration;
            this.context = context;
        }

        [HttpPost("Register")]
        public IActionResult Register(UserDto userDto)
        {
            //check if the email address is already used
            var emailCount = context.Users.Count(u => u.Email == userDto.Email);
            if(emailCount > 0)
            {
                ModelState.AddModelError("Email", "This email address is already used");
                return BadRequest(ModelState);
            }

            //encrypt the password

            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            //create a new account
            User user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? "",
                Address = userDto.Address,
                Password = encryptedPassword,
                Role = "client",
                createdAt = DateTime.Now
            };

            context.Users.Add(user);
            context.SaveChanges();

            var jwt = CreateJWToken(user);

            UserProfileDto profile = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = "client",
                createdAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = user
            };

            return Ok(response);
        }

        [HttpPost("Login")]
        public IActionResult Login(string email, string password)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if(user == null)
            {
                ModelState.AddModelError("Error", "Emil or Password not valid");
                return BadRequest(ModelState);
            }

            //verify the password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(new User(), user.Password, password);

            if(result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "Wrong Password");
                return BadRequest(ModelState);
            }

            var jwt = CreateJWToken(user);
            UserProfileDto profile = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = "client",
                createdAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = profile
            };

            return Ok(response);
        }

/*        [HttpGet("TestToken")]
        public IActionResult TestToken()
        {
            User user = new User()
            {
                Id = 2,
                Role = "admin"
            };

            string jwt = CreateJWToken(user);
            var response = new { JWToken = jwt };

            return Ok(response);
        }*/

        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("sub", "" + user.Id),
                new Claim("role", user.Role)
            };

            string strKey = configuration["JwtSettings:Key"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["JwtSettings:Issuer"],
                audience: configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
