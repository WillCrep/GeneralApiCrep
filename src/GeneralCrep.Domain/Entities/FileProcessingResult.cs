using GeneralCrep.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Domain.Entities
{
    public class FileProcessingResult
    {
        public string FileName { get; set; }
        public FileType FileType { get; set; }
        public object Data { get; set; }
        public byte[] FileBytes { get; set; }
    }
}
