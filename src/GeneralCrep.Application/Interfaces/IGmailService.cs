using GeneralCrep.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Interfaces
{
    public interface IGmailService
    {
        Task<List<FileProcessingResult>> ProcessRecentEmailsAsync(int maxResults = 5);
        Task<List<FileProcessingResult>> ProcessEmailByIdAsync(string messageId);
        Task<List<FileProcessingResult>> ProcessEmailBySubjectAsync(string subject);
        Task<List<FileProcessingResult>> ProcessEmailsByLabelAsync(string labelName, int maxResults = 10);
    }
}
