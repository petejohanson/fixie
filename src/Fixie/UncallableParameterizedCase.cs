﻿using System;
using System.Reflection;

namespace Fixie
{
    public class UncallableParameterizedCase : Case
    {
        public UncallableParameterizedCase(MethodInfo caseMethod)
            : base(caseMethod)
        {
        }

        public override void Execute(object instance, CaseExecution caseExecution)
        {
            try
            {
                throw new ArgumentException("This parameterized test could not be executed, because no input values were available.");
            }
            catch (Exception exception)
            {
                caseExecution.Fail(exception);
            }
        }
    }
}