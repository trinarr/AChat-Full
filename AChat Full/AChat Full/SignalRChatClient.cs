using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace AChat_Full
{
    public class SignalRChatClient : IDisposable
    {
        private readonly string _hubUrl;
        private HubConnection _connection;

        /// <summary>Вызывается при успешном (re)connect.</summary>
        public event Action Connected;
        /// <summary>Вызывается при отключении.</summary>
        public event Action Disconnected;
        /// <summary>Вызывается при ошибке.</summary>
        public event Action<Exception> Error;
        /// <summary>Вызывается при получении нового сообщения.</summary>
        public event Action<string, string, string, DateTime> MessageReceived;

        public SignalRChatClient(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        public async Task ConnectAsync(string accessToken = null)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, opts =>
                {
                    if (!string.IsNullOrEmpty(accessToken))
                        opts.AccessTokenProvider = () => Task.FromResult(accessToken);
                })
                .WithAutomaticReconnect() // встроенный реконнект
                .Build();

            // Обработчики
            _connection.Reconnecting += error =>
            {
                Error?.Invoke(error);
                return Task.CompletedTask;
            };
            _connection.Reconnected += connId =>
            {
                Connected?.Invoke();
                return Task.CompletedTask;
            };
            _connection.Closed += async error =>
            {
                Disconnected?.Invoke();
                await Task.Delay(2000);
                // можно попытаться восстановить:
                await ConnectAsync(accessToken);
            };

            // Подписка на событие от сервера
            _connection.On<string, string, string, DateTime>(
                "ReceiveMessage",
                (chatId, userId, text, timestamp) =>
                {
                    MessageReceived?.Invoke(chatId, userId, text, timestamp);
                });

            try
            {
                await _connection.StartAsync();
                Connected?.Invoke();
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }

        /// <summary>
        /// Отправить сообщение на сервер.
        /// Серверный хаб должен иметь метод SendMessage(string chatId, string text).
        /// </summary>
        public async Task SendMessageAsync(string chatId, string text)
        {
            if (_connection?.State != HubConnectionState.Connected)
                throw new InvalidOperationException("SignalR is not connected.");

            try
            {
                await _connection.InvokeAsync("SendMessage", chatId, text);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }

        public void Dispose()
        {
            _ = _connection?.StopAsync();
            _connection?.DisposeAsync();
        }
    }
}