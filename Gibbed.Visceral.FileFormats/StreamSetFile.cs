﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.Visceral.FileFormats
{
    public class StreamSetFile
    {
        public bool LittleEndian = true;
        public List<StreamSet.ContentInfo> Contents
            = new List<StreamSet.ContentInfo>();

        public void Deserialize(Stream input)
        {
            this.Contents.Clear();

            if (input.Length < 8)
            {
                throw new FormatException();
            }

            // read options
            {
                var magic = input.ReadValueU32(true);
                if (magic != 0x6F6C7333 && magic.Swap() != 0x6F6C7333)
                {
                    throw new FormatException();
                }

                this.LittleEndian = magic == 0x6F6C7333;
                var size = input.ReadValueU32(this.LittleEndian);
                if (size != 12)
                {
                    throw new FormatException();
                }

                var unknown00 = input.ReadValueU16(this.LittleEndian);
                var unknown02 = input.ReadValueU16(this.LittleEndian);

                /* Dead Space:
                 * unknown00 = 2
                 * unknown02 = 259
                 * 
                 * Dead Space 2:
                 * unknown00 = 2
                 * unknown02 = 259
                 * 
                 * Dante's Inferno:
                 * unknown00 = 2
                 * unknown02 = 1537
                 */
            }

            var contentInfos = new List<StreamSet.ContentInfo>();
            while (input.Position + 8 <= input.Length)
            {
                long blockPosition = input.Position;
                var blockType = (StreamSet.BlockType)input.ReadValueU32(this.LittleEndian);
                var blockSize = input.ReadValueU32(this.LittleEndian);

                if (blockSize < 8 ||
                    blockPosition + blockSize > input.Length)
                {
                    throw new FormatException();
                }

                switch (blockType)
                {
                    case StreamSet.BlockType.Content:
                    {
                        var type = (StreamSet.ContentType)input.ReadValueU32(this.LittleEndian);
                        Debug.Assert(
                            type == StreamSet.ContentType.Header ||
                            type == StreamSet.ContentType.Data ||
                            type == StreamSet.ContentType.CompressedData);

                        this.Contents.Add(new StreamSet.ContentInfo()
                            {
                                Type = type,
                                Offset = input.Position,
                                Size = blockSize - 12,
                            });
                        break;
                    }

                    case StreamSet.BlockType.Padding:
                    {
                        break;
                    }

                    default:
                    {
                        throw new FormatException("unhandled block");
                    }
                }

                input.Seek(blockPosition + blockSize, SeekOrigin.Begin);
            }
        }
    }
}