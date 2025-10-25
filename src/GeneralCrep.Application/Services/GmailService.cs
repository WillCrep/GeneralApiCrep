using GeneralCrep.Application.Dtos;
using GeneralCrep.Application.Interfaces;
using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Services
{
    public class GmailService : IGmailService
    {
        private readonly IGmailApiClient _gmailClient;
        private readonly IFileProcessorFactory _processorFactory;

        public GmailService(IGmailApiClient gmailClient, IFileProcessorFactory processorFactory)
        {
            _gmailClient = gmailClient;
            _processorFactory = processorFactory;
        }

        public async Task<List<FileProcessingResult>> ProcessRecentEmailsAsync(int maxResults = 5)
        {
            var emailsId = await _gmailClient.GetRecentEmailsAsync(maxResults);
            var results = new List<FileProcessingResult>();

            foreach (var id in emailsId)
            {
                IEnumerable<MessageGmailPartsDto> messagePartsDtos = await _gmailClient.GetEmailByIdAsync(id);
                if (!messagePartsDtos.Any()) continue;

                foreach (var part in messagePartsDtos)
                {
                    if (!string.IsNullOrEmpty(part.Filename) && part.AttachmentId != null)
                    {
                        string fileName = part.Filename;
                        byte[] fileBytes = await _gmailClient.GetAttachmentAsync(id, part.AttachmentId);

                        // Guardar temporalmente
                        string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                        await File.WriteAllBytesAsync(tempPath, fileBytes);

                        Console.WriteLine($"Archivo descargado: {fileName}");

                        // Detectar tipo de archivo
                        var fileType = GetFileType(fileName);

                        // Procesar según tipo
                        var processor = _processorFactory.GetProcessor(fileType);

                        if (processor != null)
                        {
                            var processedResult = processor.Process(tempPath);
                            processedResult.FileBytes = fileBytes; // adjuntar bytes originales
                            results.Add(processedResult);
                        }
                    }
                }
            }

            return results;
        }

        public async Task<List<FileProcessingResult>> ProcessEmailByIdAsync(string messageId)
        {
            var results = new List<FileProcessingResult>();
            IEnumerable<MessageGmailPartsDto> messagePartsDtos = await _gmailClient.GetEmailByIdAsync(messageId);

            if (!messagePartsDtos.Any())
                return results;

            foreach (var part in messagePartsDtos)
            {
                if (!string.IsNullOrEmpty(part.Filename) && part?.AttachmentId != null)
                {
                    string fileName = part.Filename;
                    byte[] fileBytes = await _gmailClient.GetAttachmentAsync(messageId, part.AttachmentId);

                    // Guardar temporalmente
                    string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                    await File.WriteAllBytesAsync(tempPath, fileBytes);

                    Console.WriteLine($"Archivo descargado: {fileName}");

                    // Detectar tipo de archivo
                    var fileType = GetFileType(fileName);

                    // Procesar según tipo
                    var processor = _processorFactory.GetProcessor(fileType);

                    if (processor != null)
                    {
                        var processedResult = processor.Process(tempPath);

                        processedResult.FileBytes = fileBytes;
                        results.Add(processedResult);
                    }
                }
            }

            return results;
        }

        public async Task<List<FileProcessingResult>> ProcessEmailBySubjectAsync(string subject)
        {
            MessageGmailDto message = await _gmailClient.SearchEmailBySubjectAsync(subject);
            var results = new List<FileProcessingResult>();

            if (message == null || !message.Parts.Any())
                return results;

            foreach (var part in message.Parts)
            {
                if (part.Filename != null && part?.AttachmentId != null)
                {
                    string fileName = part.Filename;
                    byte[] fileBytes = await _gmailClient.GetAttachmentAsync(message.Id, part.AttachmentId);

                    string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                    File.WriteAllBytes(tempPath, fileBytes);

                    var fileType = GetFileType(fileName);
                    var processor = _processorFactory.GetProcessor(fileType);

                    if (processor != null)
                    {
                        var processedResult = processor.Process(tempPath);
                        processedResult.FileBytes = fileBytes;
                        results.Add(processedResult);
                    }
                }
            }

            return results;
        }

        public async Task<List<FileProcessingResult>> ProcessEmailsByLabelAsync(string labelName, int maxResults = 10)
        {
            List<MessageGmailDto> emails = await _gmailClient.GetEmailsByLabelAsync(labelName, maxResults);
            var results = new List<FileProcessingResult>();

            foreach (var msg in emails)
            {
                if (msg.Parts == null || msg.Parts.Count == 0) continue;

                foreach (var part in msg.Parts)
                {
                    if (!string.IsNullOrEmpty(part.Filename) && part?.AttachmentId != null)
                    {
                        string fileName = part.Filename;
                        byte[] fileBytes = await _gmailClient.GetAttachmentAsync(msg.Id, part.AttachmentId);

                        // Guardar temporalmente
                        string tempPath = Path.Combine(Path.GetTempPath(), fileName);
                        await File.WriteAllBytesAsync(tempPath, fileBytes);

                        Console.WriteLine($"Archivo descargado desde etiqueta '{labelName}': {fileName}");

                        // Detectar tipo de archivo
                        var fileType = GetFileType(fileName);

                        // Procesar según tipo
                        var processor = _processorFactory.GetProcessor(fileType);

                        if (processor != null)
                        {
                            var processedResult = processor.Process(tempPath);
                            processedResult.FileBytes = fileBytes;
                            results.Add(processedResult);
                        }
                    }
                }
            }

            return results;
        }


        private static FileType GetFileType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".xls" or ".xlsx" => FileType.Excel,
                ".pdf" => FileType.Pdf,
                ".txt" => FileType.Text,
                _ => FileType.Unknown
            };
        }
    }
}
