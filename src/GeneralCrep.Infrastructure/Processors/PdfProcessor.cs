using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Infrastructure.Processors
{
    public class PdfProcessor : IFileProcessor
    {
        public FileProcessingResult Process(string filePath)
        {
            var result = new FileProcessingResult
            {
                FileName = System.IO.Path.GetFileName(filePath),
                FileType = FileType.Pdf,
                FileBytes = File.ReadAllBytes(filePath),
                Data = string.Empty
            };
            try
            {
                using var reader = new PdfReader(filePath);
                var pages = new List<string>();
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(reader, i);
                    string snippet = text.Length > 200 ? text.Substring(0, 200) + "..." : text;
                    pages.Add(snippet);
                }

                result.Data = new
                {
                    TotalPages = reader.NumberOfPages,
                    PageSnippets = pages
                };
            }
            catch (BadPasswordException ex)
            {
                result.Data = "El archivo está protegido con contraseña y no se puede leer su contenido.";
            }
            
            return result;
        }
    }
}
