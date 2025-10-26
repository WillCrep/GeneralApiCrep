using ExcelDataReader;
using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Infrastructure.Processors
{
    public class ExcelProcessor : IFileProcessor
    {
        public FileProcessingResult Process(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var result = new FileProcessingResult {
                FileName = Path.GetFileName(filePath),
                FileType = FileType.Excel,
                FileBytes = File.ReadAllBytes(filePath),
                Data = new List<Dictionary<string, object>>()
            };

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            int sheetIndex = 0;

            do
            {
                var sheetData = new List<Dictionary<string, object>>();
                var header = new List<string>();

                int rowIndex = 0;
                while (reader.Read())
                {
                    if(rowIndex == 0)
                    {
                        for(int i = 0; i < reader.FieldCount; i++)
                        {
                            header.Add(reader.GetValue(i)?.ToString() ?? $"Column{i}");
                        }
                    }
                    else
                    {
                        var row = new Dictionary<string, object>();
                        for(int i = 0; i < reader.FieldCount; i++)
                        {
                            row[header[i]] = reader.GetValue(i);
                        }
                        
                        sheetData.Add(row);
                    }
                    rowIndex++;
                }

                ((List<Dictionary<string, object>>)result.Data).AddRange(sheetData);
                sheetIndex++;
            }while (reader.NextResult());

            return result;
        }
    }
}
