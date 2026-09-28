using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.Services;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class MockClipboardService : IClipboardService
{
    public string? LastCopiedText { get; private set; }

    public Task SetTextAsync(string text)
    {
        LastCopiedText = text;
        return Task.CompletedTask;
    }
}

public class SoundTests
{
    [Fact]
    public void LoadSounds_ReturnsExpectedSoundEntries()
    {
        var sounds = SoundService.LoadSounds();
        Assert.NotEmpty(sounds);
        Assert.Equal(13729, sounds.Count);

        var first = sounds[0];
        Assert.Equal((uint)1071316888, first.Id);
        Assert.Equal("01_first_text_appear", first.Name);
        Assert.Equal(@"\Actor-Mixer Hierarchy\simon_sfx\intro_sequence\01_first_text_appear", first.WwisePath);
    }

    [Fact]
    public async Task SelectedSound_CopiesIdToClipboard()
    {
        var mockClipboard = new MockClipboardService();
        var vm = new SoundsViewModel(mockClipboard);
        var sound = vm.FilteredSounds.First();

        vm.SelectedSound = sound;
        await vm.CopySoundIdAsync(sound);

        Assert.Equal(sound.Id.ToString(), mockClipboard.LastCopiedText);
        Assert.Contains($"Copied ID {sound.Id} to clipboard", vm.StatusText);
    }

    [Fact]
    public void ParseLines_HandlesCsvLinesCorrectly()
    {
        var lines = new[]
        {
            "ID,Name,Wwise Object Path",
            "1071316888,01_first_text_appear,\\Actor-Mixer Hierarchy\\simon_sfx\\intro_sequence\\01_first_text_appear"
        };

        var parsed = SoundService.ParseLines(lines);
        Assert.Single(parsed);
        Assert.Equal((uint)1071316888, parsed[0].Id);
        Assert.Equal("01_first_text_appear", parsed[0].Name);
        Assert.Equal(@"\Actor-Mixer Hierarchy\simon_sfx\intro_sequence\01_first_text_appear", parsed[0].WwisePath);
    }

    [Fact]
    public void ParseLines_HandlesTsvLinesCorrectly()
    {
        var lines = new[]
        {
            "Event\tID\tName\t\t\tWwise Object Path\tNotes",
            "\t123456\tCustomSound\t\t\t\\Custom\\Path\\Sound\tSome Note"
        };

        var parsed = SoundService.ParseLines(lines);
        Assert.Single(parsed);
        Assert.Equal((uint)123456, parsed[0].Id);
        Assert.Equal("CustomSound", parsed[0].Name);
        Assert.Equal(@"\Custom\Path\Sound", parsed[0].WwisePath);
        Assert.Equal("Some Note", parsed[0].Notes);
    }

    [Fact]
    public void SoundsViewModel_FiltersById()
    {
        var vm = new SoundsViewModel();
        Assert.Equal(13729, vm.FilteredSounds.Count);

        vm.SearchText = "1071316888";
        Assert.Single(vm.FilteredSounds);
        Assert.Equal("01_first_text_appear", vm.FilteredSounds[0].Name);
    }

    [Fact]
    public void SoundsViewModel_FiltersByName_CaseInsensitive()
    {
        var vm = new SoundsViewModel();
        
        vm.SearchText = "vital_sign_scan";
        Assert.True(vm.FilteredSounds.Count > 0);
        Assert.All(vm.FilteredSounds, s => Assert.Contains("vital_sign_scan", s.Name, System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SoundsViewModel_FiltersByWwisePath()
    {
        var vm = new SoundsViewModel();

        vm.SearchText = "intro_sequence";
        Assert.True(vm.FilteredSounds.Count > 0);
        Assert.All(vm.FilteredSounds, s => 
            Assert.True(
                s.WwisePath.Contains("intro_sequence", System.StringComparison.OrdinalIgnoreCase) ||
                s.Name.Contains("intro_sequence", System.StringComparison.OrdinalIgnoreCase) ||
                s.IdString.Contains("intro_sequence", System.StringComparison.OrdinalIgnoreCase)
            ));
    }

    [Fact]
    public void SoundsViewModel_ClearingSearch_RestoresAllSounds()
    {
        var vm = new SoundsViewModel();
        vm.SearchText = "1071316888";
        Assert.Single(vm.FilteredSounds);

        vm.SearchText = "";
        Assert.Equal(13729, vm.FilteredSounds.Count);
    }

    [Fact]
    public void SoundsViewModel_NonMatchingSearch_ReturnsEmpty()
    {
        var vm = new SoundsViewModel();
        vm.SearchText = "NonExistentSoundSearchQuery_12345!@#$";
        Assert.Empty(vm.FilteredSounds);
        Assert.Contains("Showing 0", vm.StatusText);
    }
}
