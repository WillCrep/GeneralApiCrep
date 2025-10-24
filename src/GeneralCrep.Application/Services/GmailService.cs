using GeneralCrep.Application.Interfaces;
using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using GeneralCrep.Infrastructure.External;
using GeneralCrep.Infrastructure.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Services
{
    public class GmailService : IGmailService
    {
        private readonly GmailApiClient _gmailClient;
        private readonly FileProcessorFactory _processorFactory;

        public GmailService(GmailApiClient gmailClient, FileProcessorFactory processorFactory)
        {
            _gmailClient = gmailClient;
            _processorFactory = processorFactory;
        }

        public async Task<List<FileProcessingResult>> ProcessRecentEmailsAsync(int maxResults = 5)
        {
            var emails = await _gmailClient.GetRecentEmailsAsync(maxResults);
            var results = new List<FileProcessingResult>();

            foreach (var msg in emails)
            {
                var fullMessage = await _gmailClient.GetEmailByIdAsync(msg.Id);
                if (fullMessage?.Payload?.Parts == null) continue;

                foreach (var part in fullMessage.Payload.Parts)
                {
                    if (!string.IsNullOrEmpty(part.Filename) && part.Body?.AttachmentId != null)
                    {
                        string fileName = part.Filename;
                        var attachment = await _gmailClient.GetAttachmentAsync(msg.Id, part.Body.AttachmentId);

                        // Convertir base64 a bytes
                        byte[] fileBytes = Convert.FromBase64String(
                            attachment.Data.Replace('-', '+').Replace('_', '/')
                        );

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
            var fullMessage = await _gmailClient.GetEmailByIdAsync(messageId);

            if (fullMessage?.Payload?.Parts == null)
                return results;

            foreach (var part in fullMessage.Payload.Parts)
            {
                if (!string.IsNullOrEmpty(part.Filename) && part.Body?.AttachmentId != null)
                {
                    string fileName = part.Filename;
                    var attachment = await _gmailClient.GetAttachmentAsync(messageId, part.Body.AttachmentId);

                    // Convertir base64 a bytes
                    byte[] fileBytes = Convert.FromBase64String(
                        attachment.Data.Replace('-', '+').Replace('_', '/')
                    );

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
            var message = await _gmailClient.SearchEmailBySubjectAsync(subject);
            var results = new List<FileProcessingResult>();

            if (message == null || message.Payload?.Parts == null)
                return results;

            foreach (var part in message.Payload.Parts)
            {
                if (part.Filename != null && part.Body?.AttachmentId != null)
                {
                    string fileName = part.Filename;
                    var attachment = await _gmailClient.GetAttachmentAsync(message.Id, part.Body.AttachmentId);

                    // Convertir base64 a bytes
                    byte[] fileBytes = Convert.FromBase64String(
                        attachment.Data.Replace('-', '+').Replace('_', '/')
                    );

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
            var emails = await _gmailClient.GetEmailsByLabelAsync(labelName, maxResults);
            var results = new List<FileProcessingResult>();

            foreach (var msg in emails)
            {
                if (msg?.Payload?.Parts == null) continue;

                foreach (var part in msg.Payload.Parts)
                {
                    if (!string.IsNullOrEmpty(part.Filename) && part.Body?.AttachmentId != null)
                    {
                        string fileName = part.Filename;
                        var attachment = await _gmailClient.GetAttachmentAsync(msg.Id, part.Body.AttachmentId);

                        // Convertir base64 a bytes
                        byte[] fileBytes = Convert.FromBase64String(
                            attachment.Data.Replace('-', '+').Replace('_', '/')
                        );

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
