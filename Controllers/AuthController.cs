using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using VinderenApi.DbContext;
using VinderenApi.Models.DTO;
using VinderenApi.Authentication;
using VinderenApi.Configurations;

namespace VinderenApi.Controllers
{
    [Route("api/auth/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        // private readonly IConfiguration _configuration;
        private readonly DbContextOptions<EntityContext> _identityDbContextOptions;

        public AuthController(UserManager<IdentityUser> userManager,
            JwtConfig jwtConfig,
            //IConfiguration configuration,
            DbContextOptions<EntityContext> identityDbContextOptions)
        {
            _userManager = userManager;
            _jwtConfig = jwtConfig;
            //_configuration = configuration;
            _identityDbContextOptions = identityDbContextOptions;

        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto requestDto) //[FromBody] means that it has to read from the body of the HTTP request
        {
            //Validate incoming request
            if (ModelState.IsValid) //if all the conditions are complied in UserRegRequestDto...
            {
                //Need to check if the email already exists
                var user_exist = await _userManager.FindByEmailAsync(requestDto.Email);

                if (user_exist != null)
                {
                    return BadRequest(new AuthResult
                    {
                        Result = false,
                        Errors = new List<string>()
                        {
                            "Email already exist"
                        }
                    });
                }

                //Create a user
                var new_user = new IdentityUser() //TODO: Access the new_user.Id and map it to another table.
                {//Maps the HTTP post request to the properties in IdentityUser
                    Email = requestDto.Email,
                    UserName = requestDto.Email
                };

                var is_created = await _userManager.CreateAsync(new_user, requestDto.Password);

                if (is_created.Succeeded)
                {
                    //Generate token
                    var token = GenerateJwtToken(new_user);

                    //var person = new Person
                    //{
                    //    Name = requestDto?.Name,
                    //    Email = requestDto?.Email,
                    //    Password = requestDto?.Password

                    //};

                    //using var context = new EntityContext(_identityDbContextOptions);
                    //context.identities?.Add(person);
                    //await context.SaveChangesAsync();

                    return Ok(new AuthResult()
                    {
                        Result = true,
                        Token = token
                    });
                }

                return BadRequest(new AuthResult()
                {
                    Errors = new List<string>()
                    {
                        "Server error"
                    },
                    Result = false
                });

            }

            return BadRequest();
        }

        [Route("Login")]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginRequest)
        {
            if (ModelState.IsValid)
            {
                //check if user exist
                var existing_user = await _userManager.FindByEmailAsync(loginRequest.Email);

                if (existing_user == null)
                {
                    return BadRequest(new AuthResult()
                    {
                        Errors = new List<string>()
                        {
                            "Invalid payload"
                        },
                        Result = false
                    });
                }

                var isCorrect = await _userManager.CheckPasswordAsync(existing_user, loginRequest.Password); 
                //Because without await, the code would continue executing immediately after calling CheckPasswordAsync, which might result in incorrect behavior because you would not have the result of the validation yet. 
                if (!isCorrect)
                    return BadRequest(new AuthResult()
                    {
                        Errors = new List<string>()
                        {
                            "Invalid credentials"
                        },
                        Result = false
                    });

                var jwtToken = GenerateJwtToken(existing_user);

                return Ok(new AuthResult()
                {
                    Token = jwtToken,
                    Result = true
                });
            }

            return BadRequest(new AuthResult()
            {
                Errors = new List<string>()
                {
                    "Invalid payload"
                },
                Result = false
            });
        }

        private string GenerateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = _jwtConfig?.Secret != null ? Encoding.UTF8.GetBytes(_jwtConfig.Secret) : null;
            //var key = Encoding.UTF8.GetBytes(_configuration.GetSection("JwtConfig:Secret").Value);

            //Token descriptor is describing the payload, which is going to allow us to put the configurations needed inside the token.
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[] 
                {
                    new Claim("Id", user.Id), //Claims the "Id" as user.Id which is a property of IdentityUser.
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())
                }),

                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return jwtToken;

        }


    }
}
