using GameTrack.Domain.Common;
using GameTrack.Domain.Repositories;

namespace GameTrack.Domain.Services;

/// <summary>
/// Изоляция данных по пользователю. Доменный сервис получает текущего
/// пользователя явно, поэтому не может «забыть» про фильтр: без
/// <see cref="UserContext"/> операция с библиотекой просто не выполнится.
/// </summary>
public interface IUserContextProvider
{
    /// <summary>Пользователь текущего запроса или исключение, если он не определён.</summary>
    UserContext GetRequiredUser();
}
