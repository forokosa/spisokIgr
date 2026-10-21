namespace GameTrack.Domain.Common;

/// <summary>Доменная сущность не найдена (HTTP 404).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Нарушено уникальное бизнес-правило (HTTP 409).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Пользователь не прошёл аутентификацию (HTTP 401).</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
