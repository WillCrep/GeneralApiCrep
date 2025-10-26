using GeneralCrep.Domain.Enums;
using GeneralCrep.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Infrastructure.Processors
{
    public class FileProcessorFactory : IFileProcessorFactory
    {
        public IFileProcessor GetProcessor(FileType type)
        {
            return type switch
            {
                FileType.Excel => new ExcelProcessor(),
                FileType.Pdf => new PdfProcessor(),
                FileType.Text => new TxtProcessor(),
                _ => null
            };
        }
    }
}
