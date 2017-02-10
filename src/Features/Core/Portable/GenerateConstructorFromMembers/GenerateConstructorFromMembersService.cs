﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal partial class GenerateConstructorFromMembersService : AbstractGenerateFromMembersService
    {
        public static readonly GenerateConstructorFromMembersService Instance = new GenerateConstructorFromMembersService();

        private GenerateConstructorFromMembersService()
        {
        }

        public async Task<ImmutableArray<CodeAction>> GenerateConstructorFromMembersAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateConstructorFromMembers, cancellationToken))
            {
                var info = await GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    var state = State.Generate(this, document, textSpan, info.ContainingType, info.SelectedMembers, cancellationToken);
                    if (state != null)
                    {
                        return GetCodeActions(document, state).AsImmutableOrNull();
                    }
                }

                return default(ImmutableArray<CodeAction>);
            }
        }

        private IEnumerable<CodeAction> GetCodeActions(Document document, State state)
        {
            yield return new FieldDelegatingCodeAction(this, document, state);
            if (state.DelegatedConstructor != null)
            {
                yield return new ConstructorDelegatingCodeAction(this, document, state);
            }
        }
    }
}