using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace mecanico_plus.Pages.Principal.CitaVirtual
{
    public class ChatHandler
    {
        // Dictionary para almacenar las conexiones por token de sala
        private static Dictionary<string, List<WebSocket>> _roomConnections = new();
        private static Dictionary<WebSocket, (string room, string user)> _connectionInfo = new();

        private readonly mecanico_plus.Data.local _context;

        public ChatHandler(mecanico_plus.Data.local context)
        {
            _context = context;
        }

        public async Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket)
        {
            string sessionUser = context.Session.GetString("SessionUser");
            string roomToken = context.Request.Query["token"].ToString();
            bool isAnonymousUser = string.IsNullOrEmpty(sessionUser);

            // Para usuarios anónimos, usar un identificador temporal
            if (isAnonymousUser)
            {
                sessionUser = $"Invitado_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            if (string.IsNullOrEmpty(roomToken))
            {
                throw new Exception("Token no encontrado.");
            }

            // Validar el token antes de permitir la conexión
            if (!await ValidateToken(roomToken))
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "Token inválido o expirado",
                    CancellationToken.None
                );
                return;
            }

            // Agregar la conexión al diccionario de la sala correspondiente
            if (!_roomConnections.ContainsKey(roomToken))
            {
                _roomConnections[roomToken] = new List<WebSocket>();
            }
            _roomConnections[roomToken].Add(webSocket);
            _connectionInfo[webSocket] = (roomToken, isAnonymousUser ? "Paciente" : sessionUser);

            var buffer = new byte[4 * 1024]; // 4KB
            var sb = new StringBuilder();
            WebSocketReceiveResult result;

            try
            {
                do
                {
                    // Recibimos una porción (frame) del mensaje
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    // Si es un mensaje de texto, lo acumulamos
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        // Cuando EndOfMessage sea true, significa que el mensaje está completo
                        if (result.EndOfMessage)
                        {
                            var receivedMessage = sb.ToString();
                            // Limpiamos el StringBuilder para el siguiente mensaje
                            sb.Clear();

                            // Procesamos el mensaje
                            if (receivedMessage.StartsWith("webrtc:"))
                            {
                                await HandleWebRTCMessage(webSocket, receivedMessage);
                            }
                            else if (receivedMessage.StartsWith("Tú:"))
                            {
                                // Manejo especial para mensajes de usuarios anónimos
                                var messageContent = receivedMessage.Substring(3).Trim();
                                var messageToSend = isAnonymousUser
                                    ? $"Paciente: {messageContent}"
                                    : $"{sessionUser}: {messageContent}";

                                await BroadcastToRoomAsync(roomToken, messageToSend, webSocket);
                            }
                            else if (receivedMessage.StartsWith("chat:"))
                            {
                                var messageContent = receivedMessage.Substring(5).Trim();
                                var messageToSend = isAnonymousUser
                                    ? $"Paciente: {messageContent}"
                                    : $"{sessionUser}: {messageContent}";

                                await BroadcastToRoomAsync(roomToken, messageToSend, webSocket);
                            }
                        }
                    }

                } while (!result.CloseStatus.HasValue);
            }
            finally
            {
                // Limpiar conexiones al desconectarse
                if (_roomConnections.ContainsKey(roomToken))
                {
                    _roomConnections[roomToken].Remove(webSocket);
                    if (_roomConnections[roomToken].Count == 0)
                    {
                        _roomConnections.Remove(roomToken);
                    }
                }
                _connectionInfo.Remove(webSocket);
            }
        }

        /// <summary>
        /// Valida en la base de datos si el token existe y no está expirado.
        /// </summary>
        private async Task<bool> ValidateToken(string token)
        {
            try
            {
                var tokenEntity = await _context.t012_token
                    .FirstOrDefaultAsync(t => t.f012_token == token);

                if (tokenEntity == null)
                    return false;

                if (tokenEntity.IsExpired)
                {
                    // Opcional: eliminar token expirado
                    _context.t012_token.Remove(tokenEntity);
                    await _context.SaveChangesAsync();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validando token: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Difunde un mensaje a todos los sockets en la sala, realizando ajustes
        /// si el emisor es un usuario anónimo (cambiando "Paciente:" a "Tú:" para él).
        /// </summary>
        private async Task BroadcastToRoomAsync(string roomToken, string message, WebSocket senderSocket)
        {
            if (!_roomConnections.ContainsKey(roomToken)) return;

            foreach (var socket in _roomConnections[roomToken])
            {
                if (socket.State == WebSocketState.Open)
                {
                    var finalMessage = message;

                    // Si es el socket que envía y es anónimo, traducir "Paciente" a "Tú" para él
                    if (socket == senderSocket && message.StartsWith("Paciente:"))
                    {
                        finalMessage = "Tú:" + message.Substring(message.IndexOf(':') + 1);
                    }

                    var encodedMessage = Encoding.UTF8.GetBytes(finalMessage);
                    await socket.SendAsync(
                        new ArraySegment<byte>(encodedMessage),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }

        private async Task HandleWebRTCMessage(WebSocket sender, string message)
 {
     var (roomToken, _) = _connectionInfo[sender];

     if (_roomConnections.ContainsKey(roomToken))
     {
         var encodedMessage = Encoding.UTF8.GetBytes(message);
         foreach (var socket in _roomConnections[roomToken].Where(s => s != sender))
         {
             if (socket.State == WebSocketState.Open)
             {
                 await socket.SendAsync(
                     new ArraySegment<byte>(encodedMessage),
                     WebSocketMessageType.Text,
                     true,
                     CancellationToken.None
                 );
             }
         }
     }
 }
    }
}
