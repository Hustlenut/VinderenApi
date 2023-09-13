using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VinderenApi.DbContext;
using VinderenApi.Configurations;

var builder = WebApplication.CreateBuilder(args);
//TODO: Provide secure policies
builder.Services.AddCors(
    options =>
    {
        options.AddPolicy("AllowAll",
            builder => builder
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod()

        );
    }
);

// Add services to the container.

//Razor pages
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AddPageRoute("/", "/Pages");
    });

// Adding IdentityDbContext
// Get the secret connection string before declaring the var.
builder.Configuration.AddUserSecrets<Program>();
// For development:
var dbConn = builder.Configuration["Secret:SmarterASPNET"];

builder.Services.AddDbContext<EntityContext>(options =>
{
    options.UseSqlServer(dbConn); //builder.Configuration.GetConnectionString() gets the string from appsettings.
    options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()));
});

//TODO: Need to configure secret for production pipeline...

// Create a singleton from a secret value to later generate a JWT token...
var jwtConfigValue = builder.Configuration["Secret2:JwtConfig"];

//TODO: Need to configure secret2 for production pipeline...

var jwtConfig = new JwtConfig
{
    Secret = jwtConfigValue
};

builder.Services.AddSingleton(jwtConfig);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(jwt =>
    {
        var key = Encoding.ASCII.GetBytes(jwtConfig.Secret); // Upon receiving the token back from the client, this specifies where the "authorizer" should be comparing.

        jwt.SaveToken = true;
        // These parameters ensures that the token is validated correctly by intercepting the http requests
        jwt.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true, //if true, an issuer is generally the host server for (this) API
            ValidateAudience = true, // if true, validates the expected receiver 
            RequireExpirationTime = false, 
            ValidateLifetime = true
        };
    });
//Configures the usage of Identity.
//Essensially connects and configures the EntityContext to the tables like AspNetUser and AspNetRoles in the database.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
//.AddEntityFrameworkStores<IdentityEntity>();
.AddRoles<IdentityRole>()
.AddUserStore<UserStore<IdentityUser, IdentityRole, EntityContext, string>>()
.AddRoleStore<RoleStore<IdentityRole, EntityContext, string>>()
.AddDefaultTokenProviders()
.AddEntityFrameworkStores<EntityContext>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapRazorPages();

app.Run();
