﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Debugging information associated with the specified method that is emitted by the compiler to support Edit and Continue.
    /// </summary>
    public struct EditAndContinueMethodDebugInformation
    {
        internal readonly ImmutableArray<LocalSlotDebugInfo> LocalSlots;

        internal EditAndContinueMethodDebugInformation(ImmutableArray<LocalSlotDebugInfo> localSlots)
        {
            this.LocalSlots = localSlots;
        }

        public static EditAndContinueMethodDebugInformation Create(ImmutableArray<byte> compressedSlotMap)
        {
            return new EditAndContinueMethodDebugInformation(UncompressSlotMap(compressedSlotMap));
        }

        // TODO: remove, we don't need this 
        private static bool HasSubordinal(SynthesizedLocalKind kind)
        {
            return false;
        }

        private const byte AlignmentValue = 0xff;

        private unsafe static ImmutableArray<LocalSlotDebugInfo> UncompressSlotMap(ImmutableArray<byte> compressedSlotMap)
        {
            if (compressedSlotMap.IsDefaultOrEmpty)
            {
                return default(ImmutableArray<LocalSlotDebugInfo>);
            }

            var mapBuilder = ArrayBuilder<LocalSlotDebugInfo>.GetInstance();
            
            fixed (byte* compressedSlotMapPtr = &compressedSlotMap.ToArray()[0])
            {
                var blobReader = new BlobReader(compressedSlotMapPtr, compressedSlotMap.Length);

                while (blobReader.RemainingBytes > 0)
                {
                    byte b = blobReader.ReadByte();

                    if (b == AlignmentValue)
                    {
                        break;
                    }

                    if (b == 0)
                    {
                        // short-lived temp, no info
                        mapBuilder.Add(new LocalSlotDebugInfo(SynthesizedLocalKind.LoweringTemp, default(LocalDebugId)));
                        continue;
                    }

                    var kind = (SynthesizedLocalKind)((b & 0x3f) - 1);
                    bool hasOrdinal = (b & (1 << 7)) != 0;
                    bool hasSubordinal = HasSubordinal(kind);

                    // TODO: Right now all integers are >= -1, but we should not assume that and read Ecma335 compressed int instead.
                    int syntaxOffset;
                    if (!blobReader.TryReadCompressedInteger(out syntaxOffset)) 
                    {
                        return default(ImmutableArray<LocalSlotDebugInfo>);
                    }

                    syntaxOffset--;

                    int ordinal = 0;
                    if (hasOrdinal && !blobReader.TryReadCompressedInteger(out ordinal))
                    {
                        return default(ImmutableArray<LocalSlotDebugInfo>);
                    }

                    int subordinal = 0;
                    if (hasSubordinal && !blobReader.TryReadCompressedInteger(out subordinal))
                    {
                        return default(ImmutableArray<LocalSlotDebugInfo>);
                    }

                    mapBuilder.Add(new LocalSlotDebugInfo(kind, new LocalDebugId(syntaxOffset, ordinal, subordinal)));
                }
            }

            return mapBuilder.ToImmutableAndFree();
        }

        internal void SerializeCustomDebugInformation(ArrayBuilder<Cci.MemoryStream> customDebugInfo)
        {
            if (this.LocalSlots.IsDefaultOrEmpty)
            {
                return;
            }

            Cci.MemoryStream customMetadata = new Cci.MemoryStream();
            Cci.BinaryWriter cmw = new Cci.BinaryWriter(customMetadata, true);
            cmw.WriteByte(4); // version
            cmw.WriteByte(6); // kind: EditAndContinueLocalSlotMap
            cmw.Align(4);

            // length (will be patched)
            uint lengthPosition = cmw.BaseStream.Position;
            cmw.WriteUint(0);

            foreach (LocalSlotDebugInfo localSlot in this.LocalSlots)
            {
                var kind = localSlot.SynthesizedKind;
                bool hasOrdinal = localSlot.Id.Ordinal > 0;
                bool hasSubordinal = HasSubordinal(kind);

                if (!kind.IsLongLived())
                {
                    cmw.WriteByte(0);
                    continue;
                }

                byte b = (byte)(kind + 1);
                Debug.Assert((b & (1 << 7)) == 0);

                if (hasOrdinal)
                {
                    b |= 1 << 7;
                }

                cmw.WriteByte(b);

                // TODO: Right now all integers are >= -1, but we should not assume that and write Ecma335 compressed int instead.
                cmw.WriteCompressedUInt(unchecked((uint)(localSlot.Id.SyntaxOffset + 1)));

                if (hasOrdinal)
                {
                    cmw.WriteCompressedUInt((uint)localSlot.Id.Ordinal);
                }

                if (hasSubordinal)
                {
                    cmw.WriteCompressedUInt((uint)localSlot.Id.Subordinal);
                }
            }

            uint length = customMetadata.Position;

            // align with values that the reader skips
            while (length % 4 != 0)
            {
                cmw.WriteByte(AlignmentValue);
                length++;
            }

            cmw.BaseStream.Position = lengthPosition;
            cmw.WriteUint(length);
            cmw.BaseStream.Position = length;

            customDebugInfo.Add(customMetadata);
        }
    }
}
