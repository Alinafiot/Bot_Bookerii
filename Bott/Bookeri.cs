using Bott.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bott
{
    internal class Bookeri
    {
        private readonly TelegramBotClient botClient = new TelegramBotClient("7024311169:AAFAUiFSSGL9svI8752cSqQ3mMv8miMTj8k");
        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri(Constants.Address) };

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, new ReceiverOptions { AllowedUpdates = { } });
            var botMe = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {botMe.Username} почав працювати ");
            Console.ReadKey();
        }

        private Task HandlerError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ:\n {apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandlerMessageAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery && update?.CallbackQuery?.Data != null)
            {
                await HandlerCallbackQueryAsync(botClient, update.CallbackQuery);
            }
        }

        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть команду /keyboard або /inline");
                return;
            }

            if (message.Text == "/inline")
            {
                InlineKeyboardMarkup keyboardMarkup = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("10 рандомних книг", "randomBooks") }
                });
                await botClient.SendTextMessageAsync(message.Chat.Id, "Оберіть", replyMarkup: keyboardMarkup);
                return;
            }

            if (message.Text == "/keyboard")
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new
                  (
                     new[]
                     {
                       new KeyboardButton[] { "Hello", "Bye" },
                       new KeyboardButton[] { "Пошук за назвою", "Пошук за автором" },
                       new KeyboardButton[] { "Пошук за жанром" },
                       new KeyboardButton[] { "Додати прочитану книгу" },
                       new KeyboardButton[] { "Видалити прочитану книгу" },
                       new KeyboardButton[] { "Переглянути прочитані книги" }
                     }
                  )
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню:", replyMarkup: replyKeyboardMarkup);
                return;
            }

            if (message.Text == "Hello")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "І Вам привіт!");
                return;
            }

            if (message.Text == "Bye")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "На все добре!");
                return;
            }


            if (message.Text == "Пошук за назвою")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву книги у форматі 'Назва: Ваша назва'");
                return;
            }

            if (message.Text == "Пошук за автором")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть прізвище або ім'я автора у форматі 'Автор: Ім'я/Прізвище'");
                return;
            }

            if (message.Text == "Пошук за жанром")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть жанр  'Жанр: Ваш жанр'");
                return;
            }           

            if (message.Text == "Додати прочитану книгу")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву книги для додавання у форматі 'Додати: Ваша назва'");
                return;
            }

            if (message.Text == "Видалити прочитану книгу")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Введіть назву книги для видалення у форматі 'Видалити: Ваша назва'");
                return;
            }

            if (message.Text == "Переглянути прочитані книги")
            {
                var books = await GetBooksFromDatabaseAsync();
                var booksMessage = books != null && books.Any() ? string.Join("\n", books) : "Ви ще недодали книги.";
                await botClient.SendTextMessageAsync(message.Chat.Id, booksMessage);
                return;
            }

            if (message.Text.StartsWith("Назва:"))
            {
                var query = message.Text.Substring(6).Trim();
                var books = await GetBooksByTitleAsync(query);
                await botClient.SendTextMessageAsync(message.Chat.Id, books);
                return;
            }

            if (message.Text.StartsWith("Автор:"))
            {
                var query = message.Text.Substring(6).Trim();
                var books = await GetBooksByAuthorAsync(query);
                await botClient.SendTextMessageAsync(message.Chat.Id, books);
                return;
            }

            if (message.Text.StartsWith("Жанр:"))
            {
                var query = message.Text.Substring(5).Trim();
                var books = await GetBooksByGenreAsync(query, 10);
                await botClient.SendTextMessageAsync(message.Chat.Id, books);
                return;
            }

          

            if (message.Text.StartsWith("Додати:"))
            {
                var query = message.Text.Substring(7).Trim();
                await AddBookToDatabaseAsync(query);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Книгу додано Вашого списку.");
                return;
            }
         

            if (message.Text.StartsWith("Видалити:"))
            {
                var query = message.Text.Substring(9).Trim();
                await RemoveBookFromDatabaseAsync(query);
                await botClient.SendTextMessageAsync(message.Chat.Id, "Книгу видалено з Вашого списку.");
                return;
            }

        }

        private async Task HandlerCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            if (callbackQuery.Data == "randomBooks")
            {
                var books = await GetRandomBooksAsync();
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, books);
            }
        }


        private async Task<string> GetBooksByTitleAsync(string title)
        {
            try
            {
                var response = await httpClient.GetAsync($"/book/title?title={title}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var books = ExtractBooksFromResponse(responseBody);
                return FormatBooksMessage(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при пошуку книг за назвою: {ex.Message}");
                return "Не вдалося знайти книги за вказаною назвою.";
            }
        }

        private async Task<string> GetBooksByAuthorAsync(string author)
        {
            try
            {
                var response = await httpClient.GetAsync($"/book/author?author={author}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var books = ExtractBooksFromResponse(responseBody);
                return FormatBooksMessage(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при пошуку книг за автором: {ex.Message}");
                return "Не вдалося знайти книги за вказаним автором.";
            }
        }

        private async Task<string> GetBooksByGenreAsync(string genre, int count)
        {
            try
            {
                var response = await httpClient.GetAsync($"/book/genre?genre={genre}&count={count}");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var books = ExtractBooksFromResponse(responseBody);
                return FormatBooksMessage(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при пошуку книг за жанром: {ex.Message}");
                return "Не вдалося знайти книги за вказаним жанром.";
            }
        }

        private async Task<string> GetRandomBooksAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("/book/randomBooks");
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var books = ExtractBooksFromResponse(responseBody);
                return FormatBooksMessage(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при отриманні випадкових книг: {ex.Message}");
                return "Не вдалося отримати список випадкових книг.";
            }
        }

        public async Task AddBookToDatabaseAsync(string title)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{Constants.Address}/book/reviews/{title}");
                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                }
                else
                {
                    Console.WriteLine($"Помилка HTTP-запиту: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Помилка HTTP-запиту: {ex.Message}");
            }
        }

        public async Task RemoveBookFromDatabaseAsync(string title)
        {
            try
            {
                var address = $"{Constants.Address}/book/reviews/{title}";
                var response = await httpClient.DeleteAsync(address);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Помилка HTTP-запиту: {ex.Message}");
            }
        }

        public async Task<List<string>> GetBooksFromDatabaseAsync()
        {
            try
            {
                var address = $"{Constants.Address}/book/reviews";
                var response = await httpClient.GetAsync(address);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<ReviewRequest>(responseBody);

                return apiResponse.Books;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Помилка HTTP-запиту: {ex.Message}");
                return null;
            }
        }

        private List<Model.BookItem> ExtractBooksFromResponse(string responseBody)
        {
            var json = JObject.Parse(responseBody);
            var booksToken = json["books"];
            if (booksToken == null)
            {
                throw new Exception("Не знайдено книги в відповіді API.");
            }
            return booksToken.ToObject<List<Model.BookItem>>();
        }

        private string FormatBooksMessage(List<Model.BookItem> books)
        {
            var message = "Книги:\n";
            foreach (var book in books)
            {
                var authors = book.Authors != null ? string.Join(", ", book.Authors) : "Невідомий автор";
                var title = book.Title ?? "Невідома назва";
                message += $"Назва: {title}\nАвтор(и): {authors}\n\n";
            }
            return message;
        }
    }
}

