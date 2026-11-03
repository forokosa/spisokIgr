using GameTrack.Domain.Repositories;
using GameTrack.Domain.Services;

namespace GameTrack.Tests.Fakes;

/// <summary>Провайдер текущего пользователя для unit-тестов.</summary>
public sealed class FakeUserContextProvider : IUserContextProvider
{
    public FakeUserContextProvider(UserContext user)
    {
        User = user;
    }

    public FakeUserContextProvider(Guid userId, string login = "demo")
        : this(new UserContext(userId, login))
    {
    }

    public UserContext User { get; set; }

    public UserContext GetRequiredUser() => User;
}
