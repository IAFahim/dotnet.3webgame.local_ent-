using Bogus;
using Rest.Models;

namespace Rest.Tests.Helpers;

public static class TestDataBuilder
{
    public static Faker<ApplicationUser> GetUserFaker()
    {
        return new Faker<ApplicationUser>()
            .RuleFor(u => u.Id, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.UserName, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.EmailConfirmed, f => f.Random.Bool())
            .RuleFor(u => u.NormalizedUserName, (f, u) => u.UserName!.ToUpper())
            .RuleFor(u => u.NormalizedEmail, (f, u) => u.Email!.ToUpper())
            .RuleFor(u => u.SecurityStamp, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.ConcurrencyStamp, f => Guid.NewGuid().ToString())
            .RuleFor(u => u.CreatedAt, f => f.Date.Past())
            .RuleFor(u => u.LastLoginAt, f => f.Date.Recent());
    }

    public static ApplicationUser CreateValidUser(string? username = null, string? email = null)
    {
        var faker = GetUserFaker();
        var user = faker.Generate();

        if (username != null)
        {
            user.UserName = username;
        }

        if (email != null)
        {
            user.Email = email;
        }

        return user;
    }

    public static RefreshToken CreateValidRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow
        };
    }
}
