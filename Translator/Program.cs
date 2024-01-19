using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

class Program
{
    static void Main()
    {
        string filePath = "G:\\Projects\\Translator\\Translator\\dictionary.xml";
        string apiKey = "203cbf2c-5eea-913c-fa0c-f0c2212528f6:fx";

        try
        {
            // Проверка доступа к файлу
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Файл не найден.");
                return;
            }

            // Чтение файла в память
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            // Проверка чтения файла в память
            if (string.IsNullOrEmpty(fileContent))
            {
                Console.WriteLine("Ошибка при чтении файла. Файл пуст или не удалось прочитать его содержимое.");
                return;
            }

            Console.WriteLine("Файл успешно прочитан в память.");

            // Замена пустых значений
            string updatedContent = ReplaceEmptyValues(fileContent, apiKey).GetAwaiter().GetResult();

            // Проверка наличия изменений
            if (updatedContent == fileContent)
            {
                Console.WriteLine("Нет пустых значений для замены.");
                return;
            }

            // Запись измененного содержимого обратно в файл
            File.WriteAllText(filePath, updatedContent, Encoding.UTF8);
            Console.WriteLine("Замена выполнена успешно. Файл успешно перезаписан.");
        }
        catch (IOException e)
        {
            Console.WriteLine("Ошибка при чтении или записи файла: " + e.Message);
        }
    }

    static async Task<string> ReplaceEmptyValues(string content, string apiKey)
    {
        XDocument xmlDocument = XDocument.Parse(content);
        //Тут указать язык НА который надо перевести
        var valueElements = xmlDocument.Descendants("Value").Where(e => e.Attribute("Lang")?.Value == "ES" && string.IsNullOrEmpty(e.Value)).ToList();

        foreach (XElement valueElement in valueElements)
        {
            //Тут указать язык С Которого переводить
            var sourValueElement = valueElement.ElementsAfterSelf("Value").LastOrDefault(e => e.Attribute("Lang")?.Value == "RU");

            if (sourValueElement != null && !string.IsNullOrEmpty(sourValueElement.Value))
            {
                string sourValue = sourValueElement.Value;
                string endValue = await TranslateText(sourValue, apiKey);

                if (!string.IsNullOrEmpty(endValue))
                {
                    valueElement.SetValue(endValue);
                }
            }
        }

        return xmlDocument.ToString();
    }

    static async Task<string> TranslateText(string text, string apiKey)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = "https://api-free.deepl.com/v2/translate";
            string targetLanguage = "ES";

            // Создание параметров запроса
            var parameters = new Dictionary<string, string>
            {
                { "auth_key", apiKey },
                { "text", text },
                { "source_lang", "RU" },
                { "target_lang", targetLanguage }
            };

            // Отправка POST-запроса к API DeepL
            var response = await client.PostAsync(apiUrl, new FormUrlEncodedContent(parameters));
            var responseContent = await response.Content.ReadAsStringAsync();

            // Проверка ответа от API
            if (response.IsSuccessStatusCode)
            {
                // Извлечение переведенного текста из ответа JSON
                var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<TranslationResponse>(responseContent);
                string translatedText = responseObject.Translations.FirstOrDefault()?.Text;

                return translatedText;
            }
            else
            {
                Console.WriteLine("Ошибка при выполнении перевода: " + responseContent);
                return null;
            }
        }
    }
}

public class TranslationResponse
{
    public List<TranslationItem> Translations { get; set; }
}

public class TranslationItem
{
    public string Text { get; set; }
}