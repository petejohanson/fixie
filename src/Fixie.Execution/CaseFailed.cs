﻿namespace Fixie.Execution
{
    using Internal;

    public class CaseFailed : CaseCompleted
    {
        public CaseFailed(Case @case, AssertionLibraryFilter filter)
            : base(
                @class: @case.Class,
                method: @case.Method,
                name: @case.Name,
                status: CaseStatus.Failed,
                duration: @case.Duration,
                output: @case.Output,

                exceptions: new CompoundException(@case.Exceptions, filter),
                skipReason: null
                )
        {
        }
    }
}