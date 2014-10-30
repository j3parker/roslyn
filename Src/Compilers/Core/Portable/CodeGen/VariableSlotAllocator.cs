﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal abstract class VariableSlotAllocator
    {
        public abstract void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder);

        public abstract LocalDefinition GetPreviousLocal(
            Cci.ITypeReference type,
            ILocalSymbolInternal symbol,
            string nameOpt,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags);

        public abstract string GetPreviousHoistedLocal(
            SyntaxNode currentDeclarator,
            Cci.ITypeReference currentType,
            SynthesizedLocalKind synthesizedKind,
            LocalDebugId currentId);

        public abstract int HoistedLocalSlotCount { get; }

        public abstract string GetPreviousAwaiter(Cci.ITypeReference currentType);
    }
}
