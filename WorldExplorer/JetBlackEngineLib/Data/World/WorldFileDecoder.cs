﻿using JetBlackEngineLib.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace JetBlackEngineLib.Data.World;

public abstract class WorldFileDecoder : ISupportsSpecificEngineVersions
{
    private readonly Dictionary<int, Model> _modelCache = new();
        
    public abstract IReadOnlyList<EngineVersion> SupportedVersions { get; }

    public WorldData Decode(ReadOnlySpan<byte> data, WorldTexFile? texFile)
    {
        _modelCache.Clear();
        WorldData worldData = new();
            
        var headerSpan = MemoryMarshal.Cast<byte, WorldFileHeader>(data);
        var header = headerSpan[0];
        var texX0 = header.Texll % 100;
        var texY0 = header.Texll / 100;
        var texX1 = header.Texur % 100;
        var texY1 = header.Texur / 100;

        worldData.TextureChunkOffsets =
            ReadTextureChunkOffsets(data, header.WorldTexOffsetsOffset, texX0, texY0, texX1 + 1, texY1);

        foreach (var element in ReadElements(data, header))
        {
            PopulateElementRelatedData(element, data, worldData, texFile);
            worldData.WorldElements.Add(element);
        }

        return worldData;
    }
        
    protected abstract IEnumerable<WorldElement> ReadElements(ReadOnlySpan<byte> data, WorldFileHeader header);
        
    protected abstract WriteableBitmap? GetElementTexture(WorldElementDataInfo dataInfo, WorldTexFile texFile,
        WorldData worldData);

    protected abstract int[,] ReadTextureChunkOffsets(ReadOnlySpan<byte> data, int offset, int x1, int y1, int x2,
        int y2);

    protected List<WorldElement> IterateElements<T>(ReadOnlySpan<byte> data, WorldFileHeader header,
        int elementSize, Func<T, int, WorldElement?> elementParseFunc) where T : struct
    {
        List<WorldElement> elements = new(header.NumberOfElements);
        for (var elementIdx = 0; elementIdx < header.NumberOfElements; ++elementIdx)
        {
            var rawElSpan = MemoryMarshal.Cast<byte, T>(
                data.Slice(header.ElementArrayStart + (elementSize * elementIdx), elementSize)
            );
            var rawEl = rawElSpan[0];
            var element = elementParseFunc(rawEl, elementIdx);
            if (element == null) continue;
            elements.Add(element);
        }

        return elements;
    }

    private void PopulateElementRelatedData(WorldElement element, ReadOnlySpan<byte> data,
        WorldData worldData, WorldTexFile? texFile)
    {
        if (element.DataInfo == null)
        {
            return;
        }

        if (texFile != null)
        {
            element.Texture = GetElementTexture(element.DataInfo, texFile, worldData);
            if (element.Texture != null)
            {
                element.DataInfo.TextureWidth = element.Texture.PixelWidth;
                element.DataInfo.TextureHeight = element.Texture.PixelHeight;
            }
        }

        var nRegs = data[element.DataInfo.VifDataOffset + 0x10];
        var vifStartOffset = (nRegs + 2) * 0x10;
        var absoluteVifStartOffset = element.DataInfo.VifDataOffset + vifStartOffset;
        var vifDataLength = (element.DataInfo.VifDataLength * 0x10) - vifStartOffset;

        if (vifDataLength > 0)
        {
            element.Model = GetElementModel(
                NullLogger.Instance,
                absoluteVifStartOffset,
                data.Slice(absoluteVifStartOffset, vifDataLength),
                element.DataInfo.TextureWidth,
                element.DataInfo.TextureHeight
            );
        }
    }

    private Model GetElementModel(ILogger log, int startOffset, ReadOnlySpan<byte> data, int texWidth, int texHeight)
    {
        if (!_modelCache.TryGetValue(startOffset, out var model))
        {
            model = DecodeModel(log, data, texWidth, texHeight);
            _modelCache.Add(startOffset, model);
        }
        return model;
    }

    private Model DecodeModel(ILogger log, ReadOnlySpan<byte> data, int texWidth, int texHeight)
    {
        return new Model(new[]
        {
            VifDecoder.DecodeMesh(
                log,
                data,
                texWidth,
                texHeight)
        });
    }
}