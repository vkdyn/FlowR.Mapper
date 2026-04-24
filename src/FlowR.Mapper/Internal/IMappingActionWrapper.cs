using FlowR.Mapper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowR.Mapper.Internal
{
    /// <summary>
    /// Wrapper for before/after map actions that handles both 2-param and 3-param signatures.
    /// </summary>
    internal interface IMappingActionWrapper
    {
        void Execute(object source, object destination, ResolutionContext? context);
    }

}
