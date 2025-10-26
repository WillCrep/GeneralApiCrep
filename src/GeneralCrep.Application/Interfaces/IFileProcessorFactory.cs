using GeneralCrep.Domain.Enums;
using GeneralCrep.Infrastructure.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Interfaces
{
    public interface IFileProcessorFactory
    {
        IFileProcessor GetProcessor(FileType type);
    }
}
