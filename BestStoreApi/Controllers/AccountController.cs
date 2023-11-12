using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
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
                CreatedAt = DateTime.Now
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
                CreatedAt = DateTime.Now
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
                Role = "admin",
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = profile
            };

            return Ok(response);
        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(string email) 
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if(user == null)
            {
                return NotFound();
            }

            //delete old password
            var oldPasswordReset = context.PasswordResets.FirstOrDefault(r => r.Email == email);
            if(oldPasswordReset != null)
            {
                context.Remove(oldPasswordReset);
            }

            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            var pwdReset = new PasswordReset()
            { 
                Email = email,
                Token = token,
                CreatedAt = DateTime.Now
            };

            context.PasswordResets.Add(pwdReset);
            context.SaveChanges();

            return Ok(token);
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword(string token, string password)
        {
            var pwdReset = context.PasswordResets.FirstOrDefault(r => r.Token == token);
            if(pwdReset == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token1");
                return BadRequest(ModelState);
            }

            var user = context.Users.FirstOrDefault(u => u.Email == pwdReset!.Email);
            if(user == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token2");
                return BadRequest(ModelState);
            }

            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new Models.User(), password);

            //save the new encrypted password
            user.Password = encryptedPassword;

            //delete the token
            context.PasswordResets.Remove(pwdReset!);

            context.SaveChanges();

            return Ok();
        }

        [Authorize]
        [HttpGet("Profile")]
        public IActionResult GetProfile()
        {
            int id = GetUserId();

            var user = context.Users.Find(id);
            if(user == null)
            {
                return Unauthorized();
            }

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto);
        }

        [Authorize]
        [HttpPut("UpdateProfile")]
        public IActionResult UpdateProfile(UserProfileUpdateDto userProfileUpdateDto)
        {
            int id = GetUserId();

            var user = context.Users.Find(id);
            if(user == null)
            {
                return Unauthorized();
            }

            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone ?? "";
            user.Address = userProfileUpdateDto.Address;

            context.SaveChanges();

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto);
        }

        [Authorize]
        [HttpPut("UpdatePassword")]
        public IActionResult UpdatePassword([Required, MinLength(8), MaxLength(20)]string password)
        {
            int id = GetUserId();
            
            var user = context.Users.Find(id);
            if(user == null)
            {
                return Unauthorized();
            }

            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new User(), password);

            user.Password = encryptedPassword;

            context.SaveChanges();

            return Ok();
        }

        private int GetUserId()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return 0;
            }

            var claim = identity.Claims.FirstOrDefault(c => c.Type.ToLower() == "id");
            if (claim == null)
            {
                return 0;
            }

            int id;
            try
            {
                id = int.Parse(claim.Value);
            }
            catch (Exception)
            {
                return 0;
            }

            return id;
        }

        [Authorize]
        [HttpGet("GetTokenClaims")]
        public IActionResult GetTokenClaims()
        {
            var identity = User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                Dictionary<string, string> claims = new Dictionary<string, string>();

                foreach (var claim in identity.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }

                return Ok(claims);
            }

            return Ok();
        }

        /* [Authorize]
         [HttpGet("AuthorizeAuthenticatedUsers")]
         public IActionResult AuthorizeAuthenticatedUsers()
         {
             return Ok("You are authorized");
         }

         [Authorize(Roles = "admin")]
         [HttpGet("AuthorizeAdmin")]
         public IActionResult AuthorizeAdmin()
         {
             return Ok("You are authorized");
         }

         [Authorize(Roles = "admin, seller")]
         [HttpGet("AuthorizeAdminAndSeller")]
         public IActionResult AuthorizeAdminAndSeller()
         {
             return Ok("You are authorized");
         }*/


        /*[HttpGet("TestToken")]
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
                new Claim("id", "" + user.Id),
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
