using System;
using System.Threading.Tasks;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Jackett.Common.Services
{
    public class TelegramNotificationService : ITelegramNotificationService, IObserver<ServerConfig>
    {
        private readonly Logger _logger;
        private TelegramBotClient _botClient;
        private string _chatId;
        private bool _enabled;
        private IDisposable _configSubscription;

        public bool IsConfigured => _enabled && _botClient != null && !string.IsNullOrWhiteSpace(_chatId);

        public TelegramNotificationService(Logger logger, ServerConfig serverConfig)
        {
            _logger = logger;
            _configSubscription = serverConfig.Subscribe(this);
            UpdateConfiguration(serverConfig);
        }

        private void UpdateConfiguration(ServerConfig config)
        {
            _enabled = config.TelegramEnabled;
            _chatId = config.TelegramChatId;

            if (_enabled && !string.IsNullOrWhiteSpace(config.TelegramBotToken))
            {
                try
                {
                    _botClient = new TelegramBotClient(config.TelegramBotToken);
                    _logger.Info("Telegram bot client initialized");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to initialize Telegram bot client");
                    _botClient = null;
                }
            }
            else
            {
                _botClient = null;
            }
        }

        public async Task<bool> SendNotificationAsync(string message)
        {
            if (!IsConfigured)
            {
                _logger.Debug("Telegram notifications not configured, skipping notification");
                return false;
            }

            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: new ChatId(_chatId),
                    text: message
                );
                _logger.Debug($"Telegram notification sent successfully: {message.Substring(0, Math.Min(50, message.Length))}...");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to send Telegram notification: {message}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (!_enabled)
            {
                _logger.Warn("Telegram is not enabled");
                return false;
            }

            if (_botClient == null)
            {
                _logger.Warn("Telegram bot client is not initialized");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_chatId))
            {
                _logger.Warn("Telegram chat ID is not configured");
                return false;
            }

            try
            {
                // Test by getting bot info
                var me = await _botClient.GetMeAsync();
                _logger.Info($"Telegram bot connected: {me.Username}");

                // Try to send a test message
                await _botClient.SendTextMessageAsync(
                    chatId: new ChatId(_chatId),
                    text: "✅ Jackett Telegram bot test message - connection successful!"
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test Telegram connection");
                return false;
            }
        }

        public void OnNext(ServerConfig value)
        {
            UpdateConfiguration(value);
        }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error in Telegram configuration observer");
        }

        public void OnCompleted()
        {
            _configSubscription?.Dispose();
        }
    }
}
