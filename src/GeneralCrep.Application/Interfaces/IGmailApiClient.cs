using GeneralCrep.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Interfaces
{
    public interface IGmailApiClient
    {
        string GetAuthorizationUrl();
        Task ExchangeCodeForTokenAsync(string code);
        Task<List<string>> GetRecentEmailsAsync(int maxResults = 5);
        Task<IEnumerable<MessageGmailPartsDto>> GetEmailByIdAsync(string messageId);
        Task<byte[]> GetAttachmentAsync(string messageId, string attachmentId);
        Task<MessageGmailDto> SearchEmailBySubjectAsync(string subject, int maxSearchResults = 10);
        Task<List<MessageGmailDto>> GetEmailsByLabelAsync(string labelName, int maxResults = 10);

    }
}
