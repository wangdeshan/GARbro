//! \file       HuffmanDecoder.cs
//! \date       2024 Aug 2
//! \brief      Custom Huffman decoder for DXA archives.
//
// Copyright (C) 2017 by morkt - GetBits function
// Copyright (C) 2024 by MrSoup678
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
using System.IO;
using System.Runtime.CompilerServices;
using GameRes.Utility;

namespace GameRes.Formats.DxLib
{
    internal struct DXA8HuffmanNode
    {
        public UInt64 Weight;
        public int bitNumber;
        public byte[] bitArray; //32 bytes here
        public int Index;

        public int ParentNode; // index of parent node.
        public int[] ChildNode; //two children nodes, -1 if not existent.
    }

    internal sealed class HuffmanDecoder
    {
        byte[]          m_input;
        byte[]          m_output;

        int             m_src;
        ulong             m_bits;
        int             m_bit_count;

        ulong m_readBytes;
        ulong m_readBits;

        
        DXA8HuffmanNode[] nodes; //256+255 nodes

        ulong originalSize;
        ulong compressedSize;
        ulong headerSize;

        //ushort token = 256;

        public HuffmanDecoder (byte[] src, byte[] dst)
        {
            m_input = src;
            m_output = dst;

            m_src = 0;
            m_bit_count = 0;
            m_readBytes = 0;
            m_readBits = 0;
            originalSize = compressedSize = headerSize = 0;
            ushort[] weights = new ushort[256];
            nodes = new DXA8HuffmanNode[256+255]; //256 "base" nodes, then 255 nodes making a pyramid.
        }

        public byte[] Unpack ()
        {

            for (int i=0; i<nodes.Length; i++)
            {
                nodes[i].ParentNode = -1;
                nodes[i].ChildNode[0] = -1;
                nodes[i].ChildNode[1] = -1;
            }
            SetupWeights();
            CreateTree();
            throw new NotImplementedException();
        }

        private void SetupWeights()
        {
            int sizeA, sizeB;
            byte BitNum;
            byte Minus;
            ushort SaveData;
            ushort[] weights = new ushort[256];
            sizeA = (int)GetBits(6) + 1;
            originalSize = GetBits(sizeA);
            sizeB = (int)GetBits(6)+1;
            compressedSize = GetBits(sizeB);

            BitNum = (byte)(((int)GetBits(3) + 1) * 2);
            Minus = (byte)GetBits(1);
            SaveData = (ushort)GetBits(BitNum);
            nodes[0].Weight = SaveData;
            for (int i = 1; i < 256; i++)
            {
                BitNum = (byte)(((int)GetBits(3) + 1) * 2);
                Minus = (byte)GetBits(1);
                SaveData = (ushort)GetBits(BitNum);
                weights[i] = (ushort)(Minus == 1 ? weights[i - 1] - SaveData : weights[i - 1] + SaveData);
            }
            for (int i = 0;i < 256; i++)
            {
                nodes[i].Weight = weights[i];
            }

        }

        void CreateTree()
        {
            int NodeNum=256, DataNum=256;

            while (DataNum > 1)
            {
                int MinNode1 = -1;
                int MinNode2 = -1;
                int NodeIndex = 0;


                for (int i = 0; i < DataNum; NodeIndex++) {
                    //don't do anything if we already have a parent set.
                    if (nodes[NodeIndex].ParentNode != -1) continue;
                    i++;
                    //we need to get the two lowest numbers for parenting.
                    if (MinNode1 == -1 || nodes[MinNode1].Weight > nodes[NodeIndex].Weight)
                    {
                        {
                            MinNode2 = MinNode1;
                            MinNode1 = NodeIndex;
                        }
                    } else if (MinNode2 == -1 || nodes[MinNode2].Weight > nodes[NodeIndex].Weight)
                    {
                        MinNode2 = NodeIndex;
                    }
                }
                nodes[NodeNum].ParentNode = -1;
                nodes[NodeNum].Weight = nodes[MinNode1].Weight + nodes[MinNode2].Weight;
                nodes[NodeNum].ChildNode[0] = MinNode1;
                nodes[NodeNum].ChildNode[1] = MinNode2;
                nodes[MinNode1].Index = 0;
                nodes[MinNode2].Index = 1;
                nodes[MinNode1].ParentNode = NodeNum;
                nodes[MinNode2].ParentNode = NodeNum;

                NodeNum++;
                DataNum--;
            }
        }

        ulong GetBits (int count)
        {
            ulong bits = 0;
            while (count --> 0)
            {
                if (0 == m_bit_count)
                {
                    m_bits = LittleEndian.ToUInt64 (m_input, m_src);
                    m_src += 8;
                    m_bit_count = 64;
                }
                bits = bits << 1 | (m_bits & 1);
                m_bits >>= 1;
                --m_bit_count;
                m_readBits++;
                if (m_readBits ==8)
                {
                    m_readBits = 0;
                    m_readBytes++;
                }
            }
            return bits;
        }

        ulong GetReadBytes()
        {
            return m_readBytes + (m_readBits != 0 ? 1ul : 0ul);
        }
    }
}
