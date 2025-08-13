using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace CompanyApp.Web.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<IdentityUser> userManager, IConfiguration cfg) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user==null || !(await userManager.CheckPasswordAsync(user, dto.Password)))
            return Unauthorized();

        var jwt = cfg.GetSection("Jwt");
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.UserName!) },
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

public record LoginDto(string Email, string Password);
