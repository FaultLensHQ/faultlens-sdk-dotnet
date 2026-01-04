using FaultLens.Sdk.Envelopes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FaultLens.Sdk.Builders
{
    internal static class ExceptionMapper
    {
        public static ExceptionInfo Map(Exception ex)
        {
            return new ExceptionInfo(ex.GetType().FullName!, ex.Message,
                ex.StackTrace?
                    .Split(Environment.NewLine)
                    .Select(line => new StackFrameInfo(method: line))
                    .ToList() ?? new List<StackFrameInfo>());
        }
    }
}
