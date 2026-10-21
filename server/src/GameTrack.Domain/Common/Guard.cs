namespace GameTrack.Domain.Common;

/// <summary>
/// Набор защитных проверок доменного слоя.
/// Любые данные, пришедшие извне, считаются недоверенными.
/// </summary>
public static class Guard
{
    public static T NotNull<T>(T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName, $"Параметр '{paramName}' обязателен.");

    public static string NotBlank(string? value, string paramName, int minLength = 1, int maxLength = 500)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName, $"Параметр '{paramName}' обязателен.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < minLength)
        {
            throw new ArgumentException(
                $"Параметр '{paramName}' должен содержать минимум {minLength} символов.",
                paramName);
        }

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                $"Параметр '{paramName}' не должен превышать {maxLength} символов.");
        }

        return trimmed;
    }

    public static string? OptionalString(string? value, string paramName, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                $"Параметр '{paramName}' не должен превышать {maxLength} символов.");
        }

        return trimmed;
    }

    public static decimal? Rating(decimal? value, string paramName)
    {
        if (value is null)
        {
            return null;
        }

        if (value is < 0m or > 10m)
        {
            throw new ArgumentOutOfRangeException(paramName, "Рейтинг должен быть в диапазоне от 0 до 10.");
        }

        return decimal.Round(value.Value, 1, MidpointRounding.AwayFromZero);
    }

    public static int? ReleaseYear(int? value, string paramName, int minYear = 1970, int maxYear = 2100)
    {
        if (value is null)
        {
            return null;
        }

        if (value < minYear || value > maxYear)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                $"Год выпуска должен быть в диапазоне от {minYear} до {maxYear}.");
        }

        return value;
    }

    public static string Email(string? value, string paramName)
    {
        var email = NotBlank(value, paramName, 5, 254);
        if (!IsValidEmail(email))
        {
            throw new ArgumentException("Введите корректный email.", paramName);
        }

        return email.ToLowerInvariant();
    }

    /// <summary>Проверяет пароль для регистрации: длина, буквы и цифры.</summary>
    public static string Password(string? value, string paramName, int minLength = 8, int maxLength = 128)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName, $"Параметр '{paramName}' обязателен.");
        }

        if (value.Length < minLength || value.Length > maxLength)
        {
            throw new ArgumentException(
                $"Пароль должен содержать от {minLength} до {maxLength} символов.",
                paramName);
        }

        if (!value.Any(char.IsLetter) || !value.Any(char.IsDigit))
        {
            throw new ArgumentException(
                "Пароль должен содержать минимум одну букву и одну цифру.",
                paramName);
        }

        return value;
    }

    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var at = value.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at == value.Length - 1 || value.Count(c => c == '@') != 1)
        {
            return false;
        }

        var domain = value[(at + 1)..];
        return domain.Contains('.', StringComparison.Ordinal)
               && !domain.StartsWith(".", StringComparison.Ordinal)
               && !domain.EndsWith(".", StringComparison.Ordinal)
               && !value.Any(char.IsWhiteSpace);
    }

    public static Guid Id(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Идентификатор не может быть пустым.", paramName);
        }

        return value;
    }

    public static int Page(int page, string paramName = "page")
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(paramName, "Номер страницы должен быть больше или равен 1.");
        }

        return page;
    }

    public static int PageSize(int pageSize, int maxPageSize, string paramName = "pageSize")
    {
        if (pageSize < 1 || pageSize > maxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                $"Размер страницы должен быть от 1 до {maxPageSize}.");
        }

        return pageSize;
    }
}
