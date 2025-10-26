using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using GeneralCrep.Application.Services;
using GeneralCrep.Application.Dtos;
using GeneralCrep.Application.Interfaces;
using GeneralCrep.Domain.Entities;
using GeneralCrep.Domain.Enums;
using GeneralCrep.Infrastructure.Processors;

namespace GeneralCrep.Tests.Application.Services
{
    public class GmailServiceTests
    {
        private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

        [Fact]
        public async Task ProcessRecentEmailsAsync_WhenMessageHasAttachment_ProcessesAndReturnsResult()
        {
            // Arrange
            var messageId = "msg1";
            var attachmentId = "att1";
            var fileName = "report.xlsx";
            var fileBytes = Bytes("dummy excel content");

            var mockGmail = new Mock<IGmailApiClient>();
            mockGmail.Setup(x => x.GetRecentEmailsAsync(It.IsAny<int>()))
                     .ReturnsAsync(new List<string> { messageId });
            mockGmail.Setup(x => x.GetEmailByIdAsync(messageId))
                     .ReturnsAsync(new List<MessageGmailPartsDto> {
                         new MessageGmailPartsDto { Filename = fileName, AttachmentId = attachmentId }
                     }.AsEnumerable());
            mockGmail.Setup(x => x.GetAttachmentAsync(messageId, attachmentId))
                     .ReturnsAsync(fileBytes);

            var processedResult = new FileProcessingResult
            {
                FileName = fileName,
                FileType = FileType.Excel,
                Data = "processed"
            };

            var mockProcessor = new Mock<IFileProcessor>();
            mockProcessor.Setup(p => p.Process(It.IsAny<string>())).Returns(processedResult);

            var mockFactory = new Mock<IFileProcessorFactory>();
            mockFactory.Setup(f => f.GetProcessor(FileType.Excel)).Returns(mockProcessor.Object);

            var service = new GmailService(mockGmail.Object, mockFactory.Object);

            // Act
            var results = await service.ProcessRecentEmailsAsync(1);

            // Assert
            results.Should().HaveCount(1);
            var r = results[0];
            r.FileType.Should().Be(FileType.Excel);
            r.FileName.Should().Be(processedResult.FileName);
            r.FileBytes.Should().Equal(fileBytes);
            r.Data.Should().Be(processedResult.Data);
        }

        [Fact]
        public async Task ProcessEmailByIdAsync_WhenNoParts_ReturnsEmpty()
        {
            // Arrange
            var messageId = "no-parts";
            var mockGmail = new Mock<IGmailApiClient>();
            mockGmail.Setup(x => x.GetEmailByIdAsync(messageId))
                     .ReturnsAsync(Enumerable.Empty<MessageGmailPartsDto>());

            var mockFactory = new Mock<IFileProcessorFactory>();
            var service = new GmailService(mockGmail.Object, mockFactory.Object);

            // Act
            var results = await service.ProcessEmailByIdAsync(messageId);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessEmailBySubjectAsync_WhenMessageNull_ReturnsEmpty()
        {
            // Arrange
            var subject = "no-existe";
            var mockGmail = new Mock<IGmailApiClient>();
            mockGmail.Setup(x => x.SearchEmailBySubjectAsync(It.IsAny<string>(), It.IsAny<int>()))
                     .ReturnsAsync((MessageGmailDto)null);

            var mockFactory = new Mock<IFileProcessorFactory>();
            var service = new GmailService(mockGmail.Object, mockFactory.Object);

            // Act
            var results = await service.ProcessEmailBySubjectAsync(subject);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessEmailsByLabelAsync_WhenProcessorNull_SkipsProcessing()
        {
            // Arrange
            var labelName = "LABEL";
            var messageId = "m1";
            var attachmentId = "a1";
            var fileName = "doc.pdf";
            var fileBytes = Bytes("pdf content");

            var msg = new MessageGmailDto
            {
                Id = messageId,
                Parts = new List<MessageGmailPartsDto> {
                    new MessageGmailPartsDto { Filename = fileName, AttachmentId = attachmentId }
                }
            };

            var mockGmail = new Mock<IGmailApiClient>();
            mockGmail.Setup(x => x.GetEmailsByLabelAsync(labelName, It.IsAny<int>()))
                     .ReturnsAsync(new List<MessageGmailDto> { msg });
            mockGmail.Setup(x => x.GetAttachmentAsync(messageId, attachmentId))
                     .ReturnsAsync(fileBytes);

            var mockFactory = new Mock<IFileProcessorFactory>();
            mockFactory.Setup(f => f.GetProcessor(It.IsAny<FileType>())).Returns((IFileProcessor)null);

            var service = new GmailService(mockGmail.Object, mockFactory.Object);

            // Act
            var results = await service.ProcessEmailsByLabelAsync(labelName, 5);

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ProcessEmailByIdAsync_WhenAttachmentAndProcessor_ProcessesAndAttachesBytes()
        {
            // Arrange
            var messageId = "byid1";
            var attachmentId = "att-100";
            var fileName = "notes.txt";
            var fileBytes = Bytes("hello text file");

            var part = new MessageGmailPartsDto { Filename = fileName, AttachmentId = attachmentId };

            var mockGmail = new Mock<IGmailApiClient>();
            mockGmail.Setup(x => x.GetEmailByIdAsync(messageId)).ReturnsAsync(new List<MessageGmailPartsDto> { part });
            mockGmail.Setup(x => x.GetAttachmentAsync(messageId, attachmentId)).ReturnsAsync(fileBytes);

            var processedResult = new FileProcessingResult
            {
                FileName = fileName,
                FileType = FileType.Text,
                Data = "ok"
            };

            var mockProcessor = new Mock<IFileProcessor>();
            mockProcessor.Setup(p => p.Process(It.IsAny<string>())).Returns(processedResult);

            var mockFactory = new Mock<IFileProcessorFactory>();
            mockFactory.Setup(f => f.GetProcessor(FileType.Text)).Returns(mockProcessor.Object);

            var service = new GmailService(mockGmail.Object, mockFactory.Object);

            // Act
            var results = await service.ProcessEmailByIdAsync(messageId);

            // Assert
            results.Should().HaveCount(1);
            results[0].FileType.Should().Be(FileType.Text);
            results[0].FileBytes.Should().Equal(fileBytes);
            results[0].FileName.Should().Be(fileName);
        }
    }
}