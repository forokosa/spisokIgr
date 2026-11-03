using System.Buffers.Binary;
using System.Text;

namespace GameTrack.Infrastructure.Persistence;

/// <summary>
/// Однозначное соответствие между публичным идентификатором игры (Guid в Domain
/// и в контрактах API) и техническим ключом БД (games.id bigint identity).
///
/// Формула обратима и не требует хранения соответствий:
/// <code>
///   guid[0..7]   = bigint id (Big-Endian)
///   guid[8..15]  = первые 8 байт SHA-256("gametrack.game")
/// </code>
/// Сравнение <c>guid[0..7] == id</c> транслируется EF Core в SQL-условие
/// <c>id = @p</c>, поэтому фильтрация по игроку остаётся на стороне БД
/// и использует первичный ключ, а схема не меняется.
/// </summary>
public static class GameKey
{
    private const int SequenceBytes = 8;

    /// <summary>Фиксированный хвост: отличает ключ игры от прочих UUID.</summary>
    private static readonly byte[] Suffix = ComputeSuffix();

    public static Guid ToPublicId(long sequence)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Ключ игры должен быть положительным.");
        }

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt64BigEndian(bytes[..SequenceBytes], sequence);
        Suffix.CopyTo(bytes[SequenceBytes..]);

        return new Guid(bytes, bigEndian: true);
    }

    public static bool TryGetSequence(Guid publicId, out long sequence)
    {
        Span<byte> bytes = stackalloc byte[16];
        publicId.TryWriteBytes(bytes, bigEndian: true, out _);

        if (!bytes[SequenceBytes..].SequenceEqual(Suffix))
        {
            sequence = 0;
            return false;
        }

        sequence = BinaryPrimitives.ReadInt64BigEndian(bytes[..SequenceBytes]);
        return sequence > 0;
    }

    private static byte[] ComputeSuffix()
    {
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("gametrack.game"));
        return hash[..SequenceBytes];
    }
}
