using GeneralCrep.Application.Dtos;
using GeneralCrep.Application.Interfaces;
using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Services
{
    public class GmailService : IGmailService
    {
        private readonly IGmailApiClient _gmailClient;
        private readonly IFileProcessorFactory _processorFactory;

        public GmailService(IGmailApiClient gmailClient, IFileProcessorFactory processorFactory)
        {
            _gmailClient = gmailClient ?? throw new ArgumentNullException(nameof(gmailClient));
            _processorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
        }

        public async Task<List<FileProcessingResult>> ProcessRecentEmailsAsync(int maxResults = 5)
        {
            var emailIds = await _gmailClient.GetRecentEmailsAsync(maxResults);
            var results = new List<FileProcessingResult>();

            foreach (var id in emailIds)
            {
                var messageParts = await _gmailClient.GetEmailByIdAsync(id);
                if (messageParts == null) continue;

                foreach (var part in messageParts)
                {
                    var processed = await ProcessAttachmentAsync(id, part);
                    if (processed != null) results.Add(processed);
                }
            }

            return results;
        }

        public async Task<List<FileProcessingResult>> ProcessEmailByIdAsync(string messageId)
        {
            var results = new List<FileProcessingResult>();
            var messageParts = await _gmailClient.GetEmailByIdAsync(messageId);
            if (messageParts == null) return results;

            foreach (var part in messageParts)
            {
                var processed = await ProcessAttachmentAsync(messageId, part);
                if (processed != null) results.Add(processed);
            }

            return results;
        }

        public async Task<List<FileProcessingResult>> ProcessEmailBySubjectAsync(string subject)
        {
            var results = new List<FileProcessingResult>();
            var message = await _gmailClient.SearchEmailBySubjectAsync(subject);

            if (message == null || message.Parts == null || message.Parts.Count == 0)
                return results;

            foreach (var part in message.Parts)
            {
                var processed = await ProcessAttachmentAsync(message.Id, part);
                if (processed != null) results.Add(processed);
            }

            return results;
        }

        public async Task<List<FileProcessingResult>> ProcessEmailsByLabelAsync(string labelName, int maxResults = 10)
        {
            var emails = await _gmailClient.GetEmailsByLabelAsync(labelName, maxResults);
            var results = new List<FileProcessingResult>();

            if (emails == null) return results;

            foreach (var msg in emails)
            {
                if (msg?.Parts == null || msg.Parts.Count == 0) continue;
                foreach (var part in msg.Parts)
                {
                    var processed = await ProcessAttachmentAsync(msg.Id, part);
                    if (processed != null) results.Add(processed);
                }
            }

            return results;
        }

        private async Task<FileProcessingResult?> ProcessAttachmentAsync(string messageId, MessageGmailPartsDto part)
        {
            if (part == null || string.IsNullOrEmpty(part.Filename) || part.AttachmentId == null)
                return null;

            string tempPath = null;
            try
            {
                string fileName = part.Filename;
                byte[] fileBytes = await _gmailClient.GetAttachmentAsync(messageId, part.AttachmentId);

                tempPath = Path.Combine(Path.GetTempPath(), fileName);
                await File.WriteAllBytesAsync(tempPath, fileBytes);

                Console.WriteLine($"Archivo descargado: {fileName}");

                var fileType = GetFileType(fileName);
                var processor = _processorFactory.GetProcessor(fileType);

                if (processor == null) return null;

                var processedResult = processor.Process(tempPath);
                processedResult.FileBytes = fileBytes;
                return processedResult;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath))
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }
                }
            }
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
