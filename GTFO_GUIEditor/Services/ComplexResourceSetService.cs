using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Services;

public static class ComplexResourceSetService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ComplexResourceSetBlock CreateFreshBlock(uint id, string name, ComplexType complexType = ComplexType.Mining, SubComplexType primarySubComplex = SubComplexType.DigSite)
    {
        var template = DatablockManager.Instance.GetComplexResourceTemplate(complexType);
        var templateBlock = template == null
            ? null
            : template.Deserialize<ComplexResourceSetBlock>(JsonOptions);
        if (templateBlock != null)
        {
            templateBlock.PersistentId = id;
            templateBlock.Name = name;
            templateBlock.InternalEnabled = true;
            templateBlock.ComplexType = complexType;
            templateBlock.PrimareSubComplexUsed = primarySubComplex;
            return templateBlock;
        }

        BundleNameType bundle = complexType switch
        {
            ComplexType.Mining => BundleNameType.Complex_Mining,
            ComplexType.Tech => BundleNameType.Complex_Tech,
            ComplexType.Service => BundleNameType.Complex_Service,
            _ => BundleNameType.Complex_Mining
        };

        return new ComplexResourceSetBlock
        {
            PersistentId = id,
            Name = name,
            ComplexType = complexType,
            PrimareSubComplexUsed = primarySubComplex,
            BundleName = bundle,
            InternalEnabled = true,
            RandomizeGeomorphOrder = false,
            LevelGenConfig = new LevelGenConfig
            {
                GridSize = 40,
                CellDimension = 64.0,
                AltitudeOffset = 6.0,
                TransitionDirection = TransitionDirection.FloorUp,
                LevelProgression = LevelProgression.StartLevel
            }
        };
    }

    public static ComplexResourceSetBlock CreateBlockFromTemplate(ComplexResourceSetBlock template, uint newId, string newName)
    {
        var clone = template.Clone();
        clone.PersistentId = newId;
        clone.Name = newName;
        return clone;
    }
}
