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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace VinderenApi.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtConfig _jwtConfig;
        private readonly DbContextOptions<EntityContext> _identityDbContextOptions;
        private readonly ILogger<AuthController> _logger;
		private readonly IMemoryCache _memoryCache;
        private readonly CacheKeyConfig _cacheKeyConfig;
        private string loginAttemptsCacheKeyPrefix;
        //See here: I did not have to instantiate the LoginAttemptInfo class here or anything right?

        private int maxLoginAttempts = 5;
        private int baseDelaySeconds = 5;
        //INFO TO FRONTEND: The login attempt delay is doubling the delay on each attempt.


		public AuthController(UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtConfig jwtConfig,
            DbContextOptions<EntityContext> identityDbContextOptions,
            ILogger<AuthController> logger,
            IMemoryCache memoryCache
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtConfig = jwtConfig;
            _identityDbContextOptions = identityDbContextOptions;
            _logger = logger;
            _memoryCache = memoryCache;


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
                    //Add user to the Student role
                    await _userManager.AddToRoleAsync(new_user, "Student");

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
                        Token = await token
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
                loginAttemptsCacheKeyPrefix = AssignLoginCacheKey(); //Null value is handled in method
				// Create a cache key for the user's login attempts based additionally on the email.
				string cacheKey = $"{loginAttemptsCacheKeyPrefix}:{loginRequest.Email}";

				// Try to retrieve the login attempts counter from the cache
				if (!_memoryCache.TryGetValue(cacheKey, out LoginAttemptInfo loginAttemptInfo))
				{
					loginAttemptInfo = new LoginAttemptInfo
					{
						Attempts = 0,
						ExpiryDate = DateTime.Now
					};
				}

                // If current date-time is less than the expiry date-time, it means the key has not expired
                if (DateTime.Now < loginAttemptInfo.ExpiryDate)
                {
                    if (loginAttemptInfo.Attempts >= maxLoginAttempts)
                    {
                        return BadRequest(new AuthResult()
                        {
                            Errors = new List<string>()
                            {
                                "Too many login attempts. Please try again later."
                            },
                            Result = false
                        });
                    }
					// Enforce the delay currently stored in cache
					int delaySeconds = (int)Math.Pow(2, loginAttemptInfo.Attempts) * baseDelaySeconds; //basically 2^loginAttemptInfo.Attempts * baseDelaySeconds
					await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
				}

					//Check if user exist
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
                {
					// Increment the login attempts counter 
					loginAttemptInfo.Attempts++;

					// Calculate the delay based on the number of failed attempts (doubling on each consecutive attempt)
					int delaySeconds = (int)Math.Pow(2, loginAttemptInfo.Attempts - 1) * baseDelaySeconds;

					// Store the updated login attempts counter in the cache with an expiration time
					_memoryCache.Set(cacheKey, loginAttemptInfo, TimeSpan.FromSeconds(delaySeconds)); //The exp. time automatically removes the item from cache once the time is due.

					return BadRequest(new AuthResult()
                    {
                        Errors = new List<string>()
                        {
                            "Invalid credentials"
                        },
                        Result = false
                    });
                }

                string jwtToken = await GenerateJwtToken(existing_user);

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

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
                {
                    new Claim("user", user.ToString()),
                    //new Claim("role", "Admin")
				};

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim("roles", userRole)); //A new claim of type ClaimTypes.Role is added for each role in the list. This claim is added to the claims collection.

                var role = await _roleManager.FindByNameAsync(userRole); //Retrieves the role entity based on the role name (userRole) from the role manager.

                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);

                    foreach (var roleClaim in roleClaims)
                    {
                        claims.Add(roleClaim);
                    }
                }
            }

            var jwtTokenDescr = JwtHelper.GetJwtToken(
                user.ToString(),
                _jwtConfig.Secret,
                _jwtConfig.Issuer,
                _jwtConfig.Audience,
                2, //TODO: Re-evalueate 
                claims.ToArray()
                );

            var jwtToken = jwtTokenHandler.WriteToken(jwtTokenDescr);

            return jwtToken;
        }

        private string? AssignLoginCacheKey()
        {
            if (_cacheKeyConfig.LoginAttemptsCacheKeyPrefix != null)
            {
                string loginAttemptsCacheKeyPrefix = _cacheKeyConfig.LoginAttemptsCacheKeyPrefix;
                return loginAttemptsCacheKeyPrefix;
            }else
            {
                Console.Error.WriteLine("Login cache key could not be assigned");
                return null;
            }
        }

        //        private async Task<string> GenerateJwtToken(IdentityUser user)
        //        {
        //            var jwtTokenHandler = new JwtSecurityTokenHandler();
        //            var key = _jwtConfig?.Secret != null ? Encoding.UTF8.GetBytes(_jwtConfig.Secret) : null;
        //            //var key = Encoding.UTF8.GetBytes(_configuration.GetSection("JwtConfig:Secret").Value);

        //            var claims = GetAllValidClaims(user);

        //			//Token descriptor is describing the payload, which is going to allow us to put the configurations needed inside the token.
        //			var tokenDescriptor = new SecurityTokenDescriptor()
        //            {
        //                Subject = await claims,
        //                Expires = DateTime.Now.AddHours(1),
        //                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //            };

        //			/* Following method is used to create a JwtSecurityToken object, which represents the JWT. 
        //			 * Providing the necessary information for the JWT, such as claims, expiration, issuer, audience, and signing credentials. 
        //			 * It constructs the token in memory but doesn't encode or sign it. It's like preparing the content of the JWT.*/
        //			var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        //			/* Following method is used to take the JwtSecurityToken created with CreateToken() and serialize it into a string that 
        //			 * represents a JWT in the compact serialization format. This includes encoding the header and payload as Base64Url 
        //			 * strings and signing them with the specified signing algorithm and key. It produces the final JWT string that you 
        //			 * can use for authentication and authorization purposes.*/
        //            var jwtToken = jwtTokenHandler.WriteToken(token);

        //            return jwtToken;

        //        }

        //        private async Task<ClaimsIdentity> GetAllValidClaims(IdentityUser user) //Using ClaimsIdentity because SecurityTokenDescriptor.Subject requires it.
        //		{
        //			var claims = new ClaimsIdentity(new[]
        //{
        //					//new Claim("id", user.Id), //Claims the "Id" as user.Id which is a property of IdentityUser.
        //                    new Claim(ClaimTypes.NameIdentifier, user.Id),
        //					new Claim(ClaimTypes.Name, user.Id),
        //					new Claim(ClaimTypes.NameIdentifier, user.Email),
        //					new Claim(ClaimTypes.Name, user.Email),
        //					new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        //					new Claim(JwtRegisteredClaimNames.Email, user.Email),
        //					new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //					new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString())
        //			});

        //			// Get the user role and add it to the claims
        //			var userRoles = await _userManager.GetRolesAsync(user); //Gets a list of user roles the user belongs to

        //			/*The first loop adds a claim for each role the user belongs to, 
        //			 * and the second loop adds any additional claims associated with each of those roles. In the case where a role can have additional claims*/
        //			foreach (var userRole in userRoles)
        //			{
        //				claims.AddClaim(new Claim(ClaimTypes.Role, userRole)); //A new claim of type ClaimTypes.Role is added for each role in the list. This claim is added to the claims collection.

        //				var role = await _roleManager.FindByNameAsync(userRole); //Retrieves the role entity based on the role name (userRole) from the role manager.

        //				if (role != null)
        //				{
        //					var roleClaims = await _roleManager.GetClaimsAsync(role);

        //					foreach (var roleClaim in roleClaims)
        //					{
        //						claims.AddClaim(roleClaim);
        //					}
        //				}
        //			}

        //            return claims;
        //		}
    }
}
