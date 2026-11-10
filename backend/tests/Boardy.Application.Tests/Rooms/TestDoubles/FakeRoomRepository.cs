namespace Boardy.Application.Tests.Rooms.TestDoubles;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Domain.Rooms;

internal sealed class FakeRoomRepository : IRoomRepository
{
    private readonly FakeUnitOfWork _unitOfWork;

    public Dictionary<Guid, Room> CommittedRoomsById { get; } = [];
    public Dictionary<string, Guid> CommittedRoomIdsByCode { get; } = [];

    public bool ShouldFailOnAdd { get; set; }
    public bool ShouldFailOnUpdate { get; set; }

    public FakeRoomRepository(FakeUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommittedRoomsById.TryGetValue(id, out var room);
        return Task.FromResult(room);
    }

    public Task<Room?> GetByCodeAsync(RoomCode code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (CommittedRoomIdsByCode.TryGetValue(code.Value, out var id) &&
            CommittedRoomsById.TryGetValue(id, out var room) &&
            room.Code == code)
        {
            return Task.FromResult<Room?>(room);
        }

        return Task.FromResult<Room?>(null);
    }

    public Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldFailOnAdd)
        {
            throw new InvalidOperationException("Simulated repository failure on Add.");
        }

        _unitOfWork.RegisterCommitAction(() =>
        {
            var oldEntries = CommittedRoomIdsByCode
                .Where(kvp => kvp.Value == room.Id && kvp.Key != room.Code.Value)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var oldCode in oldEntries)
            {
                CommittedRoomIdsByCode.Remove(oldCode);
            }

            CommittedRoomsById[room.Id] = room;
            CommittedRoomIdsByCode[room.Code.Value] = room.Id;
        });

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldFailOnUpdate)
        {
            throw new InvalidOperationException("Simulated repository failure on Update.");
        }

        _unitOfWork.RegisterCommitAction(() =>
        {
            var oldEntries = CommittedRoomIdsByCode
                .Where(kvp => kvp.Value == room.Id && kvp.Key != room.Code.Value)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var oldCode in oldEntries)
            {
                CommittedRoomIdsByCode.Remove(oldCode);
            }

            CommittedRoomsById[room.Id] = room;
            CommittedRoomIdsByCode[room.Code.Value] = room.Id;
        });

        return Task.CompletedTask;
    }
}
