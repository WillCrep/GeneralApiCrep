using GeneralCrep.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Infrastructure.Processors
{
    public interface IFileProcessor
    {
        FileProcessingResult Process(string filePath);
    }
}
