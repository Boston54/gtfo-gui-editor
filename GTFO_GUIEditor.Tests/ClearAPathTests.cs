using System;
using GTFO_GUIEditor.Models;
using GTFO_GUIEditor.ViewModels;
using Xunit;

namespace GTFO_GUIEditor.Tests;

public class ClearAPathTests
{
    [Fact]
    public void ObjectiveType_ClearAPath_ExistsInEnum()
    {
        Assert.True(Enum.IsDefined(typeof(ObjectiveType), "ClearAPath"));
    }

    [Fact]
    public void ViewModel_ClearAPath_Visibility()
    {
        var vm = new MainWindowViewModel();
        var layout = vm.LevelLayout;
        layout.OpenSectorSettings(SectorType.Main);

        layout.SelectedSectorObjectiveType = ObjectiveType.ClearAPath;
        Assert.True(layout.IsClearAPathObjective);
        Assert.False(layout.IsHsuObjective);
        Assert.False(layout.IsReactorObjective);
    }
}
