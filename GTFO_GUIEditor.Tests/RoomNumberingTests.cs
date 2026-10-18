using System.Collections.Generic;
using System.Linq;
using GTFO_GUIEditor.Models;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class RoomNumberingTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(1, "B")]
    [InlineData(2, "C")]
    [InlineData(25, "Z")]
    [InlineData(26, "A2")]
    [InlineData(27, "B2")]
    [InlineData(51, "Z2")]
    [InlineData(52, "A3")]
    [InlineData(77, "Z3")]
    [InlineData(78, "A4")]
    public void PlacedRoomNode_GetRoomLetter_CyclesCorrectly(int roomNumber, string expectedLetter)
    {
        string letter = PlacedRoomNode.GetRoomLetter(roomNumber);
        Assert.Equal(expectedLetter, letter);
    }

    [Fact]
    public void PlacedRoomNode_Properties_FormatCorrectly()
    {
        var room = new PlacedRoomNode
        {
            Zone = 0,
            RoomNumber = 0
        };

        Assert.Equal(0, room.RoomNumber);
        Assert.Equal("A", room.RoomLetter);
        Assert.Equal("Room A", room.RoomName);
        Assert.Equal("Zone 0, Room A", room.UserFacingDisplayName);

        room.Zone = 5;
        room.RoomNumber = 26;

        Assert.Equal(26, room.RoomNumber);
        Assert.Equal("A2", room.RoomLetter);
        Assert.Equal("Room A2", room.RoomName);
        Assert.Equal("Zone 5, Room A2", room.UserFacingDisplayName);
    }

    [Fact]
    public void LevelLayoutGridState_SequentialRoomsInZone_AreAssignedIncreasingNumbers()
    {
        var grid = new LevelLayoutGridState();
        var geo = new PlacedGeomorphNode { Id = "geo1", DisplayName = "geo1" };
        grid.PlacedGeomorphs.Add(geo);

        var rooms = new List<PlacedRoomNode>();
        for (int i = 0; i < 10; i++)
        {
            var room = new PlacedRoomNode
            {
                Id = $"room_{i}",
                Geomorph = geo,
                AreaIndex = i,
                Zone = 0
            };
            rooms.Add(room);
            grid.PlacedRooms.Add(room);
        }

        grid.UpdateRoomNumbers();

        var expectedLetters = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(i, rooms[i].RoomNumber);
            Assert.Equal(expectedLetters[i], rooms[i].RoomLetter);
            Assert.Equal($"Room {expectedLetters[i]}", rooms[i].RoomName);
            Assert.Equal($"Zone 0, Room {expectedLetters[i]}", rooms[i].UserFacingDisplayName);
        }
    }

    [Fact]
    public void LevelLayoutGridState_RemovingRoom_ReindexesWithoutGaps()
    {
        var grid = new LevelLayoutGridState();
        var geo = new PlacedGeomorphNode { Id = "geo1", DisplayName = "geo1" };
        grid.PlacedGeomorphs.Add(geo);

        var rooms = new List<PlacedRoomNode>();
        for (int i = 0; i < 10; i++)
        {
            var room = new PlacedRoomNode
            {
                Id = $"room_{i}",
                Geomorph = geo,
                AreaIndex = i,
                Zone = 0
            };
            rooms.Add(room);
            grid.PlacedRooms.Add(room);
        }

        grid.UpdateRoomNumbers();

        // 10 rooms: A, B, C, D, E, F, G, H, I, J
        // Delete room 2 (C)
        grid.PlacedRooms.Remove(rooms[2]);
        grid.UpdateRoomNumbers();

        Assert.Equal(9, grid.PlacedRooms.Count);

        var expectedLetters = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I" };
        for (int i = 0; i < 9; i++)
        {
            Assert.Equal(i, grid.PlacedRooms[i].RoomNumber);
            Assert.Equal(expectedLetters[i], grid.PlacedRooms[i].RoomLetter);
            Assert.Equal($"Room {expectedLetters[i]}", grid.PlacedRooms[i].RoomName);
            Assert.Equal($"Zone 0, Room {expectedLetters[i]}", grid.PlacedRooms[i].UserFacingDisplayName);
        }

        // Specifically, the old room 3 (D) is now room 2 (C), old room 9 (J) is now room 8 (I)
        Assert.Equal("room_3", grid.PlacedRooms[2].Id);
        Assert.Equal("C", grid.PlacedRooms[2].RoomLetter);
        Assert.Equal("room_9", grid.PlacedRooms[8].Id);
        Assert.Equal("I", grid.PlacedRooms[8].RoomLetter);
    }

    [Fact]
    public void LevelLayoutGridState_MultipleZones_HaveIndependentRoomLetterSequences()
    {
        var grid = new LevelLayoutGridState();
        var geo = new PlacedGeomorphNode { Id = "geo1", DisplayName = "geo1" };
        grid.PlacedGeomorphs.Add(geo);

        var z0r0 = new PlacedRoomNode { Id = "z0_0", Geomorph = geo, AreaIndex = 0, Zone = 0 };
        var z1r0 = new PlacedRoomNode { Id = "z1_0", Geomorph = geo, AreaIndex = 1, Zone = 1 };
        var z0r1 = new PlacedRoomNode { Id = "z0_1", Geomorph = geo, AreaIndex = 2, Zone = 0 };
        var z1r1 = new PlacedRoomNode { Id = "z1_1", Geomorph = geo, AreaIndex = 3, Zone = 1 };
        var z0r2 = new PlacedRoomNode { Id = "z0_2", Geomorph = geo, AreaIndex = 4, Zone = 0 };

        grid.PlacedRooms.Add(z0r0);
        grid.PlacedRooms.Add(z1r0);
        grid.PlacedRooms.Add(z0r1);
        grid.PlacedRooms.Add(z1r1);
        grid.PlacedRooms.Add(z0r2);

        grid.UpdateRoomNumbers();

        // Zone 0 rooms in order of creation: z0_0 (0, A), z0_1 (1, B), z0_2 (2, C)
        Assert.Equal(0, z0r0.RoomNumber);
        Assert.Equal("A", z0r0.RoomLetter);
        Assert.Equal("Zone 0, Room A", z0r0.UserFacingDisplayName);

        Assert.Equal(1, z0r1.RoomNumber);
        Assert.Equal("B", z0r1.RoomLetter);
        Assert.Equal("Zone 0, Room B", z0r1.UserFacingDisplayName);

        Assert.Equal(2, z0r2.RoomNumber);
        Assert.Equal("C", z0r2.RoomLetter);
        Assert.Equal("Zone 0, Room C", z0r2.UserFacingDisplayName);

        // Zone 1 rooms in order of creation: z1_0 (0, A), z1_1 (1, B)
        Assert.Equal(0, z1r0.RoomNumber);
        Assert.Equal("A", z1r0.RoomLetter);
        Assert.Equal("Zone 1, Room A", z1r0.UserFacingDisplayName);

        Assert.Equal(1, z1r1.RoomNumber);
        Assert.Equal("B", z1r1.RoomLetter);
        Assert.Equal("Zone 1, Room B", z1r1.UserFacingDisplayName);
    }

    [Fact]
    public void LevelLayoutGridState_CreateRoomNode_AutomaticallyAssignsRoomNumber()
    {
        var grid = new LevelLayoutGridState();
        var geo = new PlacedGeomorphNode { Id = "geo1", DisplayName = "geo1" };
        grid.PlacedGeomorphs.Add(geo);

        var r0 = grid.CreateRoomNode(geo, 0);
        var r1 = grid.CreateRoomNode(geo, 1);
        var r2 = grid.CreateRoomNode(geo, 2);

        Assert.NotNull(r0);
        Assert.NotNull(r1);
        Assert.NotNull(r2);

        Assert.Equal(0, r0.RoomNumber);
        Assert.Equal("A", r0.RoomLetter);
        Assert.Equal("Room A", r0.RoomName);

        Assert.Equal(1, r1.RoomNumber);
        Assert.Equal("B", r1.RoomLetter);
        Assert.Equal("Room B", r1.RoomName);

        Assert.Equal(2, r2.RoomNumber);
        Assert.Equal("C", r2.RoomLetter);
        Assert.Equal("Room C", r2.RoomName);
    }
}
