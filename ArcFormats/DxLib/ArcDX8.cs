//! \file       ArcDX8.cs
//! \date       2022 Jun 05
//! \brief      DxLib archive version 8.
//
// Copyright (C) 2022 by morkt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace GameRes.Formats.DxLib
{
    [Export(typeof(ArchiveFormat))]
    public class Dx8Opener : DxOpener
    {
        public override string         Tag { get { return "BIN/DXLIB"; } }
        public override string Description { get { return "DxLib archive version 8"; } }
        public override uint     Signature { get { return 0x00085844; } }
        public override bool  IsHierarchic { get { return true; } }
        public override bool      CanWrite { get { return false; } }

        public Dx8Opener ()
        {
            Extensions = new string[] { "dxa", "hud", "usi", "med", "dat", "bin", "bcx", "wolf" };
            Signatures = new[] { 0x00085844u };
        }

        static readonly byte[] DefaultKey = new byte[] { 0xBE, 0xC8, 0x8A, 0xF5, 0x28, 0x50, 0xC9 };


        DxScheme DefaultScheme = new DxScheme { KnownKeys = new List<IDxKey>() };


        internal struct DxHeaderV8
        {
            public long BaseOffset;
            public long IndexOffset;
            public uint IndexSize;
            public long FileTable;
            public long DirTable;
            public int CodePage;
            public DXA8Flags Flags;
            public byte HuffmanKB;
            //15 bytes of padding.
        }

        internal enum DXA8Flags : UInt32
        {
            DXA_FLAG_NO_KEY=1, //file is not encrypted
            DXA_FLAG_NO_HEAD_PRESS=1<<1, //do not compress headers
        }

        public override ArcFile TryOpen (ArcView file)
        {
            var dx = new DxHeaderV8 {
                IndexSize  = file.View.ReadUInt32 (4),
                BaseOffset = file.View.ReadInt64 (8),
                IndexOffset = file.View.ReadInt64 (0x10),
                FileTable  = file.View.ReadInt64 (0x18),
                DirTable   = file.View.ReadInt64 (0x20),
                CodePage   = file.View.ReadInt32 (0x28),
                Flags      = (DXA8Flags)file.View.ReadUInt32(0x2C),
                HuffmanKB = file.View.ReadByte(0x30)
            };
            if (dx.DirTable >= dx.IndexSize || dx.FileTable >= dx.IndexSize)
                return null;
            //at this point we cannot proceed without user input. If NO_HEAD_PRESS is set we could maybe restore the 7-byte key
            //Otherwise (assuming the archive is encrypted) we have no way to continue without user input.

            //TODO: Ask for key here.
          
            var key = DefaultKey;
            if ((dx.Flags & DXA8Flags.DXA_FLAG_NO_HEAD_PRESS) != 0)
            {
                var index = file.View.ReadBytes(dx.IndexOffset, dx.IndexSize);
                Decrypt(index, 0, index.Length, 0, key);
            } else
            {
                //input is compressed. First by huffman then by LZ. if it's also encrypted then we're stuck.
                throw new NotImplementedException();
            }
            // decrypt-2
            // decompress
            return null;
        }
    }
}
