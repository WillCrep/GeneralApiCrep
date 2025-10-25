using GeneralCrep.Application.Interfaces;
using GeneralCrep.Infrastructure.External;
using Microsoft.AspNetCore.Mvc;

namespace GeneralCrep.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GmailController : ControllerBase
    {
        private readonly IGmailService _gmailService;
        private readonly GmailApiClient _gmailApiClient;

        public GmailController(IGmailService gmailService)
        {
            _gmailService = gmailService;
            _gmailApiClient = new GmailApiClient();
        }

        [HttpGet("processRecentEmails")]
        public async Task<IActionResult> ProcessRecentEmails()
        {
            var results = await _gmailService.ProcessRecentEmailsAsync();

            // Convertir FileBytes a Base64 para JSON
            var jsonReady = results.Select(r => new
            {
                r.FileName,
                r.FileType,
                r.Data,
                FileBase64 = Convert.ToBase64String(r.FileBytes)
            });

            return Ok(jsonReady);
        }

        [HttpGet("email/{messageId}")]
        public async Task<IActionResult> GetEmailByIdAsync(string messageId)
        {
            var results = await _gmailService.ProcessEmailByIdAsync(messageId);

            if (results == null || !results.Any())
                return NotFound(new { message = "No se encontraron archivos en el correo especificado." });

            var jsonReady = results.Select(r => new
            {
                r.FileName,
                r.FileType,
                r.Data,
                FileBase64 = Convert.ToBase64String(r.FileBytes)
            });

            return Ok(jsonReady);
        }

        [HttpGet("searchBySubject")]
        public async Task<IActionResult> SearchEmailBySubject([FromQuery] string subject)
        {
            var results = await _gmailService.ProcessEmailBySubjectAsync(subject);

            if (results == null || !results.Any())
                return NotFound(new { message = $"No se encontró ningún correo con el asunto que contenga '{subject}'." });

            var jsonReady = results.Select(r => new
            {
                r.FileName,
                r.FileType,
                r.Data,
                FileBase64 = Convert.ToBase64String(r.FileBytes)
            });

            return Ok(jsonReady);
        }

        [HttpGet("getByLabel")]
        public async Task<IActionResult> GetEmailsByLabelAsync([FromQuery] string labelName, [FromQuery] int maxResults = 10)
        {
            try
            {
                var results = await _gmailService.ProcessEmailsByLabelAsync(labelName, maxResults);

                if (results == null || !results.Any())
                    return NotFound(new { message = $"No se encontraron correos en la etiqueta '{labelName}'." });

                var jsonReady = results.Select(r => new
                {
                    r.FileName,
                    r.FileType,
                    r.Data,
                    FileBase64 = Convert.ToBase64String(r.FileBytes)
                });

                return Ok(jsonReady);
            }
            catch (InvalidOperationException ex)
            {
                // Error lanzado cuando la etiqueta no existe
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al procesar correos de la etiqueta '{labelName}': {ex.Message}" });
            }
        }

        //  Nuevo endpoint: inicia flujo OAuth
        [HttpGet("login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Login()
        {
            var authUrl = _gmailApiClient.GetAuthorizationUrl();
            return Redirect(authUrl);
        }

        //  Nuevo endpoint: callback desde Google
        [HttpGet("oauth2callback")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> OAuth2Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("No authorization code received.");

            await _gmailApiClient.ExchangeCodeForTokenAsync(code);

            return Ok("Gmail authentication successful and token stored!");
        }
    }
}
