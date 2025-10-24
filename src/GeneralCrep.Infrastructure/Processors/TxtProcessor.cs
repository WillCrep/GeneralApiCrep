using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Infrastructure.Processors
{
    public class TxtProcessor : IFileProcessor
    {
        public FileProcessingResult Process(string filePath)
        {
            var result = new FileProcessingResult
            {
                FileName = Path.GetFileName(filePath),
                FileType = FileType.Text,
                FileBytes = File.ReadAllBytes(filePath),
                Data = File.ReadAllLines(filePath)
            };

            return result;
        }
    }
}
