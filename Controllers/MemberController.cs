using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinderenApi.Configurations;
using VinderenApi.DbContext;

namespace VinderenApi.Controllers
{
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[ApiController]
	[Route("api/[controller]")]
	public class MemberController : ControllerBase
	{
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly ILogger<AuthController> _logger;

		public MemberController(UserManager<IdentityUser> userManager, 
			RoleManager<IdentityRole> roleManager, 
			JwtConfig jwtConfig, 
			DbContextOptions<EntityContext> identityDbContextOptions, 
			ILogger<AuthController> logger) 
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_logger = logger;
		}

		[HttpGet]
		[Route("GetAllRoles")]
		public IActionResult GetAllRoles()
		{
			var roles = _roleManager.Roles.ToList();

			return Ok(roles);
		}

		[HttpGet]
		[Route("GetAllUsers")]
		public IActionResult GetAllUsers()
		{
			var users = _userManager.Users.ToList();

			return Ok(users);
		}

		[HttpPost]
		[Route("CreateRole")]
		public async Task<IActionResult> CreateRole(string name)
		{
			//Check if the role exist
			var roleExist = await _roleManager.RoleExistsAsync(name);

			if (!roleExist)
			{
				var roleResult = await _roleManager.CreateAsync(new IdentityRole(name));

				if (roleResult.Succeeded)
				{
					_logger.LogInformation($"The role {name} has been added successfully");
					//201 response
					return CreatedAtAction("GetAllRoles", new { name }, new
					{
						//Defining GetAllRoles refers the method above, but this response will generate GET request URL like: "https://localhost:7070/api/auth/Auth?name=Admin"
						result = $"The role {name} has been added successfully"
					});
				}
				else
				{
					_logger.LogInformation($"The role {name} has NOT been added successfully");
					return BadRequest(new
					{
						result = $"The role {name} has NOT been added"
					});

				}
			}

			return BadRequest(new { error = "Role already exist" });
		}

		[HttpPost]
		[Route("AddUserToRole")]
		public async Task<IActionResult> AddUserToRole(string email, string roleName)
		{
			//Check if user exist
			var user = await _userManager.FindByNameAsync(email);

			if (user == null)
			{
				_logger.LogInformation($"User {email}, does not exist");
				return BadRequest(new
				{
					error = "User does not exist"
				});
			}

			//Check if the role exist
			var roleExist = await _roleManager.RoleExistsAsync(roleName);

			if (!roleExist)
			{
				_logger.LogInformation($"Role {roleName}, does not exist");
				return BadRequest(new
				{
					error = "The role does not exist"
				});
			}

			var result = await _userManager.AddToRoleAsync(user, roleName);

			//Check if user has been assigned to the role successfully
			if (result.Succeeded)
			{
				return Ok(new
				{
					result = "Success, user has been added to the role!"
				});

			}
			else
			{
				_logger.LogInformation($"User {user}, was not added to the role");
				return BadRequest(new
				{
					error = "User was not able to be added to the role"
				});
			}

		}

		[HttpGet]
		[Route("GetUserRole")]
		public async Task<IActionResult> GetUserRole(string email)
		{
			var user = await _userManager.FindByEmailAsync(email);

			if (user == null) //if user does not exist
			{
				_logger.LogInformation($"The user with email: {email} does not exist");
				return BadRequest(new
				{
					error = "User does not exist"
				});
			}

			var roles = await _userManager.GetRolesAsync(user);

			return Ok(roles.ToList()); //to a list, because one user can have multiple roles...
		}

		[HttpPost]
		[Route("RemoveUserFromRole")]
		public async Task<IActionResult> RemoveUserFromRole(string email, string roleName)
		{
			//Check if user exist
			var user = await _userManager.FindByEmailAsync(email);

			if(user == null) 
			{
				_logger.LogInformation($"The user with email: {email} does not exist");
				return BadRequest(new
				{
					error = "User does not exist"
				});
			}

			//Check if the role exist
			var roleExist = await _roleManager.RoleExistsAsync(roleName);

			if(!roleExist) 
			{
				_logger.LogInformation($"Role {roleName}, does not exist");
				return BadRequest(new
				{
					error = "The role does not exist"
				});
			}

			var result = await _userManager.RemoveFromRoleAsync(user, roleName);

			if(result.Succeeded) 
			{
				return Ok(new
				{
					result = $"User {email} has been removed from role {roleName}"
				});
			} else
			{
				return BadRequest(new
				{
					error = $"Unable to remove the User {email} from role {roleName}"
				});
			}
		}

		[HttpPost]
		[Route("DeleteUser")]
		public async Task<IActionResult> DeleteUser(string email)
		{
			//Check if user exist
			var user = await _userManager.FindByEmailAsync(email);

			if (user == null)
			{
				_logger.LogInformation($"The user with email: {email} does not exist");
				return BadRequest(new
				{
					error = "User does not exist"
				});
			}
			
			var result = await _userManager.DeleteAsync(user);

			if (result.Succeeded)
			{
				return Ok(new
				{
					result = $"The User {email} has been deleted successfully."
				});
			}else
			{
				return BadRequest(new
				{
					error = $"Unable to delete User: {email}"
				});
			}
		}

	}
}
