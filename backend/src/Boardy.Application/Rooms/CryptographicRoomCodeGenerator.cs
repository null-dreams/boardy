namespace Boardy.Application.Rooms;

using System.Security.Cryptography;
using Boardy.Domain.Rooms;

public sealed class CryptographicRoomCodeGenerator : IRoomCodeGenerator
{
    public const int DefaultLength = 6;
    private const string AllowedCharacters = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private readonly int _length;

    public CryptographicRoomCodeGenerator(int length = DefaultLength)
    {
        if (length is < 6 or > 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "Room code length must be between 6 and 8 characters.");
        }

        _length = length;
    }

    public RoomCode Generate()
    {
        Span<char> chars = stackalloc char[_length];

        for (int i = 0; i < _length; i++)
        {
            int index = RandomNumberGenerator.GetInt32(AllowedCharacters.Length);
            chars[i] = AllowedCharacters[index];
        }

        return RoomCode.Create(new string(chars));
    }
}
