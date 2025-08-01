using System;
using AChatFull.Views;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace AChatFull
{
    public class SignalRChatClient : IDisposable
    {
        private readonly string _hubUrl;
        private HubConnection _connection;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<Exception> Error;
        public event Action<string, string, string, DateTime> MessageReceived;

        public SignalRChatClient(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        public async Task ConnectAsync(string accessToken = null)
        {
            Debug.WriteLine("TESTLOG ConnectAsync: accessToken" + accessToken);

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, opts =>
                {
                    if (!string.IsNullOrEmpty(accessToken))
                        opts.AccessTokenProvider = () => Task.FromResult(accessToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnecting += error => { Error?.Invoke(error); return Task.CompletedTask; };
            _connection.Reconnected += connId => { Connected?.Invoke(); return Task.CompletedTask; };
            _connection.Closed += async error => { Disconnected?.Invoke(); await Task.Delay(2000); await ConnectAsync(accessToken); };

            _connection.On<string, string, string, DateTime>(
                "ReceiveMessage",
                (chatId, userId, text, timestamp) => MessageReceived?.Invoke(chatId, userId, text, timestamp)
            );

            await _connection.StartAsync();
            Connected?.Invoke();
        }

        /// <summary>
        /// Вызывает на сервере метод GetChats и возвращает список.
        /// Серверный ChatHub должен реализовать метод:
        /// Task<List<ChatSummary>> GetChats();
        /// </summary>
        public async Task<List<ChatSummary>> GetChatsAsync()
        {
            if (_connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("SignalR is not connected.");

            return await _connection.InvokeAsync<List<ChatSummary>>("GetChats");
        }
        public async Task<List<ChatSummary>> GetTestChatsAsync()
        {
            List<ChatSummary> TestData = new List<ChatSummary>();;
            TestData.Add(new ChatSummary("001", "Xeno1", "Сам решу", new DateTime(), 4));
            TestData.Add(new ChatSummary("002", "Xeno2", "Сам решу1", new DateTime(), 3));
            TestData.Add(new ChatSummary("003", "Xeno3", "Сам решу2", new DateTime(), 0));

            return TestData;
        }

        public async Task SendMessageAsync(string chatId, string text)
            => await _connection.InvokeAsync("SendMessage", chatId, text);

        public void Dispose()
        {
            _ = _connection?.StopAsync();
            _connection?.DisposeAsync();
        }
    }
}