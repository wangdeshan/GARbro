using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GameRes.Formats.Kid
{
    public class BipFormat : ImageFormat
    {
        public override string Tag { get { return "BIP/PS2"; } }
        public override string Description { get { return "PS2 tiled bitmap format"; } }
        public override uint Signature { get { return 0; } } //0x05000000

        public override ImageMetaData ReadMetaData(IBinaryStream file)
        {
            uint header = file.ReadUInt32();
            if (header == 9)
            {
                throw new NotSupportedException(string.Format("BIP Chara format not supported."));
            }
            else if (header != 5)
            {
                return null;
            }

            file.Seek(0x14, SeekOrigin.Begin);
            if (file.ReadUInt16() != 256)
            {
                return null;
            }
            uint sign = file.ReadUInt16();
            uint dy = 0;
            bool sliced = true;
            if (sign == 0x17)
            {
                dy = 16;
            }
            else if (sign == 0x16)
            {
                dy = 16;
                sliced = false;
            }
            else if (sign == 0x13)
            {
                dy = 32;
            }
            else
            {
                return null;
            }

            //uint dx = dy * 32;
            file.Seek(0x88, SeekOrigin.Begin);
            uint width = file.ReadUInt16();
            uint height = file.ReadUInt16();
            if (width >= 1280 || height >= 1280) // suppose not so large
                return null;

            file.Seek(0x90, SeekOrigin.Begin);
            uint size = file.ReadUInt32();
            size &= 0x00FFFFFF;
            if (sign == 0x13 && size != (file.Length - 0x100) || sign == 0x17 && size != (file.Length - 0x100)* 2)
            {
                return null;
            }
            return new BipImageMetaData
            {
                Width = width,
                Height = height,
                BlockSize = dy,
                Sliced = sliced,
            };
        }
        public override ImageData Read(IBinaryStream file, ImageMetaData info)
        {
            if (info == null)
                throw new NotSupportedException(string.Format("Not BIP texture format."));
            var bipheader = (BipImageMetaData)info;
            file.Seek(0x100, SeekOrigin.Begin);
            byte[] pixels = new byte[bipheader.iWidth * bipheader.iHeight * 4];
            uint dy = bipheader.BlockSize;
            uint dx = dy * 32;
            if (bipheader.Sliced)
            {
                long dwidth = ((bipheader.iWidth + (dy - 2) - 1) / (dy - 2)) * dy;
                long dheight = ((bipheader.iHeight + (dy - 2) - 1) / (dy - 2)) * dy;
                long focus_H = (dwidth* dheight + dx - 1) / dx;
                long focus_T = (focus_H + dy - 1) / dy;
                
                for (int t = 0; t < focus_T; t++)
                {
                    for(int y = 0; y < dy; y++)
                    {
                        for (int x = 0; x < dx; x++)
                        {
                            var pixel = file.ReadBytes(4); //RGBA with wrong A
                            long i2x = x + t * dx;
                            long i3t = i2x / dwidth;
                            long i3x = i2x - i3t * dwidth;
                            long i3y = i3t * (dy - 2) + y;
                            long i4x = i3x - i3x / dy * dy + i3x / dy * (dy - 2);
                            if (i3x >= dwidth || i4x >= bipheader.iWidth || i3y >= bipheader.iHeight)
                                continue;
                            long target = (i4x + i3y * bipheader.iWidth) * 4;
                            //BGRA
                            pixels[target] = pixel[2];
                            pixels[target + 1] = pixel[1];
                            pixels[target + 2] = pixel[0];
                            if (pixel[3] >= byte.MaxValue / 2)
                                pixels[target + 3] = byte.MaxValue;
                            else
                                pixels[target + 3] = (byte)(pixel[3] << 1);
                        }
                    }
                }
            }
            else
            {
                long focus_H = (bipheader.iWidth * bipheader.iHeight + dx - 1) / dx;
                long focus_T = (focus_H + dy - 1) / dy;
                for (int t = 0; t < focus_T; t++)
                {
                    for (int y = 0; y < dy; y++)
                    {
                        for (int x = 0; x < dx; x++)
                        {
                            var pixel = file.ReadBytes(4); //RGBA with wrong A
                            long i2x = x + t * dx;
                            long i3t = i2x / bipheader.iWidth;
                            long i3x = i2x - i3t * bipheader.iWidth;
                            long i3y = i3t * dy + y;
                            if (i3x >= bipheader.iWidth || i3y >= bipheader.iHeight)
                                continue;
                            long target = (i3x + i3y * bipheader.iWidth) * 4;
                            //BGRA
                            pixels[target] = pixel[2];
                            pixels[target + 1] = pixel[1];
                            pixels[target + 2] = pixel[0];
                            if (pixel[3] >= byte.MaxValue / 2)
                                pixels[target + 3] = byte.MaxValue;
                            else
                                pixels[target + 3] = (byte)(pixel[3] << 1);
                        }
                    }
                }
            }
            return ImageData.Create(info, PixelFormats.Bgra32, null, pixels); ;
        }
        public override void Write(Stream file, ImageData image)
        {
            throw new System.NotImplementedException("BipFormat.Write not implemented");
        }

        class BipImageMetaData : ImageMetaData
        {
            public uint BlockSize { get; set; }
            public bool Sliced { get; set; }
        }
    }
}
