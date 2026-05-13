using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lc.Services.Interfaces
{
    public interface IUndoableAction
    {
        Task UndoAsync();
        Task RedoAsync();
    }
}
