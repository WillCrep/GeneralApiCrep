using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneralCrep.Infrastructure.External
{
    public class GmailApiClient
    {
        private readonly string[] Scopes = { GmailService.Scope.GmailReadonly };
        private readonly string ApplicationName = "GeneralCrep Gmail Integration";

        private GmailService _service;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly string _tokenPath;

        public GmailApiClient()
        {
            _clientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID");
            _clientSecret = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET");
            _redirectUri = Environment.GetEnvironmentVariable("GMAIL_REDIRECT_URI");

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret) || string.IsNullOrEmpty(_redirectUri))
                throw new InvalidOperationException("Las variables de entorno GMAIL_CLIENT_ID, GMAIL_CLIENT_SECRET o GMAIL_REDIRECT_URI no están configuradas.");

            _tokenPath = Path.Combine(AppContext.BaseDirectory, "External", "Gmail", "token.json");
        }

        // Genera la URL de autorización para el navegador
        public string GetAuthorizationUrl()
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = Scopes,
                DataStore = new FileDataStore(_tokenPath, true)
            });

            var url = flow.CreateAuthorizationCodeRequest(_redirectUri);
            return url.Build().AbsoluteUri;
        }

        // Intercambia el "code" por un token y lo almacena
        public async Task ExchangeCodeForTokenAsync(string code)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = Scopes,
                DataStore = new FileDataStore(_tokenPath, true)
            });

            var token = await flow.ExchangeCodeForTokenAsync(
                userId: "user",
                code: code,
                redirectUri: _redirectUri,
                taskCancellationToken: CancellationToken.None
            );

            _service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = new UserCredential(flow, "user", token),
                ApplicationName = ApplicationName
            });
        }

        // Inicializa el servicio si hay token guardado
        private async Task InitializeServiceAsync()
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                Scopes = Scopes,
                DataStore = new FileDataStore(_tokenPath, true)
            });

            var token = await flow.LoadTokenAsync("user", CancellationToken.None);

            if (token == null)
                throw new InvalidOperationException("No hay token de Gmail guardado. Debes autenticarte primero.");

            _service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = new UserCredential(flow, "user", token),
                ApplicationName = ApplicationName
            });
        }

        public async Task<List<Message>> GetRecentEmailsAsync(int maxResults = 5)
        {
            if (_service == null)
                await InitializeServiceAsync();

            var request = _service.Users.Messages.List("me");
            request.MaxResults = maxResults;
            var response = await request.ExecuteAsync();

            return response.Messages?.ToList() ?? new List<Message>();
        }

        public async Task<Message> GetEmailByIdAsync(string messageId)
        {
            if (_service == null)
                await InitializeServiceAsync();

            var request = _service.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
            return await request.ExecuteAsync();
        }

        public async Task<MessagePartBody> GetAttachmentAsync(string messageId, string attachmentId)
        {
            if (_service == null)
                await InitializeServiceAsync();

            var request = _service.Users.Messages.Attachments.Get("me", messageId, attachmentId);
            return await request.ExecuteAsync();
        }

        public async Task<Message?> SearchEmailBySubjectAsync(string subject, int maxSearchResults = 10)
        {
            if (_service == null)
                await InitializeServiceAsync();

            // 1) Buscar por asunto (devuelve lista de ids)
            var listReq = _service.Users.Messages.List("me");
            listReq.Q = $"subject:{subject}";
            listReq.MaxResults = maxSearchResults; // limita cuántos ids traeremos para no hacer demasiadas llamadas
            var listResp = await listReq.ExecuteAsync();

            if (listResp?.Messages == null || listResp.Messages.Count == 0)
                return null;

            // 2) Para cada id, pedir el mensaje completo (con InternalDate) y seleccionar el más reciente
            Message? newest = null;
            long newestInternalDate = -1;

            foreach (var m in listResp.Messages)
            {
                try
                {
                    var full = await GetEmailByIdAsync(m.Id); // asume que tienes GetEmailByIdAsync implementado
                                                              // InternalDate viene como long (milisegundos desde epoch)
                    if (full != null && full.InternalDate.HasValue)
                    {
                        long internalDate = full.InternalDate.Value;
                        if (internalDate > newestInternalDate)
                        {
                            newestInternalDate = internalDate;
                            newest = full;
                        }
                    }
                    else if (newest == null)
                    {
                        // si no hay InternalDate, fallback a tomar el primero
                        newest = full;
                    }
                }
                catch
                {
                    // ignorar errores en mensajes individuales (podrías loggear)
                }
            }

            return newest;
        }

        public async Task<List<Message>> GetEmailsByLabelAsync(string labelName, int maxResults = 10)
        {
            if (_service == null)
                await InitializeServiceAsync();

            // 1️⃣ Obtener todas las etiquetas para mapear el nombre al ID real
            var labelsList = await _service.Users.Labels.List("me").ExecuteAsync();
            var label = labelsList.Labels.FirstOrDefault(l => l.Name.Equals(labelName, StringComparison.OrdinalIgnoreCase));

            if (label == null)
                throw new InvalidOperationException($"La etiqueta '{labelName}' no existe en la cuenta de Gmail.");

            // 2️⃣ Obtener los mensajes que pertenecen a esa etiqueta
            var listRequest = _service.Users.Messages.List("me");
            listRequest.LabelIds = label.Id; // puedes pasar un solo ID o varios
            listRequest.MaxResults = maxResults;

            var response = await listRequest.ExecuteAsync();

            if (response?.Messages == null || response.Messages.Count == 0)
                return new List<Message>();

            // 3️⃣ Obtener los mensajes completos
            var fullMessages = new List<Message>();

            foreach (var msg in response.Messages)
            {
                var full = await GetEmailByIdAsync(msg.Id);
                fullMessages.Add(full);
            }

            return fullMessages;
        }
    }
}
