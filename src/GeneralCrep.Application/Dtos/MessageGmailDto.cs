using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Application.Dtos
{
    public class MessageGmailDto
    {
        public string Id { get; set; }
        public List<MessageGmailPartsDto> Parts { get; set; }
    }
}
