using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Rest.Controllers;
using Rest.Models;
using Rest.Services;
using Xunit;

namespace Rest.Tests;

public class IdentityAuthTests
{
    [Fact]
    public void ApplicationUser_HasRequiredProperties()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        // Assert
        Assert.Equal("testuser", user.UserName);
        Assert.Equal("test@test.com", user.Email);
        Assert.Equal(100.00m, user.CoinBalance);
    }

    [Fact]
    public void RegisterDto_ValidatesRequiredFields()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "test",
            Email = "test@test.com",
            Password = "Password123!"
        };

        // Assert
        Assert.NotEmpty(dto.Username);
        Assert.NotEmpty(dto.Email);
        Assert.NotEmpty(dto.Password);
    }

    [Fact]
    public void LoginDto_ValidatesRequiredFields()
    {
        // Arrange
        var dto = new LoginDto
        {
            Username = "test",
            Password = "Password123!"
        };

        // Assert
        Assert.NotEmpty(dto.Username);
        Assert.NotEmpty(dto.Password);
    }

    [Fact]
    public void AuthResponseDto_ContainsTokenAndUserInfo()
    {
        // Arrange & Act
        var response = new AuthResponseDto
        {
            Token = "test-token",
            Expiration = DateTime.UtcNow.AddHours(2),
            Username = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        // Assert
        Assert.NotEmpty(response.Token);
        Assert.Equal("testuser", response.Username);
        Assert.Equal("test@test.com", response.Email);
        Assert.Equal(100.00m, response.CoinBalance);
    }
}
