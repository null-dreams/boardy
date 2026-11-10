namespace Boardy.Application.Tests.Rooms.TestDoubles;

using Boardy.Application.Rooms;
using Boardy.Domain.Rooms;

internal sealed class StubRoomCodeGenerator : IRoomCodeGenerator
{
    private readonly Queue<RoomCode> _codes;

    public StubRoomCodeGenerator(params string[] codes)
    {
        _codes = new Queue<RoomCode>(codes.Select(RoomCode.Create));
    }

    public RoomCode Generate()
    {
        if (_codes.Count > 0)
        {
            return _codes.Dequeue();
        }

        return RoomCode.Create("DEFAULT");
    }
}
