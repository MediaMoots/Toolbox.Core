﻿using System;
using System.Collections.Generic;
using System.Text;
using Toolbox.Core.IO;

namespace Toolbox.Core
{
    public class ASTC : STGenericTexture, IFileFormat, IExportableTexture
    {
        const int MagicFileConstant = 0x5CA1AB13;

        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Adaptive Scalable Texture" };
        public string[] Extension { get; set; } = new string[] { "*.astc" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.ReadInt32() == MagicFileConstant;
            }
        }

        //https://github.com/ARM-software/astc-encoder/blob/a47b80f081f10c43d96bd10bcb713c71708041b9/Source/astc_toplevel.cpp
        public byte[] magic;
        public byte BlockDimX;
        public byte BlockDimY;
        public byte BlockDimZ;
        public byte[] xsize;
        public byte[] ysize;
        public byte[] zsize;
        public byte[] DataBlock;

        public void Load(System.IO.Stream stream)
        {
            using (FileReader reader = new FileReader(stream))
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.LittleEndian;

                magic = reader.ReadBytes(4);

                uint magicval = magic[0] + 256 * (uint)(magic[1]) + 65536 * (uint)(magic[2]) + 16777216 * (uint)(magic[3]);

                if (magicval != MagicFileConstant)
                    throw new Exception("Invalid identifier");

                BlockDimX = reader.ReadByte();
                BlockDimY = reader.ReadByte();
                BlockDimZ = reader.ReadByte();
                xsize = reader.ReadBytes(3);
                ysize = reader.ReadBytes(3);
                zsize = reader.ReadBytes(3);

                Width = (uint)(xsize[0] + 256 * xsize[1] + 65536 * xsize[2]);
                Height = (uint)(ysize[0] + 256 * ysize[1] + 65536 * ysize[2]);
                Depth = (uint)(zsize[0] + 256 * zsize[1] + 65536 * zsize[2]);

                reader.Seek(0x10, System.IO.SeekOrigin.Begin);
                DataBlock = reader.ReadBytes((int)(reader.BaseStream.Length - reader.Position));

                Console.WriteLine(Width);
                Console.WriteLine(Height);
                Console.WriteLine(Depth);

                if (BlockDimX == 4 && BlockDimY == 4)
                    Format = TexFormat.ASTC_4x4;
                else if (BlockDimX == 5 && BlockDimY == 4)
                    Format = TexFormat.ASTC_5x4;
                else if (BlockDimX == 5 && BlockDimY == 5)
                    Format = TexFormat.ASTC_5x5;
                else if (BlockDimX == 6 && BlockDimY == 5)
                    Format = TexFormat.ASTC_6x5;
                else if (BlockDimX == 6 && BlockDimY == 6)
                    Format = TexFormat.ASTC_6x6;
                else if (BlockDimX == 8 && BlockDimY == 5)
                    Format = TexFormat.ASTC_8x5;
                else if (BlockDimX == 8 && BlockDimY == 6)
                    Format = TexFormat.ASTC_8x6;
                else if (BlockDimX == 8 && BlockDimY == 8)
                    Format = TexFormat.ASTC_8x8;
                else if (BlockDimX == 10 && BlockDimY == 10)
                    Format = TexFormat.ASTC_10x10;
                else if (BlockDimX == 10 && BlockDimY == 5)
                    Format = TexFormat.ASTC_10x5;
                else if (BlockDimX == 10 && BlockDimY == 6)
                    Format = TexFormat.ASTC_10x6;
                else if (BlockDimX == 10 && BlockDimY == 8)
                    Format = TexFormat.ASTC_10x8;
                else
                    throw new Exception($"Unsupported block dims! ({BlockDimX} x {BlockDimY})");
            }

            stream.Dispose();
            stream.Close();
        }
        public void Unload()
        {

        }
        public void Save(System.IO.Stream stream)
        {
            using (FileWriter writer = new FileWriter(stream, true))
            {
                if (Depth == 0)
                    Depth = 1;

                writer.ByteOrder = Syroot.BinaryData.ByteOrder.LittleEndian;
                writer.Write(MagicFileConstant);
                writer.Write(BlockDimX);
                writer.Write(BlockDimY);
                writer.Write(BlockDimZ);
                writer.Write(IntTo3Bytes((int)Width));
                writer.Write(IntTo3Bytes((int)Height));
                writer.Write(IntTo3Bytes((int)Depth));
                writer.Write(DataBlock);

                writer.Close();
                writer.Dispose();
            }
        }

        private static byte[] IntTo3Bytes(int value)
        {
            byte[] newValue = new byte[3];
            newValue[0] = (byte)(value & 0xFF);
            newValue[1] = (byte)((value >> 8) & 0xFF);
            newValue[2] = (byte)((value >> 16) & 0xFF);
            return newValue;
        }

        public void Export(STGenericTexture texture, TextureExportSettings settings, string filePath)
        {
            List<Surface> surfaces = texture.GetSurfaces(settings.ArrayLevel, settings.ExportArrays);

            ASTC atsc = new ASTC();
            atsc.Width = texture.Width;
            atsc.Height = texture.Height;
            atsc.Depth = texture.Depth;
            atsc.BlockDimX = (byte)TextureFormatHelper.GetBlockWidth(texture.Format);
            atsc.BlockDimY = (byte)TextureFormatHelper.GetBlockHeight(texture.Format);
            atsc.BlockDimZ = (byte)TextureFormatHelper.GetBlockDepth(texture.Format);
            atsc.DataBlock = ByteUtils.CombineArray(surfaces[0].mipmaps.ToArray());
            atsc.Save(new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite));
        }

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0) {
            return DataBlock;
        }
    }
}
