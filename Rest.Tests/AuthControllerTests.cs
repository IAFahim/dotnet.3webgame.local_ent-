using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rest.Controllers;
using Rest.Models;
using Rest.Services;
using System.Security.Claims;
using Xunit;

namespace Rest.Tests;

public class AuthControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);
        
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null, null, null, null);

        _controller = new AuthController(
            _userServiceMock.Object,
            _tokenServiceMock.Object,
            _signInManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Register_ValidModel_ReturnsOkWithToken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password123!"
        };

        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        _userServiceMock
            .Setup(x => x.RegisterAsync(registerDto))
            .ReturnsAsync((true, user, Enumerable.Empty<string>()));

        _tokenServiceMock
            .Setup(x => x.GenerateJwtToken(user))
            .Returns("test-token");

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal("test-token", response.Token);
        Assert.Equal("testuser", response.Username);
        Assert.Equal("test@test.com", response.Email);
        Assert.Equal(100.00m, response.CoinBalance);
    }

    [Fact]
    public async Task Register_FailedRegistration_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "weak"
        };

        var errors = new[] { "Password too weak" };
        _userServiceMock
            .Setup(x => x.RegisterAsync(registerDto))
            .ReturnsAsync((false, null, errors));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "testuser",
            Email = "test@test.com",
            CoinBalance = 100.00m
        };

        _userServiceMock
            .Setup(x => x.LoginAsync(loginDto))
            .ReturnsAsync((true, user));

        _tokenServiceMock
            .Setup(x => x.GenerateJwtToken(user))
            .Returns("test-token");

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        Assert.Equal("test-token", response.Token);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        _userServiceMock
            .Setup(x => x.LoginAsync(loginDto))
            .ReturnsAsync((false, null));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
    

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = "123";
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _userServiceMock
            .Setup(x => x.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword))
            .ReturnsAsync((true, Enumerable.Empty<string>()));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.ChangePassword(changePasswordDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
