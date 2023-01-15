#region Copyright & License Information

/*
 * Copyright 2007-2023 The OpenE2140 Developers (see AUTHORS)
 * This file is part of OpenE2140, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OpenRA.Graphics;
using OpenRA.Mods.E2140.Assets.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.E2140.Assets.SpriteLoaders;

public class MixSpriteFrame : ISpriteFrame
{
	public SpriteFrameType Type { get; }
	public Size Size { get; }
	public Size FrameSize { get; }
	public float2 Offset { get; }
	public byte[] Data { get; }
	public bool DisableExportPadding => true;

	public MixSpriteFrame(SpriteFrameType type, Size size, byte[] pixels)
	{
		this.Type = type;
		this.Size = size;
		this.FrameSize = size;
		this.Offset = new float2(0, 0);
		this.Data = pixels;
	}
}

[UsedImplicitly]
public class MixSpriteLoader : ISpriteLoader
{
	public bool TryParseSprite(Stream stream, string filename, [NotNullWhen(true)] out ISpriteFrame[]? frames, out TypeDictionary? metadata)
	{
		var start = stream.Position;
		var identifier = stream.ReadASCII(10);
		stream.Position = start;

		frames = null;
		metadata = null;

		// This happens on MIXMAX files, which are simply 1x1, 2x2 and 4x4 minimap variants of the tiles.
		if (identifier != "MIX FILE  ")
			return false;

		var mix = new Mix(stream);

		if (mix.Frames.Length == 0)
			return false;

		var framesList = new List<ISpriteFrame>();

		var hasTransparency =
			new[] { "spro0.mix", "spro1.mix", "spro2.mix", "spro3.mix", "spro4.mix", "spro5.mix", "spro6.mix" }.Any(
				f => filename.EndsWith(f, StringComparison.OrdinalIgnoreCase)
			);

		foreach (var frame in mix.Frames)
		{
			var size = new Size(frame.Width, frame.Height);

			if (frame.Is32Bpp)
				framesList.Add(new MixSpriteFrame(SpriteFrameType.Rgba32, size, frame.Pixels));
			else
			{
				var argbImage = new byte[frame.Pixels.Length * 4];
				var indexedImage = new byte[frame.Pixels.Length];
				var palette = mix.Palettes[frame.Palette].Colors;

				for (var i = 0; i < frame.Pixels.Length; i++)
				{
					var index = frame.Pixels[i];
					var color = palette[index];

					if (hasTransparency && index == 254)
						color = Color.FromArgb(0x80000000);

					indexedImage[i] = index;

					argbImage[i * 4 + 0] = color.R;
					argbImage[i * 4 + 1] = color.G;
					argbImage[i * 4 + 2] = color.B;
					argbImage[i * 4 + 3] = color.A;
				}

				framesList.Add(new MixSpriteFrame(SpriteFrameType.Rgba32, size, argbImage));
			}
		}

		frames = framesList.ToArray();

		return true;
	}
}
